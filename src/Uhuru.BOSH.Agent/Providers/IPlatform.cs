using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Uhuru.BOSH.Agent.Providers
{
    /// <summary>
    /// Interface for platform
    /// </summary>
    public interface IPlatform
    {
        void MountPersistentDisk(int diskId);

        void UpdateLogging();

        void UpdatePasswords(Collection<string> settings);

        string LookupDiskByCid(string cid);

        string GetDataDiskDeviceName();

        void SetupNetworking();

    }
}
