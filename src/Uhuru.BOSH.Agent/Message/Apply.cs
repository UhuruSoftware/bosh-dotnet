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
    /// TODO: Update summary.
    /// </summary>
    public class Apply : IMessage
    {
      ////def self.long_running?; true end
        dynamic newSpec;
        Plan oldPlan;
        Plan newPlan;
      ////def self.process(args)
      ////  self.new(args).apply
      ////end
        public Apply()
        {

        }

        

        public string Process(dynamic args)
        {
            Logger.Info("Processing apply message");

            if (args.Count < 1)
                throw new ArgumentException("not enough arguments");


            //args = YamlMapping.FromYaml(args.ToString());

            newSpec = args[0];

            //if (newSpec.ContainsKey("networks"))
            if (newSpec["networks"] != null)
            {
                Logger.Info("The new spec contains networks element");
                foreach (dynamic network in newSpec["networks"])
                {
                    dynamic networkSettings = Config.Infrastructure.GetNetworkSettings(network.Name, network.Value);
                    if (networkSettings != string.Empty)
                        throw new NotImplementedException();
                    Logger.Info("Plantform does not require additional settings");
                }
            }

            Logger.Info("Retieving old plan");
            oldPlan = new Plan(Config.State.ToHash());

            Logger.Info("Retrieving new plan");
            newPlan = new Plan(newSpec);

            string newState = ApplyPlan().ToString();

            return newState;
        }

      ////# TODO: adapt for job collocation
      ////def initialize(args)
      ////  @platform = Bosh::Agent::Config.platform

      ////  if args.size < 1
      ////    raise ArgumentError, "not enough arguments"
      ////  end

      ////  @new_spec = args.first
      ////  unless @new_spec.is_a?(Hash)
      ////    raise ArgumentError, "invalid spec, Hash expected, " +
      ////                         "#{@new_spec.class} given"
      ////  end

      ////  # Note: new spec needs to be updated before a plan is
      ////  # created which binds to this new spec
      ////  #
      ////  # Collect network state from the infrastructure
      ////  # - Loop through each network
      ////  # - Get network settings for each network
      ////  if @new_spec["networks"]
      ////    @new_spec["networks"].each do |network, properties|
      ////      infrastructure = Bosh::Agent::Config.infrastructure
      ////      network_settings =
      ////          infrastructure.get_network_settings(network, properties)
      ////      if network_settings
      ////        @new_spec["networks"][network].merge!(network_settings)
      ////      end
      ////    end
      ////  end

      ////  @old_spec = Bosh::Agent::Config.state.to_hash

      ////  @old_plan = Bosh::Agent::ApplyPlan::Plan.new(@old_spec)
      ////  @new_plan = Bosh::Agent::ApplyPlan::Plan.new(@new_spec)

      ////  %w(bosh jobs packages monit).each do |dir|
      ////    FileUtils.mkdir_p(File.join(base_dir, dir))
      ////  end
      ////end

      ////def apply
      ////  logger.info("Applying: #{@new_spec.inspect}")

      ////  if !@old_plan.deployment.empty? &&
      ////      @old_plan.deployment != @new_plan.deployment
      ////    raise Bosh::Agent::MessageHandlerError,
      ////          "attempt to apply #{@new_plan.deployment} " +
      ////          "to #{old_plan.deployment}"
      ////  end

      ////  # FIXME: tests
      ////  # if @state["configuration_hash"] == @new_spec["configuration_hash"]
      ////  #   return @state
      ////  # end

      ////  if @new_plan.configured?
      ////    begin
      ////      delete_job_monit_files
      ////      apply_job
      ////      apply_packages
      ////      configure_job
      ////      reload_monit
      ////      @platform.update_logging(@new_spec)
      ////    rescue Exception => e
      ////      raise Bosh::Agent::MessageHandlerError,
      ////            "#{e.message}: #{e.backtrace}"
      ////    end
      ////  end

      ////  # FIXME: assumption right now: if apply succeeds state should be
      ////  # identical with apply spec
      ////  Bosh::Agent::Config.state.write(@new_spec)
      ////  @new_spec

      ////rescue Bosh::Agent::StateError => e
      ////  raise Bosh::Agent::MessageHandlerError, e
      ////end

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
                    DeleteJobMonitFile();
                    ApplyJob();
                    ApplyPackage();
                    ConfigureJob();
                    //ReloadMonit() //TODO After we find a replacement
                    
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

      ////private

      ////def delete_job_monit_files
      ////  dir = File.join(base_dir, "monit", "job")
      ////  logger.info("Removing job-specific monit files: #{dir}")

      ////  # Remove all symlink targets
      ////  Dir.glob(File.join(dir, "*")).each do |f|
      ////    if File.symlink?(f)
      ////      logger.info("Removing monit symlink target file: " +
      ////                  "#{File.readlink(f)}")
      ////      FileUtils.rm(File.readlink(f))
      ////    end
      ////  end

      ////  FileUtils.rm_rf(dir)
      ////end

        private void DeleteJobMonitFile()
        {
            Logger.Info("TODO MONIT");
        }

      ////def apply_job
      ////  if @new_plan.has_job?
      ////    @new_plan.install_job
      ////  else
      ////    logger.info("No job")
      ////  end
      ////end
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

      ////def apply_packages
      ////  if @new_plan.has_packages?
      ////    @new_plan.install_packages
      ////  else
      ////    logger.info("No packages")
      ////  end

      ////  cleanup_packages
      ////end
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
      ////def configure_job
      ////  if @new_plan.has_job?
      ////    @new_plan.configure_job
      ////  end
      ////end
        private void ConfigureJob()
        {
            if (newPlan.HasJob)
                newPlan.ConfigureJob();
        }

      ////# We GC packages - leaving the package union of old spec and new spec
      ////def cleanup_packages
      ////  delete_old_packages
      ////  delete_old_symlinks
      ////end
        private void CleanupPackages()
        {
            DeleteOldPackages();
            DeleteOldSymlinks();
        }

      ////def delete_old_packages
      ////  files_to_keep = Set.new

      ////  (@old_plan.packages + @new_plan.packages).each do |package|
      ////    files_to_keep << package.install_path
      ////  end

      ////  glob = File.join(base_dir, "data", "packages", "*", "*")

      ////  Dir[glob].each do |path|
      ////    unless files_to_keep.include?(path)
      ////      logger.info("Removing old package version: #{path}")
      ////      FileUtils.rm_rf(path)
      ////    end
      ////  end
      ////end
        private void DeleteOldPackages()
        {
            Logger.Info("TODO delete old packages");
        }
      ////def delete_old_symlinks
      ////  files_to_keep = Set.new

      ////  (@old_plan.packages + @new_plan.packages).each do |package|
      ////    files_to_keep << package.link_path
      ////  end

      ////  glob = File.join(base_dir, "packages", "*")

      ////  Dir[glob].each do |path|
      ////    unless files_to_keep.include?(path)
      ////      logger.info("Removing old package link: #{path}")
      ////      FileUtils.rm_rf(path)
      ////    end
      ////  end
      ////end
        private void DeleteOldSymlinks()
        {
            Logger.Info("TODO Delete old symlinks");
        }
        
      ////def reload_monit
      ////  if Bosh::Agent::Config.configure
      ////    Bosh::Agent::Monit.reload
      ////  end
      ////end

        public bool IsLongRunning()
        {
            return true;
        }
    }
}
