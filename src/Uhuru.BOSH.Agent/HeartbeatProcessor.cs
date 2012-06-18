// -----------------------------------------------------------------------
// <copyright file="HeartbeatProcessor.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Uhuru.BOSH.Agent.Errors;
using System.Timers;
    using Uhuru.Utilities;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class HeartbeatProcessor
    {
        private Timer timer = null;
        public static int pending;
    ////MAX_OUTSTANDING_HEARTBEATS = 2

    ////def enable(interval)
    ////  unless EM.reactor_running?
    ////    raise Bosh::Agent::HeartbeatError, "Event loop must be running in order to enable heartbeats"
    ////  end
        public void Enable(int interval)
        {
            if (Config.Nats == null)
            {
                throw new HeartbeatException("Event loop must be running in order to enable heartbeats");
            }

            if (timer != null)
            {
               Logger.Warning("Heartbeat timer already running, canceling");
                return;
            }
            pending = 0;
            timer = Utilities.TimerHelper.RecurringLongCall(interval, Beat);
        }

    ////  if @timer
    ////    Config.logger.warn("Heartbeat timer already running, canceling")
    ////    disable
    ////  end

    ////  @pending = 0

    ////  @timer = EM.add_periodic_timer(interval) do
    ////    beat
    ////  end
    ////end

    ////def disable
    ////  Config.logger.info("Disabled heartbeats")
    ////  @timer.cancel if @timer
    ////  @timer = nil
    ////end

    ////def beat
    ////  raise HeartbeatError, "#{@pending} outstanding heartbeat(s)" if @pending > MAX_OUTSTANDING_HEARTBEATS

    ////  Heartbeat.new.send_via_mbus do
    ////    @pending -= 1
    ////  end
    ////  @pending += 1
    ////rescue => e
    ////  Config.logger.warn("Error sending heartbeat: #{e}")
    ////  Config.logger.warn(e.backtrace.join("\n"))
    ////  raise e if @pending > MAX_OUTSTANDING_HEARTBEATS
    ////end
        void Beat()
        {
            Logger.Info("Timer hartbeat");

            try
            {
                Heartbeat hearbeat = new Heartbeat();
                hearbeat.SendViaMbus();
                pending++;
            } 
            catch (Exception ex)
            {
                Logger.Warning("Error sending heartbeat: " + ex.ToString());
            }
        }
    }
}
