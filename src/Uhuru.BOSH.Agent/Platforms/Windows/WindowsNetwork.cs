using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using Uhuru.Utilities;

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

                if (macAddreses.Contains(macAddress.ToLower()))
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

       private Collection<string> GetExistingMacAddresses()
        {
            Logger.Info("Retrieving local machine MAC addresses");
            Collection<string> macAddresses = new Collection<string>();
            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    macAddresses.Add(objMO["MACAddress"].ToString().ToLower());
                }
            }
           Logger.Info(string.Format("Found {0} MAC addresses ", macAddresses.Count.ToString()));
           return macAddresses;
        }


        private void SetIP(string ipAddress, string subnetMask, string macAddress)
        {
            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    if (objMO["MACAddress"].ToString().ToLower().Equals(macAddress.ToLower()))
                    {
                        Logger.Info(string.Format("Configuring new ip {0} with subnet mask {1} on mac {2}", ipAddress, subnetMask, macAddress));

                        try
                        {
                            ManagementBaseObject setIP;
                            ManagementBaseObject newIP =
                                objMO.GetMethodParameters("EnableStatic");

                            newIP["IPAddress"] = new string[] { ipAddress };
                            newIP["SubnetMask"] = new string[] { subnetMask };

                            setIP = objMO.InvokeMethod("EnableStatic", newIP, null);
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

        /// <summary>
        /// Set's a new Gateway address of the local machine
        /// </summary>
        /// <param name="gateway">The Gateway IP Address</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        private void SetGateway(string gateway, string macAddress)
        {
            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    if (objMO["MACAddress"].ToString().ToLower().Equals(macAddress.ToLower()))
                    {
                        Logger.Info(string.Format("Setting gateway {0} on mac {1}", gateway, macAddress));
                        try
                        {
                            ManagementBaseObject setGateway;
                            ManagementBaseObject newGateway =
                                objMO.GetMethodParameters("SetGateways");

                            newGateway["DefaultIPGateway"] = new string[] { gateway };
                            newGateway["GatewayCostMetric"] = new int[] { 1 };

                            setGateway = objMO.InvokeMethod("SetGateways", newGateway, null);
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

        /// <summary>
        /// Set's the DNS Server of the local machine
        /// </summary>
        /// <param name="NIC">NIC address</param>
        /// <param name="DNS">DNS server address</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        private void SetDNS(string DNS, string macAddress)
        {
            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    if (objMO["MACAddress"].ToString().ToLower().Equals(macAddress.ToLower()))
                    {
                        Logger.Info(string.Format("Setting DNS {0} for mac {1}", DNS, macAddress));
                        try
                        {
                            ManagementBaseObject newDNS =
                                objMO.GetMethodParameters("SetDNSServerSearchOrder");
                            newDNS["DNSServerSearchOrder"] = DNS.Split(',');
                            ManagementBaseObject setDNS =
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
        /// <summary>
        /// Set's WINS of the local machine
        /// </summary>
        /// <param name="NIC">NIC Address</param>
        /// <param name="priWINS">Primary WINS server address</param>
        /// <param name="secWINS">Secondary WINS server address</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        private void SetWINS(string priWINS, string secWINS, string macAddress)
        {
            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    if (objMO["MACAddress"].ToString().ToLower().Equals(macAddress.ToLower()))
                    {
                        try
                        {
                            ManagementBaseObject setWINS;
                            ManagementBaseObject wins =
                            objMO.GetMethodParameters("SetWINSServer");
                            wins.SetPropertyValue("WINSPrimaryServer", priWINS);
                            wins.SetPropertyValue("WINSSecondaryServer", secWINS);

                            setWINS = objMO.InvokeMethod("SetWINSServer", wins, null);
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
