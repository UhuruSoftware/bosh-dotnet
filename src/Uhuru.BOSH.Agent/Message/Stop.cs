// -----------------------------------------------------------------------
// <copyright file="Stop.cs" company="Uhuru Software, Inc.">
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
    using Uhuru.Utilities;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Stop", Justification = "Keeping the name so it's the same as VMWare's")]
    public class Stop: IMessage
    {
        public object Process(dynamic args)
        {
            try
            {
                if (Config.Configure)
                {
                    Monit.GetInstance().StopServices();
                }
                return "stopped";
            }
            catch (Exception e)
            {
                throw new MessageHandlerException(String.Format("Cannot stop job: {0}", e.ToString()));
            }
        }

        public bool IsLongRunning()
        {
            return true;
        }
    }
}
