// -----------------------------------------------------------------------
// <copyright file="ReleaseApplySpec.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
using YamlDotNet.RepresentationModel;
    using System.IO;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ReleaseApplySpec : Base
    {

        public static YamlStream Process(string[] args)
        {
            return ApplySpec();
        }

        public static string ReleaseApplySpecPath
        {
            get
            {
                return "/var/vcap/micro/apply_spec.yml"; //TODO: define path
            }
        }

        static YamlStream ApplySpec()
        {
            StreamReader input = new StreamReader(ReleaseApplySpecPath);
            YamlStream yaml = new YamlStream();
            yaml.Load(input);
            return yaml;
        }
    }
}
