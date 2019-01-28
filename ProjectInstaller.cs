using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.Management;

namespace NuGetServerMirror
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        /// <summary>
        /// Service description which appears in the Service control manager.
        /// </summary>
        private const string SERVICE_DESCRIPTION = "Set Interact with Desktop to see DHP host popups";

        /// <summary>
        /// Service installer
        /// </summary>
        private System.ServiceProcess.ServiceInstaller serviceInstaller;

        /// <summary>
        /// Process installer
        /// </summary>
        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller;

        /// <summary>
        ///
        /// </summary>
        public ProjectInstaller()
        {
            InitializeComponent();

            this.serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller = new System.ServiceProcess.ServiceInstaller();

            this.serviceProcessInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.StarterWinServiceProcessInstallerAfterInstall);

            //
            // ServiceInstaller
            //
            this.serviceInstaller.ServiceName = MirrorService.RegisteredServiceName;
            this.serviceInstaller.Description = SERVICE_DESCRIPTION;
            this.serviceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Manual;

            //
            // ProjectInstaller
            //
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
                                                                 this.serviceProcessInstaller,
                                                                 this.serviceInstaller});
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="stateSaver"></param>
        public override void Install(System.Collections.IDictionary stateSaver)
        {
            this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            base.Install(stateSaver);

            ConnectionOptions coOptions = new ConnectionOptions();
            coOptions.Impersonation = ImpersonationLevel.Impersonate;

            ManagementScope mgmtScope = new System.Management.ManagementScope(@"root\CIMV2", coOptions);
            mgmtScope.Connect();

            using (ManagementObject wmiService = new ManagementObject("Win32_Service.Name='" + MirrorService.RegisteredServiceName + "'"))
            {
                ManagementBaseObject InParam = wmiService.GetMethodParameters("Change");
                InParam["DesktopInteract"] = false;
                wmiService.InvokeMethod("Change", InParam, null);
            }
            ConfigureRecoveryMechanism();
        }

        /// <summary>
        ///
        /// </summary>
        private static void ConfigureRecoveryMechanism()
        {
            ServiceControlManager SCManager = null;

            try
            {
                SCManager = new ServiceControlManager();
                SCManager.SetRestartOnFailure(MirrorService.RegisteredServiceName);
            }
            catch (Exception)
            {
            }
            finally
            {
                if (null != SCManager)
                {
                    SCManager.Dispose();
                }
            }
        }

        private void StarterWinServiceProcessInstallerAfterInstall(object sender, System.Configuration.Install.InstallEventArgs e)
        {
            //do nothing...
        }
    }
}