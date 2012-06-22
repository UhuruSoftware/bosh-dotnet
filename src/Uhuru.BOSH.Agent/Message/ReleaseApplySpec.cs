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

    using System.IO;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ReleaseApplySpec : Base
    {

        public static string Process(string[] args)
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

        static string ApplySpec()
        {
            //TODO
            return string.Empty;
            
        }
    }
}
