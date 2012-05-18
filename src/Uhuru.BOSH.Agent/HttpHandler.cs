// -----------------------------------------------------------------------
// <copyright file="HttpHandler.cs" company="Uhuru Software, Inc.">
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
    public class HttpHandler
    {
    ////def self.start
    ////  new.start
    ////end

    ////def start
    ////  handler = self

    ////  EM.run do
    ////    uri = URI.parse(Config.mbus)

    ////    @server = Thin::Server.new(uri.host, uri.port) do
    ////      use Rack::CommonLogger

    ////      if uri.userinfo
    ////        use Rack::Auth::Basic do |user, password|
    ////          "#{user}:#{password}" == uri.userinfo
    ////        end
    ////      end

    ////      map "/" do
    ////        run AgentController.new(handler)
    ////      end
    ////    end

    ////    @server.start!
    ////  end
    ////end

    ////def shutdown
    ////  @logger.info("Exit")
    ////  @server.stop
    ////end

    ////def handle_message(json)
    ////  result = {}
    ////  result.extend(MonitorMixin)

    ////  cond = result.new_cond
    ////  timeout_time = Time.now.to_f + 30

    ////  @callback = Proc.new do |response|
    ////    result.synchronize do
    ////      result.merge!(response)
    ////      cond.signal
    ////    end
    ////  end

    ////  super(json)

    ////  result.synchronize do
    ////    while result.empty?
    ////      timeout = timeout_time - Time.now.to_f
    ////      unless timeout > 0
    ////        raise "Timed out"
    ////      end
    ////      cond.wait(timeout)
    ////    end
    ////  end

    ////  result
    ////end

    ////def publish(reply_to, payload, &blk)
    ////  response = @callback.call(payload)
    ////  blk.call if blk
    ////  response
    ////end
    }
}
