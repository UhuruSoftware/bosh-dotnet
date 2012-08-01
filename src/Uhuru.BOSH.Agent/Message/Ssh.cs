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

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ssh", Justification = "FxCop Bug")]
    public class Ssh : IMessage 
    {
        public bool IsLongRunning()
        {
           return false;
        }

        public string Process(dynamic args)
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

        private string SetupSsh(dynamic parm)
        {
            
            string userName = parm["user"].Value;
            string password = "password1234!";//parm["password"].Value;
            Logger.Info("Setting up SSH with user:" + userName +" and password: " + password);

            SshResult sshResult = new SshResult();
            sshResult.Command = "setup";

            try
            {
                Uhuru.Utilities.WindowsVCAPUsers.CreateUser(userName, password);
                sshResult.Status = "success";
                Logger.Info("Created user for SSH");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create user " + ex.ToString());
                sshResult.Status = "failed";
            }
            sshResult.Ip = Config.DefaultIp;

            string result = JsonConvert.SerializeObject(sshResult);

            return result;
        }

        private string CleanupSsh(dynamic parm)
        {
            string userRegex = parm["user_regex"].Value;
            string userName = userRegex.Remove(0, 1);
            userName = userName.Remove(userName.Length - 1, 1);
            SshResult sshResult = new SshResult();
            sshResult.Command = "cleanup";
            Logger.Info("Cealnning up SSH");

            try
            {
                
                WindowsVCAPUsers.DeleteUser(userName);
                sshResult.Status = "success";
                Logger.Info("Deleted user for SSH");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to delete user " + ex.ToString());
                sshResult.Status = "failed";
            }

            sshResult.Ip = null;
            string result = JsonConvert.SerializeObject(sshResult);
            return result;
        }


    }
}
