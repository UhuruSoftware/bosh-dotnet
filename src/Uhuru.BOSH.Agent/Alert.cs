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

        /// <summary>
        /// Registers the specified attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        public static void Register(AlertAttrib attribute)
        {
            Alert alert = new Alert(attribute);
            alert.Register();
        }
  
        /// <summary>
        /// Initializes a new instance of the <see cref="Alert"/> class.
        /// </summary>
        /// <param name="attribute">The alert attribute.</param>
        public Alert(AlertAttrib attribute)
        {
            this.alertAttrib = attribute;
        }
 
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
                Utilities.TimerHelper.DelayedCall(i * RETRY_PERIOD, new Utilities.TimerCallback(SendViaMBus));
            }
        }

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
            if (Config.State.Job == null)
            {
                Logger.Warning("No job, ignoring alert");
                return;
            }
            
            Config.Nats.Publish(string.Format(CultureInfo.InvariantCulture,"hm.agent.alert.{0}", Config.AgentId), null, ConvertAlertData());

        }

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

         private string GetTitle()
         {
             string title = string.Empty;
             List<string> ips = Config.State.GetIps().ToList();
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
