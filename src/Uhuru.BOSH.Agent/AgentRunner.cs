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
    using System.Threading;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class AgentRunner
    {

        private string configFile = @"c:\vcap\bosh\settings.json";


        /// <summary>
        /// Initializes a new instance of the <see cref="AgentRunner"/> class.
        /// </summary>
        public AgentRunner()
        {
            
            Logger.Info("Starting agent");
            bool firstRun = true;

            if (File.Exists(configFile))
            {
                firstRun = false;
                dynamic root = JsonConvert.DeserializeObject(File.ReadAllText(configFile));
                Logger.Info("Configuring agent");
                Config.Setup(root, false);    
            }
            else
            {
                Logger.Info("Configuring agent for first run");
                Config.Setup(new JObject(), true);
            }

            Thread executionThread = new Thread(new ThreadStart(delegate
                {
                    if (firstRun)
                    {
                        Bootstrap bootStrap = new Bootstrap();
                        bootStrap.Configure();
                    }
                    Monit.GetInstance().Start();
                    Monit.GetInstance().StartServices();               
                    Handler.Start();
                }
                ));
            executionThread.Name = "Execution Thread";
            executionThread.IsBackground = true;
            executionThread.Start();
            
            
        }

    }
}
