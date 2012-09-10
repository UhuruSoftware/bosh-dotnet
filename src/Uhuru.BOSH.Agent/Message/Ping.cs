// -----------------------------------------------------------------------
// <copyright file="Ping.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// The ping message
    /// </summary>
    public class Ping : IMessage
    {
        /// <summary>
        /// Processes ping message.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public object Process(dynamic args)
        {
            return "pong";
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
    }
}
