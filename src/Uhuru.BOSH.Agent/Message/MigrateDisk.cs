// -----------------------------------------------------------------------
// <copyright file="MigrateDisk.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Uhuru.Utilities;
    using Uhuru.BOSH.Agent.Errors;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Globalization;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class MigrateDisk : Base, IMessage
    {
        string oldCid;
        string newCid;

        public bool IsLongRunning()
        {
            return true;
        }

        public object Process(dynamic args)
        {
            Migrate(args);
            return new object();
        }

        public void Migrate(dynamic args)
        {
            Logger.Info(String.Format("MigrateDisk:{0}", args));
            oldCid = args[0].ToString();
            newCid = args[1].ToString();
            DiskUtil.UnmountGuard(StorePath);
            MountStoreReadOnly(oldCid);

            if (CheckMountpoints())
            {
                Logger.Info("Copy data from old to new store disk");

                char[] trimChars = { '\\' };

                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = "robocopy";
                info.Arguments = String.Format(CultureInfo.InvariantCulture, "{0} {1} /MIR /R:1 /W:1 /COPYALL", StorePath, StoreMigrationTarget);
                info.RedirectStandardOutput = true;
                info.UseShellExecute = false;

                Process p = new Process();
                p.StartInfo = info;
                p.Start();
                p.WaitForExit();
                Logger.Debug(p.StandardOutput.ReadToEnd());

                if (p.ExitCode != 0)
                {
                    throw new MessageHandlerException(String.Format(CultureInfo.InvariantCulture, "Failed to copy data from old to new store disk"));
                }
            }

            DiskUtil.UnmountGuard(StorePath);
            DiskUtil.UnmountGuard(StoreMigrationTarget);

            MountStore(newCid);
        }

        public bool CheckMountpoints()
        {
            if (DiskUtil.IsMountPoint(StorePath) && DiskUtil.IsMountPoint(StoreMigrationTarget))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void MountStore(string cid, string options = "")
        {
            int diskId = int.Parse(Config.Platform.LookupDiskByCid(cid), CultureInfo.InvariantCulture);

            Logger.Info(String.Format(CultureInfo.InvariantCulture, "Mount Partition {0} {1}", diskId, StorePath));

            int returnCode = DiskUtil.MountPartition(diskId, StorePath);

            if (returnCode != 0)
            {
                throw new MessageHandlerException(String.Format(CultureInfo.InvariantCulture, "Failed mount disk {0} on {1}. Exit code: {2}", diskId, StorePath, returnCode));
            }
        }

        public void MountStoreReadOnly(string cid)
        {
            int diskId = int.Parse(Config.Platform.LookupDiskByCid(cid), CultureInfo.InvariantCulture);
            int diskIndex = DiskUtil.GetDiskIndexForDiskId(diskId);

            Logger.Info("Mounting {0} {1}", cid, StorePath);

            int returnCode = -2;

            string script = String.Format(CultureInfo.InvariantCulture, @"SELECT Disk {0}
ATTRIBUTE DISK SET READONLY
SELECT Disk {0}
ONLINE DISK NOERR
SELECT Disk {0}
SELECT PARTITION 1
REMOVE ALL NOERR
ASSIGN MOUNT={1}
EXIT", diskIndex, StorePath);

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
                    returnCode = -1;
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
                        returnCode = p.ExitCode;
                        break;
                    }
                }
            }

            if (returnCode != 0)
            {
                throw new MessageHandlerException(String.Format(CultureInfo.InvariantCulture, "Failed mount disk {0} on {1}. Exit code: {2}", diskIndex, StorePath, returnCode));
            }
        }
    }
}
