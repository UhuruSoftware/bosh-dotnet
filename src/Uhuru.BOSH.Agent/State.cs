// -----------------------------------------------------------------------
// <copyright file="State.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Collections.ObjectModel;
    using System.IO;
    using Uhuru.BOSH.Agent.Objects;
    using Uhuru.BOSH.Agent.Errors;
    using System.Globalization;
    using System.Yaml.Serialization;
    using System.Yaml;

    /// <summary>
    /// This is a thin abstraction on top of a state.yml file that is managed by agent.
    /// It can be used for getting the whole state as a hash or for querying
    /// particular keys. Flushing the state to a file only happens on write call.
    /// 
    /// It aims to be threadsafe but doesn' use file locking so as long as everyone agrees on using the same
    /// singleton of this class to read and write state it should be fine. However two agent processes using the same state file
    /// ould still be stepping on each other as well as two classes using different instances of this.
    /// </summary>
    public class State
    {
        private string stateFile;
        private dynamic data = null;
        private Job job= null;
        private Collection<Network> networks = new Collection<Network>();

        private static readonly object locker = new object();

       

        /// <summary>
        /// Gets or sets the job.
        /// </summary>
        /// <value>
        /// The job.
        /// </value>
        public Job Job
        {
            get
            {
                return job;
            }            
        }

        /// <summary>
        /// Gets the networks from the yaml.
        /// </summary>
        public Collection<Network> Networks
        {
            get
            {
                return networks;               
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="State"/> class.
        /// </summary>
        /// <param name="file">The file.</param>
        public State(string file)
        {
            this.stateFile = file;
            Read();
            job = GetCurrentJob();
            networks = GetCurrentNetworks();
        }

        /// <summary>
        /// Gets a value based on the key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public YamlNode GetValue(string key)
        {
            lock (locker)
            {
                return data[key];
            }
        }

        public dynamic ToHash()
        {
            return data;
        }

        public void SetValue(string key, dynamic value)
        {
            lock (locker)
            {
                data[key] = value;
            }
        }

        /// <summary>
        /// Gets the existing ips in the state file.
        /// </summary>
        /// <returns>Collection of ips</returns>
        public Collection<string> GetIps()
        {
            Collection<string> ips = new Collection<string>();

            foreach (Network network in networks)
            {
                ips.Add(network.Ip);
            }

            return ips;
        }


    ////# Reads the current agent state from the state file and saves it internally.
    ////# Empty file is fine but malformed file raises an exception.
        private void Read()
        {
            lock (locker)
            {
                if (File.Exists(stateFile))
                {
                    try
                    {
                        
                        using (TextReader textReader = new StreamReader(stateFile))
                        {
                            YamlNode[] nodes = YamlNode.FromYaml(textReader);
                            if (nodes.Length > 0)
                                data = nodes[0];
                            else
                                data = GetDefaultState();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new StateException(string.Format(CultureInfo.InvariantCulture, "Cannot read agent state file {0}", stateFile), ex);
                    }
                }
                else
                {
                    data = GetDefaultState();   
                }
            }
        }



        /// <summary>
        /// Writes the specified new state.
        /// </summary>
        /// <param name="newState">The new state.</param>
        public void Write(YamlNode newState)
        {
            
            try
            {
                newState.ToYamlFile(stateFile);
            }
            catch (Exception ex)
            {
                throw new StateException(string.Format(CultureInfo.InvariantCulture, "Cannot write agent state file {0}:", stateFile), ex);
            }

            //This is because we do not support multiple documents in the same yaml
            data = newState;
            job = GetCurrentJob();
            networks = GetCurrentNetworks();
            File.WriteAllText(stateFile, data.ToString());
        }


        /// <summary>
        /// Gets the default state.
        /// </summary>
        /// <returns></returns>
        private YamlNode GetDefaultState()
        {
            string defaultState = @"---
            deployment    : 
            networks      : { }
            resource_pool : { }";
            return YamlNode.FromYaml(defaultState)[0];
        }
   
        private Job GetCurrentJob()
        {
            if (!data.ContainsKey("job"))
                return null;
            
            Job currentJob = new Job();
            currentJob.Name = data["job"]["name"].Value;
            currentJob.Version = data["job"]["version"].Value;
            currentJob.Sha1 = data["job"]["sha1"].Value;
            currentJob.Template = data["job"]["template"].Value;
            currentJob.Blobstore_id = data["job"]["blobstore_id"].Value;

            return currentJob;
        }

        private Collection<Network> GetCurrentNetworks()
        {
            Collection<Network> currentNetworks =null;

            if (data.ContainsKey("networks"))
            {
                currentNetworks = new Collection<Network>();
                foreach (dynamic net in data["networks"])
                {
                    Network network = new Network();
                    network.Name = net.Key.Value;
                    network.Ip = net.Value["ip"].Value;
                    currentNetworks.Add(network);
                }
            }

            return currentNetworks;
        }
    }
}
