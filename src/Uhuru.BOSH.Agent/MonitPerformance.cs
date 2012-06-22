using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using Microsoft.VisualBasic.Devices;
using Newtonsoft.Json;

namespace Uhuru.BOSH.Agent
{
    public class MonitPerformance
    {
        PerformanceCounter cpuCounter = null;
        PerformanceCounter userCpuCounter = null;
        PerformanceCounter ramCounter = null;
        double totalMemory;

        public MonitPerformance()
        {
            //Initialize user Cpu Counter
            userCpuCounter = new PerformanceCounter();
            userCpuCounter.CategoryName = "Processor";
            userCpuCounter.CounterName = "% User Time";
            userCpuCounter.InstanceName = "_Total";
            //userCpuCounter.NextValue();
            //Initialize cpu Counter
            cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            
            //Initialize ram counter
            ramCounter = new PerformanceCounter();
            ramCounter.CategoryName = "Memory";
            ramCounter.CounterName = "Available kBytes";
            ComputerInfo computerInfo = new ComputerInfo();
            totalMemory = Convert.ToDouble(computerInfo.TotalPhysicalMemory / 1024);

        }

        public Vitals GetVitals()
        {
            Vitals result = new Vitals();

            return result;
            //Populate CPU
            result.CPU = new Vitals.CPUInfo();
            double userCpuCount = userCpuCounter.NextValue();
            result.CPU.Sys = (Math.Round(cpuCounter.NextValue() - userCpuCount, 2)).ToString();
            result.CPU.User = Math.Round(userCpuCount,2).ToString(); 

            //Populate RAM
            result.Mem = new Vitals.MemInfo();
            double availableMemory = ramCounter.NextValue();
            double usedPercentage = 100 - (availableMemory * 100 / totalMemory);
            result.Mem.Kb = Math.Round(totalMemory - availableMemory, 2).ToString();
            result.Mem.Percent = Math.Round(usedPercentage, 2).ToString();

            //Populate load
            result.Load = new List<string>();
            result.Load.Add(Math.Round(ComputeAverage(1), 2).ToString());
            result.Load.Add(Math.Round(ComputeAverage(5), 2).ToString());
            result.Load.Add(Math.Round(ComputeAverage(15), 2).ToString());

            //Populate disk
            result.Disk = new Vitals.DiskInfo();
            int disku = GetDiskUsagePercentege("C:\\");
            if (disku != 0)
            {
                result.Disk.SystemDisk = new Vitals.DiskInfo.SystemDiskInfo();
                result.Disk.SystemDisk.Percent = disku.ToString();
            }

            return result;
        }

        private int GetDiskUsagePercentege(string driveName)
        {
            int result = 0;

            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
            {
                if (drive.Name == driveName)
                {
                    result = 100 - Convert.ToInt32(drive.TotalFreeSpace * 100 / drive.TotalSize);
                    break;
                }
            }


            return result;
        }


        private double ComputeAverage(int minutes)
        {
            Process[] allProcesses = Process.GetProcesses();

            TimeSpan lifeInterval = new TimeSpan(0, minutes, 0);

            double average = 0;

            int i = 0;
            foreach (Process process in allProcesses)
            {
                if (process.SessionId == 0)
                {
                    try
                    {
                        double processMilisecounds = process.TotalProcessorTime.TotalMilliseconds;
                        average = average + processMilisecounds / lifeInterval.TotalMilliseconds;
                        i++;
                    }
                    catch (Win32Exception)
                    {
                    }
                }
            }

            return average / i;
        }

    }

    [JsonObject("vitals")]
    public class Vitals
    {
        [JsonProperty("load")]
        public List<string> Load { get; set; }

        [JsonProperty("cpu")]
        public CPUInfo CPU;

        [JsonProperty("mem")]
        public MemInfo Mem;

        [JsonProperty("disk")]
        public DiskInfo Disk;

        public class CPUInfo
        {
            [JsonProperty("user")]
            public string User;

            [JsonProperty("sys")]
            public string Sys;

            [JsonProperty("wait")]
            public string Wait;
        }

        public class MemInfo
        {
            [JsonProperty("percent")]
            public string Percent;
            [JsonProperty("kb")]
            public string Kb;
        }

        public class DiskInfo
        {
            [JsonProperty("system")]
            public SystemDiskInfo SystemDisk;

            [JsonProperty("ephemeral")]
            public EphemeralDiskInfo EphemeralDisk;

            [JsonProperty("persistent")]
            public PersistantDiskInfo PersistantDisk;

            public class SystemDiskInfo
            {
                [JsonProperty("percent")]
                public string Percent;
            }
            public class EphemeralDiskInfo
            {
                [JsonProperty("percent")]
                public string Percent;
            }
            public class PersistantDiskInfo
            {
                [JsonProperty("percent")]
                public string PersistantPercent;
            }
        }
    }
}

