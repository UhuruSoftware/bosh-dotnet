using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management;
using System.Threading;
using Newtonsoft.Json;
using Uhuru.Utilities;

namespace Uhuru.BOSH.Agent.Platforms.Windows
{
    public static class WindowsNetwork
    {
        public static void SetupNetwork()
        {
            
            //List<string> macAddresses = GetMacAddresses().ToList();

            dynamic networks = Config.Settings["networks"];
            foreach (dynamic net in networks)
            {
                dynamic network = net.Value;

                string macAddress = network["mac"].Value;
                Collection<string> macAddreses = GetExistingMacAddresses();

                if (macAddreses.Contains(macAddress.ToUpperInvariant()))
                {
                    Logger.Info("Trying to configure the NIC with the mac: " + macAddress);

                    string ip = network["ip"].Value;
                    string netmask = network["netmask"].Value;
                    SetIP(ip, netmask, macAddress);

                    string gateway = network["gateway"].Value;
                    SetGateway(gateway, macAddress);

                    string dnsServers = string.Join(",", JsonConvert.DeserializeObject<ICollection<string>>(network["dns"].ToString()));

                    SetDNS(dnsServers, macAddress);

                    Logger.Info("Done setting network with mac" + macAddress);
                }
            }
        }

       private static Collection<string> GetExistingMacAddresses()
        {
            Logger.Info("Retrieving local machine MAC addresses");
            Collection<string> macAddresses = new Collection<string>();
            int retryCount = 30;

            using (ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                while (retryCount > 0)
                {
                    ManagementObjectCollection objMOC = objMC.GetInstances();

                    foreach (ManagementObject objMO in objMOC)
                    {
                        if ((bool)objMO["IPEnabled"])
                        {
                            macAddresses.Add(objMO["MACAddress"].ToString().ToUpperInvariant());
                            retryCount = 0;
                        }
                    }
                    if (macAddresses.Count == 0)
                    {
                        Thread.Sleep(5000);
                        retryCount--;
                    }
                }
            }
            Logger.Info(string.Format(CultureInfo.InvariantCulture, "Found {0} MAC addresses ", macAddresses.Count.ToString(CultureInfo.InvariantCulture)));
            return macAddresses;
        }


        private static void SetIP(string ipAddress, string subnetMask, string macAddress)
        {
            using (ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    if ((bool)objMO["IPEnabled"])
                    {
                        if (objMO["MACAddress"].ToString().ToUpperInvariant().Equals(macAddress.ToUpperInvariant()))
                        {
                            Logger.Info(string.Format(CultureInfo.InvariantCulture, "Configuring new ip {0} with subnet mask {1} on mac {2}", ipAddress, subnetMask, macAddress));

                            try
                            {
                                ManagementBaseObject newIP =
                                    objMO.GetMethodParameters("EnableStatic");

                                newIP["IPAddress"] = new string[] { ipAddress };
                                newIP["SubnetMask"] = new string[] { subnetMask };

                                objMO.InvokeMethod("EnableStatic", newIP, null);
                                Logger.Info("Done setting ip address");
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set's a new Gateway address of the local machine
        /// </summary>
        /// <param name="gateway">The Gateway IP Address</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        private static void SetGateway(string gateway, string macAddress)
        {
            using (ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    if ((bool)objMO["IPEnabled"])
                    {
                        if (objMO["MACAddress"].ToString().ToUpperInvariant().Equals(macAddress.ToUpperInvariant()))
                        {
                            Logger.Info(string.Format(CultureInfo.InvariantCulture, "Setting gateway {0} on mac {1}", gateway, macAddress));
                            try
                            {
                                ManagementBaseObject newGateway =
                                    objMO.GetMethodParameters("SetGateways");

                                newGateway["DefaultIPGateway"] = new string[] { gateway };
                                newGateway["GatewayCostMetric"] = new int[] { 1 };

                                objMO.InvokeMethod("SetGateways", newGateway, null);
                                Logger.Info("Done setting ip address");
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set's the DNS Server of the local machine
        /// </summary>
        /// <param name="NIC">NIC address</param>
        /// <param name="DNS">DNS server address</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        private static void SetDNS(string DNS, string macAddress)
        {
            using (ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    if ((bool)objMO["IPEnabled"])
                    {
                        if (objMO["MACAddress"].ToString().ToUpperInvariant().Equals(macAddress.ToUpperInvariant()))
                        {
                            Logger.Info(string.Format(CultureInfo.InvariantCulture, "Setting DNS {0} for mac {1}", DNS, macAddress));
                            try
                            {
                                ManagementBaseObject newDNS =
                                    objMO.GetMethodParameters("SetDNSServerSearchOrder");
                                newDNS["DNSServerSearchOrder"] = DNS.Split(',');
                                objMO.InvokeMethod("SetDNSServerSearchOrder", newDNS, null);
                                Logger.Info("Done setting DNS address");
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Set's WINS of the local machine
        /// </summary>
        /// <param name="NIC">NIC Address</param>
        /// <param name="priWINS">Primary WINS server address</param>
        /// <param name="secWINS">Secondary WINS server address</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        private static void SetWINS(string priWINS, string secWINS, string macAddress)
        {
            using (ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    if ((bool)objMO["IPEnabled"])
                    {
                        if (objMO["MACAddress"].ToString().ToUpperInvariant().Equals(macAddress.ToUpperInvariant()))
                        {
                            try
                            {
                                ManagementBaseObject wins =
                                objMO.GetMethodParameters("SetWINSServer");
                                wins.SetPropertyValue("WINSPrimaryServer", priWINS);
                                wins.SetPropertyValue("WINSSecondaryServer", secWINS);

                                objMO.InvokeMethod("SetWINSServer", wins, null);
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }
                }
            }
        }
    }
}
