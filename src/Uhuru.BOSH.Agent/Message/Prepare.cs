// -----------------------------------------------------------------------
// <copyright file="Prepare.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Uhuru.BOSH.Agent.ApplyPlan;

    /// <summary>
    /// Prepare Message.
    /// </summary>
    public class Prepare : IMessage
    {

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

        
        public object Process(dynamic args)
        {
            
            Monit.GetInstance().StopServices();
            
            dynamic spec;
            if (args.GetType().Name == "JObject")
                spec = args;
            else
                spec = args[0];

            Plan plan = new Plan(spec);
            foreach (Job job in plan.Jobs)
            {
                job.PrepareForInstall();
            }
            foreach (Package package in plan.Packages)
            {
                package.PrepareForInstall();
            }

            return new object();
        }
    }
}
