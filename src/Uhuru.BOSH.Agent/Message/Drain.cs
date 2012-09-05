// -----------------------------------------------------------------------
// <copyright file="Drain.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
using Uhuru.NatsClient;
    using Uhuru.Utilities;
    using Uhuru.BOSH.Agent.Errors;
    using System.Threading;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Drain :IMessage

    {
        public bool IsLongRunning()
        {
            return true;
        }

        string baseDir;
        Reactor nats;
        string agentId;
        Agent.State oldSpec;
        dynamic args;
        string drainType;
        dynamic spec;

        public object Process(dynamic args)
        {
            this.baseDir = Config.BaseDir;
            this.nats = Config.Nats;
            this.agentId = Config.AgentId;
            this.oldSpec = Config.State;
            this.args = args;
            drainType = args[0].Value;
            if (args.Count > 1)
            {
                spec = args[1];
            }


            Logger.Info("Draining " + agentId);

            if (Config.Configure)
            {
                //TODO unmonitor service
            }

            switch (drainType)
            {
                case "shutdown": return DrainForShutdown();
                    break;
                case "update": return DrainForUpdate();
                    break;
                case "status": return DrainCheckStatus();
                    break;
                default: throw new MessageHandlerException("Unknown drain type " + drainType);

            }
            //return string.Empty;

        }

        private object DrainForShutdown()
        {
            //bool delivered = false;
            //ThreadPool.QueueUserWorkItem((data) =>
            //    {
                    nats.Publish("hm.agent.shutdown." + agentId);
              //      delivered = true;
            //    });
            //TODO for drainscript
            //if (oldSpec.ToHash().ContainsKey("job") && drain
            //while (!delivered)
            //{
            //    Thread.Sleep(1000);
            //}
            return RunDrainScript();
            //Monit.GetInstance().StopServices();
            
            
           // return "0";
        }

        private object RunDrainScript()
        {
            string result = Monit.GetInstance().RunPreScripts(false);
            
            return result;
            //return "0";
        }

        private string DrainForUpdate()
        {
            if (spec == null)
            {
                throw new MessageHandlerException("Drain update called without apply spec");
            }
            spec.Remove("configuration_hash");
            Message.Apply applyMessage = new Message.Apply();

            applyMessage.Process(spec);

            //Logger.Warning("Not implemented yet");

            return "0";
            
        }

        private string DrainCheckStatus()
        {
            throw new NotImplementedException();
        }
        
    }
}
