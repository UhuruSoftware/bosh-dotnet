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
    using Uhuru.Utilities;
    using Newtonsoft.Json;
    using Uhuru.BOSH.Agent.Errors;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Unmount", Justification = "FxCop Bug")]
    public class UnmountDisk : Base, IMessage
    {
        /// <summary>
        /// Gets a value indicating whether [long running].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [long running]; otherwise, <c>false</c>.
        /// </value>
       

        public void Unmount(dynamic args)
        {

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

        public bool IsLongRunning()
        {
            return true;
        }

        public object Process(dynamic args)
        {
            Logger.Info("Processing unmount disk :" + args.ToString());
            string cid = args[0].Value.ToString();

            int diskId = int.Parse(Config.Platform.LookupDiskByCid(cid));
            string mountEntry = DiskUtil.MountEntry(diskId);
            if (mountEntry != null)
            {
                DiskUtil.UnmountGuard(mountEntry);
                UnmountMessage unmountMessage = new UnmountMessage();
                unmountMessage.Message = string.Format("Unmounted {0} on {1}", mountEntry, diskId);
                return unmountMessage;
            }
            else
            {
                UnmountMessage unmountMessage = new UnmountMessage();
                unmountMessage.Message = string.Format("Unknown mount for partition {0}", diskId.ToString());
                return unmountMessage;
            }
        }

        public class UnmountMessage
        {
            [JsonProperty("message")]
            public string Message
            {
                get;
                set;
            }
        }
    }
}
