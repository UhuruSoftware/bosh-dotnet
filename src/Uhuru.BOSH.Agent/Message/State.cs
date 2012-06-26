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
    //using YamlDotNet.RepresentationModel;
    using Uhuru.BOSH.Agent.Objects;
    using Newtonsoft.Json;

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

                response.SetValue("job_state", GetJobState());
                response.SetValue("bosh_protocol", "1"); // TODO: response["bosh_protocol"] = Bosh::Agent::BOSH_PROTOCOL
                HeartbeatMessage.NtpMessage ntpMessage = new HeartbeatMessage.NtpMessage();

                ntpMessage.Offset = Ntp.GetNtpOffset().Offset.ToString();
                ntpMessage.Timestamp = DateTime.Now.ToString("dd MMM HH:mm:ss");

                response.SetValue("ntp", JsonConvert.SerializeObject(ntpMessage));

                return response.ToHash().ToString();
            }
            catch (StateException e)
            {
                throw new MessageHandlerException(e.Message, e);
            }
        }

        public string GetJobState()
        {

            string serviceState = Monit.GetInstance().GetServiceGourpState();
            return serviceState;
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
