// -----------------------------------------------------------------------
// <copyright file="SshdMonitor.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Globalization;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sshd", Justification = "FxCop Bug")]
    public static class SshdMonitor
    {
      ////def ok_to_stop?
      ////  Config.sshd_monitor_enabled && @start_time && (Time.now - @start_time) > @start_delay
      ////end

      ////def test_service(status)
      ////  success = false
      ////  3.times do |_|
      ////    ssh_status = %x[service ssh status]
      ////    success = $?.exitstatus == 0 && ssh_status =~ /#{status}/
      ////    break if success
      ////    sleep 1
      ////  end
      ////  success
      ////end

      ////def start_sshd
      ////  @lock.synchronize do
      ////    %x[service ssh start]
      ////    raise "Failed to start sshd #{ssh_status}" if $?.exitstatus != 0 && !test_service("running")

      ////    @start_time = Time.now
      ////    @logger.info("started sshd #{@start_time}")
      ////  end
      ////end

      ////def stop_sshd
      ////  @lock.synchronize do
      ////    return if !ok_to_stop?
      ////    # No need to check for logged in users as existing ssh connections are not
      ////    # affected by stopping ssh
      ////    @logger.info("stopping sshd")

      ////    %x[service ssh stop]
      ////    raise "Failed to stop sshd" if $?.exitstatus != 0 && !test_service("stop")
      ////    @start_time = nil
      ////  end
      ////end

      ////def enable(interval, start_delay)
      ////  @logger = Config.logger
      ////  @lock = Mutex.new
      ////  @start_time = nil
      ////  @start_delay = start_delay

      ////  EventMachine.add_periodic_timer(interval) do
      ////    EventMachine.defer { stop_sshd } if SshdMonitor.ok_to_stop?
      ////  end
      ////end
        internal static void Enable(int interval, int p)
        {
            throw new NotImplementedException(string.Format(CultureInfo.InvariantCulture, "{0} {1}", interval.ToString(CultureInfo.InvariantCulture), p.ToString(CultureInfo.InvariantCulture)));
        }
    }
}
