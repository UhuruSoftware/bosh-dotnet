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

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ListDisk : Base, IMessage
    {
        public string Process(dynamic args)
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

            cids = settings["disks"]["persistent"] ?? new string[] {};

            if (cids.Count == 0)
                return cids.ToString();

            foreach (var cid in cids)
            {
                string diskId = Config.Platform.LookupDiskByCid(cid.Value);
                if (!(DiskUtil.MountEntry(diskId) == null))
                {
                    diskInfo.Add(cid);
                }
            }

            JObject obj = JObject.FromObject(diskInfo);

            return obj.ToString();
        }

        public bool IsLongRunning()
        {
            return false;
        }

    }
}
