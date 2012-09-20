using System;
using Uhuru.BOSH.Agent.Platforms.Windows;
using Uhuru.BOSH.Agent.Providers;

namespace Uhuru.BOSH.Agent.Platforms
{
    public class WindowsPlatform : IPlatform
    {
        public void MountPersistentDisk(int diskId)
        {
            Disk.MountPersistentDisk(diskId);
        }

        // TODO: JIRA UH-1206
        public void UpdateLogging()
        {
            throw new NotImplementedException();
        }

        public void UpdatePasswords(dynamic settings)
        {
            Password.Update(settings);
        }

        public string LookupDiskByCid(string cid)
        {
            return Disk.LookupDiskByCid(cid);
        }

        public string GetDataDiskDeviceName
        {
            get
            {
                return Disk.GetDataDiskDeviceName;
            }
        }

        public void SetupNetworking()
        {
            WindowsNetwork.SetupNetwork();
        }
    }
}
