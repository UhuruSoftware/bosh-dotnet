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
            string dataDisk = disks["ephemeral"].Value.ToString();
            return dataDisk;
        }

        public void MountPersistentDisk(int diskId)
        {
            if (!Directory.Exists(StorePath))
            {
                Directory.CreateDirectory(StorePath);
            }
            if (!DiskUtil.DiskHasPartition(diskId))
            {
                if (DiskUtil.CreatePrimaryPartition(diskId, "store") != 0)
                {
                    Logger.Error("Could not create partition on drive " + diskId);
                }
            }

            if (DiskUtil.MountPartition(diskId, StorePath) != 0)
            {
                Logger.Error("Could not mount disk " + diskId + " to " + StorePath);
                throw new Errors.FatalBoshException("Failed to mount: " + diskId + " to " + StorePath);
            }
        }

        public string LookupDiskByCid(string cid)
        {
            Logger.Info("Looking disk by CID :" + Config.Settings.ToString());
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
