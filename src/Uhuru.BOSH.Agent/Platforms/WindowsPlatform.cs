using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uhuru.BOSH.Agent.Providers;
using Uhuru.BOSH.Agent.Platforms.Windows;
using System.Collections.ObjectModel;

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

        // TODO: JIRA UH-1206
        public void UpdatePasswords(Collection<string> settings)
        {
            throw new NotImplementedException();
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
            WindowsNetwork windowsNetwork = new WindowsNetwork();
            windowsNetwork.SetupNetwork();
        }
    }
}
