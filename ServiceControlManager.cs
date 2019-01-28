// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceRecoveryClass.cs" company="GE Bently Nevada">
//  Class III - GE Confidential
//  This computer code is proprietary and confidential to the General Electric Company and/or its affiliate(s).
//  It may not be used, disclosed, modified, transferred, or reproduced without GE’s prior written consent, and must be returned on demand.
//  Unpublished Work © General Electric Company and/or its affiliate(s).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace NuGetServerMirror
{
    #region Win32 API Declarations

    [Flags]
    enum ServiceControlAccessRights : int
    {
        // Required to connect to the service control manager. 
        SC_MANAGER_CONNECT = 0x0001,
        // Required to call the CreateService function to create a service object and add it to the database. 
        SC_MANAGER_CREATE_SERVICE = 0x0002,
        // Required to call the EnumServicesStatusEx function to list the services that are in the database. 
        SC_MANAGER_ENUMERATE_SERVICE = 0x0004,
        // Required to call the LockServiceDatabase function to acquire a lock on the database. 
        SC_MANAGER_LOCK = 0x0008,
        // Required to call the QueryServiceLockStatus function to retrieve the lock status information for the database
        SC_MANAGER_QUERY_LOCK_STATUS = 0x0010,
        // Required to call the NotifyBootConfigStatus function. 
        SC_MANAGER_MODIFY_BOOT_CONFIG = 0x0020,
        // Includes STANDARD_RIGHTS_REQUIRED, in addition to all access rights in this table. 
        SC_MANAGER_ALL_ACCESS = 0xF003F
    }

    [Flags]
    enum ServiceAccessRights : int
    {
        // Required to call the QueryServiceConfig and QueryServiceConfig2 functions to query the service configuration. 
        SERVICE_QUERY_CONFIG = 0x0001,
        // Required to call the ChangeServiceConfig or ChangeServiceConfig2 function to change the service configuration. Because this grants the caller the right to change the executable file that the system runs, it should be granted only to administrators. 
        SERVICE_CHANGE_CONFIG = 0x0002,
        // Required to call the QueryServiceStatusEx function to ask the service control manager about the status of the service. 
        SERVICE_QUERY_STATUS = 0x0004,
        // Required to call the EnumDependentServices function to enumerate all the services dependent on the service. 
        SERVICE_ENUMERATE_DEPENDENTS = 0x0008,
        // Required to call the StartService function to start the service. 
        SERVICE_START = 0x0010,
        // Required to call the ControlService function to stop the service. 
        SERVICE_STOP = 0x0020,
        // Required to call the ControlService function to pause or continue the service. 
        SERVICE_PAUSE_CONTINUE = 0x0040,
        // Required to call the ControlService function to ask the service to report its status immediately. 
        SERVICE_INTERROGATE = 0x0080,
        // Required to call the ControlService function to specify a user-defined control code.
        SERVICE_USER_DEFINED_CONTROL = 0x0100,
        // Includes STANDARD_RIGHTS_REQUIRED in addition to all access rights in this table. 
        SERVICE_ALL_ACCESS = 0xF01FF
    }

    enum ServiceConfig2InfoLevel : int
    {
        // The lpBuffer parameter is a pointer to a SERVICE_DESCRIPTION structure.
        SERVICE_CONFIG_DESCRIPTION = 0x00000001,
        // The lpBuffer parameter is a pointer to a SERVICE_FAILURE_ACTIONS structure.
        SERVICE_CONFIG_FAILURE_ACTIONS = 0x00000002
    }

    enum SC_ACTION_TYPE : uint
    {
        // No action.
        SC_ACTION_NONE = 0x00000000,
        // Restart the service.
        SC_ACTION_RESTART = 0x00000001,
        // Reboot the computer.
        SC_ACTION_REBOOT = 0x00000002,
        // Run a command.
        SC_ACTION_RUN_COMMAND = 0x00000003
    }

    struct SERVICE_FAILURE_ACTIONS
    {
        [MarshalAs(UnmanagedType.U4)]
        public UInt32 dwResetPeriod;
        [MarshalAs(UnmanagedType.LPStr)]
        public String lpRebootMsg;
        [MarshalAs(UnmanagedType.LPStr)]
        public String lpCommand;
        [MarshalAs(UnmanagedType.U4)]
        public UInt32 cActions;
        public IntPtr lpsaActions;
    }

    struct SC_ACTION
    {
        [MarshalAs(UnmanagedType.U4)]
        public SC_ACTION_TYPE Type;
        [MarshalAs(UnmanagedType.U4)]
        public UInt32 Delay;
    }

    #endregion

    #region Service Recovery Class

    internal static class NativeMethods
    {

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "OpenSCManager")]
        public static extern IntPtr OpenSCManager(
            string machineName,
            string databaseName,
            ServiceControlAccessRights desiredAccess);

        [DllImport("advapi32.dll", EntryPoint = "CloseServiceHandle")]
        public static extern int CloseServiceHandle(IntPtr hSCObject);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "OpenService")]
        public static extern IntPtr OpenService(
            IntPtr hSCManager,
            string serviceName,
            ServiceAccessRights desiredAccess);

        [DllImport("advapi32.dll", EntryPoint = "ChangeServiceConfig2")]
        public static extern int ChangeServiceConfig2(
            IntPtr hService,
            ServiceConfig2InfoLevel dwInfoLevel,
            IntPtr lpInfo);
    }

    #endregion

    /// <summary>
    /// 
    /// </summary>
    public class ServiceControlManager : IDisposable
    {
        private IntPtr SCManager;
        private bool disposed;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="desiredAccess"></param>
        /// <returns></returns>
        private IntPtr OpenService(string serviceName, ServiceAccessRights desiredAccess)
        {
            // Open the service
            IntPtr service = NativeMethods.OpenService(
                SCManager,
                serviceName,
                desiredAccess);

            // Verify if the service is opened
            if (service == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to open the requested Service.");
            }

            return service;
        }

        /// <summary>
        /// 
        /// </summary>
        [SecurityCritical]
        public ServiceControlManager()
        {
            // Open the service control manager
            SCManager = NativeMethods.OpenSCManager(
                null,
                null,
                ServiceControlAccessRights.SC_MANAGER_CONNECT);

            // Verify if the SC is opened
            if (SCManager == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to open Service Control Manager.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        [SecurityCritical]
        public void SetRestartOnFailure(string serviceName)
        {
            //const int actionCount = 2;
            const int actionCount = 1;
            const uint delay = 60000; // 1 minute

            IntPtr service = IntPtr.Zero;
            IntPtr failureActionsPtr = IntPtr.Zero;
            IntPtr actionPtr = IntPtr.Zero;

            try
            {
                // Open the service
                service = OpenService(serviceName,
                    ServiceAccessRights.SERVICE_CHANGE_CONFIG |
                    ServiceAccessRights.SERVICE_START);

                // Allocate memory for the individual actions
                actionPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SC_ACTION)) * actionCount);

                // Set up the restart action
                SC_ACTION action = new SC_ACTION();
                action.Type = SC_ACTION_TYPE.SC_ACTION_RESTART;
                action.Delay = delay;
                Marshal.StructureToPtr(action, actionPtr, false);

                // Set up the failure actions
                SERVICE_FAILURE_ACTIONS failureActions = new SERVICE_FAILURE_ACTIONS();
                failureActions.dwResetPeriod = 0;
                failureActions.cActions = actionCount;
                failureActions.lpsaActions = actionPtr;
                failureActions.lpRebootMsg = null;
                failureActions.lpCommand = null;

                failureActionsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SERVICE_FAILURE_ACTIONS)));
                Marshal.StructureToPtr(failureActions, failureActionsPtr, false);

                // Make the change
                int changeResult = NativeMethods.ChangeServiceConfig2(
                    service,
                    ServiceConfig2InfoLevel.SERVICE_CONFIG_FAILURE_ACTIONS,
                    failureActionsPtr);

                // Check that the change occurred
                if (changeResult == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to change the Service configuration.");
                }
            }
            finally
            {
                // Clean up
                if (failureActionsPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(failureActionsPtr);
                }

                if (actionPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(actionPtr);
                }

                if (service != IntPtr.Zero)
                {
                    NativeMethods.CloseServiceHandle(service);
                }
            }
        }

        #region IDisposable Members

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here
                }

                // Unmanaged resources always need disposing
                if (SCManager != IntPtr.Zero)
                {
                    NativeMethods.CloseServiceHandle(SCManager);
                    SCManager = IntPtr.Zero;
                }
            }
            disposed = true;
        }

        /// <summary>
        /// 
        /// </summary>
        ~ServiceControlManager()
        {
            Dispose(false);
        }

        #endregion
    }
}

