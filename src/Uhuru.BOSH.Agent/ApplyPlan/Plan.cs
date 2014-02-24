// -----------------------------------------------------------------------
// <copyright file="Plan.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.ApplyPlan
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Collections.ObjectModel;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Plan
    {
        ////attr_reader :deployment
        ////attr_reader :job
        ////attr_reader :packages

        public dynamic Deployment
        {
            get
            {
                return deployment;
            }
        }


        public Collection<Job> Jobs
        {
            get
            {
                return jobs;
            }
        }


        public Collection<Package> Packages
        {
            get
            {
                return packages;
            }
        }

        private dynamic deployment;
        private Collection<Job> jobs;
        private Collection<Package> packages;
        private dynamic spec;


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public Plan(dynamic spec)
        {

            this.spec = spec;
            this.deployment = spec["deployment"];
            this.jobs = null;
            this.packages = new Collection<Package>();
            string jobName = "";

            dynamic jobSpec = spec["job"];
            dynamic packageSpecs = spec["packages"];
            
           
            if (jobSpec != null && jobSpec.ToString() != "")
            {
                jobName = jobSpec["name"].Value;
                this.jobs = new Collection<Job>();

                if (IsLegacySpec(jobSpec))
                {
                    Job job = new Job(jobName, jobSpec["template"], jobSpec,spec);
                    this.jobs.Add(job);
                }
                else
                {
                    foreach (var templateSpec in jobSpec["templates"])
                    {
                        Job job = new Job(jobName, templateSpec["name"].Value, templateSpec, spec);
                        jobs.Add(job);

                    }
                }
                
            }

            if (packageSpecs != null)
            {

                foreach (var i in packageSpecs)
                {
                    this.packages.Add(new Package(i.Value));
                }

            }
        }

        

        public bool HasJobs
        {
            get
            {
                return this.jobs != null;
            }
        }

        

        public bool HasPackages
        {
            get
            {
                return packages.Count != 0;
            }
        }

        
        public bool Configured
        {
            get
            {
                return (spec["configuration_hash"] != null);
            }
        }


        public void InstallJobs()
        {
            if (this.HasJobs)
            {
                foreach (Job job in jobs)
                {
                    job.Install();
                }
            }
        }


        public void InstallPackages()
        {
            if (this.HasJobs)
            {
                foreach (Job job in jobs)
                {
                    foreach (var package in packages)
                    {
                        package.InstallForJob(job);                        
                    }
                }
            }
        }

       
        public void ConfigureJobs()
        {
            if (this.HasJobs)
            {
                foreach (Job job in jobs)
                {
                    job.Configure();
                }
            }
        }

        private static bool IsLegacySpec(dynamic jobSpec)
        {
            if (jobSpec["template"] != null && jobSpec["templates"] == null)
            {
                return true;
            }
            return false;
        }
    }
}
