// -----------------------------------------------------------------------
// <copyright file="State.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using Uhuru.BOSH.Agent.Errors;
    using Uhuru.Utilities;
    using YamlDotNet.RepresentationModel;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class State : Base , IMessage
    {

        //public static State Process(string[] args)
        //{
        //    return new State();
        //}

        public string GetState()
        {
            try
            {
                Agent.State response = Config.State;
                Logger.Info(String.Format("Agent state: {0}", response.ToString()));

                if (Settings != null)
                {
                    response.SetValue("agent_id", Settings["agent_id"]);
                    response.SetValue("vm", Settings["vm"]);
                }

                response.SetValue("job_state", JobState);
                response.SetValue("bosh_protocol", "1"); // TODO: response["bosh_protocol"] = Bosh::Agent::BOSH_PROTOCOL
                response.SetValue("ntp", Ntp.GetNtpOffset().Offset.ToString());

                return response.ToHash().ToString();
            }
            catch (StateException e)
            {
                throw new MessageHandlerException(e.Message, e);
            }
        }

        public string JobState
        {
            get
            {
                //TODO afte we implement monit
                return "running";
                //throw new NotImplementedException();
                //  Bosh::Agent::Monit.service_group_state
            }
        }

        public bool IsLongRunning()
        {
            return false;
        }

        public string Process(dynamic args)
        {
            return GetState();
        }
    }
}
