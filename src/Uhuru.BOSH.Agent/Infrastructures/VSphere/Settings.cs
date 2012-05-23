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

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Settings
    {
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
        private void LoadSettings()
        {

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

        private void LoadCDRomSettings()
        {

        }

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

    ////def mount_cdrom
    ////  output = `mount /dev/cdrom #{@cdrom_settings_mount_point} 2>&1`
    ////  raise Bosh::Agent::LoadSettingsError,
    ////    "Failed to mount settings on #{@cdrom_settings_mount_point}: #{output}" unless $?.exitstatus == 0
    ////end

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

    ////def eject_cdrom
    ////  `eject /dev/cdrom`
    ////end
    }
}
