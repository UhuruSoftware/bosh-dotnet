﻿// -----------------------------------------------------------------------
// <copyright file="Util.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Uhuru.BOSH.BlobstoreClient;
    using Uhuru.BOSH.BlobstoreClient.Clients;
    using System.IO;
    using Uhuru.Utilities;
    using System.Security.Cryptography;
    using Uhuru.BOSH.Agent.Errors;
    using System.Diagnostics;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Util", Justification = "FxCop bug"), 
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", Justification = "Keeping name the same as VMWare's code")]
    public class Util
    {
    ////class BindingHelper
    ////  attr_reader :name
    ////  attr_reader :index
    ////  attr_reader :properties
    ////  attr_reader :spec

    ////  def initialize(name, index, properties, spec)
    ////    @name = name
    ////    @index = index
    ////    @properties = properties
    ////    @spec = spec
    ////  end

    ////  def get_binding
    ////    binding
    ////  end
    ////end

    ////class << self
    ////  def base_dir
    ////    Bosh::Agent::Config.base_dir
    ////  end

    ////  def logger
    ////    Bosh::Agent::Config.logger
    ////  end

    ////  def unpack_blob(blobstore_id, sha1, install_path)
    ////    bsc_options = Bosh::Agent::Config.blobstore_options
    ////    bsc_provider = Bosh::Agent::Config.blobstore_provider
    ////    blobstore_client = Bosh::Blobstore::Client.create(bsc_provider, bsc_options)

    ////    data_tmp = File.join(base_dir, 'data', 'tmp')
    ////    FileUtils.mkdir_p(data_tmp)

    ////    begin
    ////      tf = Tempfile.open(blobstore_id, data_tmp)
    ////      logger.info("Retrieving blob: #{blobstore_id}")

    ////      blobstore_client.get(blobstore_id, tf)
    ////      logger.info("Done retrieving blob")

    ////      tf.flush
    ////      blob_data_file = tf.path

    ////      logger.info("creating #{install_path}")
    ////      FileUtils.mkdir_p(install_path)

    ////      blob_sha1 = Digest::SHA1.file(blob_data_file).hexdigest
    ////      logger.info("hexdigest of #{blob_data_file}")

    ////      unless blob_sha1 == sha1
    ////        raise Bosh::Agent::MessageHandlerError, "Expected sha1: #{sha1}, Downloaded sha1: #{blob_sha1}"
    ////      end

    ////      logger.info("Installing to: #{install_path}")
    ////      Dir.chdir(install_path) do
    ////        output = `tar --no-same-owner -zxvf #{blob_data_file}`
    ////        raise Bosh::Agent::MessageHandlerError.new(
    ////          "Failed to unpack blob", output) unless $?.exitstatus == 0
    ////      end
    ////    rescue Exception => e
    ////      logger.info("Failure unpacking blob: #{e.inspect} #{e.backtrace}")
    ////      raise e
    ////    ensure
    ////      if tf
    ////        tf.close
    ////        tf.unlink
    ////      end
    ////    end

    ////  end

    ////  # Provide binding for a particular variable
    ////  def config_binding(config)
    ////    name = config["job"] ? config["job"]["name"] : nil
    ////    index = config["index"]
    ////    properties = (config["properties"] || {}).to_openstruct
    ////    spec = config.to_openstruct
    ////    BindingHelper.new(name, index, properties, spec).get_binding
    ////  end

    ////  def partition_disk(dev, sfdisk_input)
    ////    if File.blockdev?(dev)
    ////      sfdisk_cmd = "echo \"#{sfdisk_input}\" | sfdisk -uM #{dev}"
    ////      output = %x[#{sfdisk_cmd}]
    ////      unless $? == 0
    ////        logger.info("failed to parition #{dev}")
    ////        logger.info(output)
    ////      end
    ////    end
    ////  end

    ////  def block_device_size(block_device)
    ////    unless File.blockdev?(block_device)
    ////      raise Bosh::Agent::MessageHandlerError, "Not a blockdevice"
    ////    end

    ////    child = POSIX::Spawn::Child.new('/sbin/sfdisk', '-s', block_device)
    ////    result = child.out
    ////    unless result.match(/\A\d+\Z/) && child.status.exitstatus == 0
    ////      raise Bosh::Agent::MessageHandlerError,
    ////        "Unable to determine disk size"
    ////    end
    ////    result.to_i
    ////  end

    ////  def run_hook(hook, job_template_name)
    ////    hook_file = File.join(base_dir, 'jobs', job_template_name, 'bin', hook)

    ////    unless File.exists?(hook_file)
    ////      return nil
    ////    end

    ////    unless File.executable?(hook_file)
    ////      raise Bosh::Agent::MessageHandlerError, "`#{hook}' hook for `#{job_template_name}' job is not an executable file"
    ////    end

    ////    env = {
    ////      'PATH' => '/usr/sbin:/usr/bin:/sbin:/bin',
    ////    }

    ////    child = POSIX::Spawn::Child.new(env, hook_file, :unsetenv_others => true)

    ////    result = child.out
    ////    logger.info("Hook #{hook} for job #{job_template_name}: #{result}")

    ////    unless child.status.exitstatus == 0
    ////      exception_message = "Hook #{hook} for #{job_template_name} failed "
    ////      exception_message += "(exit: #{child.status.exitstatus}) "
    ////      exception_message += " stderr: #{child.err}, stdout: #{result}"
    ////      logger.info(exception_message)

    ////      raise Bosh::Agent::MessageHandlerError, exception_message
    ////    end
    ////    result
    ////  end

    ////  def create_symlink(src, dst)
    ////    # FileUtils doesn have 'no-deference' for links - causing ln_sf to
    ////    # attempt to create target link in dst rather than to overwrite it.
    ////    # BROKEN: FileUtils.ln_sf(src, dst)
    ////    out = %x(ln -nsf #{src} #{dst} 2>&1)
    ////    unless $?.exitstatus == 0
    ////      raise Bosh::Agent::MessageHandlerError,
    ////            "Failed to link '#{src}' to '#{dst}': #{out}"
    ////    end
    ////  end

    ////  # Poor mans idempotency
    ////  def update_file(data, path)
    ////    name = File.basename(path)
    ////    dir = File.dirname(path)

    ////    if_tmp_file = Tempfile.new(name, dir)
    ////    if_tmp_file.write(data)
    ////    if_tmp_file.flush

    ////    old = nil
    ////    begin
    ////      old = Digest::SHA1.file(path).hexdigest
    ////    rescue Errno::ENOENT
    ////      logger.debug("missing file: #{path}")
    ////    end
    ////    new = Digest::SHA1.file(if_tmp_file.path).hexdigest

    ////    updated = false
    ////    unless old == new
    ////      FileUtils.cp(if_tmp_file.path, path)
    ////      updated = true
    ////    end
    ////    updated
    ////  ensure
    ////    if if_tmp_file
    ////       if_tmp_file.close
    ////       FileUtils.rm_rf(if_tmp_file.path)
    ////    end
    ////  end

    ////end
        internal static dynamic conigBinding(dynamic spec)
        {
            throw new NotImplementedException();
        }

        internal static void PackBlob(string directory, string fileName)
        {
            FileArchive.ZipFile(directory, fileName);
        }

        internal static void UnpackBlob(string blobstoreId, string checksum, string installPath)
        {
            Logger.Info("Retrieving blob: ", blobstoreId);

            IClient blobstoreClient = Blobstore.CreateClient(Config.BlobstoreProvider, Config.BlobstoreOptions);

            FileInfo fileInfo = new FileInfo(Path.Combine(Config.BaseDir, "data", "tmp", blobstoreId + ".tgz"));

            try
            {
                if (!Directory.Exists(fileInfo.Directory.ToString()))
                    Directory.CreateDirectory(fileInfo.Directory.ToString());

                blobstoreClient.Get(blobstoreId, fileInfo);

                string blobDataFile = fileInfo.FullName;

                Logger.Info("Done retrieving blob");

                if (!Directory.Exists(installPath))
                {
                    Logger.Info("Creating ", installPath);
                    Directory.CreateDirectory(installPath);
                }

                string blobSHA1;
                using (FileStream fs = fileInfo.Open(FileMode.Open))
                {
                    SHA1 sha = new SHA1CryptoServiceProvider();
                    blobSHA1 = BitConverter.ToString(sha.ComputeHash(fs)).Replace("-","");
                }
                if (String.Compare(blobSHA1,checksum, true) != 0)
                {
                    throw new MessageHandlerException(String.Format("Expected sha1: {0}, Downloaded sha1: {1}", checksum, blobSHA1));
                }

                string tarFile = Path.ChangeExtension(blobDataFile, "tar");
                FileArchive.UnzipFile(fileInfo.DirectoryName, blobDataFile);
                if (File.Exists(tarFile))
                    FileArchive.UnzipFile(installPath, tarFile);
                else
                    FileArchive.UnzipFile(installPath, blobDataFile);
            }
            catch (Exception ex)
            {
                Logger.Error("Failure unpacking blob.", ex.ToString());
                throw ex;
            }
        }

        internal static void CreateSymLink(string installPath, string linkPath)
        {
            Process p = Process.Start("cmd.exe", String.Format("/c mklink /D {0} {1}", linkPath, installPath));
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                Logger.Error(String.Format("Failed creating symbolic link between {0} and {1}", installPath, linkPath));
            }
        }
    }
}