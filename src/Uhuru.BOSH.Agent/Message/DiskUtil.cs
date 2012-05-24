// -----------------------------------------------------------------------
// <copyright file="DiskUtil.cs" company="Uhuru Software, Inc.">
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Util", Justification = "FxCop Bug")]
    public class DiskUtil
    {
        string BaseDir
        {
            get
            {
                return Config.BaseDir;
            }
        }

        public string MountEntry(string partition)
        {
            throw new NotImplementedException();
            ////  File.read('/proc/mounts').lines.select { |l| l.match(/#{partition}/) }.first
        }

        int GUARD_RETRIES = 600;
        int GUARD_SLEEP = 1;

        public void UnmountGuard(string mountpoint)
        {
            int unmountAttempts = GUARD_RETRIES;

            throw new NotImplementedException();

            ////  loop do
            ////    umount_output = `umount #{mountpoint} 2>&1`

            ////    if $?.exitstatus == 0
            ////      break
            ////    elsif umount_attempts != 0 && umount_output =~ /device is busy/
            ////      #when umount2 syscall fails and errno == EBUSY, umount.c outputs:
            ////      # "umount: %s: device is busy.\n"
            ////      sleep GUARD_SLEEP
            ////      umount_attempts -= 1
            ////    else
            ////      raise Bosh::Agent::MessageHandlerError,
            ////        "Failed to umount #{mountpoint}: #{umount_output}"
            ////    end
            ////  end

            ////  attempts = GUARD_RETRIES - umount_attempts
            ////  logger.info("umount_guard #{mountpoint} succeeded (#{attempts})")
        }

        public bool EnsureNoPartition(string disk, string partition)
        {
            throw new NotImplementedException();
            ////# Pay a penalty on this check the first time a persistent disk is added to a system
            ////def ensure_no_partition?(disk, partition)
            ////  check_count = 2
            ////  check_count.times do
            ////    if sfdisk_lookup_partition(disk, partition).empty?
            ////      # keep on trying
            ////    else
            ////      if File.blockdev?(partition)
            ////        return false # break early if partition is there
            ////      end
            ////    end
            ////    sleep 1
            ////  end

            ////  # Double check that the /dev entry is there
            ////  if File.blockdev?(partition)
            ////    return false
            ////  else
            ////    return true
            ////  end
            ////end
        }


        public string LookupPartition(string disk, string partition)
        {
            throw new NotImplementedException();
            //// `sfdisk -Llq #{disk}`.lines.select { |l| l.match(%q{/\A#{partition}.*83.*Linux}) }
        }

        public Dictionary<string, object> GetUsage()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result.Add("system", null);
            result.Add("ephemeral", null);

            DriveInfo[] allDrives = DriveInfo.GetDrives();

            throw new NotImplementedException();

            ////  disk_usage = `#{disk_usage_command}`

            ////  if $?.to_i != 0
            ////    logger.error("Failed to get disk usage data, df exit code = #{$?.to_i}")
            ////    return result
            ////  end

            ////  disk_usage.split("\n")[1..-1].each do |line|
            ////    usage, mountpoint = line.split(/\s+/)
            ////    usage.gsub!(/%$/, '')

            ////    case mountpoint
            ////    when "/"
            ////      result["system"]["percent"] = usage
            ////    when File.join("#{base_dir}", "data")
            ////      result["ephemeral"]["percent"] = usage
            ////    when File.join("#{base_dir}", "store")
            ////      # Only include persistent disk data if
            ////      # persistent disk is there
            ////      result["persistent"] = { }
            ////      result["persistent"]["percent"] = usage
            ////    end
            ////  end

            ////  result

        }
    }
}
