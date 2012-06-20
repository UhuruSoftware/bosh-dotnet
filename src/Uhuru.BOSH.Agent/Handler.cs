// -----------------------------------------------------------------------
// <copyright file="Handler.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Uhuru.NatsClient;
    using System.Threading;
    using Uhuru.Utilities;
    using System.Diagnostics;
    using System.IO;
    using System.Yaml;
    using Uhuru.BOSH.Agent.Objects;
    using System.Globalization;
    using Uhuru.BOSH.Agent.Message;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Handler
    {
        public Reactor Nats
        {
            get;
            set;
        }

        public Dictionary<string, string> Processors
        {
            get;
            set;
        }

        public static void Start()
        {
            Logger.Info("Starting Agent MBus Handler");
            new Handler().StartHandler();
        }

        const int MAX_NATS_RETRIES = 10;

        const int NATS_RECONNECT_SLEEP = 500;

        // Seconds after an unexpected error until we kill the agent so it can be
        // restarted.
        const int KILL_AGENT_THREAD_TIMEOUT = 15;

        public Handler()
        {
            this.AgentId = Config.AgentId;
            this.NatsUri = Config.MessageBus;
            this.BaseDir = Config.BaseDir;

            // Alert processing
            this.ProcessAlerts = (bool)Config.ProcessAlerts;
            this.SmtpUser = Config.SmtpUser;
            this.SmtpPassword = Config.SmtpPassword;
            this.SmtpPort = Config.SmtpPort;

            this.HeartBeatProcessor = new HeartbeatProcessor();

            this.Lock = new Mutex();

            this.Results = new List<HandlerResult>();
            this.LongRunningAgentTask = new List<string>();
            this.RestartingAgent = false;

            this.NatsFailCount = 0;

            this.Credentials = Config.Credentials;
            this.Sessions = new Dictionary<string, object>();
            this.SessionReplyMap = new Dictionary<string, object>();

            this.FindMessageProcessors();
        }

        private void FindMessageProcessors()
        {
            
            //messageConsts = Message.
            //@processors = {}
            //message_consts.each do |c|
            //klazz = Bosh::Agent::Message.const_get(c)
            //if klazz.respond_to?(:process)
            //    # CamelCase -> under_score -> downcased
            //    processor_key = c.to_s.gsub(/(.)([A-Z])/,'\1_\2').downcase
            //    @processors[processor_key] = klazz
            //end
            //end
            //@logger.info("Message processors: #{@processors.inspect}")
        }

        public IMessage Lookup(string method)
        {
            //TODO 
            Logger.Info("Processing :" + method);
            switch (method.ToLower())
            {
                case "ping":
                    return new Ping();
                case "apply":
                    return new Apply();
                case "state":
                    return new Message.State();
                case "start":
                    return new Start();
                case "drain":
                    return new Drain();
                case "stop":
                    return new Stop();
                case "mount_disk":
                    return new MountDisk();
                case "list_disk":
                    return new ListDisk();
                case "compile_package":
                    return new CompilePackage();
                case "test":
                    return new TestMessage();

            }
            return null;
         //   return this.Processors[method];
        }

        private void Trap(string s, Action action)
        {
            //throw new NotImplementedException();
        }

        public void StartHandler()
        {
            foreach (string s in new string[] { "TERM", "INT", "QUIT" })
            {
                Trap(s, () => Shutdown());
            }

            ThreadPool.QueueUserWorkItem((data) =>
                {
                    try
                    {
                        this.Nats = new Reactor();
                        this.Nats.OnConnect += new EventHandler<ReactorErrorEventArgs>(Nats_OnConnect);
                        Config.Nats = this.Nats;

                        int retries = MAX_NATS_RETRIES;
                        do
                        {
                            try
                            {
                                this.Nats.Start(this.NatsUri);
                                break;
                            }
                            catch (ReactorException ex)
                            {
                                if (retries <= 0)
                                {
                                    throw ex;
                                }
                                else
                                {
                                    Thread.Sleep(NATS_RECONNECT_SLEEP);
                                }
                            }
                        } while (retries-- > 0);

                        this.HeartBeatProcessor.Enable(Config.HeartbeatInterval * 1000);
                        //this.SetupHeartbeats();
                        //this.SetupSshdMonitor();

                        if (this.ProcessAlerts)
                        {
                            if (string.IsNullOrEmpty(this.SmtpPort) || string.IsNullOrEmpty(this.SmtpUser) || string.IsNullOrEmpty(this.SmtpPassword))
                            {
                                Logger.Error("Cannot start alert processor without having SMTP port, user and password configured");
                                Logger.Error("Agent will be running but alerts will NOT be properly processed");
                            }
                            else
                            {
                                Logger.Debug(string.Format("SMTP: {0}", this.SmtpPassword));
                                //this.Processor = AlertProcessor.Start("127.0.0.1", this.SmtpPort, this.SmtpUser, this.SmtpPassword);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.NatsFailCount += 1;
                        Logger.Error("NATS connection error: {0}", ex.ToString());
                        Thread.Sleep(NATS_RECONNECT_SLEEP);
                        // only retry a few times and then exit which lets the agent recover if we change credentials
                        if (this.NatsFailCount < MAX_NATS_RETRIES)
                        {
                            this.Retry();
                        }
                        Logger.Fatal(string.Format("Unable to reconnect to NATS after {0} retries, exiting...", MAX_NATS_RETRIES));
                    }
                });
        }

        private void Retry()
        {
            StartHandler();
            //throw new NotImplementedException();
        }

        public void Shutdown()
        {
            Logger.Info("Exit");
            this.Nats.Close();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        public void Nats_OnConnect(object sender, EventArgs e)
        {
            string subscription = string.Format("agent.{0}", this.AgentId);

            this.Nats.Subscribe(subscription, (string msg, string reply, string subject) =>
                {
                    this.HandleMessage(msg);
                });

            this.NatsFailCount = 0;
        }

        public void SetupHeartbeats()
        {
            int interval = Convert.ToInt32(Config.HeartbeatInterval);
            if (interval > 0)
            {
                this.HeartBeatProcessor.Enable(interval);
                Logger.Info(string.Format("Heartbeats are enabled and will be sent every {0} seconds", interval));
            }
            else
            {
                Logger.Warning("Heartbeats are disabled");
            }
        }

        public void SetupSshdMonitor()
        {
            int interval = Convert.ToInt32(Config.SshdMonitorInterval);
            if (interval > 0)
            {
                SshdMonitor.Enable(interval, Config.SshdStartDelay);
                Logger.Info(string.Format("sshd monitor is enabled, interval of {0} and start delay of {0} seconds", interval, Config.SshdStartDelay));
            }
            else
            {
                Logger.Warning("SSH is disabled");
            }
        }

        public void HandleMessage(string json)
        {
            try
            {
                 dynamic msg = YamlMapping.FromYaml(json)[0];

                //TODO Improve this functionality. This hack is needed for removing !!int values
                string aux = msg.ToString();
                msg = YamlMapping.FromYaml(aux.Replace(" !!int", string.Empty))[0];
                
                if (!msg.ContainsKey("reply_to"))
                {
                    Logger.Info("Missing reply_to in: {0}", json);
                    return;
                }

                Logger.Info("Message: {0}", json);

                if (this.Credentials != null)
                {
                    msg = Decrypt(msg);
                    if (msg == null)
                    {
                        return;
                    }
                }

                string replyTo = msg["reply_to"].Value;
                string method = msg["method"].Value;
                dynamic args = msg["arguments"];

                if (method == "get_state")
                {
                    method = "state";
                }

                IMessage processor = this.Lookup(method);

                if (processor != null)
                {
                    ThreadPool.QueueUserWorkItem((data) =>
                        {
                            ProcessInThread(processor, replyTo, method, args);
                        });
                }
                else if (method == "get_task")
                {
                    HandleGetTask(replyTo, ((args as YamlSequence).First() as YamlScalar).Value);
                }
                else if (method == "shutdown")
                {
                    HandleShutdown(replyTo);
                }
                else
                {
                    RemoteException re = new RemoteException(string.Format("Unknown message {0}", json));
                    // todo: vlad: fix the hash here
                    this.Publish(replyTo, re.ToHash().ToString());
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to parse message: {0}: {1}", json, ex.ToString());
            }
        }

        private void ProcessInThread(IMessage processor, string replyTo, string method, object args)
        {
            try
            {
                // TODO: vladi: implement the long running part
                //if (processor.GetType().GetMethod(method) != null)
                if (processor != null)
                {
                    if (processor.IsLongRunning())
                    {
                        if (this.RestartingAgent)
                        {
                            RemoteException exception = new RemoteException("restarting agent");
                            // todo: vlad: fix the hash here
                            this.Publish(replyTo, exception.ToHash().ToString());
                        }
                        else
                        {
                            this.Lock.WaitOne();
                            try
                            {
                                if (this.LongRunningAgentTask.Count == 0)
                                {
                                    this.ProcessLongRunning(replyTo, processor, args);
                                }
                                else
                                {
                                    RemoteException exception = new RemoteException("already running long running task");
                                    // todo: vlad: fix the hash here
                                    this.Publish(replyTo, exception.ToHash().ToString());
                                }
                            }
                            finally
                            {
                                this.Lock.ReleaseMutex();
                            }
                        }
                    }
                    else
                    {
                        string payload = this.Process(processor, args);

                        if (Config.Configure != null && (method == "prepare_network_change"))
                        {
                            // todo: vladi: fix payload
                            this.Publish(replyTo, payload, () => this.PostPrepareNetworkChange());
                        }
                        else
                        {
                            // todo: vladi: fix payload
                            this.Publish(replyTo, payload.ToString());
                        }
                    }
                }
              
            }
            catch (Exception ex)
            {
                // since this is running in a thread we're going to be nice and
                // log an error as this would otherwise be lost
                Logger.Error("{0}: {1)", processor.ToString(), ex.ToString());
            }
        }

        public void HandleGetTask(string replyTo, string agentTaskId)
        {
            if (this.LongRunningAgentTask.Contains(agentTaskId))
            {
                this.Publish(replyTo, "{\"value\":{\"state\":\"running\",\"agent_task_id\": \""+agentTaskId +"\"}}");
            }
            else
            {
                HandlerResult rs = this.Results.FirstOrDefault(r => r.AgentTaskId == agentTaskId);
                if (rs != null)
                {
                    DateTime time = rs.Time;
                    string taskId = rs.AgentTaskId;
                    string result = rs.Result;
                    // todo: vladi: fix result serialization
                    this.Publish(replyTo, result.ToString());
                }
                else
                {
                    // todo: vladi: implement payload class
                    this.Publish(replyTo, @"{""exception"" => ""unknown agent_task_id"" }");
                }
            }
        }


        // TODO once we upgrade to nats 0.4.22 we can use
        // NATS.server_info[:max_payload] instead of NATS_MAX_PAYLOAD_SIZE
        const int NATS_MAX_PAYLOAD_SIZE = 1024 * 1024;

        public void Publish(string replyTo, string payload)
        {
            this.Publish(replyTo, payload,
                () =>
                {
                    return;
                });
        }

        public void Publish(string replyTo, string payload, SimpleCallback block)
        {
            Logger.Info("reply_to: {0}: payload: {1}", replyTo, payload);

            string unencrypted = string.Empty;
            if (this.Credentials != null)
            {
                unencrypted = payload;
                payload = this.Encrypt(replyTo, payload);
            }

            //todo: vladi: encode JSON;
            //YamlNode json =payload;

            // TODO figure out if we want to try to scale down the message instead
            // of generating an exception
            //if (json.Bytesize < NATS_MAX_PAYLOAD_SIZE)
            //{
                this.Nats.Publish(replyTo, block, payload);
            //}
            //else
            //{
            //    string msg = "message > NATS_MAX_PAYLOAD, stored in blobstore";
            //    string original = this.Credentials != null ? payload : unencrypted;
            //    RemoteException exception = new RemoteException(msg, null, original);
            //    Logger.Fatal(msg);
            //    // todo: vladi: fix exception serialization
            //    this.Nats.Publish(replyTo, block, exception.ToHash().ToString());
            //}
        }

        public void ProcessLongRunning(string replyTo, dynamic processor, dynamic args)
        {
            string agentTaskId = GenerateAgentTaskId();

            this.LongRunningAgentTask.Add(agentTaskId);

            string payload = @"{""value"":{""state"":""running"",""agent_task_id"":""" + agentTaskId + @"""}}";

            //Dictionary<string, object> payload = new Dictionary<string, object>()
            //{
            //        {   "value", new Dictionary<string, string>()
            //            { 
            //                {"state", "running"} , 
            //                {"agent_task_id", "agent_task_id"}
            //            }
            //        }
            //};

            //YamlNode payloadYaml = YamlMapping.FromYaml(payload)[0];
            // todo: vladi: fix payload serialization
            this.Publish(replyTo, payload);

            //TODO : fix process
            string result = this.Process(processor, args).ToString();

            // todo: vladi: create a proper object
            HandlerResult resultsItem = new HandlerResult();
            resultsItem.Time = DateTime.Now;
            resultsItem.AgentTaskId = agentTaskId;
            resultsItem.Result = result;

            this.Results.Add(resultsItem);
            this.LongRunningAgentTask.Remove(agentTaskId);
        }

        public void KillMainThreadIn(int seconds)
        {
            this.RestartingAgent = true;
            ThreadPool.QueueUserWorkItem((data) =>
                {
                    Thread.Sleep(seconds * 1000);
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                });
        }

        public string Process(dynamic processor, dynamic args)
        {
            try
            {
                Logger.Info("Processing :" + args.ToString() +" using processor " + processor.ToString());
                string result = processor.Process(args);
                return string.Format("{{ \"value\": {0} }}", result);
                //return new Dictionary<string, object>() { { "value", result } };
            }
            catch (AgentException aex)
            {
                Logger.Info("{0}", aex);
                //TODO
                return null;
               // return RemoteException.From(aex).ToHash();
            }
            catch (Exception ex)
            {
                KillMainThreadIn(KILL_AGENT_THREAD_TIMEOUT);
                Logger.Error("{0}", ex.ToString());
                return "{ \"exception\": \"" + ex.Message.ToString() + "\" }";
                //return new Dictionary<string, object>() { { "exception", ex.ToString() } };
            }
        }

        public string GenerateAgentTaskId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public void PostPrepareNetworkChange()
        {
            if (Config.Configure != null)
            {
                string udevFile = "/etc/udev/rules.d/70-persistent-net.rules";

                if (File.Exists(udevFile))
                {
                    Logger.Info("deleting 70-persistent-net.rules - again");
                    File.Delete(udevFile);
                }

                Logger.Info("Removing settings.json");
                string settingsFile = Config.SettingsFile;
                File.Delete(settingsFile);
            }

            Logger.Info("Halt after networking change");
            // todo: vladi: implement halt/restart?
        }


        public void HandleShutdown(string replyTo)
        {
            Logger.Info("Shutting down {0} connection", Config.MessageBus.ToUpper());
            string payload = "{:value => \"shutdown\"}";

            if (Config.Configure != null)
            {
                //todo: vladi We should never come back up again
                //at_exit { `sv stop agent` }
            }

            this.Publish(replyTo, payload, () => this.Shutdown());
        }

        public EncryptionHandler LookupEncryptionHandler(dynamic arg)
        {
            if (arg.SessionId != null)
            {
                string messageSessionId = arg.SessionId;
                this.Sessions[messageSessionId] = new EncryptionHandler(this.AgentId, this.Credentials);
                EncryptionHandler encryptionHandler = this.Sessions[messageSessionId] as EncryptionHandler;
                return encryptionHandler;
            }
            else if (arg.ReplyTo != null)
            {
                string replyTo = arg.ReplyTo;
                return this.SessionReplyMap[replyTo] as EncryptionHandler;
            }

            return null;
        }

        public Dictionary<string, object> Decrypt(Dictionary<string, object> msg)
        {
            if (!msg.ContainsKey("session_id"))
            {
                Logger.Info("Missing session_id in {0}", msg.ToString());
                return null;
            }

            if (!msg.ContainsKey("encrypted_data"))
            {
                Logger.Info("Missing encrypted_data in {0}", msg.ToString());
                return null;
            }

            string messageSessionId = msg["session_id"] as string;
            string replyTo = msg["reply_to"] as string;

            EncryptionHandler encryptionHandler = LookupEncryptionHandler(new Dictionary<string, string>() { { "session_id", messageSessionId } });

            // save message handler for the reply
            this.SessionReplyMap[replyTo] = encryptionHandler;

            // Log exceptions from the EncryptionHandler, but stay quiet on the wire.
            try
            {
                msg = encryptionHandler.Decrypt(msg["encrypted_data"]);
            }
            catch (Exception ex)
            {
                LogEncryptionError(ex);
                return null;
            }

            msg["reply_to"] = replyTo;

            Logger.Info("Decrypted Message: #{msg}");
            return msg;
        }

        public void LogEncryptionError(Exception ex)
        {
            Logger.Info("Encryption Error: {0}", ex);
        }

        public string Encrypt(string replyTo, string payload)
        {
            // todo: vladi fix arg's class
            dynamic arg = new object();
            arg.ReplyTo = replyTo;
            EncryptionHandler encryptionHandler = LookupEncryptionHandler(arg);
            string sessionId = encryptionHandler.SessionId;

            // todo: vladi: fix payload;
            payload = @"{
                ""session_id"" => session_id,
                ""encrypted_data"" => encryption_handler.encrypt(payload)""}";

            return payload;
        }

        
        

        public Dictionary<string, object> SessionReplyMap { get; set; }

        public Dictionary<string, object> Sessions { get; set; }

        public string Credentials { get; set; }

        public int NatsFailCount { get; set; }

        public bool RestartingAgent { get; set; }

        public List<string> LongRunningAgentTask { get; set; }

        public List<HandlerResult> Results { get; set; }

        public Mutex Lock { get; set; }

        public HeartbeatProcessor HeartBeat { get; set; }

        public string SmtpPort { get; set; }

        public string SmtpPassword { get; set; }

        public string SmtpUser { get; set; }

        public bool ProcessAlerts { get; set; }

        public string BaseDir { get; set; }

        public string NatsUri { get; set; }

        public string AgentId { get; set; }

        public AlertProcessor Processor { get; set; }

        public HeartbeatProcessor HeartBeatProcessor;

        class Ping : IMessage
        {
            public string Process(dynamic args)
            {
                return "\"pong\"";
            }

            public bool IsLongRunning()
            {
                return false;
            }
        }

        class Noop :IMessage
        {
            public string Process(dynamic args)
            {
                return "\"nope\"";
            }
            public bool IsLongRunning()
            {
                return false;
            }
        }

    }
}