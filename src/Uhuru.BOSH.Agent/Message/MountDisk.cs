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

        public static void Process(dynamic[] args)
        {
            new MountDisk(args).Mount();
        }

        public MountDisk(dynamic[] args)
        {
            cid = args[0];
        }

        public void Mount()
        {
            if (Config.Configure)
            {
                UpdateSettings();
                Logger.Info("MountDisk: {0} - {1}", cid, Settings["disk"].ToString());

                SetupDisk();
            }
        }

        public void UpdateSettings()
        {
            // TODO: Bosh::Agent::Config.settings = Bosh::Agent::Config.infrastructure.load_settings
            Logger.Info(String.Format("Settings: {0}", Settings.ToString()));
            throw new NotImplementedException();
        }

      ////def update_settings
      ////  Bosh::Agent::Config.settings = Bosh::Agent::Config.infrastructure.load_settings
      ////  logger.info("Settings: #{settings}")
      ////end

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

        public static bool LongRunning { get { return true; } }
    }
}
