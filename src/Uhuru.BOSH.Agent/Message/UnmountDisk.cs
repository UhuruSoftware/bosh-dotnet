// -----------------------------------------------------------------------
// <copyright file="UnmountDisk.cs" company="Uhuru Software, Inc.">
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Unmount", Justification = "FxCop Bug")]
    public class UnmountDisk
    {
      ////def self.long_running?; true; end

      ////def self.process(args)
      ////  self.new.unmount(args)
      ////end

      ////def unmount(args)
      ////  cid = args.first
      ////  disk = Bosh::Agent::Config.platform.lookup_disk_by_cid(cid)
      ////  partition = "#{disk}1"

      ////  if DiskUtil.mount_entry(partition)
      ////    @block, @mountpoint = DiskUtil.mount_entry(partition).split
      ////    DiskUtil.umount_guard(@mountpoint)
      ////    logger.info("Unmounted #{@block} on #{@mountpoint}")
      ////    return {:message => "Unmounted #{@block} on #{@mountpoint}" }
      ////  else
      ////    # TODO: should we raise MessageHandlerError here?
      ////    return {:message => "Unknown mount for partition: #{partition}"}
      ////  end
      ////end
    }
}
