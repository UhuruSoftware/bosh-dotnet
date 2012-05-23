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
    using Uhuru.NatsClient;
    using Uhuru.BOSH.Agent.Providers;
    using Uhuru.BOSH.Agent.Errors;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class Config
    {
        const string DEFAULT_BASE_DIR = "/var/vcap";
        const int DEFAULT_SSHD_MONITOR_INTERVAL = 30;
        const int DEFAULT_SSHD_START_DELAY = 30;

        private static Platform platform = null;

        public static string BaseDir
        {
            get;
            set;
        }

        public static string MessageBus
        {
            get;
            set;
        }

        public static string AgentId
        {
            get;
            set;
        }

        public static string Configure
        {
            get;
            set;
        }

        public static string Blobstore
        {
            get;
            set;
        }

        public static string BlobstoreProvider
        {
            get;
            set;
        }

        public static string BlobstoreOptions
        {
            get;
            set;
        }

        public static string SystemRoot
        {
            get;
            set;
        }

        public static string InfrastructureName
        {
            get;
            set;
        }

        public static string PlatformName
        {
            get;
            set;
        }

        public static Reactor Nats
        {
            get;
            set;
        }

        public static string ProcessAlerts
        {
            get;
            set;
        }

        public static string SmtpPort
        {
            get;
            set;
        }

        public static string SmtpUser
        {
            get;
            set;
        }

        public static string SmtpPassword
        {
            get;
            set;
        }

        public static string HeartbeatInterval
        {
            get;
            set;
        }

        public static string SettingsFile
        {
            get;
            set;
        }

        public static Dictionary<string, string> Settings
        {
            get;
            set;
        }

        public static State State
        {
            get;
            set;
        }

        public static int SshdMonitorInterval
        {
            get;
            set;
        }

        public static int SshdStartDelay
        {
            get;
            set;
        }

        public static string SshdMonitorEnabled
        {
            get;
            set;
        }

        public static string Credentials
        {
            get;
            set;
        }


        public static void Clear()
        {
            Config.BaseDir = null;
            Config.MessageBus = null;
            Config.AgentId = null;
            Config.Configure = null;
            Config.Blobstore = null;
            Config.BlobstoreProvider = null;
            Config.BlobstoreOptions = null;
            Config.SystemRoot = null;
            Config.InfrastructureName = null;
            Config.PlatformName = null;
            Config.Nats = null;
            Config.ProcessAlerts = null;
            Config.SmtpPort = null;
            Config.SmtpUser = null;
            Config.SmtpPassword = null;
            Config.HeartbeatInterval = null;
            Config.SettingsFile = null;
            Config.Settings = null;
            Config.State = null;
            Config.SshdMonitorInterval = 0;
            Config.SshdStartDelay = 0;
            Config.SshdMonitorEnabled = null;
            Config.Credentials = null;
        }


        public static void Setup(YamlStream config)
        {
            YamlMappingNode root = (YamlMappingNode)config.Documents[0].RootNode;
            
            Config.Configure = root.GetString("configure");

            Config.BaseDir = root.GetString("base_dir") == null ? DEFAULT_BASE_DIR : root.GetChild("base_dir").ToString();
            Config.AgentId = root.GetString("agent_id");

          Config.MessageBus = root.GetString("mbus");

          Config.BlobstoreOptions  = root.GetString("blobstore_options");
          Config.BlobstoreProvider = root.GetString("blobstore_provider");

          Config.InfrastructureName = root.GetString("infrastructure_name");
          Config.PlatformName = root.GetString("platform_name");

          Config.SystemRoot = root.GetString("root_dir") ?? "/";

          Config.ProcessAlerts = root.GetString("process_alerts");
          Config.SmtpPort      = root.GetString("smtp_port");
          Config.SmtpUser      = "vcap";
          Config.SmtpPassword  = RandomPassword(8);

          Config.HeartbeatInterval = root.GetString("heartbeat_interval");

          Config.SshdMonitorInterval = root.GetInt("sshd_monitor_interval") ?? DEFAULT_SSHD_MONITOR_INTERVAL;
          Config.SshdStartDelay = root.GetInt("sshd_start_delay") ?? DEFAULT_SSHD_START_DELAY;
          Config.SshdMonitorEnabled = root.GetString("sshd_monitor_enabled");

            if (!string.IsNullOrEmpty(Config.Configure))
            {
                Logger.Info(string.Format("Configuring Agent with: {0}", root.ToString()));
            }

          Config.SettingsFile = Path.Combine(Config.BaseDir, "bosh", "settings.json");

          Config.Credentials = root.GetString("credentials");

          Config.Settings = new Dictionary<string,string>();

          Config.State = new State(Path.Combine(Config.BaseDir, "bosh", "state.yml"));
        }

        /// <summary>
        /// Gets the current infrastructure.
        /// </summary>
        public static IInfrastructure Infrastructure
        {
            get
            {

                try
                {
                    return UnityProvider.GetInstance.GetProvider<IInfrastructure>();
                }
                catch (Exception ex)
                {
                    throw new UnknownInfrastructure(ex);
                }

                
            }
        }

        public static Platform Platform
        {
            get
            {
                if (platform == null)
                {
                    platform = new Platform(Config.PlatformName).ProperPlatform;
                }
                return platform;
            }
        }

        public static string RandomPassword(int length)
        {
            return Uhuru.Utilities.Credentials.GenerateCredential(length);
        }

        public static string DefaultIp
        {
            get
            {
                string ip = null;
                foreach (Network network in Config.State.Networks)
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
