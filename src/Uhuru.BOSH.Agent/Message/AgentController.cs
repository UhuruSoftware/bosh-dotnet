﻿// -----------------------------------------------------------------------
// <copyright file="AgentController.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
using System.ServiceModel;
using System.ServiceModel.Web;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class AgentController : IDisposable
    {
    ////def initialize(handler)
    ////  super()
    ////  @handler = handler
    ////end

        ////configure do
        ////  set(:show_exceptions, false)
        ////  set(:raise_errors, false)
        ////  set(:dump_errors, false)
        ////end

        int serverPort;

        public int ServerPort
        {
            get { return serverPort; }
            set { serverPort = value; }
        }
        WebServiceHost host;
        HttpHandler httpHandler;

        public AgentController(HttpHandler handler)
        {
            httpHandler = handler;
        }

        public void Start()
        {
            if (this.serverPort == 0)
            {
                this.serverPort = Uhuru.Utilities.NetworkInterface.GrabEphemeralPort();
            }
            Uri baseAddress = new Uri("http://localhost:" + this.serverPort);
            Controller controller = new Controller();

            WebHttpBinding httpBinding = new WebHttpBinding();
            httpBinding.Security.Mode = WebHttpSecurityMode.TransportCredentialOnly;
            httpBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

            this.host = new WebServiceHost(controller, baseAddress);
            this.host.AddServiceEndpoint(typeof(IController), httpBinding, baseAddress);

            ((Controller)this.host.SingletonInstance).Initialize(this.httpHandler);
            this.host.Open();
        }

        public void Stop()
        {
            this.host.Close();
        }

        public void Dispose()
        {
            if (this.host != null)
            {
                this.host.Close();
            }
        }
    }

    [ServiceContract]
    interface IController
    {
        [WebGet(UriTemplate = "/agent")]
        System.ServiceModel.Channels.Message Responde();
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    class Controller : IController
    {
        HttpHandler httpHandler;

        public void Initialize(HttpHandler handler)
        {
            this.httpHandler = handler;
        }

        public System.ServiceModel.Channels.Message Responde()
        {
            ////post "/agent" do
            ////  body = request.env["rack.input"].read
            ////  response = handle_message(body)
            ////  content_type(:json)
            ////  response
            ////end
            throw new NotImplementedException();
        }

        private System.ServiceModel.Channels.Message HandleMessage(string json)
        {
            throw new NotImplementedException();

            ////  begin
            ////    payload = @handler.handle_message(json)
            ////  rescue => e
            ////    payload = RemoteException.from(e).to_hash
            ////  end

            ////  Yajl::Encoder.encode(payload, :terminator => "\n")
        }
    }
}