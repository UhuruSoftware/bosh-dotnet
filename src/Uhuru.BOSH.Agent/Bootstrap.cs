// -----------------------------------------------------------------------
// <copyright file="Bootstrap.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Management;
    using System.Threading;
    using Newtonsoft.Json;
    using Uhuru.BOSH.Agent.Errors;
    using Uhuru.BOSH.Agent.Message;
    using Uhuru.BOSH.Agent.Providers;
    using Uhuru.Utilities;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Bootstrap
    {
        private IPlatform platform;
        private dynamic settings;

        public Bootstrap()
        {
            Logger.Info("Starting bootstrap");
            Directory.CreateDirectory(Path.Combine(BaseDir, "bosh"));
            this.platform = Config.Platform;
        }

        static string BaseDir
        {
            get
            {
                return Config.BaseDir;
            }
        }

        static string SotrePath
        {
            get
            {
                return Path.Combine(BaseDir, "store");
            }
        }

        public Dictionary<string, object> Configure()
        {
            Logger.Info("Configure instance");

            LoadSettings();

            if (this.settings != null)
            {
                UpdateIPTables();
                UpdatePasswords();
                UpdateAgentId();
                UpdateCredentials();
                UpdateHostname();
                UpdateMbus();
                UpdateBlobStore();
                SetupNetwork();
                Thread.Sleep(5000);
                UpdateTime();
                SetupDiskData();
                SetupTemp();

                MountPersistentDisk();
                HardenPermissions();

                ActivateWindows();
            }

            var ret = new Dictionary<string, object>();
            //ret["settings"] = this.settings;

            return ret;
        }

        private void ActivateWindows()
        {
            if (SettingNotNull("env", "windows", "product_key"))
            {
                if (!string.IsNullOrEmpty(this.settings["env"]["windows"]["product_key"].ToString()))
                {
                    Logger.Info("Activating Windows");
                    try
                    {
                        Util.ActivateWindows(this.settings["env"]["windows"]["product_key"].Value);
                        Logger.Info("Finished activating Windows");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed activating windows: {0}", ex.ToString());
                        throw new BoshException("Failed activating windows", ex);
                    }
                }
            }
        }

        private void SetupDiskData()
        {
            int dataDiskId = int.Parse(this.platform.GetDataDiskDeviceName, CultureInfo.InvariantCulture);

            string dataDir = Path.Combine(BaseDir, "data");
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            if (!DiskUtil.DiskHasPartition(dataDiskId))
            {
                Logger.Info("Creating partition on drive " + dataDiskId);
                if (DiskUtil.CreatePrimaryPartition(dataDiskId, "data") != 0)
                {
                    Logger.Error("Could not create partition on drive " + dataDiskId);
                }
            }

            if (!DiskUtil.IsMountPoint(dataDir))
            {
                if (DiskUtil.MountPartition(dataDiskId, dataDir) != 0)
                {
                    Logger.Error("Could not mount disk " + dataDiskId + " to " + dataDir);
                }
            }
            SetupDataSys();
        }

        private void UpdateTime()
        {
            Logger.Info("Updating time");
            if (this.settings["ntp"] == null)
            {
                Logger.Warning("no ntp-servers configured");
                return;
            }

            foreach (dynamic ntpServer in this.settings["ntp"])
            {
                Ntp ntp = Ntp.GetNtpOffset(Convert.ToString(ntpServer.Value).Trim());
                if (string.IsNullOrEmpty(ntp.Message))
                {
                    Logger.Info("Current time offset is :" + ntp.Offset + " to time server " + ntpServer);
                    Ntp.SetTime(ntp.Offset);
                    break;
                }
            }
        }

        private void SetupNetwork()
        {
            this.platform.SetupNetworking();
        }

        private void UpdateBlobStore()
        {
            Logger.Info("Setting blob store provider");
            Config.BlobstoreProvider = this.settings["blobstore"]["plugin"].Value;
            Logger.Info("Set blob store provider to : " + Config.BlobstoreProvider);

            // TODO: analyze if a merge is necessary
            Config.BlobstoreOptions = this.settings["blobstore"]["properties"];
        }

        private void UpdateMbus()
        {
            Config.MessageBus = this.settings["mbus"].Value;
            Logger.Info("Setting message bus endpoint to to " + Config.MessageBus);
        }

        private static void UpdateHostname()
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

        private void LoadSettings()
        {
            Logger.Info("Loading settings");

            this.settings = Config.Infrastructure().LoadSettings();
            Config.Settings = this.settings;

            Logger.Info("Loaded settings :" + this.settings.ToString());
        }

        ////    def iptables(cmd)
        ////      output = %x{iptables #{cmd} 2> /dev/null}
        ////      if $?.exitstatus != 0
        ////        raise Bosh::Agent::Error, "`iptables #{cmd}` failed"
        ////      end
        ////      output
        ////    end

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "cmd", Justification = "TODO: JIRA UH-1211"),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "TODO: JIRA UH-1211")]
        public void IPTables(string cmd)
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

        private void UpdateIPTables()
        {
            if (this.settings["iptables"] == null)
            {
                Logger.Info("No Ip table found in the config, skipping ip tables update");
                return;
            }

            throw new NotImplementedException();
        }

        private void UpdatePasswords()
        {
            if (this.settings["env"] != null && this.settings["env"].Count > 0)
            {
                this.platform.UpdatePasswords(this.settings);
            }
            else
            {
                Logger.Info("No ENV settings detects, skipping password update process");
            }

        }

        private void UpdateAgentId()
        {

            Logger.Info("Updating agent Id");
            Config.AgentId = this.settings["agent_id"].Value;
            Logger.Info("New agent id is :" + Config.AgentId);
        }

        private void UpdateCredentials()
        {
            if (this.settings["env"] != null)
            {
                if (this.settings["env"]["bosh"] != null)
                {
                    if (this.settings["env"]["bosh"]["credentials"] != null)
                    {
                        Config.Credentials = this.settings["env"]["bosh"]["credentials"];
                    }
                }
            }
            else
            {
                Logger.Info("No ENV settings detects, skipping credential update process");
            }
        }

        static long MemTotal()
        {
            ObjectQuery winQuery = new ObjectQuery("SELECT * FROM Win32_LogicalMemoryConfiguration");
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(winQuery))
            {

                foreach (ManagementObject item in searcher.Get())
                {
                    return (long)item["TotalPhysicalMemory"];
                }
            }
            return -1;
        }

        static void SetupDataSys()
        {
            string dataSysDirectory = Path.Combine(BaseDir, "data", "sys");

            // TODO: stefi: change directory permissions
            Directory.CreateDirectory(Path.Combine(BaseDir, "data", "sys", "log")); // create dir: /var/vcap/data/sys/log
            Directory.CreateDirectory(Path.Combine(BaseDir, "data", "sys", "run")); // create dir: /var/vcap/data/sys/run

            string sysDirectory = Path.Combine(BaseDir, "sys");

            Util.CreateSymLink(dataSysDirectory, sysDirectory);
        }

        static void SetupTemp()
        {
            string agentTmpDir = Path.Combine(BaseDir, "data", "tmp");
            if (!Directory.Exists(agentTmpDir))
            {
                Directory.CreateDirectory(agentTmpDir);
            }

            Environment.SetEnvironmentVariable("TMPDIR", agentTmpDir);
        }

        void MountPersistentDisk()
        {
            if (this.settings["disks"]["persistent"].Count > 0)
            {
                if (this.settings["disks"]["persistent"].Count > 1)
                {
                    throw new FatalBoshException("Fatal: more than one persistent disk on boot");
                }
                else
                {
                    string cid = ((Dictionary<string, int>)(JsonConvert.DeserializeObject<Dictionary<string, int>>(this.settings["disks"]["persistent"].ToString()))).Keys.FirstOrDefault();
                    string diskId = this.platform.LookupDiskByCid(cid);
                    if (!string.IsNullOrEmpty(cid))
                    {
                        this.platform.MountPersistentDisk(int.Parse(diskId, CultureInfo.InvariantCulture));
                    }
                }
            }
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "TODO: JIRA UH-1211")]
        void HardenPermissions()
        {
            // Most of the code doesn't apply to Windows systems.
            // Analyze what steps are required for Windows.
        }


        private bool SettingNotNull(params string[] keys)
        {
            dynamic hash = this.settings;
            for (int i = 0; i < keys.Count(); i++)
            {
                try
                {
                    if (hash[keys[i]] != null)
                    {
                        hash = hash[keys[i]];
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }
    }
}
