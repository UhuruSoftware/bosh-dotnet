// -----------------------------------------------------------------------
// <copyright file="Config.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
using YamlDotNet.RepresentationModel;
using Uhuru.Utilities;
using System.IO;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Config
    {
        const string DEFAULT_BASE_DIR = "/var/vcap";
        const int DEFAULT_SSHD_MONITOR_INTERVAL = 30;
        const int DEFAULT_SSHD_START_DELAY = 30;

        private Infrastructure infrastructure = null;
        private Platform platform = null;

        public string BaseDir
        {
            get;
            set;
        }

        public string MessageBus
        {
            get;
            set;
        }

        public string AgentId
        {
            get;
            set;
        }

        public string Configure
        {
            get;
            set;
        }

        public string Blobstore
        {
            get;
            set;
        }

        public string BlobstoreProvider
        {
            get;
            set;
        }

        public string BlobstoreOptions
        {
            get;
            set;
        }

        public string SystemRoot
        {
            get;
            set;
        }

        public string InfrastructureName
        {
            get;
            set;
        }

        public string PlatformName
        {
            get;
            set;
        }

        public string Nats
        {
            get;
            set;
        }

        public string ProcessAlerts
        {
            get;
            set;
        }

        public string SmtpPort
        {
            get;
            set;
        }

        public string SmtpUser
        {
            get;
            set;
        }

        public string SmtpPassword
        {
            get;
            set;
        }

        public string HeartbeatInterval
        {
            get;
            set;
        }

        public string SettingsFile
        {
            get;
            set;
        }

        public Dictionary<string, string> Settings
        {
            get;
            set;
        }

        public State State
        {
            get;
            set;
        }

        public int SshdMonitorInterval
        {
            get;
            set;
        }

        public int SshdStartDelay
        {
            get;
            set;
        }

        public string SshdMonitorEnabled
        {
            get;
            set;
        }

        public string Credentials
        {
            get;
            set;
        }


        public void Clear()
        {
            this.BaseDir = null;
            this.MessageBus = null;
            this.AgentId = null;
            this.Configure = null;
            this.Blobstore = null;
            this.BlobstoreProvider = null;
            this.BlobstoreOptions = null;
            this.SystemRoot = null;
            this.InfrastructureName = null;
            this.PlatformName = null;
            this.Nats = null;
            this.ProcessAlerts = null;
            this.SmtpPort = null;
            this.SmtpUser = null;
            this.SmtpPassword = null;
            this.HeartbeatInterval = null;
            this.SettingsFile = null;
            this.Settings = null;
            this.State = null;
            this.SshdMonitorInterval = 0;
            this.SshdStartDelay = 0;
            this.SshdMonitorEnabled = null;
            this.Credentials = null;
        }


        public void setup(YamlStream config)
        {
            YamlMappingNode root = (YamlMappingNode)config.Documents[0].RootNode;
            
            this.Configure = root.GetString("configure");

            this.BaseDir = root.GetString("base_dir") == null ? DEFAULT_BASE_DIR : root.GetChild("base_dir").ToString();
            this.AgentId = root.GetString("agent_id");

          this.MessageBus = root.GetString("mbus");

          this.BlobstoreOptions  = root.GetString("blobstore_options");
          this.BlobstoreProvider = root.GetString("blobstore_provider");

          this.InfrastructureName = root.GetString("infrastructure_name");
          this.PlatformName = root.GetString("platform_name");

          this.SystemRoot = root.GetString("root_dir") ?? "/";

          this.ProcessAlerts = root.GetString("process_alerts");
          this.SmtpPort      = root.GetString("smtp_port");
          this.SmtpUser      = "vcap";
          this.SmtpPassword  = RandomPassword(8);

          this.HeartbeatInterval = root.GetString("heartbeat_interval");

          this.SshdMonitorInterval = root.GetInt("sshd_monitor_interval") ?? DEFAULT_SSHD_MONITOR_INTERVAL;
          this.SshdStartDelay = root.GetInt("sshd_start_delay") ?? DEFAULT_SSHD_START_DELAY;
          this.SshdMonitorEnabled = root.GetString("sshd_monitor_enabled");

            if (!string.IsNullOrEmpty(this.Configure))
            {
                Logger.Info(string.Format("Configuring Agent with: {0}", root.ToString()));
            }

          this.SettingsFile = Path.Combine(this.BaseDir, "bosh", "settings.json");

          this.Credentials = root.GetString("credentials");

          this.Settings = new Dictionary<string,string>();

          this.State = new State(Path.Combine(this.BaseDir, "bosh", "state.yml"));
        }

        public Infrastructure Infrastructure
        {
            get
            {
                if (this.infrastructure == null)
                {
                    this.infrastructure = new Infrastructure(this.InfrastructureName).ProperInfrastructure;
                }
                return this.infrastructure;
            }
        }

        public Platform Platform
        {
            get
            {
                if (platform == null)
                {
                    platform = new Platform(this.PlatformName).ProperPlatform;
                }
                return platform;
            }
        }

        public string RandomPassword(int length)
        {
            return Uhuru.Utilities.Credentials.GenerateCredential(length);
        }

        public string DefaultIp
        {
            get
            {
                string ip = null;
                foreach (Network network in this.State.Networks)
                {
                    if (ip == null)
                    {
                        ip = network.Ip;
                    }

                    if (network.Name == "default")
                    {
                        ip = network.Ip;
                    }
                }
                return ip;
            }
        }
    }
}
