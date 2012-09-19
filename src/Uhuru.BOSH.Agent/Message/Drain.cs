// -----------------------------------------------------------------------
// <copyright file="Drain.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Uhuru.NatsClient;
    using Uhuru.Utilities;
    using Uhuru.BOSH.Agent.Errors;
    using System.Threading;
    using System.Globalization;
    using System.IO;
    using System.Diagnostics;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Drain :IMessage

    {
        /// <summary>
        /// Determines whether [is long running].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is long running]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsLongRunning()
        {
            return true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Drain"/> class.
        /// </summary>
        public Drain()
        {
            //this.monit = Monit.GetInstance();
        }
        
        Reactor nats;
        string agentId;
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        Agent.State oldSpec;        
        string drainType;
        dynamic spec;
        private string drainFile;

        /// <summary>
        /// Processes the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public object Process(dynamic args)
        {
            this.nats = Config.Nats;
            this.agentId = Config.AgentId;
            this.oldSpec = Config.State;
            this.drainType = args[0].Value;
            this.drainFile = DrainScriptFile();

            if (args.Count > 1)
            {
                spec = args[1];
            }

            Logger.Info("Draining " + agentId);


            switch (drainType)
            {
                case "shutdown": return DrainForShutdown();
                case "update": return DrainForUpdate();
                case "status": return DrainCheckStatus();
                default: throw new MessageHandlerException("Unknown drain type " + drainType);

            }

        }

        private object DrainForShutdown()
        {
            nats.Publish("hm.agent.shutdown." + agentId);

            if (oldSpec.Job != null && DrainScriptExists())
            {
                return RunDrainScript("job_shutdown", "hash_unchanged", new string[0]);

            }
            else
                return 0;
        }

        private object DrainForUpdate()
        {
            if (spec == null)
            {
                throw new MessageHandlerException("Drain update called without apply spec");
            }

            if (oldSpec.Job != null && DrainScriptExists())
            {
                return RunDrainScript(JobChange(), HashChange(), UpdatePackages());
            }

            return 0;

        }

        private string JobChange()
        {
            if (oldSpec.Job == null)
                return "job_new";
            else if (oldSpec.Job.SHA1 == spec["job"]["sha1"].Value)
                return "job_unchanged";
            else
                return "job_changed";
        }

        private string HashChange()
        {
            if (oldSpec.GetValue("configuration_hash") == null)
            {
                return "hash_new";
            }
            else if (oldSpec.GetValue("configuration_hash").Value == spec["configuration_hash"].Value)
            {
                return "hash_unchanged";
            }
            else
                return "hash_changed";
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private string[] UpdatePackages()
        {
            List<string> result = new List<string>();
            bool exists;

            if (oldSpec.GetValue("packages") == null)
                return result.ToArray();

            //Check old packages
            foreach (dynamic oldPkg in oldSpec.GetValue("packages"))
            {
            
                foreach (dynamic newPkg in spec["packages"])
                {
                    if (newPkg.Name == oldPkg.Name)
                    {
                        if (newPkg.Value["sha1"].Value != oldPkg.Value["sha1"].Value)
                        {
                            result.Add(newPkg.Name);
                        }
                    }
                    
                }
            }

            //Check for new packages
            foreach (dynamic newPkg in spec["packages"])
            {
                exists = false;
                foreach (dynamic oldPkg in oldSpec.GetValue("packages"))
                {
                    if (newPkg.Name == oldPkg.Name)
                    {
                        exists = true;
                    }
                }
                if (!exists)
                    result.Add(newPkg.Name);
            }
            
            return result.ToArray();
        }

        private bool DrainScriptExists()
        {
            if (File.Exists(this.drainFile))
                return true;
            else
                return false;
        }

        private string DrainScriptFile()
        {
            string jobTemplate = this.oldSpec.Job.Template;
            return string.Format(CultureInfo.InvariantCulture, @"{0}\jobs\{1}\drain.bat", Config.BaseDir, jobTemplate);
        }

        private object RunDrainScript(string jobUpdated, string hashUpdated, string[] updatedPackages)
        {
            int result =0;
            string standardOutput = string.Empty;
            string standardError = string.Empty;
            Logger.Info(string.Format(CultureInfo.InvariantCulture, "Running drain script with job updated: {0}, hash updated: {1} and updated packages {2}", jobUpdated, hashUpdated, string.Join(" ", updatedPackages)));
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = drainFile;
            info.EnvironmentVariables.Add("job_change", jobUpdated);
            info.EnvironmentVariables.Add("hash_change", hashUpdated);
            info.EnvironmentVariables.Add("updated_packages", String.Join(" ", updatedPackages));
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false;

            using (System.Diagnostics.Process p = new Process())
            {
                p.StartInfo = info;
                p.Start();
                p.WaitForExit();
                result = p.ExitCode;
                standardOutput = p.StandardOutput.ReadToEnd();
                standardError = p.StandardError.ReadToEnd();
            }
            if (!String.IsNullOrEmpty(standardError))
            {
                string errorMessage = string.Format(CultureInfo.InvariantCulture, "Drain script error exit {0} : {1}", result, standardError);
                MessageHandlerException massageHandlerException = new MessageHandlerException(errorMessage);
                Logger.Error(errorMessage);
                throw massageHandlerException;
            }
            else
            {
                Logger.Info(string.Format(CultureInfo.InvariantCulture, "Finished running drain script, output :{0}", standardOutput));
            }

            return result;
        }
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private object DrainCheckStatus()
        {
            return RunDrainScript("job_check_status", "hash_unchanged", new string[0]);
        }
        
    }
}
