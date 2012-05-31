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
    using System.Yaml;

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
            Logger.Info("Loading vsphere settings");
            LoadCdromSettings();

            Logger.Info("Loading settings file");
            YamlNode root = null;
            using (TextReader textReader = new StreamReader(Config.SettingsFile))
            {
                YamlNode[] nodes = YamlNode.FromYaml(textReader);
                root = nodes[0];
            }
            return root;
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

        private void LoadCdromSettings()
        {
            Logger.Info("Loading cdrom settings");
            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if (driveInfo.DriveType == DriveType.CDRom)
                {
                    Logger.Info("Found cdrom at " + driveInfo.Name);
                    Logger.Info("Checking ENV file in " + driveInfo.Name);
                    if (File.Exists(driveInfo.Name + "ENV"))
                    {
                        Logger.Info("Found ENV file in " + driveInfo.Name);
                        File.Copy(driveInfo.Name + "ENV", Config.SettingsFile);
                    }
                }
            }

        }
    }
}
