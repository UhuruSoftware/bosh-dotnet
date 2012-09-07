// -----------------------------------------------------------------------
// <copyright file="Bootstrap.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using Uhuru.Utilities;
    using System.Diagnostics;
    using System.Management;
    using Uhuru.BOSH.Agent.Providers;
    using Uhuru.BOSH.Agent.Message;
    using Uhuru.BOSH.Agent.Errors;
    using System.Globalization;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Bootstrap
    {
        private IPlatform platform;
        // private Infrastructure settings;
        private Dictionary<string, string> settings;

        ////    def initialize
        ////      FileUtils.mkdir_p(File.join(base_dir, 'bosh'))
        ////      @platform = Bosh::Agent::Config.platform
        ////    end

        public Bootstrap()
        {
            Logger.Info("Starting bootstrap");
            Directory.CreateDirectory(Path.Combine(this.BaseDir, "bosh"));
            this.platform = Config.Platform;
        }

        ////    def logger
        ////      Bosh::Agent::Config.logger
        ////    end

        object logger
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string BaseDir
        {
            get
            {
                return Config.BaseDir;
            }
        }

        string SotrePath
        {
            get
            {
                return Path.Combine(this.BaseDir, "store");
            }
        }

        ////    def configure
        ////      logger.info("Configuring instance")

        ////      load_settings
        ////      logger.info("Loaded settings: #{@settings.inspect}")

        ////      if @settings
        ////        update_iptables
        ////        update_passwords
        ////        update_agent_id
        ////        update_credentials
        ////        update_hostname
        ////        update_mbus
        ////        update_blobstore
        ////        setup_networking
        ////        update_time
        ////        setup_data_disk
        ////        setup_tmp

        ////        Bosh::Agent::Monit.setup_monit_user
        ////        Bosh::Agent::Monit.setup_alerts

        ////        mount_persistent_disk
        ////        harden_permissions
        ////      end
        ////      { "settings" => @settings }
        ////    end

        public Dictionary<string, object> Configure()
        {
            Logger.Info("Configure instance");

            LoadSettings();
            // Logger.Info("Loaded settings: {0}", this.settings.ToString());

            if (Config.Settings != null)
            {
                UpdateIptables();
                UpdatePasswords();
                UpdateAgentId();
                UpdateCredentials();
                UpdateHostname();
                UpdateMbus();
                UpdateBlobStore();
                SetupNetwork(); 
                UpdateTime();
                SetupDiskData();
                SetupTemp();

        ////Bosh::Agent::Monit.setup_monit_user
        ////Bosh::Agent::Monit.setup_alerts

                MountPersistentDisk();
                HardenPermissions();
            }
            //if (this.settings != null)
            //{
            //    UpdateIptables();
            //    UpdatePasswords();
            //    throw new NotImplementedException();
            //}

            var ret = new Dictionary<string, object>();
            // ret["settings"] = this.settings;

            return ret;
        }

        private void SetupDiskData()
        {   
            int dataDiskId = int.Parse(Config.Platform.GetDataDiskDeviceName(), CultureInfo.InvariantCulture);

            Logger.Info("Creating partition on drive " + dataDiskId);

            if (DiskUtil.CreatePrimaryPartition(dataDiskId, "data") != 0)
            {
                Logger.Error("Could not create partition on drive " + dataDiskId);
            }
            string dataDir = Path.Combine(BaseDir, "data");
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            if (DiskUtil.MountPartition(dataDiskId, dataDir) != 0)
            {
                Logger.Error("Could not mount disk " + dataDiskId + " to " + dataDir);
            }

            SetupDataSys();
        }

        private void UpdateTime()
        {
            Logger.Info("Updating time");
            if (Config.Settings["ntp"] == null)
            {
                Logger.Warning("no ntp-servers configured");
                return;
            }

            foreach (dynamic ntpServer in Config.Settings["ntp"])
            {
                Ntp ntp = Ntp.GetNtpOffset(ntpServer.Value);
                Logger.Info("Current time offset is :" + ntp.Offset + " to time server " + ntpServer); //TODO update time
            }
            
             
        }

        private void SetupNetwork()
        {
            Config.Platform.SettupNetworking();
        }

        private void UpdateBlobStore()
        {
            Logger.Info("Setting blob store provider");
            Config.BlobstoreProvider = Config.Settings["blobstore"]["plugin"].Value;
            Logger.Info("Set blob store provider to : " + Config.BlobstoreProvider);

            // TODO: analyze if a merge is necessary
            Config.BlobstoreOptions = Config.Settings["blobstore"]["properties"];
        }

        private void UpdateMbus()
        {
            Config.MessageBus = Config.Settings["mbus"].Value;
            Logger.Info("Setting message bus endpoint to to " + Config.MessageBus);
        }

        private void UpdateHostname()
        {
            string newHostName = Config.AgentId;

            if (Environment.MachineName != newHostName)
            {
                Logger.Info("Updating hostname to :" + Config.AgentId);

                string w32comp = "Win32_ComputerSystem.Name='" + System.Environment.MachineName + "'";
                using (ManagementObject computer = new ManagementObject(new ManagementPath(w32comp)))
                {
                    ManagementBaseObject param = computer.GetMethodParameters("Rename");
                    param["Name"] = newHostName;
                    ManagementBaseObject outParam = computer.InvokeMethod("Rename", param, null);
                    int returnCode = (int)(uint)outParam.Properties["returnValue"].Value;
                    if (returnCode != 0)
                    {
                        Logger.Error("Could not update hostname " + returnCode);
                    }
                }
            }
        }

        ////    def load_settings
        ////      @settings = Bosh::Agent::Config.infrastructure.load_settings
        ////      Bosh::Agent::Config.settings = @settings
        ////    end

        public void LoadSettings()
        {
            Logger.Info("Loading settings");

            Config.Settings = Config.Infrastructure.LoadSettings();

            Logger.Info("Loaded settings :" + Config.Settings.ToString());
        }

        ////    def iptables(cmd)
        ////      output = %x{iptables #{cmd} 2> /dev/null}
        ////      if $?.exitstatus != 0
        ////        raise Bosh::Agent::Error, "`iptables #{cmd}` failed"
        ////      end
        ////      output
        ////    end

        public void IpTables(string cmd)
        {
            // Consider using an alternative on windows, i.e. "ROUTE" command
            throw new NotImplementedException();
        }

        ////    def update_iptables
        ////      return unless rules = @settings['iptables']

        ////      if rules["drop_output"]
        ////        chain = "agent-filter"
        ////        append_chain = "-A OUTPUT -j #{chain}"

        ////        begin
        ////          iptables("-N #{chain}")
        ////        rescue
        ////          iptables("-F #{chain}")
        ////        end

        ////        unless iptables("-S").include?(append_chain)
        ////          iptables(append_chain)
        ////        end

        ////        rules["drop_output"].each do |dest|
        ////          rule = "-A #{chain} -d #{dest} -m owner ! --uid-owner root -j DROP"
        ////          iptables(rule)
        ////        end
        ////      end
        ////    end

        public void UpdateIptables()
        {
            if (Config.Settings["iptables"] == null)
            {
                Logger.Info("No Ip table found in the config, skipping ip tables update");
                return;
            }

            throw new NotImplementedException();
        }


        ////    def update_passwords
        ////      @platform.update_passwords(@settings) unless @settings["env"].nil?
        ////    end

        public void UpdatePasswords()
        {
            if (Config.Settings["env"] != null && Config.Settings["env"].Count > 0)
            {
                throw new NotImplementedException();
            }
            else 
            {
                Logger.Info("No env settings detects, skipping password update process");
            }
            
        }

        ////    def update_agent_id
        ////      Bosh::Agent::Config.agent_id = @settings["agent_id"]
        ////    end

        public void UpdateAgentId()
        {
            
            Logger.Info("Updating agent Id");
            Config.AgentId = Config.Settings["agent_id"].Value;
            Logger.Info("New agent id is :" + Config.AgentId);
        }

        ////    def update_credentials
        ////      env = @settings["env"]
        ////      if env && bosh_env = env["bosh"]
        ////        if bosh_env["credentials"]
        ////          Bosh::Agent::Config.credentials = bosh_env["credentials"]
        ////        end
        ////      end
        ////    end

        public void UpdateCredentials()
        {
            if (Config.Settings["env"] != null && Config.Settings["env"].Count > 0)
            {
                throw new NotImplementedException();
            }
            else
            {
                Logger.Info("No env settings detects, skipping credential update process");
            }
        }

        ////    def update_hostname
        ////      agent_id = @settings['agent_id']

        ////      template = ERB.new(ETC_HOST_TEMPATE, 0, '%<>-')
        ////      result = template.result(binding)
        ////      File.open('/etc/hosts', 'w') { |f| f.puts(result) }

        ////      `hostname #{agent_id}`
        ////      File.open('/etc/hostname', 'w') { |f| f.puts(agent_id) }
        ////    end

        ////    def update_mbus
        ////      Bosh::Agent::Config.mbus = @settings['mbus']
        ////    end

        ////    def update_blobstore
        ////      blobstore_settings = @settings["blobstore"]

        ////      blobstore_provider =  blobstore_settings["plugin"]
        ////      blobstore_options =  blobstore_settings["properties"]

        ////      Bosh::Agent::Config.blobstore_provider = blobstore_provider
        ////      Bosh::Agent::Config.blobstore_options.merge!(blobstore_options)
        ////    end

        ////    def setup_networking
        ////      Bosh::Agent::Config.platform.setup_networking
        ////    end

        ////    def update_time
        ////      ntp_servers = @settings['ntp'].join(" ")
        ////      unless ntp_servers.empty?
        ////        logger.info("Configure ntp-servers: #{ntp_servers}")
        ////        Bosh::Agent::Util.update_file(ntp_servers, '/var/vcap/bosh/etc/ntpserver')
        ////        output = `ntpdate #{ntp_servers}`
        ////        logger.info(output)
        ////      else
        ////        logger.warn("no ntp-servers configured")
        ////      end
        ////    end

        ////    def setup_data_disk
        ////      data_disk = Bosh::Agent::Config.platform.get_data_disk_device_name
        ////      swap_partition = "#{data_disk}1"
        ////      data_partition = "#{data_disk}2"

        ////      if File.blockdev?(data_disk)

        ////        if Dir["#{data_disk}[1-9]"].empty?
        ////          logger.info("Found unformatted drive")
        ////          logger.info("Partition #{data_disk}")
        ////          Bosh::Agent::Util.partition_disk(data_disk, data_sfdisk_input)

        ////          logger.info("Create swap and data partitions")
        ////          %x[mkswap #{swap_partition}]
        ////          %x[/sbin/mke2fs -t ext4 -j #{data_partition}]
        ////        end

        ////        logger.info("Swapon and mount data partition")
        ////        %x[swapon #{swap_partition}]
        ////        %x[mkdir -p #{base_dir}/data]

        ////        data_mount = "#{base_dir}/data"
        ////        unless Pathname.new(data_mount).mountpoint?
        ////          %x[mount #{data_partition} #{data_mount}]
        ////        end

        ////        setup_data_sys
        ////      end
        ////    end

        ////    def data_sfdisk_input
        ////      ",#{swap_size},S\n,,L\n"
        ////    end

        ////    def swap_size
        ////      data_disk = Bosh::Agent::Config.platform.get_data_disk_device_name
        ////      disk_size = Util.block_device_size(data_disk)
        ////      if mem_total > disk_size/2
        ////        return (disk_size/2)/1024
        ////      else
        ////        return mem_total/1024
        ////      end
        ////    end

        ////    def mem_total
        ////      # MemTotal:        3952180 kB
        ////      File.readlines('/proc/meminfo').first.split(/\s+/)[1].to_i
        ////    end

        long MemTotal()
        {
            ObjectQuery winQuery = new ObjectQuery("SELECT * FROM Win32_LogicalMemoryConfiguration");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(winQuery);

            foreach (ManagementObject item in searcher.Get())
            {
                return (long)item["TotalPhysicalMemory"];
            }
            return -1;
        }

        void SetupDataSys()
        {
            string dataSysDirectory = Path.Combine(BaseDir, "data", "sys");
            if(!Directory.Exists(dataSysDirectory))
            {
                Directory.CreateDirectory(dataSysDirectory);
            }
            foreach (string dir in new string[] { "log", "run" })
            {
                string path = Path.Combine(BaseDir, "data", "sys", dir);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            string sysDirectory = Path.Combine(BaseDir, "sys");
            Process p = Process.Start("cmd.exe", String.Format(CultureInfo.InvariantCulture, "/c mklink /D {0} {1}", sysDirectory, dataSysDirectory));
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                Logger.Error(String.Format(CultureInfo.InvariantCulture, "Failed creating symbolic link between {0} and {1}", sysDirectory, dataSysDirectory));
            }
        }

        ////    def setup_tmp
        ////      # use a custom TMPDIR for agent itself
        ////      agent_tmp_dir = File.join(base_dir, 'data', 'tmp')
        ////      FileUtils.mkdir_p(agent_tmp_dir)
        ////      ENV["TMPDIR"] = agent_tmp_dir

        ////      # first time: for /tmp on the root fs
        ////      tmp_permissions

        ////      unless Pathname.new('/tmp').mountpoint?
        ////        tmp_size = 128
        ////        root_tmp = File.join(base_dir, 'data', 'root_tmp')

        ////        # If it's not mounted on /tmp - we don't care - blow it away
        ////        %x[/usr/bin/truncate -s #{tmp_size}M #{root_tmp}]
        ////        %x[chmod 0700 #{root_tmp}]
        ////        %x[mke2fs -t ext4 -m 1 -F #{root_tmp}]

        ////        %x[mount -t ext4 -o loop #{root_tmp} /tmp]

        ////        # 2nd time for the new /tmp mount
        ////        tmp_permissions
        ////      end
        ////    end

        void SetupTemp()
        {
            string agentTmpDir = Path.Combine(BaseDir, "data", "tmp");
            if (!Directory.Exists(agentTmpDir))
            {
                Directory.CreateDirectory(agentTmpDir);
            }

            Logger.Warning("TODO Not Implemented");
            // complate 
        }

        ////    def tmp_permissions
        ////      %x[chown root:#{BOSH_APP_USER} /tmp]
        ////      %x[chmod 0770 /tmp]
        ////      %x[chmod 0700 /var/tmp]
        ////    end

        void TempPermissiosn()
        {
            // todo: maybe not necessary for windows
        }


        ////    def mount_persistent_disk
        ////      if @settings['disks']['persistent'].keys.size > 1
        ////        # hell on earth
        ////        raise Bosh::Agent::FatalError, "Fatal: more than one persistent disk on boot"
        ////      else
        ////        cid = @settings['disks']['persistent'].keys.first
        ////        if cid
        ////          Bosh::Agent::Config.platform.mount_persistent_disk(cid)
        ////        end
        ////      end
        ////    end

        void MountPersistentDisk()
        {
            if (Config.Settings["disks"]["persistent"].Count > 0)
            {
                if (Config.Settings["disks"]["persistent"].Count > 1)
                {
                    throw new FatalBoshException("Fatal: more than one persistent disk on boot");
                }
                else
                {
                    string storeDiskId = Config.Settings["disks"]["persistent"][0].Value;
                    if (!string.IsNullOrEmpty(storeDiskId))
                    {
                        Config.Platform.MountPersistentDisk(int.Parse(storeDiskId, CultureInfo.InvariantCulture));
                    }
                }
            }
            
            // todo: implement after the settings are classes are stabilized
        }

        ////    def harden_permissions
        ////      setup_cron_at_allow

        ////      # use this instead of removing vcap from the cdrom group, as there
        ////      # is no way to easily do that from the command line
        ////      root_only_rw = %w{
        ////        /dev/sr0
        ////      }
        ////      root_only_rw.each do |path|
        ////        %x[chmod 0660 #{path}]
        ////        %x[chown root:root #{path}]
        ////      end

        ////      root_app_user_rw = %w{
        ////        /dev/log
        ////      }
        ////      root_app_user_rw.each do |path|
        ////        %x[chmod 0660 #{path}]
        ////        %x[chown root:#{BOSH_APP_USER} #{path}]
        ////      end

        ////      root_app_user_rwx = %w{
        ////        /dev/shm
        ////        /var/lock
        ////      }
        ////      root_app_user_rwx.each do |path|
        ////        %x[chmod 0770 #{path}]
        ////        %x[chown root:#{BOSH_APP_USER} #{path}]
        ////      end

        ////      root_rw_app_user_read = %w{
        ////        /etc/cron.allow
        ////        /etc/at.allow
        ////      }
        ////      root_rw_app_user_read.each do |path|
        ////        %x[chmod 0640 #{path}]
        ////        %x[chown root:#{BOSH_APP_USER} #{path}]
        ////      end

        ////      no_other_read = %w{
        ////        /data/vcap/data
        ////        /data/vcap/store
        ////      }
        ////      no_other_read.each do |path|
        ////        %[chmod o-r #{path}]
        ////      end

        ////    end

        void HardenPermissions()
        {
            // Most of the code doesn't apply to Windows systems.
            // Analyze what steps are required for Windows.
        }

        ////    def setup_cron_at_allow
        ////      %w{/etc/cron.allow /etc/at.allow}.each do |file|
        ////        File.open(file, 'w') { |fh| fh.puts(BOSH_APP_USER) }
        ////      end
        ////    end
        void SetupCronToAllow()
        {
            // Analyze what steps are required for Windows.
        }

        // todo: use C# templateing system. Google is not aware of an ERB implementation in/for C#

        ////    ETC_HOST_TEMPATE = <<TEMPLATE
        ////127.0.0.1 localhost <%= agent_id %>

        ////# The following lines are desirable for IPv6 capable hosts
        ////::1 localhost ip6-localhost ip6-loopback <%= agent_id %>
        ////fe00::0 ip6-localnet
        ////ff00::0 ip6-mcastprefix
        ////ff02::1 ip6-allnodes
        ////ff02::2 ip6-allrouters
        ////ff02::3 ip6-allhosts
        ////TEMPLATE
    }
}
