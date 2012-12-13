// -----------------------------------------------------------------------
// <copyright file="Config.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Uhuru.BOSH.Agent.Errors;
    using Uhuru.BOSH.Agent.Providers;
    using Uhuru.NatsClient;
    using Uhuru.Utilities;
    using Uhuru.BOSH.Agent.Objects;
    using System.Collections.ObjectModel;
    using Newtonsoft.Json;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class Config
    {
        const string DEFAULT_BASE_DIR = @"c:\vcap";
        const int DEFAULT_SSHD_MONITOR_INTERVAL = 30;
        const int DEFAULT_SSHD_START_DELAY = 30;

        //public const string BOSH_APP = "vcap";
        //public const string BOSH_APP_USER = "vcap";
        //public const string BOSH_APP_GROUP = "vcap";

        private static IPlatform platform = null;

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

        public static bool Configure
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

        public static dynamic BlobstoreOptions
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

        public static bool? ProcessAlerts
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

        public static int HeartbeatInterval
        {
            get;
            set;
        }

        public static string SettingsFile
        {
            get;
            set;
        }

        public static dynamic Settings
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

        public static bool SshdMonitorEnabled
        {
            get;
            set;
        }

        public static dynamic Credentials
        {
            get;
            set;
        }


        public static void Clear()
        {
            Config.BaseDir = null;
            Config.MessageBus = null;
            Config.AgentId = null;
            Config.Configure = false;
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
            Config.HeartbeatInterval = 0;
            Config.SettingsFile = null;
            Config.Settings = null;
            Config.State = null;
            Config.SshdMonitorInterval = 0;
            Config.SshdStartDelay = 0;
            Config.SshdMonitorEnabled = false;
            Config.Credentials = null;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static void Setup(dynamic config, bool firstDeployment)
        {
            Logger.Info("Running setup");
            Config.SystemRoot = @"c:\"; //TODO system root

            Config.Configure =firstDeployment;
            if (!Config.Configure)
            {
                Logger.Info("Bosh agent not in configure mode");
                Config.BaseDir = config["base_dir"] !=null ? config["base_dir"].Value : DEFAULT_BASE_DIR;
                Config.AgentId = config["agent_id"].Value;
                Config.MessageBus = config["mbus"].Value;
                Config.BlobstoreOptions = config["blobstore"]["properties"] != null ? config["blobstore"]["properties"] : null;
            }
            else
            {
                Logger.Info("Bosh agent in configure mode");
                Config.BaseDir = DEFAULT_BASE_DIR;
                Config.AgentId = "not_configured";
                Config.MessageBus = "nats://localhost:4222";
                Config.BlobstoreOptions = null;
            }
            Config.BlobstoreProvider = config["blobstore"] != null ? config["blobstore"]["plugin"].Value : null;
            Config.InfrastructureName = "Windows"; //TODO Always windows, from unity
            Config.PlatformName = "vcap"; //TODO From unity
            Config.ProcessAlerts = true; //TODO from commandline
            Config.SmtpPort = "2852"; //TODO from commandline
            Config.SmtpUser = "vcap";
            Config.SmtpPassword = RandomPassword(8);
            Config.HeartbeatInterval = 60; //TODO from commandline
            Config.SshdMonitorInterval = config["sshd_monitor_interval"] != null ? int.Parse(config["sshd_monitor_interval"].Value) : DEFAULT_SSHD_MONITOR_INTERVAL;
            Config.SshdStartDelay = config["sshd_start_delay"] != null ? int.Parse(config["sshd_start_delay"].Value) : DEFAULT_SSHD_START_DELAY;
            Config.SshdMonitorEnabled = config["sshd_monitor_enabled"] != null ? config["sshd_monitor_enabled"].Value : false;
            //Config.SshdMonitorEnabled = root.GetString("sshd_monitor_enabled");

            //if (!string.IsNullOrEmpty(Config.Configure))
            //{
            //    Logger.Info(string.Format("Configuring Agent with: {0}", root.ToString()));
            //}

            Config.SettingsFile = Path.Combine(Config.BaseDir, "bosh", "settings.json");
            Config.Settings = GetSettings(Config.SettingsFile);
            Config.State = new State(Path.Combine(Config.BaseDir, "bosh", "state.yml"));
            Logger.Info("Configuration done!");
        }

        private static dynamic GetSettings(string settingsFile)
        {
         
            if (!File.Exists(settingsFile))
                return null;
            string fileContent = File.ReadAllText(settingsFile);

            return JsonConvert.DeserializeObject(fileContent);
            
            
        }

        /// <summary>
        /// Gets the current infrastructure.
        /// </summary>
        public static IInfrastructure Infrastructure()
        {
            try
            {
                return UnityProvider.GetInstance.GetProvider<IInfrastructure>();
            }
            catch (Exception ex)
            {
                throw new UnknownInfrastructureException(ex);
            }
        }

        public static IPlatform Platform
        {
            get
            {
                if (platform == null)
                {
                    platform = UnityProvider.GetInstance.GetProvider<IPlatform>();
                }
                return platform;
            }
        }

        public static string RandomPassword(int length)
        {
            return Uhuru.Utilities.Credentials.GenerateCredential(length);
        }

        public static string DefaultIP
        {
            get
            {
                string ip = null;
                //TODO
                //foreach (Network network in Config.State.Networks)
                //{
                //    if (ip == null)
                //    {
                //        ip = network.Ip;
                //    }

                //    if (network.Name == "default")
                //    {
                //        ip = network.Ip;
                //    }
                //}
                ip = Config.State.GetValue("networks")["default"]["ip"].Value;
                return ip;
            }
        }
    }
}
