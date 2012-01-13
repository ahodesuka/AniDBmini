
#region Using Statements

using System;
using System.Management;

#endregion Using Statements

namespace AniDBmini
{
    public delegate void MPCStartedHandler(ManagementObject mpc);

    public class MPCProcWatcher
    {

        #region Fields

        public MPCStartedHandler OnMPCStarted = delegate { };

        private ManagementEventWatcher m_mpcWatcher;
        private ManagementScope m_wScope = new ManagementScope(@"\\.\root\CIMV2");

        #endregion Fields

        public MPCProcWatcher()
        {
            m_mpcWatcher = new ManagementEventWatcher();
            m_mpcWatcher.Query = new WqlEventQuery("__InstanceOperationEvent", TimeSpan.FromSeconds(1),
                @"TargetInstance ISA 'Win32_Process' AND ( TargetInstance.Name = 'mpc-hc.exe' OR TargetInstance.Name = 'mpc-hc64.exe')");
            m_mpcWatcher.Scope = m_wScope;

            m_mpcWatcher.EventArrived += new EventArrivedEventHandler(m_mpcWatcher_EventArrived);
            m_mpcWatcher.Start();
        }

        private void m_mpcWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            string eType = e.NewEvent.ClassPath.ClassName;
            UInt32 processID = (UInt32)(e.NewEvent["TargetInstance"] as ManagementBaseObject)["ProcessID"];

            if (eType == "__InstanceCreationEvent")
            {
                ObjectQuery objQuery = new ObjectQuery(String.Format(@"SELECT * FROM Win32_Process WHERE ProcessID = '{0}'", processID));
                using (ManagementObjectSearcher objSearcher = new ManagementObjectSearcher(objQuery))
                    using (ManagementObjectCollection objCollection = objSearcher.Get())
                        foreach (ManagementObject obj in objCollection)
                            OnMPCStarted(obj);
            }
        }
    }
}
