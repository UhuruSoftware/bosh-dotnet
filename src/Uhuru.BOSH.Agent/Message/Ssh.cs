// -----------------------------------------------------------------------
// <copyright file="Ssh.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Uhuru.BOSH.Agent.Objects;
    using Uhuru.Utilities;
    using Newtonsoft.Json;
    using System.Globalization;
    using Microsoft.Win32;
    using System.IO;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ssh", Justification = "FxCop Bug")]
    public class Ssh : IMessage 
    {
        /// <summary>
        /// Determines whether the message [is long running].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is long running]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsLongRunning()
        {
           return false;
        }

        /// <summary>
        /// Processes the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public object Process(dynamic args)
        {
            string sshType = args[0].Value.ToString();

            switch (sshType)
            {
                case "setup":
                    return SetupSsh(args[1]);
                case "cleanup":
                    return CleanupSsh(args[1]);
                default:
                    break;
            }

            return string.Empty;

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static object SetupSsh(dynamic parm)
        {
            
            string userName = parm["user"].Value;
            
            //Needed to enforce windows password rules 
            string password = string.Format(CultureInfo.InvariantCulture, "{0}!", parm["password"].Value);
            SaveSaltInFile(userName, password.Substring(0, 2));

            Logger.Info("Setting up SSH with user:" + userName +" and password: " + password);

            SshResult sshResult = new SshResult();
            sshResult.Command = "setup";
            try
            {
                Uhuru.Utilities.WindowsVCAPUsers.CreateUser(userName, password);
                sshResult.Status = "success";
                Logger.Info("Created user for SSH");
                SshdMonitor.StartSshd();

            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create user " + ex.ToString());
                sshResult.Status = "failed";
            }
            sshResult.IP = Config.DefaultIP;

            return sshResult;
        }

        private static void SaveSaltInFile(string user, string salt)
        {
            string saltDirPath = Path.Combine(Config.BaseDir, "bosh", "salt");
            if (!Directory.Exists(saltDirPath))
            {
                Directory.CreateDirectory(saltDirPath);
            }
            File.WriteAllText(Path.Combine(saltDirPath, user + ".salt"), salt);            
        }

       

        /// <summary>
        /// Clean the SSH.
        /// </summary>
        /// <param name="parm">User parameters.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static object CleanupSsh(dynamic parm)
        {
            string userRegex = parm["user_regex"].Value;
            string userName = userRegex.Remove(0, 1);
            userName = userName.Remove(userName.Length - 1, 1);
            SshResult sshResult = new SshResult();
            sshResult.Command = "cleanup";
            Logger.Info("Cleaning up SSH");

            try
            {
                WindowsVCAPUsers.DeleteUser(userName);
                Logger.Info("Deleted user for SSH");
                File.Delete(Path.Combine(Config.BaseDir, "bosh","salt", userName + ".salt"));
                Logger.Info("Deleted salt file");

                sshResult.Status = "success";
                SshdMonitor.StopSshd();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to delete user " + ex.ToString());
                sshResult.Status = "failed";
            }
            
            sshResult.IP = null;            
            return sshResult;
        }


    }
}
