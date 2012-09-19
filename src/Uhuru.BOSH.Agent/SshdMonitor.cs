// -----------------------------------------------------------------------
// <copyright file="SshdMonitor.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Globalization;
    using System.ServiceProcess;
    using Uhuru.Utilities;
    using System.Threading;
    using Uhuru.BOSH.Agent.Errors;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sshd", Justification = "FxCop Bug")]
    public static class SshdMonitor
    {
      ////def ok_to_stop?
      ////  Config.sshd_monitor_enabled && @start_time && (Time.now - @start_time) > @start_delay
      ////end

        private static string serviceName = "KpyM Telnet SSH Server v1.19c";
        static DateTime startTime;
        static TimeSpan startDelay;
        private static object locker = new object();
        private static System.Timers.Timer timer;

        public static bool OkToStop
        {
            get
            {
                return (Config.SshdMonitorEnabled && (DateTime.Now.Subtract(startTime) > startDelay));
            }
        }

      ////def test_service(status)
      ////  success = false
      ////  3.times do |_|
      ////    ssh_status = %x[service ssh status]
      ////    success = $?.exitstatus == 0 && ssh_status =~ /#{status}/
      ////    break if success
      ////    sleep 1
      ////  end
      ////  success
      ////end

        public static bool TestService(string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                throw new ArgumentNullException("status");
            }

            using (ServiceController sc = new ServiceController(serviceName))
            {
                int retryCount = 10;
                while (retryCount > 0)
                {
                    sc.Refresh();
                    if ((status.Equals("stop") && sc.Status == ServiceControllerStatus.Stopped) || status.Equals("running") && sc.Status == ServiceControllerStatus.Running)
                    {
                        return true;
                    }
                    else if ((status.Equals("stop") && sc.Status == ServiceControllerStatus.StopPending) || status.Equals("running") && sc.Status == ServiceControllerStatus.StartPending)
                    {
                        retryCount--;
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }

      ////def start_sshd
      ////  @lock.synchronize do
      ////    %x[service ssh start]
      ////    raise "Failed to start sshd #{ssh_status}" if $?.exitstatus != 0 && !test_service("running")

      ////    @start_time = Time.now
      ////    @logger.info("started sshd #{@start_time}")
      ////  end
      ////end

        public static void StartSshd()
        {
            lock (locker)
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        Logger.Warning("SSHD Service already started");
                        sc.Start();
                    }
                    startTime = DateTime.Now;
                    Logger.Info("Started sshd " + startTime.ToString(CultureInfo.InvariantCulture));
                }
                if (!TestService("running"))
                {
                    throw new BoshException("Failed to start sshd");
                }
            }
        }

      ////def stop_sshd
      ////  @lock.synchronize do
      ////    return if !ok_to_stop?
      ////    # No need to check for logged in users as existing ssh connections are not
      ////    # affected by stopping ssh
      ////    @logger.info("stopping sshd")

      ////    %x[service ssh stop]
      ////    raise "Failed to stop sshd" if $?.exitstatus != 0 && !test_service("stop")
      ////    @start_time = nil
      ////  end
      ////end

        public static void StopSshd()
        {
            lock (locker)
            {
                if (!OkToStop)
                {
                    return;
                }
                Logger.Info("Stopping sshd");
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        Logger.Warning("SSHD Service already stopped");
                        sc.Stop();
                    }
                    startTime = DateTime.MinValue;
                }
                if (!TestService("stop"))
                {
                    throw new BoshException("Failed to stop sshd");
                }
            }
        }

      ////def enable(interval, start_delay)
      ////  @logger = Config.logger
      ////  @lock = Mutex.new
      ////  @start_time = nil
      ////  @start_delay = start_delay

      ////  EventMachine.add_periodic_timer(interval) do
      ////    EventMachine.defer { stop_sshd } if SshdMonitor.ok_to_stop?
      ////  end
      ////end


        public static void Enable(int interval, int delay)
        {
            startTime = DateTime.MinValue;
            startDelay = TimeSpan.FromSeconds(delay);

            timer = new System.Timers.Timer(TimeSpan.FromSeconds(interval).Milliseconds);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
            timer.Enabled = false;
            //timer.Enabled = true; TODO: do we need to enable this?
        }

        static void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (OkToStop)
            {
                StopSshd();
            }
        }
    }
}
