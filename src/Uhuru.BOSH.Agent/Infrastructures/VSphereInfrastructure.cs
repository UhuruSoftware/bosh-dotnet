// -----------------------------------------------------------------------
// <copyright file="VSphereInfrastructure.cs" company="Uhuru Software, Inc.">
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
    using Uhuru.Utilities;
    using System.IO;
    using Newtonsoft.Json;
    using System.Management;
    using Uhuru.BOSH.Agent.Infrastructures.VSphere;
    //using System.Yaml;

    /// <summary>
    /// VSphere Infrastructure
    /// </summary>
    public class VSphereInfrastructure : IInfrastructure
    {
        /// <summary>
        /// Loads the settings.
        /// </summary>
        public dynamic LoadSettings()
        {
            return new Settings().LoadSettings();
        }

        /// <summary>
        /// Gets the network settings.
        /// </summary>
        /// <param name="networkName">Name of the network.</param>
        /// <param name="properties">The properties.</param>
        public dynamic GetNetworkSettings(string networkName, dynamic properties)
        {
            return string.Empty;
        }

        
    }
}
