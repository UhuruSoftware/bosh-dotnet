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

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ListDisk : Base
    {
        public static ICollection<string> Process(string[] args = null)
        {
            List<string> diskInfo = new List<string>();

            dynamic settings = Config.Settings;
            dynamic cids;

            // TODO: check for hash

            ////  # TODO abstraction for settings
            ////  if settings["disks"].kind_of?(Hash) && settings["disks"]["persistent"].kind_of?(Hash)
            ////    cids = settings["disks"]["persistent"]
            ////  else
            ////    cids = {}
            ////  end

            cids = settings["disk"]["persistent"] ?? new string[] {};

            foreach (var cid in cids)
            {
                string disk = Config.Platform.LookupDiskByCid(cid);
                if (!(DiskUtil.MountEntry(disk) == null))
                {
                    diskInfo.Add(cid);
                }
            }

            return diskInfo;
        }
    }
}
