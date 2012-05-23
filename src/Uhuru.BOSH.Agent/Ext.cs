// -----------------------------------------------------------------------
// <copyright file="Ext.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using YamlDotNet.RepresentationModel;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class Ext
    {
        public static YamlMappingNode GetChild(this YamlMappingNode yamlNode, string nodeName)
        {
            return (YamlMappingNode)yamlNode.Children[new YamlScalarNode(nodeName)];
        }

        public static string GetString(this YamlMappingNode yamlNode, string nodeName)
        {
            YamlMappingNode node = (YamlMappingNode)yamlNode.Children[new YamlScalarNode(nodeName)];
            return node == null ? null : node.ToString();
        }

        public static int? GetInt(this YamlMappingNode yamlNode, string nodeName)
        {
            YamlMappingNode node = (YamlMappingNode)yamlNode.Children[new YamlScalarNode(nodeName)];
            return node == null ? 0 : Convert.ToInt32(node.ToString());
        }

        ////class Object
        //// def to_openstruct
        ////   self
        //// end

        ////  def blank?
        ////    self.to_s.blank?
        ////  end
        ////end

        ////class String
        ////  def blank?
        ////    self =~ /^\s*$/
        ////  end
        ////end

        ////class Array
        //// def to_openstruct
        ////   map{ |el| el.to_openstruct }
        //// end
        ////end
        
        ////class Hash
        ////  def recursive_merge!(other)
        ////    self.merge!(other) do |_, old_value, new_value|
        ////      if old_value.class == Hash
        ////        old_value.recursive_merge!(new_value)
        ////      else
        ////        new_value
        ////      end
        ////    end
        ////    self
        ////  end

        //// def to_openstruct
        ////   mapped = {}
        ////   each{ |key,value| mapped[key] = value.to_openstruct }
        ////   OpenStruct.new(mapped)
        //// end
        ////end

        ////class Logger
        ////  def format_message(severity, timestamp, progname, msg)
        ////    "#[#{$$}] #{severity.upcase}: #{msg}\n"
        ////  end
        ////end
    }
}
