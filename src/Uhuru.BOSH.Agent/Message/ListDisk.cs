// -----------------------------------------------------------------------
// <copyright file="ListDisk.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Security.Policy;
    using Newtonsoft.Json.Linq;
    using Uhuru.Utilities;

    /// <summary>
    /// List Disk message.
    /// </summary>
    public class ListDisk : IMessage
    {
        /// <summary>
        /// Processes the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public object Process(dynamic args)
        {
            List<string> diskInfo = new List<string>();
            Logger.Info("Processing list disk");
            dynamic settings = Config.Settings;
            dynamic cids;

            cids = settings["disks"]["persistent"] ?? new string[] {};

            foreach (var cid in cids)
            {
                int diskId = int.Parse(Config.Platform.LookupDiskByCid(cid.Name));
                Logger.Info("Found disk with Id :" + diskId);

                if (!(DiskUtil.MountEntry(diskId) == null))
                {
                    diskInfo.Add(cid.Name);
                }
            }

            JArray obj = JArray.FromObject(diskInfo);

            return obj;
        }

        /// <summary>
        /// Determines whether the message [is long running].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is long running]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsLongRunning()
        {
            return false;
        }

    }
}
