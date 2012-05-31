// -----------------------------------------------------------------------
// <copyright file="DiskUtils.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
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
    public class DiskUtils
    {
        public static void CreatePrimaryPartition(int diskIndex, string label)
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
            info.Arguments = String.Format("/s {0}",fileName);

            Process p = new Process();
            p.Start();
            p.WaitForExit(60000);
            if (!p.HasExited || p.ExitCode != 0)
            {
                throw new Exception(String.Format("Failed to create partition on disk {0}", diskIndex));
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

        public static void MountPartition(int diskIndex, string mountPath)
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
            if (!p.HasExited || p.ExitCode != 0)
            {
                throw new Exception(String.Format("Failed to mount partition on disk {0} to path {1}", diskIndex, mountPath));
            }
        }
    }
}
