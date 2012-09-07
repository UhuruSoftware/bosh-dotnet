// -----------------------------------------------------------------------
// <copyright file="DiskUtil.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Management;
    using System.Runtime.InteropServices;
    using Uhuru.Utilities;
    using System.Threading;
    using Uhuru.BOSH.Agent.Errors;
    using System.Globalization;

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

        /// <summary>
        /// Mounts the entry.
        /// </summary>
        /// <param name="diskId">The disk Id.</param>
        /// <returns></returns>
        public static string MountEntry(int diskId)
        {
            Logger.Info("Checkin mount entry");

            int diskIndex = GetDiskIndexForDiskId(diskId);

            string script = String.Format(CultureInfo.InvariantCulture, @"SELECT DISK {0}
SELECT PARTITION 1
DETAIL PARTITION
EXIT", diskIndex);
            string fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, script);

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "diskpart.exe";
            info.Arguments = String.Format(CultureInfo.InvariantCulture, "/s {0}", fileName);
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;

            Process p = new Process();
            p.StartInfo = info;
            p.Start();
            p.WaitForExit(60000);
            if (!p.HasExited)
            {
                p.Kill();
                return null;
            }
            else
            {
                string output = p.StandardOutput.ReadToEnd(); 
                Logger.Warning(output);
                if (output.Contains(@"C:\vcap\store\"))
                    //TODO imeplemnt block and mount point
                    return @"C:\vcap\store\";
                else
                    return null;
            }
            
        }

        static int GUARD_RETRIES = 600;
        static int GUARD_SLEEP = 1000;

        /// <summary>
        /// Unmounts the guard.
        /// </summary>
        /// <param name="mountpoint">The mountpoint.</param>
        public static void UnmountGuard(string mountPoint)
        {
            int unmountAttempts = GUARD_RETRIES;

            int diskIndex = GetDiskIndexForMountPoint(mountPoint);

            string script = String.Format(CultureInfo.InvariantCulture, @"SELECT DISK {0}
SELECT PARTITION 1
REMOVE ALL
SELECT DISK {0}
OFFLINE DISK NOERR
EXIT", diskIndex);
            string fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, script);


            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "diskpart.exe";
            info.Arguments = String.Format(CultureInfo.InvariantCulture, "/s {0}", fileName);
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;

            while (unmountAttempts > 0)
            {
                Process p = new Process();
                p.StartInfo = info;
                p.Start();
                p.WaitForExit(60000);
                if (!p.HasExited)
                {
                    p.Kill();
                    Logger.Debug(p.StandardOutput.ReadToEnd());
                    Logger.Error("Failed to umount {0}", mountPoint);
                    throw new MessageHandlerException(String.Format(CultureInfo.InvariantCulture, "Failed to umount {0}", mountPoint));
                }
                else
                {
                    if (p.ExitCode != 0)
                    {
                        unmountAttempts--;
                        Thread.Sleep(GUARD_SLEEP);
                        continue;
                    }
                    else
                    {
                        Logger.Debug(p.StandardOutput.ReadToEnd());
                        return;
                    }
                }
            }

            Logger.Error("Failed to umount {0}", mountPoint);
            throw new MessageHandlerException(String.Format(CultureInfo.InvariantCulture, "Failed to umount {0}", mountPoint));

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

        /// <summary>
        /// Ensures the no partition.
        /// </summary>
        /// <param name="disk">The disk.</param>
        /// <param name="partition">The partition.</param>
        /// <returns></returns>
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


        /// <summary>
        /// Lookups the partition.
        /// </summary>
        /// <param name="disk">The disk.</param>
        /// <param name="partition">The partition.</param>
        /// <returns></returns>
        public static string LookupPartition(string disk, string partition)
        {
            throw new NotImplementedException();
            //// `sfdisk -Llq #{disk}`.lines.select { |l| l.match(%q{/\A#{partition}.*83.*Linux}) }
        }

        /// <summary>
        /// Gets the usage.
        /// </summary>
        /// <returns></returns>
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
                    result["system"] = CalculateDiskUsage(UInt64.Parse(queryObj["Capacity"].ToString(), CultureInfo.InvariantCulture), UInt64.Parse(queryObj["FreeSpace"].ToString(), CultureInfo.InvariantCulture));
                    continue;
                }
                else if (caption == dataDir)
                {
                    result["ephemeral"] = CalculateDiskUsage(UInt64.Parse(queryObj["Capacity"].ToString(), CultureInfo.InvariantCulture), UInt64.Parse(queryObj["FreeSpace"].ToString(), CultureInfo.InvariantCulture));
                    continue;
                }
                else if (caption == storeDir)
                {
                    result.Add("persistent", CalculateDiskUsage(UInt64.Parse(queryObj["Capacity"].ToString(), CultureInfo.InvariantCulture), UInt64.Parse(queryObj["FreeSpace"].ToString(), CultureInfo.InvariantCulture)));
                    continue;
                }

            }

            return result;
        }

        /// <summary>
        /// Creates the primary partition.
        /// </summary>
        /// <param name="diskId">Id of the disk.</param>
        /// <param name="label">The label.</param>
        /// <returns></returns>
        public static int CreatePrimaryPartition(int diskId, string label)
        {
            int diskIndex = GetDiskIndexForDiskId(diskId);

            string script = String.Format(CultureInfo.InvariantCulture, @"SELECT Disk {0}
ATTRIBUTE DISK CLEAR READONLY
SELECT Disk {0}
ONLINE DISK NOERR
SELECT Disk {0}
CREATE PARTITION PRIMARY
SELECT PARTITION 1
ONLINE VOLUME NOERR
FORMAT FS=NTFS LABEL={1} QUICK
EXIT", diskIndex, label);

            string fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, script);

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "diskpart.exe";
            info.Arguments = String.Format(CultureInfo.InvariantCulture, "/s {0}", fileName);
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;

            Process p = new Process();
            p.StartInfo = info;
            p.Start();
            p.WaitForExit(60000);
            if (!p.HasExited)
            {
                p.Kill();
                return -1;
            }
            else
            {
                Logger.Warning(p.StandardOutput.ReadToEnd());
                return p.ExitCode;
            }
        }

        /// <summary>
        /// Disks the has partition.
        /// </summary>
        /// <param name="diskId">Id of the disk.</param>
        /// <returns></returns>
        public static bool DiskHasPartition(int diskId)
        {
            int diskIndex = GetDiskIndexForDiskId(diskId);

            using (ManagementObjectSearcher search = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_DiskDrive "))
            {
                foreach (ManagementObject queryObj in search.Get())
                {
                    if (int.Parse(queryObj["Index"].ToString(), CultureInfo.InvariantCulture) == diskIndex)
                    {
                        if (int.Parse(queryObj["Partitions"].ToString(), CultureInfo.InvariantCulture) > 0)
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
            throw new MessageHandlerException("Disk not found " + diskIndex.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Mounts the partition.
        /// </summary>
        /// <param name="diskId">Id of the disk.</param>
        /// <param name="mountPath">The mount path.</param>
        /// <returns></returns>
        public static int MountPartition(int diskId, string mountPath)
        {
            int diskIndex = GetDiskIndexForDiskId(diskId);

            string script = String.Format(CultureInfo.InvariantCulture, @"SELECT Disk {0}
ATTRIBUTE DISK CLEAR READONLY
SELECT Disk {0}
ONLINE DISK NOERR
SELECT Disk {0}
SELECT PARTITION 1
REMOVE ALL NOERR
ASSIGN MOUNT={1}
EXIT", diskIndex, mountPath);

            string fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, script);

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "diskpart.exe";
            info.Arguments = String.Format(CultureInfo.InvariantCulture, "/s {0}", fileName);
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;

            int retryCount = 10;
            while (retryCount > 0)
            {
                Process p = new Process();
                p.StartInfo = info;
                p.Start();
                p.WaitForExit(60000);
                if (!p.HasExited)
                {
                    p.Kill();
                    return -1;
                }
                else
                {
                    if (p.ExitCode != 0)
                    {
                        retryCount--;
                        Thread.Sleep(1000);
                        continue;
                    }
                    else
                    {
                        Logger.Warning(p.StandardOutput.ReadToEnd());
                        return p.ExitCode;
                    }
                }
            }

            return -2;
        }

        /// <summary>
        /// Determines whether [is mount point] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        ///   <c>true</c> if [is mount point] [the specified path]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMountPoint(string path)
        {
            char[] trimChars = { '\\' };
            int retryCount = 10;

            while (retryCount > 0)
            {
                using (ManagementObjectSearcher search = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Volume "))
                {
                    foreach (ManagementObject queryObj in search.Get())
                    {
                        if (queryObj["Caption"].ToString().TrimEnd(trimChars).Equals(path.TrimEnd(trimChars), StringComparison.InvariantCultureIgnoreCase))
                        {
                            Logger.Debug("{0} is mount point.", path);
                            return true;
                        }
                    }
                }
                retryCount--;
                Thread.Sleep(1000);
            }
            Logger.Debug("{0} is not mount point.", path);
            return false;
        }

        private static int CalculateDiskUsage(UInt64 capacity, UInt64 freeSpace)
        {
            return (int)((capacity - freeSpace) / capacity * 100);
        }

        /// <summary>
        /// Gets the disk id for mount point.
        /// </summary>
        /// <param name="mountPoint">The mount point.</param>
        /// <returns></returns>
        public static int GetDiskIndexForMountPoint(string mountPoint)
        {
            string volumeId = GetVolumeDeviceId(mountPoint).TrimEnd(new char[] { '\\' });

            Logger.Debug("Mount point {0} volume ID: {1}", mountPoint, volumeId);

            IntPtr file = NativeMethods.CreateFile(volumeId, NativeMethods.GENERIC_READ, NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE, IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);

            int size = 0x400;
            IntPtr buffer = Marshal.AllocHGlobal(size);
            int bytesReturned = 0;

            try
            {
                NativeMethods.DeviceIoControl(file, NativeMethods.IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS, IntPtr.Zero, 0, buffer, size, out bytesReturned, IntPtr.Zero);
            }
            finally
            {
                NativeMethods.CloseHandle(file);
            }

            int diskIndex = -1;

            if (bytesReturned > 0)
            {
                int numberOfDiskExtents = (int)Marshal.PtrToStructure(buffer, typeof(int));
                for (int i = 0; i < numberOfDiskExtents; i++)
                {
                    IntPtr extentPtr = new IntPtr(buffer.ToInt32() + Marshal.SizeOf(typeof(long)) + i * Marshal.SizeOf(typeof(NativeMethods.DISK_EXTENT)));
                    NativeMethods.DISK_EXTENT extent = (NativeMethods.DISK_EXTENT)Marshal.PtrToStructure(extentPtr, typeof(NativeMethods.DISK_EXTENT));

                    diskIndex = extent.DiskNumber;
                }
            }

            if (diskIndex == -1)
            {
                throw new MessageHandlerException("Could not get disk id.");
            }
            else
            {
                Logger.Debug("Found disk index {0} for mount point {1}", diskIndex, mountPoint);
                return diskIndex;
            }

        }

        /// <summary>
        /// Gets the volume device id.
        /// </summary>
        /// <param name="mountPoint">The mount point.</param>
        /// <returns></returns>
        public static string GetVolumeDeviceId(string mountPoint)
        {
            char[] trimChars = { '\\' };

            int retryCount = 10;

            while (retryCount > 0)
            {
                using (ManagementClass volume = new ManagementClass("Win32_Volume"))
                {
                    ManagementObjectCollection moc = volume.GetInstances();
                    foreach (ManagementObject mo in moc)
                    {
                        if (mo["Caption"].ToString().TrimEnd(trimChars).Equals(mountPoint.TrimEnd(trimChars), StringComparison.InvariantCultureIgnoreCase))
                        {
                            Logger.Debug("Found VolumeId {0} for mount point {1}", mo["DeviceID"].ToString(), mountPoint);
                            return mo["DeviceID"].ToString();
                        }
                    }
                }
                retryCount--;
                Thread.Sleep(1000);
            }
            return string.Empty;
        }

        public static int GetDiskIndexForDiskId(int diskId)
        {
            int retryCount = 10;

            while (retryCount > 0)
            {
                using (ManagementClass volume = new ManagementClass("Win32_DiskDrive"))
                {
                    ManagementObjectCollection moc = volume.GetInstances();
                    foreach (ManagementObject mo in moc)
                    {
                        if (mo["SCSITargetId"].ToString().Equals(diskId.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase))
                        {
                            return int.Parse(mo["Index"].ToString(), CultureInfo.InvariantCulture);
                        }
                    }
                }
                retryCount--;
                Thread.Sleep(1000);
            }
            Logger.Debug("Could not find disk index for disk id: " + diskId);
            return -int.MaxValue;
        }
    }
}
