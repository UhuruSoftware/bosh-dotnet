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
    /// TODO: Update summary.
    /// </summary>
    public class ListDisk : Base, IMessage
    {
        public string Process(dynamic args)
        {
            List<string> diskInfo = new List<string>();
            Logger.Info("Processing list_disk");
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
                int diskId = int.Parse(Config.Platform.LookupDiskByCid(cid.Name));
                Logger.Info("Found disk with Id :" + diskId);

                if (!(DiskUtil.MountEntry(diskId) == null))
                {
                    diskInfo.Add(cid.Name);
                }
            }

            JArray obj = JArray.FromObject(diskInfo);

            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }

        public bool IsLongRunning()
        {
            return false;
        }

    }
}
