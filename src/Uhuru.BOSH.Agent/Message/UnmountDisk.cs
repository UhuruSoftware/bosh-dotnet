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
    using System.IO;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Unmount", Justification = "FxCop Bug")]
    public class UnmountDisk : Base
    {
        /// <summary>
        /// Gets a value indicating whether [long running].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [long running]; otherwise, <c>false</c>.
        /// </value>
        public bool LongRunning { get { return true; } }

        public static void Process(dynamic args)
        {
            new UnmountDisk().Unmount(args);
        }

        public void Unmount(dynamic args)
        {
            throw new NotImplementedException();
        }

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
