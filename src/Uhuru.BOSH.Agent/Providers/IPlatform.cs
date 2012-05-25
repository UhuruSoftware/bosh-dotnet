using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.BOSH.Agent.Providers
{
    /// <summary>
    /// Interface for platform
    /// </summary>
    public interface IPlatform
    {
        void MountPersistentDisk(string cid);

        void UpdateLogging();

        void UpdatePasswords(List<string> settings);

        string LookupDiskByCid(string cid);

        string GetDataDiskDeviceName();

        void SettupNetworking();

    }
}
