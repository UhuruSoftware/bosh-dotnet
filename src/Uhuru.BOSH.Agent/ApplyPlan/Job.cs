// -----------------------------------------------------------------------
// <copyright file="Job.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.ApplyPlan
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using Uhuru.BOSH.Agent.ApplyPlan.Errors;
    using Uhuru.Utilities;
    using Uhuru.BOSH.Agent.Objects;
    using Newtonsoft.Json.Linq;
    using Uhuru.BOSH.Agent.Ruby;
    using System.Globalization;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Job
    {

        public string InstallPath
        {
            get
            {
                return installPath;
            }
        }

        public string LinkPath
        {
            get
            {
                return linkPath;
            }
        }

        public string Template
        {
            get
            {
                return template;
            }
        }

        private string baseDir;
        private string name;
        private string template;
        private string installPath;
        private string linkPath;
        private string version;
        private string checksum;
        private string blobstoreId;
        private dynamic bindSpec;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification="JIRA UH-1201")]
        Monit monit;

        public Job(string jobName, string templateName, dynamic templateSpec, dynamic bindSpec)
        {

            Helpers.ValidateSpec(templateSpec);

            this.bindSpec = bindSpec;
            baseDir = Config.BaseDir;
            name = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", jobName, templateName);
            template = templateName;
            version = templateSpec["version"].Value;
            checksum = templateSpec["sha1"].Value;
            blobstoreId = templateSpec["blobstore_id"].Value;

            installPath = Path.Combine(baseDir, "data", "jobs", template, version);
            linkPath = Path.Combine(baseDir, "jobs", template);

            monit = Monit.GetInstance();

            string jobsPath = Path.Combine(baseDir, "jobs");
            if (!Directory.Exists(jobsPath))
            {
                Directory.CreateDirectory(jobsPath);
            }
        }

        public void PrepareForInstall()
        {
            Helpers.FetchBits(installPath, blobstoreId, checksum);
        }
       

        public void Install()
        {
            try
            {
                Helpers.FetchBitsAndSymlink(installPath, linkPath, blobstoreId, checksum);
                BindConfiguration();
                HardenPermissions();
            }
            catch (Exception e)
            {
                throw new InstallationException("Apply job install error.", e);
            }
        }


        public void Configure()
        {
            try
            {
                RunPreInstallHook();
            }
            catch (Exception e)
            {
                throw new ConfigurationException("Apply job configuration error.", e);
            }
        }


        private void BindConfiguration()
        {
            
            Logger.Info("Binding configuration on "+ installPath);
            string manifestPath = Path.Combine(installPath, "job.MF");
            Logger.Info("Manifest file location is :" + manifestPath);
            if (!File.Exists(manifestPath))
            {
                string message ="Cannot find job manifest " + manifestPath;
                Logger.Error(message);
                InstallFailed(message);
            }

            JobManifest currentJobManifest = null;
            try
            {
                Logger.Info("Loading manifest file");
                currentJobManifest = LoadManifest(manifestPath);
                Logger.Info("Successfully loaded manifest file");
            }
            catch (Exception ex)
            {
                Logger.Error("Malformed manifest file : " + ex.ToString());
                InstallFailed("Malformed manifest file : " + ex.ToString());
            }

            Logger.Info("Building properties ruby object using :" + bindSpec.ToString());
            string spec = GetRubyObject(bindSpec);
            Logger.Info ("Object built " + spec);

            foreach (var t in currentJobManifest.Templates)
            {
                string templatePath = Path.Combine(installPath, "templates", t.Key);
                string outputPath = Path.Combine(installPath, t.Value);

                Logger.Info("Try to apply " + templatePath + " to " + outputPath);

                if (!File.Exists(templatePath))
                {
                    Logger.Error("Template does not exist " + templatePath);
                    InstallFailed("Template does not exist " + templatePath);
                }

                Logger.Info("Running ERB");
                ErbTemplate erbTemplate = new ErbTemplate();
                string outputFile = erbTemplate.Execute(templatePath, spec);
                
                Logger.Info("Writing output file :" + outputFile);

                File.WriteAllText(outputPath, outputFile);
            }
        }

        /// <summary>
        /// Gets the ruby object.
        /// </summary>
        /// <param name="jsonProperty">The JSON property.</param>
        /// <returns></returns>
        public static string GetRubyObject(dynamic jsonProperty)
        {
            StringBuilder currentObject = new StringBuilder();
            bool isJobject = false;
            if (jsonProperty is JObject)
            {
                currentObject.Append("{");
                isJobject = true;
            }
            if ((jsonProperty as JContainer).Children().Count() != 0)
            {

                foreach (var child in jsonProperty.Children())
                {
                    if (child is JValue)
                    {
                        string childValue = child.ToString();

                        //Escaping \ character
                        childValue = childValue.Replace(@"\", @"\\");
                        return "\"" + childValue + "\"";
                    }
                    
                    ProcessJProperty(ref currentObject, child);

                    //TODO IMPROVE JARAY
                    if (child is JArray)
                        return "{}";
                    
                    if (child is JObject)
                    {
                        currentObject.Append(GetRubyObject(child));
                    }
                }

            }
            if (isJobject) 
            {
                currentObject.Append("}");
            }
            return currentObject.ToString();
        }

        private static void ProcessJProperty(ref StringBuilder currentObject, dynamic child)
        {
            if (child is JProperty)
            {
                if (currentObject.ToString() != "{")
                    currentObject.Append(", ");
                currentObject.Append(":\""+child.Name+ "\"=> ");
                currentObject.Append(GetRubyObject(child));
            }
        }


        //TODO: JIRA UH-1203
        private static void HardenPermissions()
        {
            // TODO: first determine the security level the bosh agent is running on 
            Logger.Error("Not implemented: Harden Permissions");
        }


        private void RunPreInstallHook()
        {
            // TODO: maybe this is just a Process start helper function
            Logger.Info("Running pre install script");
            this.monit.RunPrescripts(true);
            
            //Logger.Error("Not implemented: RunPostInstallHook");
        }



        private void InstallFailed(string message)
        {
            throw new InstallationException("Failed to install job" + this.name + " : " + message, null);
        }



        private static JobManifest LoadManifest(string jobManifestPath)
        {
            string[] fileContent = File.ReadAllLines(jobManifestPath);
            //dynamic job = JsonConvert.DeserializeObject(fileContent);
            JobManifest jobManifest = new JobManifest();


            for (int i = 0; i < fileContent.Length; i++)
            {
                //get name
                if (fileContent[i].StartsWith("name", StringComparison.OrdinalIgnoreCase))
                {
                    jobManifest.Name = fileContent[i].Split(':')[1].Trim();
                }

                if (fileContent[i].StartsWith("templates", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    while (!String.IsNullOrEmpty(fileContent[i]))
                    {
                        jobManifest.AddTemplate(fileContent[i].Split(':')[0].Trim(), fileContent[i].Split(':')[1].Trim());
                        i++;
                    }
                }
                if (fileContent[i].StartsWith("packages", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    while (i < fileContent.Length && !String.IsNullOrEmpty(fileContent[i]))
                    {
                        jobManifest.AddPackage(fileContent[i].Split('-')[1].Trim());
                        i++;
                    }
                }
            }
            return jobManifest;
        }
    }
}
