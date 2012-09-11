using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using Microsoft.VisualBasic.Devices;
using Newtonsoft.Json;
using System.Globalization;
using System.Collections.ObjectModel;
using Uhuru.BOSH.Agent.Objects;

namespace Uhuru.BOSH.Agent
{
    public class MonitPerformance : IDisposable
    {
        PerformanceCounter cpuCounter = null;
        PerformanceCounter userCpuCounter = null;
        PerformanceCounter ramCounter = null;
        double totalMemory;
        bool disposed = false;

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

        public Vitals GetVitals
        {
            get
            {
                Vitals result = new Vitals();

                //Populate CPU
                result.CPU = new CPUInfo();
                double userCpuCount = this.userCpuCounter.NextValue();
                result.CPU.Sys = (Math.Round(this.cpuCounter.NextValue() - userCpuCount, 2)).ToString(CultureInfo.InvariantCulture);
                result.CPU.User = Math.Round(userCpuCount, 2).ToString(CultureInfo.InvariantCulture);

                //Populate RAM
                result.Memory = new MemoryInfo();
                double availableMemory = this.ramCounter.NextValue();
                double usedPercentage = 100 - (availableMemory * 100 / this.totalMemory);
                result.Memory.KB = Math.Round(this.totalMemory - availableMemory, 2).ToString(CultureInfo.InvariantCulture);
                result.Memory.Percent = Math.Round(usedPercentage, 2).ToString(CultureInfo.InvariantCulture);

                //Populate load
                result.LoadAdd(Math.Round(ComputeAverage(1), 2).ToString(CultureInfo.InvariantCulture));
                result.LoadAdd(Math.Round(ComputeAverage(5), 2).ToString(CultureInfo.InvariantCulture));
                result.LoadAdd(Math.Round(ComputeAverage(15), 2).ToString(CultureInfo.InvariantCulture));

                //Populate disk
                result.Disk = new DiskInfo();
                int disku = GetDiskUsagePercentege("C:\\");
                if (disku != 0)
                {
                    result.Disk.SystemDisk = new SystemDiskInfo();
                    result.Disk.SystemDisk.Percent = disku.ToString(CultureInfo.InvariantCulture);
                }

                return result;
            }
        }

        private static int GetDiskUsagePercentege(string driveName)
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


        private static double ComputeAverage(int minutes)
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    this.cpuCounter.Dispose();
                    this.ramCounter.Dispose();
                    this.userCpuCounter.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MonitPerformance() // the finalizer
        {
            Dispose(false);
        }
    }

    [JsonObject("vitals")]
    public class Vitals
    {
        [JsonProperty("load")]
        public Collection<string> Load { get; private set; }

        [JsonProperty("cpu")]
        public CPUInfo CPU { get { return cpu; } set { cpu = value; } }

        private CPUInfo cpu;

        [JsonProperty("mem")]
        public MemoryInfo Memory { get { return memory; } set { memory = value; } }

        private MemoryInfo memory;

        [JsonProperty("disk")]
        public DiskInfo Disk { get { return disk; } set { disk = value; } }

        private DiskInfo disk;

        public Vitals()
        {
            Load = new Collection<string>();
        }

        public void LoadAdd(string value)
        {
            Load.Add(value);
        }
    }
}

