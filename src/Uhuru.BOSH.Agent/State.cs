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

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class State
    {
        private string p;

        public State(string p)
        {
            // TODO: Complete member initialization
            this.p = p;
        }

        /// <summary>
        /// Gets or sets the job.
        /// </summary>
        /// <value>
        /// The job.
        /// </value>
        public string Job
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the ips.
        /// </summary>
        /// <value>
        /// The ips.
        /// </value>
        public Collection<string> Ips
        {
            get;
            set;
        }
    ////def initialize(state_file)
    ////  @state_file = state_file
    ////  @lock = Mutex.new
    ////  @data = nil
    ////  read
    ////end

    ////# Fetches the state from file (unless it's been already fetched)
    ////# and returns the value of a given key.
    ////# TODO: ideally agent shouldn't expose naked hash but use
    ////# some kind of abstraction.
    ////# @param key Key that will be looked up in state hash
    ////def [](key)
    ////  @lock.synchronize { @data[key] }
    ////end

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

    ////private

    ////def default_state
    ////  {
    ////    "deployment"    => "",
    ////    "networks"      => { },
    ////    "resource_pool" => { }
    ////  }
    ////end

    ////def raise_format_error(state)
    ////  raise StateError, "Unexpected agent state format: expected Hash, got #{state.class}"
    ////end

        public IEnumerable<Network> Networks { get; set; }
    }
}
