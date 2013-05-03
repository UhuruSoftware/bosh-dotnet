// -----------------------------------------------------------------------
// <copyright file="Apply.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Uhuru.Utilities;
    using Uhuru.BOSH.Agent.ApplyPlan;
    using Uhuru.BOSH.Agent.Errors;


    /// <summary>
    /// Apply message
    /// </summary>
    public class Apply : IMessage
    {
      
        dynamic newSpec;
        Plan oldPlan;
        Plan newPlan;

        /// <summary>
        /// Initializes a new instance of the <see cref="Apply"/> class.
        /// </summary>
        public Apply()
        {
        }

        /// <summary>
        /// Processes the apply message using args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public object Process(dynamic args)
        {
            Logger.Info("Processing apply message");

            if (args.Count < 1)
                throw new ArgumentException("not enough arguments");

            if (args.GetType().Name == "JObject")
                newSpec = args;
            else
                newSpec = args[0];

            CheckNetworkSettings();

            Logger.Info("Retrieving old plan");
            oldPlan = new Plan(Config.State.ToHash());

            Logger.Info("Retrieving new plan");
            newPlan = new Plan(newSpec);

            dynamic newState = ApplyPlan();

            return newState;
        }

        /// <summary>
        /// Checks the network settings.
        /// </summary>
        /// <param name="newSpec">The new spec.</param>
        private void CheckNetworkSettings()
        {
            if (this.newSpec["networks"] != null)
            {
                Logger.Info("The new spec contains networks element");
                foreach (dynamic network in this.newSpec["networks"])
                {
                    dynamic networkSettings = Config.Infrastructure().GetNetworkSettings(network.Name, network.Value);
                    if (networkSettings != string.Empty)
                        throw new NotImplementedException();
                    Logger.Info("Platform does not require additional settings");
                }
            }
        }

        /// <summary>
        /// Applies the plan.
        /// </summary>
        /// <returns></returns>
        private dynamic ApplyPlan()
        {
            Logger.Info("Applying : " + newSpec.ToString());

            if (oldPlan.Deployment.Value != string.Empty && oldPlan.Deployment.Value != newPlan.Deployment.Value)
            {
                
                MessageHandlerException exception = new MessageHandlerException(string.Format("attempt to apply {0} to {1}", oldPlan.Deployment.Value, newPlan.Deployment.Value));
                Logger.Error(exception.Message);
                throw exception;
            }

            if (newPlan.Configured)
            {
                try
                {
                    Monit.GetInstance().StopServices();
                    ApplyJob();
                    ApplyPackage();
                    ConfigureJob();
                }
                catch (Exception ex)
                {
                    MessageHandlerException exception = new MessageHandlerException(ex.Message, ex);
                    throw exception;
                }
            }
            Config.State.Write(newSpec);
            return newSpec;
        }

        /// <summary>
        /// Applies the job.
        /// </summary>
        public void ApplyJob()
        {
            Logger.Info("Applying job");
            if (newPlan.HasJob)
            {
                newPlan.InstallJob();
            }
            else
            {
                Logger.Info("No job");
            }
        }

        /// <summary>
        /// Applies the package.
        /// </summary>
        private void ApplyPackage()
        {
            if (newPlan.HasPackages)
            {
                newPlan.InstallPackages();
            }
            else
            {
                Logger.Info("No packages");
            }
            CleanupPackages();
        }

        /// <summary>
        /// Configures the job.
        /// </summary>
        private void ConfigureJob()
        {
            if (newPlan.HasJob)
                newPlan.ConfigureJob();
        }

        /// <summary>
        /// Cleanups the packages.
        /// </summary>
        private void CleanupPackages()
        {
            DeleteOldPackages();
            DeleteOldSymlinks();
        }


        /// <summary>
        /// Deletes the old packages.
        /// </summary>
        private void DeleteOldPackages()
        {
            Logger.Info("TODO delete old packages " + this.newSpec.ToString());
        }

        /// <summary>
        /// Deletes the old symlinks.
        /// </summary>
        private void DeleteOldSymlinks()
        {
            Logger.Info("TODO Delete old symbolic link" + this.newSpec.ToString());
        }


        /// <summary>
        /// Determines whether the job [is long running].
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
