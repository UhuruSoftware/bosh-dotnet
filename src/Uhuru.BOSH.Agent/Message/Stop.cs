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
    using System.Globalization;

    /// <summary>
    /// Stop Message
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Stop", Justification = "Keeping the name so it's the same as VMWare's")]
    public class Stop: IMessage
    {
        /// <summary>
        /// Processes the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public object Process(dynamic args)
        {
            try
            {
                Monit.GetInstance().StopServices();
                return "stopped";
            }
            catch (Exception e)
            {
                throw new MessageHandlerException(String.Format(CultureInfo.InvariantCulture, "Cannot stop job: {0}", e.ToString()));
            }
        }

        /// <summary>
        /// Determines whether the message [is long running].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is long running]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsLongRunning()
        {
            return true;
        }
    }
}
