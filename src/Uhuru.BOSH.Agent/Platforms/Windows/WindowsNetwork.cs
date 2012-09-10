using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using Uhuru.Utilities;
using System.Globalization;

namespace Uhuru.BOSH.Agent.Platforms.Windows
{
    public class WindowsNetwork
    {
        public WindowsNetwork()
        {

        }

        public void SetupNetwork()
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

                    foreach (dynamic dns in network["dns"])
                    {
                        SetDNS(dns.Value, macAddress);
                    }
                    Logger.Info("Done setting network with mac" + macAddress);
                }

            }

        }

       private static Collection<string> GetExistingMacAddresses()
        {
            Logger.Info("Retrieving local machine MAC addresses");
            Collection<string> macAddresses = new Collection<string>();
            using (ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    if ((bool)objMO["IPEnabled"])
                    {
                        macAddresses.Add(objMO["MACAddress"].ToString().ToUpperInvariant());
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
