// -----------------------------------------------------------------------
// <copyright file="AgentRunner.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
using System.IO;
    using Uhuru.Utilities;
    using System.Diagnostics;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class AgentRunner
    {
////  BOSH_APP = BOSH_APP_USER = BOSH_APP_GROUP = "vcap"
        string BOSH_APP  = "vcap";
        string BOSH_APP_USER = "vcap";
        string BOSH_APP_GROUP = "vcap";
        private string configFile = @"c:\vcap\bosh\settings.json";
////  class << self
////    def run(options = {})
////      Runner.new(options).start
////    end
////  end
       

////  class Runner < Struct.new(:config)

////    def initialize(options)
////      self.config = Bosh::Agent::Config.setup(options)
////      @logger     = Bosh::Agent::Config.logger
////    end

        public AgentRunner()
        {
            
            Logger.Info("Starting agent");
            //YamlNode root = null;
            if (File.Exists(configFile))
            {
                //using (TextReader textReader = new StreamReader(configFile))
                //{
                //    YamlNode[] nodes = YamlNode.FromYaml(textReader);
                //    root = nodes[0];
                //}
                dynamic root = JsonConvert.DeserializeObject(File.ReadAllText(configFile));
                Logger.Info("Configuring agent");
                Config.Setup(root[0], false);
                Monit.GetInstance().Start();
                Monit.GetInstance().StartServices();               
            }
            else
            {
                Logger.Info("Configuring agent for first run");
                Config.Setup(new JObject(), true);

                Bootstrap bootStrap = new Bootstrap();
                bootStrap.Configure();
                Monit.GetInstance().Start();
                Monit.GetInstance().StartServices();

                //Logger.Info("Restarting...");
                //Process.Start("shutdown", "/r /t 0");
                //Environment.Exit(0);
            }
            Handler.Start();
            
            
        }

////    def start
////      $stdout.sync = true
////      @logger.info("Starting agent #{Bosh::Agent::VERSION}...")

////      if Config.configure
////        @logger.info("Configuring agent...")
////        # FIXME: this should not use message handler.
////        # The whole thing should be refactored so that
////        # regular code doesn't use RPC handlers other than
////        # for responding to RPC.
////        Bosh::Agent::Bootstrap.new.configure

////        Bosh::Agent::Monit.enable
////        Bosh::Agent::Monit.start
////        Bosh::Agent::Monit.start_services
////      else
////        @logger.info("Skipping configuration step (use '-c' argument to configure on start) ")
////      end

////      if Config.mbus.start_with?("http")
////        require "agent/http_handler"
////        Bosh::Agent::HTTPHandler.start
////      else
////        Bosh::Agent::Handler.start
////      end
////    end
////  end

////end

////if __FILE__ == $0
////  options = {
////    "configure"         => true,
////    "logging"           => { "level" => "DEBUG" },
////    "mbus"              => "nats://localhost:4222",
////    "agent_id"          => "not_configured",
////    "base_dir"          => "/var/vcap",
////    "platform_name"     => "ubuntu",
////    "blobstore_options" => {}
////  }
////  Bosh::Agent.run(options)
////end
    }
}
