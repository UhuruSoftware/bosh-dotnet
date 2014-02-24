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


        public void InstallForJob(Job job)
        {
            try
            {
                Helpers.FetchBitsAndSymlink(installPath, linkPath, blobstoreId, checksum);
                if (job != null) CreateSymlinkInJob(job);
            }
            catch(Exception e)
            {
                throw new InstallationException("Install job error.", e);
            }
        }

        public void PrepareForInstall()
        {
            Helpers.FetchBits(installPath, blobstoreId, checksum);
        }

     

        private void CreateSymlinkInJob(Job job)
        {
            string symlinkPath = SymlinkPathInJob(job);
            Directory.CreateDirectory(new DirectoryInfo(symlinkPath).Parent.FullName);

            Util.CreateSymLink(installPath, symlinkPath);
        }

     

        private string SymlinkPathInJob(Job job)
        {
            return Path.Combine(job.InstallPath, "packages", name);
        }

    }
}
