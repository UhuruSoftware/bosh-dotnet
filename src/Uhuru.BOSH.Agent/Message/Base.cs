// -----------------------------------------------------------------------
// <copyright file="Base.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System.Collections.Generic;
    using System.IO;
    using Uhuru.BOSH.Agent.Errors;
    using Uhuru.Utilities;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Base
    {
        public string BaseDir { get { return Config.BaseDir; } }

        public string LogsDir
        {
            get
            {
                return Path.Combine(BaseDir, "sys", "log");
            }
        }

        public dynamic Settings
        {
            get
            {
                return Config.Settings;
            }
        }

        public string StorePath
        {
            get
            {
                return Path.Combine(BaseDir, "store");
            }
        }

        public string StoreMigrationTarget
        {
            get
            {
                return Path.Combine(BaseDir, "store_migraton_target");
            }
        }

        public void ErrorHandler(string message)
        {
            Logger.Error(message);
            throw new MessageHandlerException(message);
        }
    }
}
