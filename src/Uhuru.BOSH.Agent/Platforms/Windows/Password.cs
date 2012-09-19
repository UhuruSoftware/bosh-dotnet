// -----------------------------------------------------------------------
// <copyright file="Password.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Platforms.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;
    using System.Collections.ObjectModel;
    using System.DirectoryServices.AccountManagement;
    using Uhuru.BOSH.Agent.Errors;
    using Uhuru.Utilities;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class Password
    {
        public static void Update(dynamic settings)
        {
            if (settings["env"]["bosh"] != null)
            {
                Dictionary<string, string> boshSettings = JsonConvert.DeserializeObject<Dictionary<string, string>>(settings["env"]["bosh"].ToString());
                if (boshSettings["password"] != null)
                {
                    UpdatePasswords(boshSettings["password"]);
                }
            }
        }

        private static void UpdatePasswords(string password)
        {
            Collection<string> users = new Collection<string>() { "administrator" };
            foreach (string user in users)
            {
                UpdatePassword(user, password);
            }
        }

        private static void UpdatePassword(string userName, string password)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Machine))
                using (var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, userName))
                {
                    user.SetPassword(password);
                    user.Save();
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.ToString());
                throw new FatalBoshException("Failed set password for " + userName, ex);
            }
        }
    }
}
