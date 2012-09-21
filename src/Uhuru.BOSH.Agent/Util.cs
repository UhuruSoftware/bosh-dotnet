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
    using System.Globalization;
    using Microsoft.Win32;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Management;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Util", Justification = "FxCop bug"), 
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", Justification = "Keeping name the same as VMWare's code")]
    public static class Util
    {

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "spec", Justification="TODO: JIRA UH-1207")]
        internal static dynamic configBinding(dynamic spec)
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
                    using (SHA1 sha = new SHA1CryptoServiceProvider())
                    {
                        blobSHA1 = BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");
                    }
                }
                if (String.Compare(blobSHA1,checksum, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    throw new MessageHandlerException(String.Format(CultureInfo.InvariantCulture, "Expected sha1: {0}, Downloaded sha1: {1}", checksum, blobSHA1));
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
                throw;
            }
        }

        internal static void CreateSymLink(string installPath, string linkPath)
        {
            // TODO: replace this with the native call from AlphaFS libarary
            // Alphaleonis.Win32.Filesystem.File.CreateSymbolicLink(linkPath, installPath, Alphaleonis.Win32.Filesystem.SymbolicLinkTarget.Directory);

            Process p = Process.Start("cmd.exe", String.Format(CultureInfo.InvariantCulture, "/c mklink /D {0} {1}", linkPath, installPath));
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                Logger.Error(String.Format(CultureInfo.InvariantCulture, "Failed creating symbolic link between {0} and {1}", installPath, linkPath));
            }
        }

        internal static RegistryKey SetupUhuruKey()
        {
            RegistryKey softwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE", true);
            RegistryKey uhuruSubKey = softwareKey.OpenSubKey("Uhuru", true);
            if (uhuruSubKey == null)
            {
                softwareKey.CreateSubKey("Uhuru");
                uhuruSubKey = softwareKey.OpenSubKey("Uhuru", true);
            }
            

            RegistryKey uhuruCloudTargetsSubKey = uhuruSubKey.OpenSubKey("BoshAgent", true);
            if (uhuruCloudTargetsSubKey == null)
            {
                uhuruSubKey.CreateSubKey("BoshAgent");
                uhuruCloudTargetsSubKey = uhuruSubKey.OpenSubKey("BoshAgent", true);
            }
            return uhuruCloudTargetsSubKey;
        }

        internal static void ActivateWindows(string productKey)
        {
            using (ManagementClass objMC = new ManagementClass("SoftwareLicensingService"))
            {
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    ManagementBaseObject methodParameters = objMO.GetMethodParameters("InstallProductKey");
                    methodParameters["ProductKey"] = productKey;
                    objMO.InvokeMethod("InstallProductKey", methodParameters, null);

                    objMO.InvokeMethod("RefreshLicenseStatus", null);
                }
            }

            using (ManagementClass objMC = new ManagementClass("SoftwareLicensingProduct"))
            {
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    if (objMO["PartialProductKey"] != null)
                    {
                        objMO.InvokeMethod("Activate", null);
                    }
                }
            }

            using (ManagementClass objMC = new ManagementClass("SoftwareLicensingService"))
            {
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    objMO.InvokeMethod("RefreshLicenseStatus", null);
                }
            }
        }
    }
}
