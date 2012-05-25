// -----------------------------------------------------------------------
// <copyright file="MigrateDisk.cs" company="Uhuru Software, Inc.">
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

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class MigrateDisk : Base
    {
        string oldCid;
        string newCid;

        public bool LongRunning
        {
            get { return true; }
        }

        public static void Process(dynamic args)
        {
            new MigrateDisk().Migrate(args);
        }

        ////  def self.process(args)
        ////    #logger = Bosh::Agent::Config.logger
        ////    #logger.info("MigrateDisk:" + args.inspect)

        ////    self.new.migrate(args)
        ////    {}
        ////  end

        public void Migrate(dynamic args)
        {
            Logger.Info(String.Format("MigrateDisk:{0}", args));
            oldCid = args[0];
            newCid = args[1];
            DiskUtil.UnmountGuard(StorePath);
            MountStore(oldCid, "-o -ro"); // TODO

            if (CheckMountpoints())
            {
                Logger.Info("Copy data from old to new store disk");
                throw new NotImplementedException();
            }

            DiskUtil.UnmountGuard(StorePath);
            DiskUtil.UnmountGuard(StoreMigrationTarget);

            MountStore(newCid);
        }

        ////  def migrate(args)
        ////    logger.info("MigrateDisk:" + args.inspect)
        ////    @old_cid, @new_cid = args

        ////    DiskUtil.umount_guard(store_path)

        ////    mount_store(@old_cid, "-o ro") #read-only

        ////    if check_mountpoints
        ////      logger.info("Copy data from old to new store disk")
        ////      `(cd #{store_path} && tar cf - .) | (cd #{store_migration_target} && tar xpf -)`
        ////    end

        ////    DiskUtil.umount_guard(store_path)
        ////    DiskUtil.umount_guard(store_migration_target)

        ////    mount_store(@new_cid)
        ////  end

        public bool CheckMountpoints()
        {
            throw new NotImplementedException();
        }

        ////  def check_mountpoints
        ////    Pathname.new(store_path).mountpoint? && Pathname.new(store_migration_target).mountpoint?
        ////  end

        public void MountStore(string cid, string options = "")
        {
            throw new NotImplementedException();
        }

        ////  def mount_store(cid, options="")
        ////    disk = Bosh::Agent::Config.platform.lookup_disk_by_cid(cid)
        ////    partition = "#{disk}1"
        ////    logger.info("Mounting: #{partition} #{store_path}")
        ////    `mount #{options} #{partition} #{store_path}`
        ////    unless $?.exitstatus == 0
        ////      raise Bosh::Agent::MessageHandlerError, "Failed to mount: #{partition} #{store_path} (exit code #{$?.exitstatus})"
        ////    end
        ////  end
    }
}
