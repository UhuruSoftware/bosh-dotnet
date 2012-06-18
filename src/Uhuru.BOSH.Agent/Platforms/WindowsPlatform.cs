using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uhuru.BOSH.Agent.Providers;
using Uhuru.BOSH.Agent.Platforms.Windows;

namespace Uhuru.BOSH.Agent.Platforms
{
    public class WindowsPlatform : IPlatform
    {
        public void MountPersistentDisk(string cid)
        {
            new Disk().MountPersistentDisk(cid);
        }

        public void UpdateLogging()
        {
            throw new NotImplementedException();
        }

        public void UpdatePasswords(List<string> settings)
        {
            throw new NotImplementedException();
        }

        public string LookupDiskByCid(string cid)
        {
            return new Disk().LookupDiskByCid(cid);
        }

        public string GetDataDiskDeviceName()
        {
            return new Disk().GetDataDiskDeviceName(); 
        }

        public void SettupNetworking()
        {
            WindowsNetwork windowsNetwork = new WindowsNetwork();
            windowsNetwork.SetupNetwork();
        }
    }
}
