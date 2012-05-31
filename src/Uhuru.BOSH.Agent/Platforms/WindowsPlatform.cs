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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public string GetDataDiskDeviceName()
        {
            throw new NotImplementedException();
        }

        public void SettupNetworking()
        {
            WindowsNetwork windowsNetwork = new WindowsNetwork();
            windowsNetwork.SetupNetwork();
        }
    }
}
