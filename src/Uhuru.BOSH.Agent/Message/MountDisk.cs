// -----------------------------------------------------------------------
// <copyright file="MountDisk.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Uhuru.Utilities;
    using System.IO;
    using Uhuru.BOSH.Agent.Errors;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class MountDisk : Base, IMessage
    {
        string cid;

        /// <summary>
        /// Processes the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        public object Process(dynamic args)
        {
            if (args.Count == 0)
                return string.Empty;
            cid = args[0].ToString();
            return Mount();
        }

        /// <summary>
        /// Mounts this instance.
        /// </summary>
        public object Mount()
        {
            if (Config.Configure)
            {
                UpdateSettings();
                Logger.Info("Current settings :" + Settings.ToString());
                Logger.Info("MountDisk: {0} - {1}", cid, Settings["disks"].ToString());

                return SetupDisk();
            }

            return string.Empty;
        }

        /// <summary>
        /// Updates the settings.
        /// </summary>
        public void UpdateSettings()
        {
            Config.Settings = Config.Infrastructure.LoadSettings();
            Logger.Info("Settings :" + Config.Settings.ToString());
        }

        /// <summary>
        /// Setups the disk.
        /// </summary>
        public object SetupDisk()
        {

            int diskId = int.Parse(Config.Platform.LookupDiskByCid(cid));
            
            Logger.Info("Setup disk settings: " + Settings.ToString());

            if (!DiskUtil.DiskHasPartition(diskId))
            {
                Logger.Info("Found blank disk "+ diskId.ToString());
                int returnCode = DiskUtil.CreatePrimaryPartition(diskId, "store");
                if (returnCode != 0)
                {
                    throw new MessageHandlerException(String.Format("Unable to create partition. Exit code: {0}", returnCode));
                }
            }
            else
            {
                Logger.Info(String.Format("Disk has partition"));
            }

            MountPersistentDisk(diskId);

            return new object();
        }

        /// <summary>
        /// Mounts the persistent disk.
        /// </summary>
        /// <param name="diskId">Id of the disk.</param>
        public void MountPersistentDisk(int diskId)
        {
            string storeMountPoint = Path.Combine(BaseDir, "store");
            string mountpoint;

            if (DiskUtil.IsMountPoint(storeMountPoint))
            {
                Logger.Info("Mounting persistent disk store migration target");
                mountpoint = Path.Combine(BaseDir, "store_migraton_target");
            }
            else
            {
                Logger.Info("Mounting persistent disk store");
                mountpoint = storeMountPoint;
            }

            if (!Directory.Exists(mountpoint))
            {
                Directory.CreateDirectory(mountpoint);
            }

            Logger.Info(String.Format("Mount Partition {0} {1}", diskId, mountpoint));

            int returnCode = DiskUtil.MountPartition(diskId, mountpoint);

            if (returnCode != 0)
            {
                throw new MessageHandlerException(String.Format("Failed mount disk {0} on {1}. Exit code: {2}", diskId, mountpoint, returnCode));
            }

        }

        public bool IsLongRunning()
        {
            return true;
        }
    }
}
