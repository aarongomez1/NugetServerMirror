using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Globalization;
using System.Linq;
using System.ServiceProcess;

namespace NuGetServerMirror
{
    public partial class MirrorService : ServiceBase
    {
        public const string InstallLogfile = @"NuGetMirrorService_Install.log";
        public const string PathArgument = "/assemblypath={0}";
        public const string RegisteredServiceName = "Bently NuGet Mirror Service";

        private NugetMirror nugetMirror;
        public MirrorService()
        {
            InitializeComponent();
        }

        internal static void Install()
        {
            if (ServiceExists())
            {
                UninstallService();
            }

            InstallService();
        }

        public void Go()
        {
            this.OnStart(new string[0]);
        }

        protected override void OnStart(string[] args)
        {
            var data = (NameValueCollection)ConfigurationManager.GetSection("Mirror");

            var sourceServer = data["source"];
            var targetServer = data["target"];
            var targetApiKey = data["targetApiKey"];
            nugetMirror = new NugetMirror(sourceServer, targetServer, targetApiKey);
            nugetMirror.Start();
        }

        protected override void OnStop()
        {
            nugetMirror.Stop();
        }
        

        private static void InstallService()
        {
            ProjectInstaller pi = null;

            try
            {
                pi = new ProjectInstaller();

                string path = string.Format(CultureInfo.InvariantCulture,
                   PathArgument,
                   System.Reflection.Assembly.GetExecutingAssembly().Location);

                string[] cmdline = { path, "LogToConsole" };

                System.IO.File.Delete(InstallLogfile);

                InstallContext ctx = new InstallContext(InstallLogfile, cmdline);
                pi.Context = ctx;

                pi.Install(new Hashtable());
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.ToString());
            }
            finally
            {
                if (null != pi)
                {
                    pi.Dispose();
                }
            }
        }

        private static bool ServiceExists()
        {
            bool serviceExists = false;
            try
            {
                ServiceController[] services = ServiceController.GetServices();

                serviceExists = services.Any(
                    t => 0 == string.Compare(t.ServiceName, RegisteredServiceName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Win32Exception exp)
            {
                Console.WriteLine(exp.ToString());
            }
            catch (ArgumentException exp)
            {
                Console.WriteLine(exp.ToString());
            }

            return serviceExists;
        }

        private static void UninstallService()
        {
            ProjectInstaller pi = null;

            try
            {
                pi = new ProjectInstaller();
                string path = string.Format(CultureInfo.InvariantCulture,
                      PathArgument,
                      System.Reflection.Assembly.GetExecutingAssembly().Location);

                string[] cmdline = { path, "LogToConsole" };

                System.IO.File.Delete(InstallLogfile);

                InstallContext ctx = new InstallContext(InstallLogfile, cmdline);
                pi.Context = ctx;

                pi.Uninstall(null);
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.ToString());
            }
            finally
            {
                if (null != pi)
                {
                    pi.Dispose();
                }
            }
        }
    }
}