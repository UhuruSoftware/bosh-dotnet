// -----------------------------------------------------------------------
// <copyright file="AgentController.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
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
        bool disposed = false;

        /// <summary>
        /// Gets or sets the server port.
        /// </summary>
        /// <value>
        /// The server port.
        /// </value>
        public int ServerPort
        {
            get { return serverPort; }
            set { serverPort = value; }
        }
        WebServiceHost host;
        HttpHandler httpHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentController"/> class.
        /// </summary>
        /// <param name="handler">The http handler.</param>
        public AgentController(HttpHandler handler)
        {
            httpHandler = handler;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
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

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            this.host.Close();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.host != null)
                    {
                        this.host.Close();
                    }
                }

                disposed = true;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "TODO: Not implemented")]
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "json", Justification = "TODO: Not implemented"), 
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "TODO: Not implemented"), 
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "TODO: Not implemented")]
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
