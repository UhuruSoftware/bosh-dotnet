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

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ListDisk
    {
      ////     def self.process(args = nil)
      ////  disk_info = []
      ////  settings = Bosh::Agent::Config.settings

      ////  # TODO abstraction for settings
      ////  if settings["disks"].kind_of?(Hash) && settings["disks"]["persistent"].kind_of?(Hash)
      ////    cids = settings["disks"]["persistent"]
      ////  else
      ////    cids = {}
      ////  end

      ////  cids.each_key do |cid|
      ////    disk = Bosh::Agent::Config.platform.lookup_disk_by_cid(cid)
      ////    partition = "#{disk}1"
      ////    disk_info << cid unless DiskUtil.mount_entry(partition).nil?
      ////  end
      ////  disk_info
      ////end
    }
}
