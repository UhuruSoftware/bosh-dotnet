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

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Monit", Justification = "FxCop Bug")]
    public class Monit
    {
   //// BUFSIZE = (32 * 1024)
   //// NUM_RETRY_MONIT_INCARNATION = 60
        int poolTime = 6000;
        static string jobDefDirectory = @"c:\vcap\jobs";
        MonitPerformance monitPerformance = null;

        private List<MonitSpec.Base.Job> specifiedJobs = new List<MonitSpec.Base.Job>();
        private Dictionary<string, ServiceControllerStatus> serviceControllers = new Dictionary<string, ServiceControllerStatus>();
        private static readonly object locker = new object();

        private static volatile Monit instance;
        private bool enabled = false;
        
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
                    if (jobDefinitionDirectory != string.Empty)
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
   //// class << self
   ////   attr_accessor :enabled

   ////   # enable supposed to be called in the very beginning as it creates
   ////   # sync primitives. Ideally this class should be refactored to minimize
   ////   # the number of singleton methods having to keep track of the state.
   ////   def enable
   ////     @enabled     = true
   ////   end
        public void Enable()
        {
            enabled = true;
        }
   ////   def start
   ////     new.run
   ////   end
        public void Start()
        {
            Logger.Info("Starting monit");
            Utilities.TimerHelper.RecurringLongCall(poolTime, new Utilities.TimerCallback(Run));
            //Utilities.TimerHelper.RecurringCall
        }

        public void Run()
        {
            ServiceController[] allServices = ServiceController.GetServices();
            lock (locker)
            {
                MonitorServices();
                foreach (MonitSpec.Base.Job currentServiceSpec in specifiedJobs)
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
   ////   def base_dir
   ////     Bosh::Agent::Config.base_dir
   ////   end

   ////   def logger
   ////     Bosh::Agent::Config.logger
   ////   end

   ////   def monit_dir
   ////     File.join(base_dir, 'monit')
   ////   end

   ////   def monit_events_dir
   ////     File.join(monit_dir, 'events')
   ////   end

   ////   def monit_user_file
   ////     File.join(monit_dir, 'monit.user')
   ////   end

   ////   def monit_alerts_file
   ////     File.join(monit_dir, 'alerts.monitrc')
   ////   end

   ////   def smtp_port
   ////     Bosh::Agent::Config.smtp_port
   ////   end

   ////   def monit_credentials
   ////     entry = File.read(monit_user_file).lines.find { |line| line.match(/\A#{BOSH_APP_GROUP}/) }
   ////     user, cred = entry.split(/:/)
   ////     [user, cred.strip]
   ////   end

   ////   def monit_api_client
   ////     # Primarily for CI - normally done during configure
   ////     unless Bosh::Agent::Config.configure
   ////       setup_monit_user
   ////     end

   ////     user, cred = monit_credentials
   ////     MonitApi::Client.new("http://#{user}:#{cred}@localhost:2822", :logger => logger)
   ////   end

   ////   def random_credential
   ////     OpenSSL::Random.random_bytes(8).unpack("H*")[0]
   ////   end

   ////   def setup_monit_dir
   ////     FileUtils.mkdir_p(monit_dir)
   ////     FileUtils.chmod(0700, monit_dir)
   ////   end

   ////   def setup_monit_user
   ////     unless File.exist?(monit_user_file)
   ////       setup_monit_dir
   ////       File.open(monit_user_file, 'w') do |f|
   ////         f.puts("vcap:#{random_credential}")
   ////       end
   ////     end
   ////   end

   ////   # This and other methods could probably be refactored into a separate management class to avoid keeping
   ////   # all this state in a metaclass (as it's weird to test)
   ////   def setup_alerts
   ////     return unless Config.process_alerts

   ////     alerts_config = <<-CONFIG
   ////     set alert bosh@localhost
   ////     set mailserver 127.0.0.1 port #{Config.smtp_port}
   ////         username "#{Config.smtp_user}" password "#{Config.smtp_password}"

   ////     set eventqueue
   ////         basedir #{monit_events_dir}
   ////         slots 5000

   ////     set mail-format {
   ////       from: monit@localhost
   ////       subject: Monit Alert
   ////       message: Service: $SERVICE
   ////       Event: $EVENT
   ////       Action: $ACTION
   ////       Date: $DATE
   ////       Description: $DESCRIPTION
   ////     }
   ////     CONFIG

   ////     setup_monit_dir
   ////     FileUtils.mkdir_p(monit_events_dir)

   ////     File.open(monit_alerts_file, 'w') do |f|
   ////       f.puts(alerts_config)
   ////     end
   ////   end

   ////   def monit_bin
   ////     File.join(base_dir, 'bosh', 'bin', 'monit')
   ////   end

   ////   def monitrc
   ////     File.join(base_dir, 'bosh', 'etc', 'monitrc')
   ////   end

   ////   def reload
   ////     old_incarnation = incarnation
   ////     logger.info("Monit: old incarnation #{old_incarnation}")
   ////     `#{monit_bin} reload`
   ////     logger.info("Monit: reload")

   ////     check_incarnation = incarnation
   ////     if !old_incarnation
   ////       logger.info("Monit is breaking because of old!")
   ////     end

   ////     if !check_incarnation
   ////       logger.info("Monit is breaking because of check!")
   ////     end

   ////     if old_incarnation < check_incarnation
   ////       logger.info("Monit: updated incarnation #{check_incarnation}")
   ////     end
   ////   end

   ////   def unmonitor_services(attempts=10)
   ////     retry_monit_request(attempts) do |client|
   ////       client.unmonitor(:group => BOSH_APP_GROUP)
   ////     end
   ////   end
        /// <summary>
        /// The system no longer monitors the described services.
        /// </summary>
        public void UnmonitorServices()
        {
            lock (locker)
            {
                specifiedJobs.Clear();
                serviceControllers.Clear();
            }
        }


   ////   def monitor_services(attempts=10)
   ////     retry_monit_request(attempts) do |client|
   ////       client.monitor(:group => BOSH_APP_GROUP)
   ////     end
   ////   end
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
            foreach (string jobDefFile in Directory.GetFiles(jobDefDirectory, "*.xml", SearchOption.AllDirectories))
            {

                if (jobDefFile.EndsWith(".xml"))
                {
                    //MonitSpec.Base.Job currentServiceSpec = null;
                    XmlSerializer serializer = new XmlSerializer(typeof(MonitSpec.Base.Job));

                    using (FileStream fileStream = File.OpenRead(jobDefFile))
                    {
                        //currentServiceSpec = ;
                        specifiedJobs.Add((MonitSpec.Base.Job)serializer.Deserialize(fileStream));
                    }
                }
            }
        }
             

   ////   def start_services(attempts=20)
   ////     retry_monit_request(attempts) do |client|
   ////       client.start(:group => BOSH_APP_GROUP)
   ////     end
   ////   end
         /// <summary>
         /// Starts all the services from all the described jobs.
         /// </summary>
        public void StartServices()
         {
             lock (locker)
             {
                 
                 Logger.Info("Starting all the services");
                 ServiceController[] allServices = ServiceController.GetServices();
                 foreach (string serviceName in serviceControllers.Keys)
                 {
                     Logger.Info("Starting service :" + serviceName);
                     ServiceController serviceController = (from entity in allServices
                                                            where entity.DisplayName == serviceName
                                                            select entity).First();
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

   ////   def stop_services(attempts=20)
   ////     retry_monit_request(attempts) do |client|
   ////       client.stop(:group => BOSH_APP_GROUP)
   ////     end
   ////   end
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
                    try
                    {
                        serviceController.Stop();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Cannot stop service " + serviceController.DisplayName + " " + ex.ToString());
                    }
                }
            }
        }

   ////   def retry_monit_request(attempts=10)
   ////     # HACK: Monit becomes unresponsive after reload
   ////     begin
   ////       yield monit_api_client if block_given?
   ////     rescue Errno::ECONNREFUSED, TimeoutError
   ////       sleep 1
   ////       logger.info("Monit Service Connection Refused: retrying")
   ////       retry if (attempts -= 1) > 0
   ////     rescue => e
   ////       messages = [
   ////         "Connection reset by peer",
   ////         "Service Unavailable"
   ////       ]
   ////       if messages.include?(e.message)
   ////         logger.info("Monit Service Unavailable (#{e.message}): retrying")
   ////         sleep 1
   ////         retry if (attempts -= 1) > 0
   ////       end
   ////       raise e
   ////     end
   ////   end

   ////   def incarnation
   ////     NUM_RETRY_MONIT_INCARNATION.times do
   ////       info = monit_info
   ////       if info && info[:incarnation]
   ////         return info[:incarnation].to_i
   ////       end
   ////       sleep 1
   ////     end

   ////     # If we ever get here we have failed to get incarnation
   ////     raise StateError, "Failed to get incarnation from Monit"
   ////   end

   ////   def monit_info
   ////     retry_monit_request { |client| client.monit_info }
   ////   end

   ////   def get_status(num_retries=10)
   ////     return {} unless @enabled
   ////     retry_monit_request(num_retries) do |client|
   ////       client.status(:group => BOSH_APP_GROUP)
   ////     end
   ////   end
       

   ////   def get_system_status(num_retries=10)
   ////     return {} unless @enabled
   ////     retry_monit_request(num_retries) do |client|
   ////       system_status = client.status(:type => :system)
   ////       return {} unless system_status.is_a?(Hash)
   ////       system_status.values.first
   ////     end
   ////   end
       

   ////   def get_vitals(num_retries=10)
   ////     return {} unless @enabled
   ////     status = get_system_status(num_retries)
   ////     return {} unless status.is_a?(Hash)

   ////     raw_data = status[:raw] || {}
   ////     sys_data = raw_data["system"] || {}
   ////     loadavg = sys_data["load"] || {}
   ////     cpu = sys_data["cpu"] || {}
   ////     mem = sys_data["memory"] || {}
   ////     swap = sys_data["swap"] || {}

   ////     {
   ////       "load" => [ loadavg["avg01"], loadavg["avg05"], loadavg["avg15"] ],
   ////       "cpu" => { "user" => cpu["user"], "sys" => cpu["system"], "wait" => cpu["wait"] },
   ////       "mem" => { "percent" => mem["percent"], "kb" => mem["kilobyte"] },
   ////       "swap" => { "percent" => swap["percent"], "kb" => swap["kilobyte"] }
   ////     }
   ////   end
        public Vitals GetVitals()
        {
            return monitPerformance.GetVitals();
        }

   ////   def service_group_state(num_retries=10)
   ////     # FIXME: state should be unknown if monit is disabled
   ////     # However right now that would break director interaction
   ////     # (at least in integration tests)
   ////     return "running" unless @enabled
   ////     status = get_status(num_retries)

   ////     not_running = status.reject do |name, data|
   ////       # break early if any service is initializing
   ////       return "starting" if data[:monitor] == :init
   ////       # at least with monit_api a stopped services is still running
   ////       (data[:monitor] == :yes && data[:status][:message] == "running")
   ////     end

   ////     not_running.empty? ? "running" : "failing"
   ////   rescue => e
   ////     logger.info("Unable to determine job state: #{e}")
   ////     "unknown"
   ////   end
        public string GetServiceGourpState()
        {
            if (!enabled) return "running";

            foreach (KeyValuePair<string, ServiceControllerStatus> service in serviceControllers )
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

   //// end

   //// def initialize
   ////   @logger = Bosh::Agent::Config.logger
   //// end

   //// def run
   ////   Thread.new { exec_monit }
   //// end

   //// def exec_monit
   ////   status = nil

   ////   pid, stdin, stdout, stderr = POSIX::Spawn.popen4(Monit.monit_bin, '-I', '-c', Monit.monitrc)
   ////   stdin.close

   ////   at_exit {
   ////     Process.kill('TERM', pid) rescue nil
   ////     Process.waitpid(pid)      rescue nil
   ////   }

   ////   log_monit_output(stdout, stderr)

   ////   status = Process.waitpid(pid) rescue nil
   //// rescue => e
   ////   # TODO: send alert to HM
   ////   @logger.error("Failed to run Monit: #{e.inspect} #{e.backtrace}")

   ////   [stdin, stdout, stderr].each { |fd| fd.close rescue nil }

   ////   if status.nil?
   ////     Process.kill('TERM', pid) rescue nil
   ////     Process.waitpid(pid)      rescue nil
   ////   end

   ////   raise
   //// ensure
   ////   [stdin, stdout, stderr].each { |fd| fd.close rescue nil }
   //// end

   //// def log_monit_output(stdout, stderr)
   ////   timeout = nil
   ////   out, err = '', ''
   ////   readers = [stdout, stderr]
   ////   writers = []

   ////   while readers.any?
   ////     ready = IO.select(readers, writers, readers + writers, timeout)
   ////     ready[0].each do |fd|
   ////       buf = (fd == stdout) ? out : err
   ////       begin
   ////         buf << fd.readpartial(BUFSIZE)
   ////       rescue Errno::EAGAIN, Errno::EINTR
   ////       rescue EOFError
   ////         readers.delete(fd)
   ////         fd.close
   ////       end
   ////       buf.gsub!(/\n\Z/,'')
   ////       @logger.info("Monit: #{buf}")
   ////     end
   ////     out, err = '', ''
   ////   end

   //// end
    }
}
