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
    public static class BaseMessage
    {
        /// <summary>
        /// Gets the base dir.
        /// </summary>
        public static string BaseDir 
        { 
            get 
            { 
                return Config.BaseDir; 
            } 
        }

        /// <summary>
        /// Gets the logs dir.
        /// </summary>
        public static string LogsDir
        {
            get
            {
                return Path.Combine(BaseDir, "sys", "log");
            }
        }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        public static dynamic Settings
        {
            get
            {
                return Config.Settings;
            }
        }

        /// <summary>
        /// Gets the store path.
        /// </summary>
        public static string StorePath
        {
            get
            {
                return Path.Combine(BaseDir, "store");
            }
        }

        /// <summary>
        /// Gets the store migration target.
        /// </summary>
        public static string StoreMigrationTarget
        {
            get
            {
                return Path.Combine(BaseDir, "store_migraton_target");
            }
        }

        /// <summary>
        /// Errors the handler.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void ErrorHandler(string message)
        {
            Logger.Error(message);
            throw new MessageHandlerException(message);
        }
    }
}
