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

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class MigrateDisk
    {
        ////def self.long_running?; true; end

        ////  def self.process(args)
        ////    #logger = Bosh::Agent::Config.logger
        ////    #logger.info("MigrateDisk:" + args.inspect)

        ////    self.new.migrate(args)
        ////    {}
        ////  end

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

        ////  def check_mountpoints
        ////    Pathname.new(store_path).mountpoint? && Pathname.new(store_migration_target).mountpoint?
        ////  end

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
