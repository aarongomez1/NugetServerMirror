using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NuGetServerMirror
{
    public class NugetServer
    {
        private string nugetServerUri;
        private string apiKey;
        public NugetServer(string server)
        {
            nugetServerUri = server;
        }

        public NugetServer(string server, string key) : this(server)
        {
            apiKey = key;
        }

        public IEnumerable<NugetPackage> ListPackages()
        {
            var packages = new List<NugetPackage>();

            var processStart = new ProcessStartInfo("nuget.exe")
            {
                Arguments = $"list -source {nugetServerUri}",
                UseShellExecute=false,
                RedirectStandardOutput=true,
                CreateNoWindow=true,
                WindowStyle= ProcessWindowStyle.Hidden
            };

            var process = Process.Start(processStart);
            if(process.WaitForExit(60000))
            {
                while(!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();

                    var parts = line.Trim().Split(' ');

                    if(parts.Length == 2)
                    {
                        packages.Add(new NugetPackage(parts[0], parts[1]));
                    }
                }
            }

            return packages;
        }

        internal string InstallPackage(NugetPackage missingPackage, string appFolder)
        {
            string nuPackageLocation = string.Empty;

            //install Bently.ThirdParty.Moq -Version 4.0.0 -Source http://mindentfs:8080/tfs/mcs_collection/_packaging/System1_Release/nuget/v3/index.json -OutputDirectory e:\dev\foo
            var processStart = new ProcessStartInfo("nuget.exe")
            {
                Arguments = $"install {missingPackage.ID} -Version {missingPackage.Version} -Source {nugetServerUri} -OutputDirectory {appFolder}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var p = Process.Start(processStart);
            if(p.WaitForExit(60000))
            {
                var packageFolder = ScrubVersionFolder(appFolder, missingPackage);
                if (!string.IsNullOrEmpty(packageFolder))
                {
                    if (Directory.Exists(packageFolder))
                    {
                        var info = new DirectoryInfo(packageFolder);
                        var nupackage = info.GetFiles("*.nupkg").FirstOrDefault();

                        if (nupackage != null)
                        {
                            nuPackageLocation = nupackage.FullName;
                        }
                    }
                }
            }

            return nuPackageLocation;
        }
        private string ScrubVersionFolder(string appFolder, NugetPackage package)
        {
            var folders = new DirectoryInfo(appFolder).GetDirectories();

            var expectedName = $"{package.ID}.{package.Version}";

            var match = folders.FirstOrDefault(folder => folder.Name.StartsWith(expectedName, StringComparison.OrdinalIgnoreCase));

            string folderName = null;

            if(match != null)
            {
                folderName = match.FullName;
            }

            return folderName;
        }
        internal void PushPackage(string nupackageLocation)
        {
            var processStart = new ProcessStartInfo("nuget.exe")
            {
                Arguments = $"push {nupackageLocation} -Source {nugetServerUri} -ApiKey {apiKey}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var p = Process.Start(processStart);
            p.WaitForExit(60000);
        }
    }
}
