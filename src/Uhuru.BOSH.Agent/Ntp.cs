// -----------------------------------------------------------------------
// <copyright file="Ntp.cs" company="Uhuru Software, Inc.">
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ntp", Justification = "FxCop Bug")]
    public class Ntp
    {
    ////BAD_SERVER = "bad ntp server"
    ////FILE_MISSING = "file missing"
    ////BAD_CONTENTS = "bad file contents"

    ////def self.offset(ntpdate="#{Config.base_dir}/bosh/log/ntpdate.out")
    ////  result = {}
    ////  if File.exist?(ntpdate)
    ////    lines = []
    ////    File.open(ntpdate) do |file|
    ////      lines = file.readlines
    ////    end
    ////    case lines.last
    ////    when /^(.+)\s+ntpdate.+offset\s+(-*\d+\.\d+)/
    ////      result["timestamp"] = $1
    ////      result["offset"] = $2
    ////    when /no server suitable for synchronization found/
    ////      result["message"] = BAD_SERVER
    ////    else
    ////      result["message"] = BAD_CONTENTS
    ////    end
    ////  else
    ////    result["message"] = FILE_MISSING
    ////  end
    ////  result
    ////end
    }
}
