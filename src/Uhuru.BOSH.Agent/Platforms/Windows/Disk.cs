// -----------------------------------------------------------------------
// <copyright file="Disk.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Platforms.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
using System.IO;
    using Uhuru.BOSH.Agent.Message;
    using Uhuru.Utilities;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Disk
    {
        public string BaseDir { get { return Config.BaseDir; } }
        public string StorePath { get { return Path.Combine(BaseDir, "store"); } }

        public string GetDataDiskDeviceName()
        {
            dynamic disks = Config.Settings["disks"];
            string dataDisk = disks["ephemeral"].Value;
            return dataDisk;
        }

        public void MountPersistentDisk(string cid)
        {
            if (!Directory.Exists(StorePath))
            {
                Directory.CreateDirectory(StorePath);
            }
            if (!DiskUtil.DiskHasPartition(int.Parse(cid)))
            {
                if (DiskUtil.CreatePrimaryPartition(int.Parse(cid), "store") != 0)
                {
                    Logger.Error("Could not create partition on drive " + cid);
                }
            }

            if (DiskUtil.MountPartition(int.Parse(cid), StorePath) != 0)
            {
                Logger.Error("Could not mount disk " + cid + " to " + StorePath);
                throw new Errors.FatalBoshException("Failed to mount: " + cid + " to " + StorePath);
            }
        }

        public string LookupDiskByCid(string cid)
        {
            if (Config.Settings["disks"]["persistent"][cid] != null)
            {
                return Config.Settings["disks"]["persistent"][cid].ToString();
            }
            else
            {
                throw new Errors.FatalBoshException("Unknown persistent disk: " + cid);
            }
        }
    }
}
