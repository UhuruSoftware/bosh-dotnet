// -----------------------------------------------------------------------
// <copyright file="Monit.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
using System.Threading;
    using System.IO;
    using System.ServiceProcess;
    using System.Xml.Serialization;
    using Uhuru.Utilities;
    using System.Diagnostics;
    using System.Globalization;
    using System.Management;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Monit", Justification = "FxCop Bug")]
    public class Monit : IDisposable
    {
   //// BUFSIZE = (32 * 1024)
   //// NUM_RETRY_MONIT_INCARNATION = 60
        int poolTime = 6000;
        static string jobDefDirectory = @"c:\vcap\jobs";
        MonitPerformance monitPerformance = null;

        private Dictionary<MonitSpec.Base.Job, FileInfo> specifiedJobs = new Dictionary<MonitSpec.Base.Job, FileInfo>();
        private Dictionary<string, ServiceControllerStatus> serviceControllers = new Dictionary<string, ServiceControllerStatus>();
        private static readonly object locker = new object();

        private static volatile Monit instance;
        private bool enabled = false;
        private bool disposed = false;
        
        private Monit() {

            //MonitorServices();
            monitPerformance = new MonitPerformance();
        }


        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <returns></returns>
        public static Monit GetInstance(string jobDefinitionDirectory)
        {
            if (instance == null)
            {
                lock (locker)
                {
                    if (!string.IsNullOrEmpty(jobDefinitionDirectory))
                    {
                        jobDefDirectory = jobDefinitionDirectory;
                    }
                    if (instance == null)
                    {
                        instance = new Monit();
                             
                    }
                }
            }
            return instance;
        }

        public static Monit GetInstance()
        {
            return GetInstance(string.Empty);
        }

        public void Enable()
        {
            enabled = true;
        }
   
        public void Start()
        {
            Logger.Info("Starting monit");
            enabled = true;
            Utilities.TimerHelper.RecurringLongCall(poolTime, new Utilities.TimerCallback(Run));
        }

        public void Run()
        {
            ServiceController[] allServices = ServiceController.GetServices();
            lock (locker)
            {
                MonitorServices();
                foreach (MonitSpec.Base.Job currentServiceSpec in new List<MonitSpec.Base.Job>(specifiedJobs.Keys))
                {
                    foreach (MonitSpec.Base.JobService service in currentServiceSpec.Service)
                    {
                        foreach (ServiceController serviceController in allServices)
                        {
                            if (serviceController.DisplayName == service.Name)
                            {
                                if (!serviceControllers.ContainsKey(serviceController.DisplayName))
                                {
                                    Logger.Info("Monitoring service :" + serviceController.DisplayName);
                                    serviceControllers.Add(serviceController.DisplayName, serviceController.Status);
                                }
                                else
                                {
                                    if (serviceControllers[serviceController.DisplayName] != serviceController.Status)
                                    {
                                        Logger.Info("Status changed for service :" + service.Name);
                                        //Reise event
                                        serviceControllers[serviceController.DisplayName] = serviceController.Status;
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }
            }


        }

 
        /// <summary>
        /// The system no longer monitors the described services.
        /// </summary>
        public void UnmonitorServices()
        {
            lock (locker)
            {
                Logger.Info("Unmonitoring services");
                specifiedJobs.Clear();
                serviceControllers.Clear();
            }
        }



        /// <summary>
        /// Monitors the all the services.
        /// </summary>
         private void MonitorServices()
        {
            if (!Directory.Exists(jobDefDirectory))
            {
                Logger.Warning("Job directory was not created");
                return;
            }
            specifiedJobs.Clear();
            foreach (string jobDefFile in Directory.GetFiles(jobDefDirectory, "*.*", SearchOption.AllDirectories))
            {

                if (jobDefFile.Trim().EndsWith("monit", StringComparison.OrdinalIgnoreCase))
                {
                    //MonitSpec.Base.Job currentServiceSpec = null;
                    XmlSerializer serializer = new XmlSerializer(typeof(MonitSpec.Base.Job));
                    try
                    {

                        using (FileStream fileStream = File.OpenRead(jobDefFile))
                        {
                            //currentServiceSpec = ;
                            specifiedJobs.Add((MonitSpec.Base.Job)serializer.Deserialize(fileStream), new FileInfo(jobDefFile));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning("File " + jobDefFile + " is not a job definition file :" + ex.ToString());
                    }
                }
            }
        }

         public object RunPrescripts(bool start)
         {
             object result = 0;
             lock (locker)
             {
                 MonitorServices();
                 Logger.Info("Nr of specified jobs found :" + specifiedJobs.Count);
                 foreach (KeyValuePair<MonitSpec.Base.Job, FileInfo> jobInfo in specifiedJobs)
                 {
                     foreach (MonitSpec.Base.JobService jobService in jobInfo.Key.Service)
                     {
                         string script = string.Empty;
                         if (start)
                            script = string.Format(CultureInfo.InvariantCulture, "/c {0}", jobService.PreStart);
                         else
                             script = string.Format(CultureInfo.InvariantCulture, "/c {0}", jobService.PreStop);

                         Logger.Info("Running script :" + script);
                         Process p = Process.Start("cmd.exe", script);
                         p.WaitForExit();
                         if (p.ExitCode != 0)
                         {
                             Logger.Error("Exception while running script " + script);
                             result = p.ExitCode;
                         }
                     }
                 }
             }
             return result;
         }

         /// <summary>
         /// Starts all the services from all the described jobs.
         /// </summary>
        public void StartServices()
         {
             lock (locker)
             {
                 Logger.Info("Starting all the services");
                 Run();
                 ServiceController[] allServices = ServiceController.GetServices();
                 foreach (string serviceName in serviceControllers.Keys)
                 {
                     Logger.Info("Starting service :" + serviceName);
                     ServiceController serviceController = (from entity in allServices
                                                            where entity.DisplayName == serviceName
                                                            select entity).First();
                     Process.Start("sc.exe", String.Format("config {0} start= demand", serviceController.ServiceName)).WaitForExit();
                     try
                     {
                         serviceController.Start();
                     }
                     catch (Exception ex)
                     {
                         Logger.Error("Error while starting " + ex.ToString());
                     }
                 }
             }
         }

        /// <summary>
        /// Stops the all the services from the described jobs.
        /// </summary>
        public void StopServices()
        {
            lock (locker)
            {
                Logger.Info("Stopping services");
                ServiceController[] allServices = ServiceController.GetServices();
                foreach (string serviceName in serviceControllers.Keys)
                {
                    Logger.Info("Stopping service " + serviceName );
                    ServiceController serviceController = (from entity in allServices
                                                           where entity.DisplayName == serviceName
                                                           select entity).First();

                    Process.Start("sc.exe", String.Format("config {0} start= disabled", serviceController.ServiceName)).WaitForExit();
                    try
                    {
                        int timeout = 30;
                        while (serviceController.Status == ServiceControllerStatus.StartPending || serviceController.Status == ServiceControllerStatus.StopPending)
                        {                            
                            if (timeout == 0)
                            {
                                KillService(serviceController.ServiceName);
                                Thread.Sleep(1000);
                                break;
                            }
                            Thread.Sleep(1000);
                            serviceController.Refresh();
                            timeout--;
                        }
                        
                        serviceController.Refresh();
                        if (serviceController.Status != ServiceControllerStatus.Stopped)
                        {
                            serviceController.Stop();
                            serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMinutes(60));
                            serviceController.Refresh();
                            if (serviceController.Status != ServiceControllerStatus.Stopped)
                            {
                                KillService(serviceController.ServiceName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Cannot stop service " + serviceController.DisplayName + " " + ex.ToString());
                    }
                    finally
                    {
                        Process.Start("sc.exe", String.Format("config {0} start= demand", serviceController.ServiceName)).WaitForExit();
                    }
                }
            }
        }

        private static void KillService(string serviceName)
        {
            using (ManagementObject service = new ManagementObject(@"Win32_service.Name='" + serviceName + "'"))
            {
                object o = service.GetPropertyValue("ProcessId");
                int processId = (int)((UInt32)o);
                using (Process process = Process.GetProcessById(processId))
                {
                    process.Kill();
                }
            }
        }

        public Vitals GetVitals
        {
            get
            {
                return monitPerformance.GetVitals;
            }
        }


        public string GetServiceGroupState
        {
            get
            {
                if (!enabled)
                {
                    Logger.Info("Heartbeat is disabled");
                    return "running";
                }

                foreach (KeyValuePair<string, ServiceControllerStatus> service in serviceControllers)
                {
                    if (service.Value == ServiceControllerStatus.StartPending)
                    {
                        return "starting";
                    }
                    else if (service.Value != ServiceControllerStatus.Running)
                    {
                        return "failing";
                    }
                }

                return "running";
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    this.monitPerformance.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Monit() // the finalizer
        {
            Dispose(false);
        }
    }
}
