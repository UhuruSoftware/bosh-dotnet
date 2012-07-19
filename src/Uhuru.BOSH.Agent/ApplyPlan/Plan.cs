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


        public Job Job
        {
            get
            {
                return job;
            }
        }


        public List<Package> Packages
        {
            get
            {
                return packages;
            }
        }

        private dynamic deployment;
        private Job job;
        private List<Package> packages;
        private dynamic spec;
        private dynamic configBinding;


        ////def initialize(spec)
        ////  unless spec.is_a?(Hash)
        ////    raise ArgumentError, "Invalid spec format, Hash expected, " +
        ////                         "#{spec.class} given"
        ////  end

        ////  @spec = spec
        ////  @deployment = spec["deployment"]
        ////  @job = nil
        ////  @packages = []
        ////  @config_binding = Bosh::Agent::Util.config_binding(spec)

        ////  job_spec = spec["job"]
        ////  package_specs = spec["packages"]

        ////  # By default stemcell VM has '' as job
        ////  # in state.yml, handling this very special case
        ////  if job_spec && job_spec != ""
        ////    @job = Job.new(job_spec, @config_binding)
        ////  end

        ////  if package_specs
        ////    unless package_specs.is_a?(Hash)
        ////      raise ArgumentError, "Invalid package specs format " +
        ////                           "in apply spec, Hash expected " +
        ////                           "#{package_specs.class} given"
        ////    end

        ////    package_specs.each_pair do |package_name, package_spec|
        ////      @packages << Package.new(package_spec)
        ////    end
        ////  end
        ////end

        public Plan(dynamic spec)
        {
            // TODO: optionally: check to se if spec is a IDidctionry<any type> if it is possible

            this.spec = spec;
            this.deployment = spec["deployment"];
            this.job = null;
            this.packages = new List<Package>();
            //this.configBinding = Util.conigBinding(spec);

            dynamic jobSpec = spec["job"];
            dynamic packageSpecs = spec["packages"];
            dynamic jobProperties = spec["properties"];

            if (jobSpec != null && jobSpec.ToString() != "")
            {
                this.job = new Job(jobSpec, jobProperties);
            }

            if (packageSpecs != null)
            {
                // todo: assert packageSpec is a Hash

                foreach (var i in packageSpecs)
                {
                    this.packages.Add(new Package(i.Value));
                }

            }
        }

        ////def has_job?
        ////  !@job.nil?
        ////end

        public bool HasJob
        {
            get
            {
                return this.job != null;
            }
        }

        ////def has_packages?
        ////  !@packages.empty?
        ////end

        public bool HasPackages
        {
            get
            {
                return packages.Count != 0;
            }
        }

        ////# TODO: figure out why it has to be an apply marker
        ////def configured?
        ////  @spec.key?("configuration_hash")
        ////end

        public bool Configured
        {
            get
            {
                return (spec["configuration_hash"] != null);
            }
        }

        ////def install_job
        ////  @job.install if has_job?
        ////end

        public void InstallJob()
        {
            if (this.HasJob)
            {
                this.job.Install();
            }
        }

        ////def install_packages
        ////  @packages.each do |package|
        ////    package.install_for_job(@job)
        ////  end
        ////end

        public void InstallPackages()
        {
            foreach (var i in packages)
            {
                i.InstallForJob(this.job);
            }
        }

        ////def configure_job
        ////  @job.configure if has_job?
        ////end

        public void ConfigureJob()
        {
            if (this.HasJob)
            {
                job.Configure();
            }
        }
    }
}
