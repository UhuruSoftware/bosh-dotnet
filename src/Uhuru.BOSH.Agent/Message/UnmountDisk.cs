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
    using System.Globalization;

    /// <summary>
    /// Unmount Disk message
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Unmount", Justification = "FxCop Bug")]
    public class UnmountDisk :  IMessage
    {


        /// <summary>
        /// Determines whether the message [is long running].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is long running]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsLongRunning()
        {
            return true;
        }

        /// <summary>
        /// Processes the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public object Process(dynamic args)
        {
            Logger.Info("Processing unmount disk :" + args.ToString());
            string cid = args[0].Value.ToString();

            int diskId = int.Parse(Config.Platform.LookupDiskByCid(cid), CultureInfo.InvariantCulture);
            string mountEntry = DiskUtil.MountEntry(diskId);
            if (mountEntry != null)
            {
                DiskUtil.UnmountGuard(mountEntry);
                UnmountMessage unmountMessage = new UnmountMessage();
                unmountMessage.Message = string.Format(CultureInfo.InvariantCulture, "done unmount {0} on {1}", mountEntry, diskId);
                return unmountMessage;
            }
            else
            {
                UnmountMessage unmountMessage = new UnmountMessage();
                unmountMessage.Message = string.Format(CultureInfo.InvariantCulture, "Unknown mount for partition {0}", diskId.ToString(CultureInfo.InvariantCulture));
                return unmountMessage;
            }
        }

        internal class UnmountMessage
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), JsonProperty("message")]
            internal string Message
            {
                get;
                set;
            }
        }
    }
}
