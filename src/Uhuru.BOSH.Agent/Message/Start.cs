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
    public class Start : IMessage
    {
        public string Process(dynamic args)
        {
            try
            {
                //if (Config.Configure)
                //{  
                    Monit.GetInstance().StartServices();
                //}
                return "\"started\"";
            }
            catch (Exception e)
            {
                throw new MessageHandlerException(String.Format("Cannot start job: {0}", e.ToString()));
            }
        }

        public bool IsLongRunning()
        {
            return false;
        }
    }
}
