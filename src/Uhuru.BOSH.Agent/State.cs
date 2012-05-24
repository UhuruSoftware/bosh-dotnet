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
    using YamlDotNet.RepresentationModel;
    using Uhuru.BOSH.Agent.Objects;
    using Uhuru.BOSH.Agent.Errors;
    using System.Globalization;

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
        private YamlMappingNode data = null;

        private static readonly object locker = new object();

       

        /// <summary>
        /// Gets or sets the job.
        /// </summary>
        /// <value>
        /// The job.
        /// </value>
        public string Job
        {
            get
            {
                return data.GetString("job");
            }            
        }

    ////def initialize(state_file)
    ////  @state_file = state_file
    ////  @lock = Mutex.new
    ////  @data = nil
    ////  read
    ////end
        /// <summary>
        /// Initializes a new instance of the <see cref="State"/> class.
        /// </summary>
        /// <param name="file">The file.</param>
        public State(string file)
        {
            this.stateFile = file;
            Read();
        }

    ////# Fetches the state from file (unless it's been already fetched)
    ////# and returns the value of a given key.
    ////# TODO: ideally agent shouldn't expose naked hash but use
    ////# some kind of abstraction.
    ////# @param key Key that will be looked up in state hash
    ////def [](key)
    ////  @lock.synchronize { @data[key] }
    ////end
        public string GetValue(string key)
        {
            lock (locker)
            {
                return data.GetString(key);
            }
        }

    ////def to_hash
    ////  @lock.synchronize { @data.dup }
    ////end

    ////def ips
    ////  result = []
    ////  networks = self["networks"] || {}
    ////  return [] unless networks.kind_of?(Hash)

    ////  networks.each_pair do |name, network |
    ////    result << network["ip"] if network["ip"]
    ////  end
        /// <summary>
        /// Gets the existing ips in the state file.
        /// </summary>
        /// <returns>Collection of ips</returns>
        public Collection<string> GetIps()
        {
            Collection<string> ips = new Collection<string>();

            YamlMappingNode networksNode = data.GetChild("networks");

            foreach (YamlMappingNode node in networksNode.AllNodes)
            {
                string ip = node.GetString("ip");
                if (!string.IsNullOrEmpty(ip))
                {
                    ips.Add(ip);
                }
            }
            return ips;
        }
    ////  result
    ////end

    ////# Reads the current agent state from the state file and saves it internally.
    ////# Empty file is fine but malformed file raises an exception.
    ////def read
    ////  @lock.synchronize do
    ////    if File.exists?(@state_file)
    ////      state = YAML.load_file(@state_file) || default_state
    ////      unless state.kind_of?(Hash)
    ////        raise_format_error(state)
    ////      end
    ////      @data = state
    ////    else
    ////      @data = default_state
    ////    end
    ////  end
        ////  self
        ////rescue SystemCallError => e
        ////  raise StateError, "Cannot read agent state file `#{@state_file}': #{e}"
        ////rescue YAML::Error
        ////  raise StateError, "Malformed agent state: #{e}"
        ////end
        private void Read()
        {
            lock (locker)
            {
                if (File.Exists(stateFile))
                {
                    YamlDotNet.RepresentationModel.YamlStream yamlStream = new YamlStream();

                    try
                    {
                        using (TextReader textReader = new StreamReader(stateFile))
                        {
                            yamlStream.Load(textReader);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new StateException(string.Format(CultureInfo.InvariantCulture, "Cannot read agent state file {0}", stateFile), ex);
                    }
                    
                    
                    //TODO Test if the yaml is in the correct format
                    try
                    {
                        data = (YamlMappingNode)yamlStream.Documents[0].RootNode;
                    }
                    catch (Exception ex)
                    {
                        throw new StateException(string.Format(CultureInfo.InvariantCulture, "Malformed agent state"), ex);
                    }
                }
                else
                {
                    data = GetDefaultState();   
                }
            }
        }

   

    ////# Writes a new agent state into the state file.
    ////# @param   new_state  Hash  New state
    ////def write(new_state)
    ////  unless new_state.is_a?(Hash)
    ////    raise_format_error(new_state)
    ////  end

    ////  @lock.synchronize do
    ////    File.open(@state_file, "w") do |f|
    ////      f.puts(YAML.dump(new_state))
    ////    end
    ////    @data = new_state
    ////  end

    ////  true
    ////rescue SystemCallError, YAML::Error => e
    ////  raise StateError, "Cannot write agent state file `#{@state_file}': #{e}"
    ////end
        public void Write(YamlMappingNode NewState)
        {

        }
    ////private

    ////def default_state
    ////  {
    ////    "deployment"    => "",
    ////    "networks"      => { },
    ////    "resource_pool" => { }
    ////  }
    ////end

        private YamlMappingNode GetDefaultState()
        {
            YamlMappingNode defaultNode = new YamlMappingNode();
            defaultNode.Add("deployment", "");
            defaultNode.Add("networks", new YamlMappingNode());
            defaultNode.Add("resource_pool", new YamlMappingNode());
            return defaultNode;
        }
    ////def raise_format_error(state)
    ////  raise StateError, "Unexpected agent state format: expected Hash, got #{state.class}"
    ////end

        public IEnumerable<Network> Networks { get; set; }
    }
}
