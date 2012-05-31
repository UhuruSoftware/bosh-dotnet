// -----------------------------------------------------------------------
// <copyright file="Start.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Uhuru.BOSH.Agent.Errors;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Start
    {
        public static string Process(string[] args)
        {
            try
            {
                if (Config.Configure)
                {
                    throw new NotImplementedException(); //TODO: florind Bosh::Agent::Monit.start_services
                }
                return "started";
            }
            catch (Exception e)
            {
                throw new MessageHandlerException(String.Format("Cannot start job: {0}", e.ToString()));
            }
        }
    }
}
