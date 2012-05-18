// -----------------------------------------------------------------------
// <copyright file="Stop.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Stop", Justification = "Keeping the name so it's the same as VMWare's")]
    public class Stop
    {
      ////def self.long_running?
      ////  true
      ////end

      ////def self.process(args)

      ////  if Config.configure
      ////    Bosh::Agent::Monit.stop_services
      ////  end

      ////  "stopped"

      ////rescue => e
      ////  # Monit retry logic should make it really hard to get here but if it happens we should yell.
      ////  # One potential problem is that drain process might have unmonitored and killed processes
      ////  # already but Monit became really unresponsive. In that case it might be a fake alert:
      ////  # however this is not common and can be handled on case-by-case basis.
      ////  raise Bosh::Agent::MessageHandlerError, "Cannot stop job: #{e}"
      ////end
    }
}
