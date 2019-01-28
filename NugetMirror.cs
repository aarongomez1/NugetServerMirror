using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace NuGetServerMirror
{
    public sealed class NugetMirror : IDisposable
    {
        private const int OneHourMilliseconds = 600000;
        private string sourceServer;
        private string targetServer;
        private string targetServerApiKey;
        private Timer timer;
        public NugetMirror(string source, string target, string targetApiKey)
        {
            sourceServer = source;
            targetServer = target;
            targetServerApiKey = targetApiKey;
        }

        public void Dispose()
        {
            timer?.Dispose();
            timer = null;
        }

        internal void Start()
        {
            timer = new Timer(new TimerCallback(OnCheckForChanges), null, 0, OneHourMilliseconds);
        }

        private void OnCheckForChanges(object state)
        {
            NugetServer source = new NugetServer(sourceServer);
            NugetServer target = new NugetServer(targetServer, targetServerApiKey);

            var sourcePackages = source.ListPackages();
            var targetPackages = target.ListPackages();

            var missingPackages = sourcePackages.Where(
                s => !targetPackages.Any(
                    t => 0 == string.Compare(t.ID, s.ID, StringComparison.OrdinalIgnoreCase) && 0 == string.Compare(t.Version, s.Version, StringComparison.OrdinalIgnoreCase)));


            var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var appFolder = Path.Combine(baseFolder, "NuGetMirror");

            if(!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            foreach(var missingPackage in missingPackages)
            {
                var packageLocation = source.InstallPackage(missingPackage, appFolder);
                if (!string.IsNullOrEmpty(packageLocation))
                {
                    target.PushPackage(packageLocation);
                }
            }
        }

        internal void Stop()
        {
            timer.Dispose();
            timer = null;
        }
    }
}
