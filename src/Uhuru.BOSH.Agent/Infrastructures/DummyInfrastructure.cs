// -----------------------------------------------------------------------
// <copyright file="DummyInfrastructure.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Infrastructures 
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Uhuru.BOSH.Agent.Providers;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DummyInfrastructure : IInfrastructure
    {

        /// <summary>
        /// Loads the settings.
        /// </summary>
        public void LoadSettings()
        {
        }
        /// <summary>
        /// Gets the network settings.
        /// </summary>
        /// <param name="networkName">Name of the network.</param>
        /// <param name="properties">The properties.</param>
        public void GetNetworkSettings(string networkName, Dictionary<string, string> properties)
        {
            throw new NotImplementedException();
        }
    }
}
