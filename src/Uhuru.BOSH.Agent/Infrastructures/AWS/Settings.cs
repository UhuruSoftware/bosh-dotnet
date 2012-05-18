// -----------------------------------------------------------------------
// <copyright file="Settings.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Infrastructures.AWS
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
    ////VIP_NETWORK_TYPE = "vip"
    ////DHCP_NETWORK_TYPE = "dynamic"
    ////AUTHORIZED_KEYS = File.join("/home/", BOSH_APP_USER, ".ssh/authorized_keys")

    ////def initialize
    ////  @settings_file = Bosh::Agent::Config.settings_file
    ////end

    ////def logger
    ////  Bosh::Agent::Config.logger
    ////end

    ////def authorized_keys
    ////  AUTHORIZED_KEYS
    ////end

    ////def setup_openssh_key
    ////  public_key = Infrastructure::Aws::Registry.get_openssh_key
    ////  if public_key.nil? || public_key.empty?
    ////    return
    ////  end
    ////  FileUtils.mkdir_p(File.dirname(authorized_keys))
    ////  File.open(authorized_keys, "w") { |f| f.write(public_key) }
    ////end

    ////def load_settings
    ////  setup_openssh_key
    ////  settings = Infrastructure::Aws::Registry.get_settings
    ////  settings_json = Yajl::Encoder.encode(settings)
    ////  File.open(@settings_file, 'w') { |f| f.write(settings_json) }
    ////  Bosh::Agent::Config.settings = settings
    ////end

    ////def get_network_settings(network_name, properties)
    ////  unless properties["type"] &&
    ////         [VIP_NETWORK_TYPE, DHCP_NETWORK_TYPE].include?(properties["type"])
    ////    raise Bosh::Agent::StateError, "Unsupported network #{properties["type"]}"
    ////  end

    ////  # Nothing to do for "vip" networks
    ////  return nil if properties["type"] == VIP_NETWORK_TYPE

    ////  sigar = Sigar.new
    ////  net_info = sigar.net_info
    ////  ifconfig = sigar.net_interface_config(net_info.default_gateway_interface)

    ////  properties = {}
    ////  properties["ip"] = ifconfig.address
    ////  properties["netmask"] = ifconfig.netmask
    ////  properties["dns"] = []
    ////  properties["dns"] << net_info.primary_dns if net_info.primary_dns &&
    ////                                               !net_info.primary_dns.empty?
    ////  properties["dns"] << net_info.secondary_dns if net_info.secondary_dns &&
    ////                                                 !net_info.secondary_dns.empty?
    ////  properties["gateway"] = net_info.default_gateway
    ////  properties
    ////end
    }
}
