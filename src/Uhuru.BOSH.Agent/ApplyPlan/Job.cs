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

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Job
    {
      ////attr_reader :install_path
      ////attr_reader :link_path
      ////attr_reader :template

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

      ////def initialize(spec, config_binding = nil)
      ////  unless spec.is_a?(Hash)
      ////    raise ArgumentError, "Invalid job spec, " +
      ////                         "Hash expected, #{spec.class} given"
      ////  end
      ////  %w(name template version sha1 blobstore_id).each do |key|
      ////    if spec[key].nil?
      ////      raise ArgumentError, "Invalid spec, #{key} is missing"
      ////    end
      ////  end

      ////  @base_dir = Bosh::Agent::Config.base_dir
      ////  @name = spec["name"]
      ////  @template = spec["template"]
      ////  @version = spec["version"]
      ////  @checksum = spec["sha1"]
      ////  @blobstore_id = spec["blobstore_id"]
      ////  @config_binding = config_binding

      ////  @install_path = File.join(@base_dir, "data", "jobs",
      ////                            @template, @version)
      ////  @link_path = File.join(@base_dir, "jobs", @template)
      ////end

        private string baseDir;
        private string name;
        private string template;
        private string installPath;
        private string linkPath;
        private string version;
        private string checksum;
        private string blobstoreId;
        private dynamic configBinding;
        private dynamic jobProperties;
        public Job(dynamic spec, dynamic jobProperties)
        {
            // TODO: check to se if any king of IDicrionty
            // if (spec is IDictionary<,>)

            var required = new string[] { "name", "template", "version", "sha1", "blobstore_id" };
            foreach(var requiredKey in required)
            {
                if (spec[requiredKey] == null)
                {
                    throw new ArgumentException(String.Format("Invalid spec. {0} is missing", requiredKey));
                }
            }
            this.jobProperties = jobProperties;
            baseDir = Config.BaseDir;
            name = spec["name"].Value;
            template = spec["template"].Value;
            version = spec["version"].Value;
            checksum = spec["sha1"] != null ? spec["sha1"].Value : null;
            blobstoreId = spec["blobstore_id"].Value;
            configBinding = spec["config_binding"] != null ? spec["config_binding"].Value : null;
            installPath = Path.Combine(baseDir, "data", "jobs", template, version);
            linkPath = Path.Combine(baseDir, "jobs", template);

            string jobsPath = Path.Combine(baseDir, "jobs");
            if (!Directory.Exists(jobsPath))
            {
                Directory.CreateDirectory(jobsPath);
            }
        }

      ////def install
      ////  fetch_template
      ////  bind_configuration
      ////  harden_permissions
      ////rescue SystemCallError => e
      ////  install_failed("system call error: #{e.message}")
      ////end

        public void Install()
        {
            try
            {
                FetchTemplate();
                BindConfiguration();
                HardenPermissions();
            }
            catch (Exception e)
            {
                throw new InstallationException("Apply job install error.", e);
            }
        }


      ////def configure
      ////  run_post_install_hook
      ////  configure_monit
      ////rescue SystemCallError => e
      ////  config_failed("system call error: #{e.message}")
      ////end

        public void Configure()
        {
            try
            {
                RunPreInstallHook();
                //ConfigureMonit();
            }
            catch (Exception e)
            {
                throw new ConfigurationException("Apply job configuration error.", e);
            }
        }

      ////private

      ////def fetch_template
      ////  FileUtils.mkdir_p(File.dirname(@install_path))
      ////  FileUtils.mkdir_p(File.dirname(@link_path))

      ////  Bosh::Agent::Util.unpack_blob(@blobstore_id, @checksum, @install_path)
      ////  Bosh::Agent::Util.create_symlink(@install_path, @link_path)
      ////end

        private void FetchTemplate()
        {
            Directory.CreateDirectory(installPath);
            //Directory.CreateDirectory(linkPath);

            Util.UnpackBlob(blobstoreId, checksum, installPath);
            Util.CreateSymLink(installPath, linkPath);
        }

      ////def bind_configuration
      ////  if @config_binding.nil?
      ////    install_failed("unable to bind configuration, " +
      ////                   "no binding provided")
      ////  end

      ////  bin_dir = File.join(@install_path, "bin")
      ////  manifest_path = File.join(@install_path, "job.MF")

      ////  unless File.exists?(manifest_path)
      ////    install_failed("cannot find job manifest")
      ////  end

      ////  FileUtils.mkdir_p(bin_dir)

      ////  begin
      ////    manifest = YAML.load_file(manifest_path)
      ////  rescue ArgumentError
      ////    install_failed("malformed job manifest")
      ////  end

      ////  unless manifest.is_a?(Hash)
      ////    install_failed("invalid job manifest, " +
      ////                   "Hash expected, #{manifest.class} given")
      ////  end

      ////  templates = manifest["templates"] || {}

      ////  unless templates.kind_of?(Hash)
      ////    install_failed("invalid value for templates in job manifest, " +
      ////                   "Hash expected, #{templates.class} given")
      ////  end

      ////  templates.each_pair do |src, dst|
      ////    template_path = File.join(@install_path, "templates", src)
      ////    output_path = File.join(@install_path, dst)

      ////    unless File.exists?(template_path)
      ////      install_failed("template '#{src}' doesn't exist")
      ////    end

      ////    template = ERB.new(File.read(template_path))
      ////    begin
      ////      result = template.result(@config_binding)
      ////    rescue Exception => e
      ////      # We are essentially running an arbitrary code,
      ////      # hence such a generic rescue clause
      ////      line = e.backtrace.first.match(/:(\d+):/).captures.first
      ////      install_failed("failed to process configuration template " +
      ////                     "'#{src}': " +
      ////                     "line #{line}, error: #{e.message}")
      ////    end

      ////    FileUtils.mkdir_p(File.dirname(output_path))
      ////    File.open(output_path, "w") { |f| f.write(result) }

      ////    if File.basename(File.dirname(output_path)) == "bin"
      ////      FileUtils.chmod(0755, output_path)
      ////    end
      ////  end
      ////end

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

            Logger.Info("Building properties ruby object using :" + jobProperties.ToString());
            string properties = GetRubyObject(jobProperties);
            Logger.Info ("Object built " + properties);

            foreach (var template in currentJobManifest.Templates)
            {
                string templatePath = Path.Combine(installPath, "templates", template.Key);
                string outputPath = Path.Combine(installPath, template.Value);

                Logger.Info("Try to apply " + templatePath + " to " + outputPath);

                if (!File.Exists(templatePath))
                {
                    Logger.Error("Template does not exist " + templatePath);
                    InstallFailed("Template does not exist " + templatePath);
                }

                Logger.Info("Running erb");
                ErbTemplate erbTemplate = new ErbTemplate();
                string outputFile = erbTemplate.Execute(templatePath, properties);
                
                Logger.Info("Writeing output file :" + outputFile);

                File.WriteAllText(outputPath, outputFile);
            }


        }

        public string GetRubyObject(dynamic jsonProperty)
        {
            StringBuilder currentObject = new StringBuilder();

            if ((jsonProperty as JContainer).Children().Count() != 0)
            {
                if (jsonProperty.GetType() == typeof(JObject))
                {
                    currentObject.Append("{");
                }
                foreach (var child in jsonProperty.Children())
                {
                    if (child.GetType() == typeof(JValue))
                    {
                        string childValue = child.ToString();
                        //Escaping \ character
                        childValue = childValue.Replace(@"\", @"\\");
                        return "\"" + childValue + "\"";
                    }
                    if (child.GetType() == typeof(JProperty))
                    {
                        if (currentObject.ToString() != "{")
                            currentObject.Append(", ");
                        currentObject.Append(child.Name + ": ");
                        currentObject.Append(GetRubyObject(child));

                    }
                    if (child.GetType() == typeof(JObject))
                    {
                        currentObject.Append(GetRubyObject(child));
                    }
                }
                if (jsonProperty.GetType() == typeof(JObject))
                {
                    currentObject.Append("}");
                }
            }
            return currentObject.ToString();
        }

      ////def harden_permissions
      ////  return unless Bosh::Agent::Config.configure

      ////  FileUtils.chown_R("root", Bosh::Agent::BOSH_APP_USER, @install_path)
      ////  chmod_others = "chmod -R o-rwx #{@install_path} 2>&1"
      ////  chmod_group = "chmod g+rx #{@install_path} 2>&1"

      ////  out = %x(#{chmod_others})
      ////  unless $?.exitstatus == 0
      ////    install_failed("error executing '#{chmod_others}': #{out}")
      ////  end

      ////  out = %x(#{chmod_group})
      ////  unless $?.exitstatus == 0
      ////    install_failed("error executing '#{chmod_group}': #{out}")
      ////  end
      ////end

        private void HardenPermissions()
        {
            // TODO: first determine the security level the bosh agent is running on 
            Logger.Error("Not implemented: HardenPermissions");
        }

      ////# TODO: move from util here? (not being used anywhere else)
      ////def run_post_install_hook
      ////  Bosh::Agent::Util.run_hook("post_install", @template)
      ////end

        private void RunPreInstallHook()
        {
            // TODO: maybe this is just a Process start helper function
            Logger.Info("Running pre install script");
            Monit.GetInstance().RunPreScripts(true);
            
            //Logger.Error("Not implemented: RunPostInstallHook");
        }

      ////def configure_monit
      ////  Dir.foreach(@install_path).each do |file|
      ////    full_path = File.expand_path(file, @install_path)

      ////    if file == "monit"
      ////      install_job_monitrc(full_path, @name)
      ////    elsif file =~ /(.*)\.monit$/
      ////      install_job_monitrc(full_path, "#{@name}_#{$1}")
      ////    end
      ////  end
      ////end

        //public void ConfigureMonit()
        //{
        //    Logger.Info("Configuring Uhuru monit");
        //    //Logger.Error("Not implemented: ConfigureMonit");
        //}

      ////def install_job_monitrc(template_path, label)
      ////  if @config_binding.nil?
      ////    config_failed("Unable to configure monit, " +
      ////                  "no binding provided")
      ////  end

      ////  template = ERB.new(File.read(template_path))
      ////  out_file = File.join(@install_path, "#{label}.monitrc")

      ////  begin
      ////    result = template.result(@config_binding)
      ////  rescue Exception => e
      ////    line = e.backtrace.first.match(/:(\d+):/).captures.first
      ////    config_failed("failed to process monit template " +
      ////                  "'#{File.basename(template_path)}': " +
      ////                  "line #{line}, error: #{e.message}")
      ////  end

      ////  File.open(out_file, "w") do |f|
      ////    f.write(add_modes(result))
      ////  end

      ////  # Monit will load all {base_dir}/monit/job/*.monitrc files,
      ////  # so we need to blow away this directory when we clean up.
      ////  link_path = File.join(@base_dir, "monit", "job", "#{label}.monitrc")

      ////  FileUtils.mkdir_p(File.dirname(link_path))
      ////  Bosh::Agent::Util.create_symlink(out_file, link_path)
      ////end

        private void InstallJobMonitrc()
        {
            Logger.Error("Not implemented: InstallJobMonitrc");
        }

      ////# HACK
      ////# Force manual mode on all services which don't have mode already set.
      ////# FIXME: this parser is very simple and thus generates space-delimited
      ////# output. Can be improved to respect indentation for mode. Also it doesn't
      ////# skip quoted tokens.
      ////def add_modes(job_monitrc)
      ////  state = :out
      ////  need_mode = true
      ////  result = ""

      ////  tokens = job_monitrc.split(/\s+/)

      ////  return "" if tokens.empty?

      ////  while (t = tokens.shift)
      ////    if t == "check"
      ////      if state == :in && need_mode
      ////        result << "mode manual "
      ////      end
      ////      state = :in
      ////      need_mode = true

      ////    elsif t == "mode" && %w(passive manual active).include?(tokens[0])
      ////      need_mode = false
      ////    end

      ////    result << t << " "
      ////  end

      ////  if need_mode
      ////    result << "mode manual "
      ////  end

      ////  result.strip
      ////end

        private void AddModes()
        {
            Logger.Error("Not implemented: AddModes");
        }

      ////def install_failed(message)
      ////  raise InstallationError, "Failed to install job '#{@name}': #{message}"
      ////end

        private void InstallFailed(string message)
        {
            throw new InstallationException("Failed to install job" + this.name + " : " + message, null);
        }

      ////def config_failed(message)
      ////  raise ConfigurationError, "Failed to configure job " +
      ////                            "'#{@name}': #{message}"
      ////end

        private JobManifest LoadManifest(string jobManifestPath)
        {
            string[] fileContent = File.ReadAllLines(jobManifestPath);
            //dynamic job = JsonConvert.DeserializeObject(fileContent);

            JobManifest jobManifest = new JobManifest();


            for (int i = 0; i < fileContent.Length; i++)
            {
                //get name
                if (fileContent[i].StartsWith("name"))
                {
                    jobManifest.Name = fileContent[i].Split(':')[1].Trim();
                }

                if (fileContent[i].StartsWith("templates"))
                {
                    i++;
                    while (fileContent[i] != string.Empty)
                    {
                        jobManifest.Templates.Add(fileContent[i].Split(':')[0].Trim(), fileContent[i].Split(':')[1].Trim());
                        i++;
                    }
                }
                if (fileContent[i].StartsWith("packages"))
                {
                    i++;
                    while (fileContent[i] != string.Empty || i == fileContent.Length)
                    {
                        jobManifest.Packages.Add(fileContent[i].Split('-')[1].Trim());
                        i++;
                    }
                }
            }
            return jobManifest;
        }
    }
}
