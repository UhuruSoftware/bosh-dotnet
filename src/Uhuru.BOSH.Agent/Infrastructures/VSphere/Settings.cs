// -----------------------------------------------------------------------
// <copyright file="Settings.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Infrastructures.VSphere
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Uhuru.Utilities;
    using Newtonsoft.Json;
    using System.IO;
    using System.Management;
    using Uhuru.BOSH.Agent.Errors;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Settings
    {
        string CdromSettingsMountPoint
        {
            get
            {
                return Path.Combine(Config.BaseDir, "bosh", "settings");
            }
        }
     


    ////def initialize
    ////  logger                      = Bosh::Agent::Config.logger
    ////  base_dir                    = Bosh::Agent::Config.base_dir
    ////  @settings_file              = Bosh::Agent::Config.settings_file
    ////  @cdrom_settings_mount_point = File.join(base_dir, 'bosh', 'settings')
    ////end
        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        public Settings()
        {

        }

    ////def load_settings
    ////  begin
    ////    load_cdrom_settings
    ////  rescue LoadSettingsError
    ////    if File.exist?(@settings_file)
    ////      load_settings_file(@settings_file)
    ////    else
    ////      raise LoadSettingsError, "No cdrom or cached settings.json"
    ////    end
    ////  end
    ////  Bosh::Agent::Config.settings = @settings
    ////end
        public dynamic LoadSettings()
        {
            Logger.Info("Loading vsphere settings");
            LoadCDRomSettings();

            Logger.Info("Loading settings file");
            //YamlNode root = null;
            string fileContent = File.ReadAllText(Config.SettingsFile);
            //using (TextReader textReader = new StreamReader(Config.SettingsFile))
            //{
            //    YamlNode[] nodes = YamlNode.FromYaml(textReader);
            //    root = nodes[0];
            //}
            return JsonConvert.DeserializeObject(fileContent);
        }

    ////def load_cdrom_settings
    ////  check_cdrom
    ////  create_cdrom_settings_mount_point
    ////  mount_cdrom

    ////  env_file = File.join(@cdrom_settings_mount_point, 'env')

    ////  begin
    ////    settings_json = File.read(env_file)
    ////    @settings = Yajl::Parser.new.parse(settings_json)

    ////    File.open(@settings_file, 'w') { |f| f.write(settings_json) }
    ////  rescue
    ////    raise Bosh::Agent::LoadSettingsError, 'Failed to read/write env/settings.json'
    ////  ensure
    ////    umount_cdrom
    ////    eject_cdrom
    ////  end
    ////end

    ////def check_cdrom
    ////  begin
    ////    File.read('/dev/cdrom', 0)
    ////  rescue Errno::ENOMEDIUM # 1.8: Errno::E123
    ////    raise Bosh::Agent::LoadSettingsError, 'No bosh cdrom env'
    ////  end
    ////end
        private void CheckCDRom()
        {

        }

    ////def create_cdrom_settings_mount_point
    ////  FileUtils.mkdir_p(@cdrom_settings_mount_point)
    ////  FileUtils.chmod(0700, @cdrom_settings_mount_point)
    ////end

        private void CreateMountPointDirectory()
        {
            if (!Directory.Exists(CdromSettingsMountPoint))
            {
                Directory.CreateDirectory(CdromSettingsMountPoint);
            }
        }

    ////def mount_cdrom
    ////  output = `mount /dev/cdrom #{@cdrom_settings_mount_point} 2>&1`
    ////  raise Bosh::Agent::LoadSettingsError,
    ////    "Failed to mount settings on #{@cdrom_settings_mount_point}: #{output}" unless $?.exitstatus == 0
    ////end

        private void MountCDROM(string volumeName)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "mountvol.exe";
            info.Arguments = String.Format("{0} {1}", CdromSettingsMountPoint, volumeName);
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;

            int retryCount = 10;
            while (retryCount > 0)
            {
                Process p = new Process();
                p.StartInfo = info;
                p.Start();
                p.WaitForExit(60000);
                if (!p.HasExited)
                {
                    p.Kill();
                    break;
                }
                else
                {
                    Logger.Debug(p.StandardOutput.ReadToEnd());
                    if (p.ExitCode != 0)
                    {
                        retryCount--;
                        Thread.Sleep(1000);
                        continue;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            throw new LoadSettingsException("Failed to mount settings on: " + CdromSettingsMountPoint);
        }

    ////def load_settings_file(settings_file)
    ////  if File.exists?(settings_file)
    ////    settings_json = File.read(settings_file)
    ////    @settings = Yajl::Parser.new.parse(settings_json)
    ////  else
    ////    raise LoadSettingsError, "No settings file #{settings_file}"
    ////  end
    ////end

    ////def umount_cdrom
    ////  `umount #{@cdrom_settings_mount_point} 2>&1`
    ////end

        private void UnmountCDROM(string volumeName)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "mountvol.exe";
            info.Arguments = String.Format("{0} /D", CdromSettingsMountPoint);
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;

            int retryCount = 10;
            while (retryCount > 0)
            {
                Process p = new Process();
                p.StartInfo = info;
                p.Start();
                p.WaitForExit(60000);
                if (!p.HasExited)
                {
                    p.Kill();
                    break;
                }
                else
                {
                    Logger.Debug(p.StandardOutput.ReadToEnd());
                    if (p.ExitCode != 0)
                    {
                        retryCount--;
                        Thread.Sleep(1000);
                        continue;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            throw new LoadSettingsException("Failed to mount settings on: " + CdromSettingsMountPoint);
        }

    ////def eject_cdrom
    ////  `eject /dev/cdrom`
    ////end

        private void EjectCDROM()
        {
        }

        private void LoadCDRomSettings()
        {
            Logger.Info("Loading cdrom settings");

            string volumeName = GetCdRomVolumeName();

            if (volumeName.Equals(string.Empty, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new LoadSettingsException("No bosh cdrom env");
            }

            CreateMountPointDirectory();
            MountCDROM(volumeName);

            string envPath = Path.Combine(CdromSettingsMountPoint, "ENV");

            try
            {
                if (File.Exists(Config.SettingsFile))
                {
                    File.Delete(Config.SettingsFile);
                }
                File.Copy(envPath, Config.SettingsFile, true);

                File.SetAttributes(Config.SettingsFile, FileAttributes.Normal);
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
                throw new LoadSettingsException("Failed to read/write env/settings.json");
            }
            finally
            {
                UnmountCDROM(volumeName);
            }
        }

        private string GetCdRomVolumeName()
        {
            string volumeName = string.Empty;
            using (ManagementClass volume = new ManagementClass("Win32_Volume"))
            {
                ManagementObjectCollection moc = volume.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if (mo["DriveType"] != null)
                    {
                        if (mo["DriveType"].ToString().Equals("5", StringComparison.InvariantCultureIgnoreCase))
                        {
                            volumeName = mo["DeviceID"].ToString();
                            break;
                        }
                    }
                }
            }
            return volumeName;
        }

    }
}
