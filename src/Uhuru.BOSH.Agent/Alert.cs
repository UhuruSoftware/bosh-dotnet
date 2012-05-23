// -----------------------------------------------------------------------
// <copyright file="Alert.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
using Uhuru.BOSH.Agent.Objects;
    using Uhuru.Utilities;
    using System.Threading;
    using System.Globalization;

    /// <summary>
    /// The main area of responsibility for this class is conversion of Monit alert format to BOSH Health Monitor alert format.
    /// </summary>
    public class Alert
    {
        int ALERT_RETRIES = 3;
        int RETRY_PERIOD = 1; //secounds
        int SEVERITY_CUTOFF = 5;
        int DEFAULT_SEVERITY = 2;

    ////# The main area of responsibility for this class is conversion
    ////# of Monit alert format to BOSH Health Monitor alert format.

    ////attr_reader :id, :service, :event, :description, :action, :date, :severity
        
        private AlertAttrib alertAttrib;

        /// <summary>
        /// Gets the alert attribute.
        /// </summary>
        public AlertAttrib AlertAttrib
        {
            get
            {
                return alertAttrib;
            }
        }
    ////def self.register(attrs)
    ////  new(attrs).register
    ////end
        

    ////def initialize(attrs)
    ////  unless attrs.is_a?(Hash)
    ////    raise ArgumentError, "#{self.class} expects an attributes Hash as a parameter"
    ////  end

    ////  @logger   = Config.logger
    ////  @nats     = Config.nats
    ////  @agent_id = Config.agent_id
    ////  @state    = Config.state

    ////  @id          = attrs[:id]
    ////  @service     = attrs[:service]
    ////  @event       = attrs[:event]
    ////  @action      = attrs[:action]
    ////  @date        = attrs[:date]
    ////  @description = attrs[:description]
    ////  @severity    = self.calculate_severity
    ////end
        /// <summary>
        /// Initializes a new instance of the <see cref="Alert"/> class.
        /// </summary>
        /// <param name="attribute">The alert attribute.</param>
        public Alert(AlertAttrib attribute)
        {
            //Config.Logger;
            //Config.Nats;
            //Config.Agent_id;
            //Config.State;
            this.alertAttrib = attribute;
        }
    ////# As we don't (currently) require ACKs for alerts we might need to
    ////# send alerts several times in case HM temporarily goes down
    ////def register
    ////  return if severity >= SEVERITY_CUTOFF || severity <= 0

    ////  ALERT_RETRIES.times do |i|
    ////    EM.add_timer(i * RETRY_PERIOD) do
    ////      send_via_mbus
    ////    end
    ////  end
    ////end
        /// <summary>
        /// Registers this instance of the alert.
        /// </summary>
        public void Register()
        {
            int serverity = CalculateSeverity();

            if (serverity >= SEVERITY_CUTOFF || serverity <= 0)
                return;

            for (int i = 0; i < ALERT_RETRIES; i++)
            {
                Thread.Sleep(i * RETRY_PERIOD);
                SendViaMBus();
            }
        }

    ////def send_via_mbus
    ////  if @state.nil?
    ////    @logger.warn("Unable to send alert: unknown agent state")
    ////    return
    ////  end
        /// <summary>
        /// Sends the via Message bus.
        /// </summary>
        private void SendViaMBus()
        {

            if (Config.State == null)
            {
                Logger.Warning("Unable to send alert: unknown agent state");
                return;
            }
            if (String.IsNullOrEmpty(Config.State.Job))
            {
                Logger.Warning("No job, ignoring alert");
                return;
            }
            
            Config.Nats.Publish(string.Format(CultureInfo.InvariantCulture,"hm.agent.alert.{0}", Config.AgentId), null, ConvertAlertData());

        }

       


    ////  if @state["job"].blank?
    ////    @logger.info("No job, ignoring alert")
    ////    return
    ////  end

    ////  @nats.publish("hm.agent.alert.#{@agent_id}", Yajl::Encoder.encode(converted_alert_data))
    ////end

    ////def converted_alert_data
    ////  # INPUT: id, service, event, action, date, description
    ////  # OUTPUT: id, severity, title, summary, created_at (unix timestamp)
    ////  {
    ////    "id"         => @id,
    ////    "severity"   => self.calculate_severity,
    ////    "title"      => self.title,
    ////    "summary"    => @description,
    ////    "created_at" => self.timestamp
    ////  }
    ////end
        /// <summary>
        /// Converts the alert data. This is used to the message to nats
        /// </summary>
        /// <returns></returns>
        private string ConvertAlertData()
        {
            AlertData alertData = new AlertData();
            alertData.Id = this.alertAttrib.Id;
            alertData.Severity = CalculateSeverity();
            alertData.Title = GetTitle();
            alertData.Summary = this.alertAttrib.Description;
            alertData.CreatedAt = DateTime.Now;
            string convertedData = Newtonsoft.Json.JsonConvert.SerializeObject(alertData);
            return convertedData;
        }

    ////def title
    ////  ips = @state.ips
    ////  service = ips.size > 0 ? "#{@service} (#{ips.sort.join(", ")})" : @service
    ////  "#{service} - #{@event} - #{@action}"
    ////end

         private string GetTitle()
         {
             string title = string.Empty;
             List<string> ips = Config.State.Ips.ToList();
             if (ips.Count > 0)
             {
                 title = alertAttrib.Service + "(";
                 ips.Sort();
                 foreach (string ip in ips)
                 {
                     title = title + ip + ", ";
                 }
                 title = title.TrimEnd(' ', ',');
                 title = title + ")";
             }
             else
             {
                 title = string.Format(CultureInfo.InvariantCulture,"{0} - {1} - {2}", alertAttrib.Service, alertAttrib.Event, alertAttrib.AlertAction);
             }
             return title;

         }

    ////def timestamp
    ////  Time.rfc822(@date).utc.to_i
    ////rescue ArgumentError => e
    ////  @logger.warn("Cannot parse monit alert date `#{@date}', using current time instead")
    ////  Time.now.utc.to_i
    ////end

    ////def calculate_severity
    ////  known_severity = SEVERITY_MAP[@event.to_s.downcase]
    ////  if known_severity.nil?
    ////    @logger.warn("Unknown monit event name `#{@event}', using default severity #{DEFAULT_SEVERITY}")
    ////    DEFAULT_SEVERITY
    ////  else
    ////    known_severity
    ////  end
    ////end
        private int CalculateSeverity()
        {
            int knownServerity = DEFAULT_SEVERITY;

            try
            {
                knownServerity = GetServerity(alertAttrib.Event);
            }
            catch (ArgumentException exception)
            {
                Logger.Warning(exception.Message);
            }

            return knownServerity;
        }


    //    SEVERITY_MAP = {
    //  "action done" => -1,
    //  "checksum failed" => 2,
    //  "checksum changed" => 4,
    //  "checksum succeeded" => -1,
    //  "checksum not changed" => -1,
    //  "connection failed" => 1,
    //  "connection succeeded" => -1,
    //  "connection changed" => 3,
    //  "connection not changed" => -1,
    //  "content failed" => 3,
    //  "content succeeded" => -1,
    //  "content match" => -1,
    //  "content doesn't match" => 3,
    //  "data access error" => 3,
    //  "data access succeeded" => -1,
    //  "data access changed" => 4,
    //  "data access not changed" => -1,
    //  "execution failed" => 1,
    //  "execution succeeded" => -1,
    //  "execution changed" => 4,
    //  "execution not changed" => -1,
    //  "filesystem flags failed" => 3,
    //  "filesystem flags succeeded" => -1,
    //  "filesystem flags changed" => 4,
    //  "filesystem flags not changed" => -1,
    //  "gid failed" => 3,
    //  "gid succeeded" => -1,
    //  "gid changed" => 4,
    //  "gid not changed" => -1,
    //  "heartbeat failed" => 3,
    //  "heartbeat succeeded" => -1,
    //  "heartbeat changed" => 4,
    //  "heartbeat not changed" => -1,
    //  "icmp failed" => 2,
    //  "icmp succeeded" => -1,
    //  "icmp changed" => 4,
    //  "icmp not changed" => -1,
    //  "monit instance failed" => 1,
    //  "monit instance succeeded" => -1,
    //  "monit instance changed" => -1,
    //  "monit instance not changed" => -1,
    //  "invalid type" => 3,
    //  "type succeeded" => -1,
    //  "type changed" => 4,
    //  "type not changed" => -1,
    //  "does not exist" => 1,
    //  "exists" => -1,
    //  "existence changed" => 4,
    //  "existence not changed" => -1,
    //  "permission failed" => 3,
    //  "permission succeeded" => -1,
    //  "permission changed" => 4,
    //  "permission not changed" => -1,
    //  "pid failed" => 2,
    //  "pid succeeded" => -1,
    //  "pid changed" => 4,
    //  "pid not changed" => -1,
    //  "ppid failed" => 2,
    //  "ppid succeeded" => -1,
    //  "ppid changed" => 4,
    //  "ppid not changed" => -1,
    //  "resource limit matched" => 3,
    //  "resource limit succeeded" => -1,
    //  "resource limit changed" => 4,
    //  "resource limit not changed" => -1,
    //  "size failed" => 3,
    //  "size succeeded" => -1,
    //  "size changed" => 3,
    //  "size not changed" => -1,
    //  "timeout" => 2,
    //  "timeout recovery" => -1,
    //  "timeout changed" => 4,
    //  "timeout not changed" => -1,
    //  "timestamp failed" => 3,
    //  "timestamp succeeded" => -1,
    //  "timestamp changed" => 4,
    //  "timestamp not changed" => -1,
    //  "uid failed" => 2,
    //  "uid succeeded" => -1,
    //  "uid changed" => 4,
    //  "uid not changed" => -1
    //}
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private int GetServerity(string eventName) 
        {
            switch (eventName)
            {
                case "action done": return -1;
                case "checksum failed": return 2;
                case "checksum changed": return 4;
                case "checksum succeeded": return -1;
                case "checksum not changed": return -1;
                case "connection failed": return 1;
                case "connection succeeded": return -1;
                case "connection changed": return 3;
                case "connection not changed": return -1;
                case "content failed": return 3;
                case "content succeeded": return -1;
                case "content match": return -1;
                case "content doesn't match": return 3;
                case "data access error": return 3;
                case "data access succeeded": return -1;
                case "data access changed": return 4;
                case "data access not changed": return -1;
                case "execution failed": return 1;
                case "execution succeeded": return -1;
                case "execution changed": return 4;
                case "execution not changed": return -1;
                case "filesystem flags failed": return 3;
                case "filesystem flags succeeded": return -1;
                case "filesystem flags changed": return 4;
                case "filesystem flags not changed": return -1;
                case "gid failed": return 3;
                case "gid succeeded": return -1;
                case "gid changed": return 4;
                case "gid not changed": return -1;
                case "heartbeat failed": return 3;
                case "heartbeat succeeded": return -1;
                case "heartbeat changed": return 4;
                case "heartbeat not changed": return -1;
                case "icmp failed": return 2;
                case "icmp succeeded": return -1;
                case "icmp changed": return 4;
                case "icmp not changed": return -1;
                case "monit instance failed": return 1;
                case "monit instance succeeded": return -1;
                case "monit instance changed": return -1;
                case "monit instance not changed": return -1;
                case "invalid type": return 3;
                case "type succeeded": return -1;
                case "type changed": return 4;
                case "type not changed": return -1;
                case "does not exist": return 1;
                case "exists": return -1;
                case "existence changed": return 4;
                case "existence not changed": return -1;
                case "permission failed": return 3;
                case "permission succeeded": return -1;
                case "permission changed": return 4;
                case "permission not changed": return -1;
                case "pid failed": return 2;
                case "pid succeeded": return -1;
                case "pid changed": return 4;
                case "pid not changed": return -1;
                case "ppid failed": return 2;
                case "ppid succeeded": return -1;
                case "ppid changed": return 4;
                case "ppid not changed": return -1;
                case "resource limit matched": return 3;
                case "resource limit succeeded": return -1;
                case "resource limit changed": return 4;
                case "resource limit not changed": return -1;
                case "size failed": return 3;
                case "size succeeded": return -1;
                case "size changed": return 3;
                case "size not changed": return -1;
                case "timeout": return 2;
                case "timeout recovery": return -1;
                case "timeout changed": return 4;
                case "timeout not changed": return -1;
                case "timestamp failed": return 3;
                case "timestamp succeeded": return -1;
                case "timestamp changed": return 4;
                case "timestamp not changed": return -1;
                case "uid failed": return 2;
                case "uid succeeded": return -1;
                case "uid changed": return 4;
                case "uid not changed": return -1;
                default: throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,"Unknown monit event name {0}, using default severity {1}", eventName, DEFAULT_SEVERITY));
            }
        }
    }
}
