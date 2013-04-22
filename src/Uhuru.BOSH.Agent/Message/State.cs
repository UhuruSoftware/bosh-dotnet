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
    using Newtonsoft.Json.Linq;
    using System.Globalization;

    /// <summary>
    /// Get state message
    /// </summary>
    public class State : IMessage
    {

        private Monit monit = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="State"/> class.
        /// </summary>
        public State()
        {
            monit = Monit.GetInstance();
        }

        private object GetState()
        {
            try
            {
                Agent.State response = Config.State;
                Logger.Info(String.Format(CultureInfo.InvariantCulture, "Agent state: {0}", response.ToString()));

                if (BaseMessage.Settings != null)
                {
                    response.SetValue("agent_id", BaseMessage.Settings["agent_id"]);
                    response.SetValue("vm", BaseMessage.Settings["vm"]);
                }

                response.SetValue("job_state", GetJobState());
                response.SetValue("bosh_protocol", "1"); // TODO: response["bosh_protocol"] = Bosh::Agent::BOSH_PROTOCOL
                NtpMessage ntpMessage = Ntp.GetNtpOffset();
                response.SetValue("ntp", JToken.FromObject(ntpMessage));

                return response.ToHash();
            }
            catch (StateException e)
            {
                throw new MessageHandlerException(e.Message, e);
            }
        }

        private string GetJobState()
        {

            string serviceState = this.monit.GetServiceGroupState;
            return serviceState;
        }

        /// <summary>
        /// Determines whether the message [is long running].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is long running]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsLongRunning()
        {
            return false;
        }

        /// <summary>
        /// Processes the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public object Process(dynamic args)
        {
            return GetState();
        }
    }
}
