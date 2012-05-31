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
    public class MountDisk : Base
    {
        int cid;

        /// <summary>
        /// Processes the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        public static void Process(dynamic[] args)
        {
            new MountDisk(args).Mount();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MountDisk"/> class.
        /// </summary>
        /// <param name="args">The args.</param>
        public MountDisk(dynamic[] args)
        {
            cid = args[0];
        }

        /// <summary>
        /// Mounts this instance.
        /// </summary>
        public void Mount()
        {
            if (Config.Configure)
            {
                UpdateSettings();
                Logger.Info("MountDisk: {0} - {1}", cid, Settings["disk"].ToString());

                SetupDisk();
            }
        }

        /// <summary>
        /// Updates the settings.
        /// </summary>
        public void UpdateSettings()
        {
            Config.Settings = Config.Infrastructure.LoadSettings();
            Logger.Info(String.Format("Settings: {0}", Settings.ToString()));
            throw new NotImplementedException();
        }

        /// <summary>
        /// Setups the disk.
        /// </summary>
        public void SetupDisk()
        {
            Logger.Info(String.Format("Setup disk settings: {0}", Settings.ToString()));

            if (!DiskUtil.DiskHasPartition(cid))
            {
                Logger.Info(String.Format("Found blank disk ", cid));
                int returnCode = DiskUtil.CreatePrimaryPartition(cid, "store");
                if (returnCode != 0)
                {
                    throw new MessageHandlerException(String.Format("Unable to create partition. Exit code: {0}", returnCode));
                }
            }
            else
            {
                Logger.Info(String.Format("Disk has partition"));
            }

            MountPersistentDisk(cid);
        }

        /// <summary>
        /// Mounts the persistent disk.
        /// </summary>
        /// <param name="diskIndex">Index of the disk.</param>
        public void MountPersistentDisk(int diskIndex)
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

            Logger.Info(String.Format("Mount Partition {0} {1}", diskIndex, mountpoint));

            int returnCode = DiskUtil.MountPartition(diskIndex, mountpoint);

            if (returnCode != 0)
            {
                throw new MessageHandlerException(String.Format("Failed mount disk {0} on {1}. Exit code: {2}", diskIndex, mountpoint, returnCode));
            }

        }

        /// <summary>
        /// Gets a value indicating whether [long running].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [long running]; otherwise, <c>false</c>.
        /// </value>
        public static bool LongRunning { get { return true; } }
    }
}
