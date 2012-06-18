using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.BOSH.Agent.Providers
{
    /// <summary>
    /// Interface for all infrastructures
    /// </summary>
    public interface IInfrastructure
    {
        /// <summary>
        /// Loads the settings.
        /// </summary>
        dynamic LoadSettings();


        /// <summary>
        /// Gets the network settings.
        /// </summary>
        /// <param name="networkName">Name of the network.</param>
        /// <param name="properties">The properties.</param>
        dynamic GetNetworkSettings(string networkName, dynamic properties);
    
    }
}
