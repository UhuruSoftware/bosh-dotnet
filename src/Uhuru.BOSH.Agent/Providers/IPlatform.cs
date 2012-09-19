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
        /// <summary>
        /// Mounts the persistent disk.
        /// </summary>
        /// <param name="diskId">The disk id.</param>
        void MountPersistentDisk(int diskId);

        /// <summary>
        /// Updates the logging.
        /// </summary>
        void UpdateLogging();

        /// <summary>
        /// Updates the passwords.
        /// </summary>
        /// <param name="settings">The settings.</param>
        void UpdatePasswords(dynamic settings);

        /// <summary>
        /// Lookups the disk by cid.
        /// </summary>
        /// <param name="cid">The cid.</param>
        /// <returns></returns>
        string LookupDiskByCid(string cid);

        /// <summary>
        /// Gets the name of the get data disk device.
        /// </summary>
        /// <value>
        /// The name of the get data disk device.
        /// </value>
        string GetDataDiskDeviceName
        {
            get;
        }

        /// <summary>
        /// Setups the networking.
        /// </summary>
        void SetupNetworking();

    }
}
