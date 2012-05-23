// -----------------------------------------------------------------------
// <copyright file="Infrastructure.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Infrastructure
    {
        private string p;

        public Infrastructure(string p)
        {
            // TODO: Complete member initialization
            this.p = p;
        }
    ////def initialize(infrastructure_name)
    ////  @name = infrastructure_name
    ////  # TODO: add to loadpath?
    ////  infrastructure = File.join(File.dirname(__FILE__), 'infrastructure', "#{infrastructure_name}.rb")

    ////  if File.exist?(infrastructure)
    ////    load infrastructure
    ////  else
    ////    raise UnknownInfrastructure, "infrastructure '#{infrastructure_name}' not found"
    ////  end
    ////end

    ////def infrastructure
    ////  Infrastructure.const_get(@name.capitalize).new
    ////end

        public Infrastructure ProperInfrastructure
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
            }
        }
    }
}
