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
        /// <summary>
        /// Determines whether [is long running].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is long running]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsLongRunning()
        {
            return true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Drain"/> class.
        /// </summary>
        public Drain()
        {
            this.monit = Monit.GetInstance();
        }
        
        Reactor nats;
        string agentId;
        //TODO: Jira UH-1202
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        Agent.State oldSpec;        
        string drainType;
        dynamic spec;
        Monit monit = null;

        /// <summary>
        /// Processes the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public object Process(dynamic args)
        {
            this.nats = Config.Nats;
            this.agentId = Config.AgentId;
            this.oldSpec = Config.State;
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
            nats.Publish("hm.agent.shutdown." + agentId);
            return this.RunDrainScript();
        }

        private object RunDrainScript()
        {
            object result =this.monit.RunPreScripts(false);
            
            return result;
            //return "0";
        }

        private object DrainForUpdate()
        {
            if (spec == null)
            {
                throw new MessageHandlerException("Drain update called without apply spec");
            }
            spec.Remove("configuration_hash");
            Message.Apply applyMessage = new Message.Apply();

            applyMessage.Process(spec);

            //Logger.Warning("Not implemented yet");

            return 0;
            
        }

        //TODO : Jira 12000
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private string DrainCheckStatus()
        {
            throw new NotImplementedException();
        }
        
    }
}
