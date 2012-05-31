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
    using System.Diagnostics;
    using System.Management;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Util", Justification = "FxCop Bug")]
    public class DiskUtil
    {
        static string BaseDir
        {
            get
            {
                return Config.BaseDir;
            }
        }

        public static string MountEntry(string partition)
        {
            throw new NotImplementedException();
            ////  File.read('/proc/mounts').lines.select { |l| l.match(/#{partition}/) }.first
        }

        static int GUARD_RETRIES = 600;
        static int GUARD_SLEEP = 1;

        public static void UnmountGuard(string mountpoint)
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

        public static bool EnsureNoPartition(string disk, string partition)
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


        public static string LookupPartition(string disk, string partition)
        {
            throw new NotImplementedException();
            //// `sfdisk -Llq #{disk}`.lines.select { |l| l.match(%q{/\A#{partition}.*83.*Linux}) }
        }

        public static Dictionary<string, object> GetUsage()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result.Add("system", null);
            result.Add("ephemeral", null);

            string dataDir = Path.Combine(BaseDir, "data").TrimEnd(new char[] { '\\' }).ToLower();
            string storeDir = Path.Combine(BaseDir, "store").TrimEnd(new char[] { '\\' }).ToLower();

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Volume");

            foreach (ManagementObject queryObj in searcher.Get())
            {
                string caption = queryObj["Caption"].ToString().TrimEnd(new char[] { '\\' }).ToLower();
                if (caption == "c:")
                {
                    result["system"] = CalculateDiskUsage(UInt64.Parse(queryObj["Capacity"].ToString()), UInt64.Parse(queryObj["FreeSpace"].ToString()));
                    continue;
                }
                else if (caption == dataDir)
                {
                    result["ephemeral"] = CalculateDiskUsage(UInt64.Parse(queryObj["Capacity"].ToString()), UInt64.Parse(queryObj["FreeSpace"].ToString()));
                    continue;
                }
                else if (caption == storeDir)
                {
                    result.Add("persistent", CalculateDiskUsage(UInt64.Parse(queryObj["Capacity"].ToString()), UInt64.Parse(queryObj["FreeSpace"].ToString())));
                    continue;
                }

            }

            return result;
        }

        public static int CreatePrimaryPartition(int diskIndex, string label)
        {
            string script = String.Format(@"SELECT Disk {0}
CREATE PARTITION PRIMARY
SELECT PARTITION 1
FORMAT FS=NTFS LABEL={1} QUICK
EXIT", diskIndex, label);

            string fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, script);

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "diskpart.exe";
            info.Arguments = String.Format("/s {0}", fileName);

            Process p = new Process();
            p.Start();
            p.WaitForExit(60000);
            if (!p.HasExited)
            {
                p.Kill();
                return -1;
            }
            else
            {
                return p.ExitCode;
            }
        }

        public static bool DiskHasPartition(int diskIndex)
        {
            using (ManagementObjectSearcher search = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_DiskDrive "))
            {
                foreach (ManagementObject queryObj in search.Get())
                {
                    if (int.Parse(queryObj["Index"].ToString()) == diskIndex)
                    {
                        if (int.Parse(queryObj["Partitions"].ToString()) > 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            throw new Exception("Disk not found");
        }

        public static int MountPartition(int diskIndex, string mountPath)
        {
            string script = String.Format(@"SELECT Disk {0}
SELECT PARTITION 1
ASSIGN MOUNT={0}", mountPath);

            string fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, script);

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "diskpart.exe";
            info.Arguments = String.Format("/s {0}", fileName);

            Process p = new Process();
            p.Start();
            p.WaitForExit(60000);
            if (!p.HasExited)
            {
                p.Kill();
                return -1;
            }
            else
            {
                return p.ExitCode;
            }
        }

        public static bool IsMountPoint(string path)
        {
            char[] trimChars = { '\\' };

            using (ManagementObjectSearcher search = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Volume "))
            {
                foreach (ManagementObject queryObj in search.Get())
                {
                    if (queryObj["Caption"].ToString().TrimEnd(trimChars) == path.TrimEnd(trimChars))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static int CalculateDiskUsage(UInt64 capacity, UInt64 freeSpace)
        {
            return (int)((capacity - freeSpace) / capacity * 100);
        }
    }
}
