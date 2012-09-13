// -----------------------------------------------------------------------
// <copyright file="PrepareNetworkChange.cs" company="Uhuru Software, Inc.">
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
    /// TODO: Update summary.
    /// </summary>
    public class PrepareNetworkChange: IMessage
    {
        public object Process(dynamic args)
        {
            return true;
        }

        public bool IsLongRunning()
        {
            return false;
        }
    }
}
