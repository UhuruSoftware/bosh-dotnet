// -----------------------------------------------------------------------
// <copyright file="Package.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.ApplyPlan
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using Uhuru.BOSH.Agent.ApplyPlan.Errors;
    using Uhuru.Utilities;
    using System.Globalization;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Package
    {
        
      ////attr_reader :install_path
      ////attr_reader :link_path

        public string InstallPath
        {
            get
            {
                return installPath;
            }
        }

        public string LinkPath
        {
            get
            {
                return linkPath;
            }
        }


        private string installPath;
        private string linkPath;
        private string baseDir;
        private string name;
        private string version;
        private string checksum;
        private string blobstoreId;

      ////def initialize(spec)
      ////  unless spec.is_a?(Hash)
      ////    raise ArgumentError, "Invalid package spec, " +
      ////                         "Hash expected, #{spec.class} given"
      ////  end

      ////  %w(name version sha1 blobstore_id).each do |key|
      ////    if spec[key].nil?
      ////      raise ArgumentError, "Invalid spec, #{key} is missing"
      ////    end
      ////  end

      ////  @base_dir = Bosh::Agent::Config.base_dir
      ////  @name = spec["name"]
      ////  @version = spec["version"]
      ////  @checksum = spec["sha1"]
      ////  @blobstore_id = spec["blobstore_id"]

      ////  @install_path = File.join(@base_dir, "data", "packages",
      ////                            @name, @version)
      ////  @link_path = File.join(@base_dir, "packages", @name)
      ////end


        public Package(dynamic spec)
        {
            // TODO: check to se if any king of IDicrionty
            // if (spec is IDictionary<,>)
            Logger.Info("Initializing package :" + spec.ToString());

            var required = new string[] { "name", "version", "sha1", "blobstore_id" };
            foreach (var requiredKey in required)
            {
                if (spec[requiredKey] == null)
                {
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "Invalid spec. {0} is missing", requiredKey));
                }
            }

            baseDir = Config.BaseDir;
            name = spec["name"].Value;
            version = spec["version"].Value;
            checksum = spec["sha1"] != null ? spec["sha1"].Value : null;
            blobstoreId = spec["blobstore_id"].Value;
            installPath = Path.Combine(baseDir, "data", "packages", name, version);
            linkPath = Path.Combine(baseDir, "packages", name);
            Directory.CreateDirectory(Path.Combine(baseDir, "packages"));

        }

      ////def install_for_job(job)
      ////  fetch_package
      ////  create_symlink_in_job(job) if job
      ////rescue SystemCallError => e
      ////  install_failed("System call error: #{e.message}")
      ////end

        public void InstallForJob(Job job)
        {
            try
            {
                FetchPackage();
                if (job != null) CreateSymlinkInJob(job);
            }
            catch(Exception e)
            {
                throw new InstallationException("Install job error.", e);
            }
        }

      ////private

      ////def fetch_package
      ////  FileUtils.mkdir_p(File.dirname(@install_path))
      ////  FileUtils.mkdir_p(File.dirname(@link_path))

      ////  Bosh::Agent::Util.unpack_blob(@blobstore_id, @checksum, @install_path)
      ////  Bosh::Agent::Util.create_symlink(@install_path, @link_path)
      ////end

        private void FetchPackage()
        {
            Directory.CreateDirectory(installPath);

            Util.UnpackBlob(blobstoreId, checksum, installPath);
            Util.CreateSymLink(installPath, linkPath);
        }

      ////def create_symlink_in_job(job)
      ////  symlink_path = symlink_path_in_job(job)
      ////  FileUtils.mkdir_p(File.dirname(symlink_path))

      ////  Bosh::Agent::Util.create_symlink(@install_path, symlink_path)
      ////end

        private void CreateSymlinkInJob(Job job)
        {
            string symlinkPath = SymlinkPathInJob(job);
            Directory.CreateDirectory(new DirectoryInfo(symlinkPath).Parent.FullName);

            Util.CreateSymLink(installPath, symlinkPath);
        }

      ////def symlink_path_in_job(job)
      ////  File.join(job.install_path, "packages", @name)
      ////end

        private string SymlinkPathInJob(Job job)
        {
            return Path.Combine(job.InstallPath, "packages", name);
        }

      ////def install_failed(message)
      ////  raise InstallationError, "Failed to install package " +
      ////                           "'#{@name}': #{message}"
      ////end
    }
}
