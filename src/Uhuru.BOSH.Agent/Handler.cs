﻿// -----------------------------------------------------------------------
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
    using Uhuru.BOSH.Agent.Objects;
    using System.Globalization;
    using Uhuru.BOSH.Agent.Message;
    using Newtonsoft.Json;
using System.Collections.ObjectModel;

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
            private set;
        }

        public static void Start()
        {
            Logger.Info("Starting Agent Mbus Handler");
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
            this.NatsUri = new Uri(Config.MessageBus);
            this.BaseDir = Config.BaseDir;

            // Alert processing
            this.ProcessAlerts = (bool)Config.ProcessAlerts;
            this.SmtpUser = Config.SmtpUser;
            this.SmtpPassword = Config.SmtpPassword;
            this.SmtpPort = Config.SmtpPort;

            this.HeartBeatProcessor = new HeartbeatProcessor();

            this.Lock = new Mutex();

            this.Results = new Collection<HandlerResult>();
            this.LongRunningAgentTask = new Collection<string>();
            this.RestartingAgent = false;

            this.NatsFailCount = 0;

            this.Credentials = Config.Credentials;
            this.Sessions = new Dictionary<string, object>();
            this.SessionReplyMap = new Dictionary<string, object>();

            FindMessageProcessors();
        }

        private static void FindMessageProcessors()
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification="TODO Methods are lower case")]
        public static IMessage Lookup(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentNullException(method);
            }

            //TODO 
            Logger.Info("Processing :" + method);
            switch (method.ToLower(CultureInfo.InvariantCulture))
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
                case "unmount_disk":
                    return new UnmountDisk();
                case "ssh":
                    return new Ssh();
                case "fetch_logs":
                    return new FetchLogs();
                case "test":
                    return new TestMessage();
                case "migrate_disk":
                    return new MigrateDisk();
                case "prepare_network_change":
                    return new PrepareNetworkChange();
                case "prepare":
                    return new Prepare();
            }
            return null;
         //   return this.Processors[method];
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "s", Justification="TODO Not implemented"),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "action", Justification = "TODO Not implemented"),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "TODO Not implemented")]
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
                        int retries = MAX_NATS_RETRIES;
                        do
                        {
                            this.Nats = new Reactor();
                            this.Nats.OnConnect += new EventHandler<ReactorErrorEventArgs>(NatsOnConnect);
                            this.Nats.OnError += new EventHandler<ReactorErrorEventArgs>(Nats_OnError);
                            try
                            {
                                this.Nats.Start(this.NatsUri);
                                Config.Nats = this.Nats;
                                break;
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Nats start Error :" + ex.ToString());
                                Thread.Sleep(NATS_RECONNECT_SLEEP);
                            }
                        } while (retries-- > 0);

                        this.HeartBeatProcessor.Enable(Config.HeartbeatInterval * 1000);

                        if (this.ProcessAlerts)
                        {
                            if (string.IsNullOrEmpty(this.SmtpPort) || string.IsNullOrEmpty(this.SmtpUser) || string.IsNullOrEmpty(this.SmtpPassword))
                            {
                                Logger.Error("Cannot start alert processor without having SMTP port, user and password configured");
                                Logger.Error("Agent will be running but alerts will NOT be properly processed");
                            }
                            else
                            {
                                Logger.Debug(string.Format(CultureInfo.InvariantCulture, "SMTP: {0}", this.SmtpPassword));
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
                        Logger.Fatal(string.Format(CultureInfo.InvariantCulture, "Unable to reconnect to NATS after {0} retries, exiting...", MAX_NATS_RETRIES));
                        Environment.FailFast(string.Format(CultureInfo.InvariantCulture, "Unable to reconnect to NATS after {0} retries, exiting...", MAX_NATS_RETRIES), ex);
                    }
                });
        }

        void Nats_OnError(object sender, ReactorErrorEventArgs e)
        {
            Logger.Error("Nats on Error :" + e.Exception.ToString());
            throw e.Exception;
        }

        private void Retry()
        {
            StartHandler();
        }

        public void Shutdown()
        {
            Logger.Info("Exit");
            this.Nats.Close();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        public void NatsOnConnect(object sender, EventArgs e)
        {
            try
            {
                string subscription = string.Format(CultureInfo.InvariantCulture, "agent.{0}", this.AgentId);

                this.Nats.Subscribe(subscription, (string msg, string reply, string subject) =>
                    {
                        this.HandleMessage(msg);
                    });

                this.NatsFailCount = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Error on nats connect: " + ex.ToString());
                this.NatsFailCount++;
            }
        }

        public void SetupHeartbeats()
        {
            int interval = Convert.ToInt32(Config.HeartbeatInterval);
            if (interval > 0)
            {
                this.HeartBeatProcessor.Enable(interval);
                Logger.Info(string.Format(CultureInfo.InvariantCulture, "Heartbeats are enabled and will be sent every {0} seconds", interval));
            }
            else
            {
                Logger.Warning("Heartbeats are disabled");
            }
        }

        public static void SetupSshdMonitor()
        {
            int interval = Config.SshdMonitorInterval;
            if (interval > 0)
            {
                SshdMonitor.Enable(interval, Config.SshdStartDelay);
                Logger.Info(string.Format(CultureInfo.InvariantCulture, "sshd monitor is enabled, interval of {0} and start delay of {1} seconds", interval, Config.SshdStartDelay));
            }
            else
            {
                Logger.Warning("SSH is disabled");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public void HandleMessage(string json)
        {
            try
            {
                dynamic msg = JsonConvert.DeserializeObject(json);

                if (String.IsNullOrEmpty(msg["reply_to"].ToString()))
                {
                    Logger.Info("Missing reply_to in: {0}", json);
                    return;
                }

                Logger.Info("Message: {0}", json);
                msg = CheckCredentials(msg);
                if (msg == null)
                {
                    return;
                }

                string replyTo = msg["reply_to"].Value;
                string method = msg["method"].Value;
                dynamic args = msg["arguments"];

                if (method.Equals("get_state", StringComparison.OrdinalIgnoreCase))
                {
                    method = "state";
                }

                IMessage processor = Lookup(method);

                if (processor != null)
                {
                    ThreadPool.QueueUserWorkItem((data) =>
                        {
                            ProcessInThread(processor, replyTo, method, args);
                        });
                }
                else if (method.Equals("get_task", StringComparison.OrdinalIgnoreCase))
                {
                    HandleGetTask(replyTo, (JsonConvert.DeserializeObject(args.ToString(), typeof(string[])) as string[])[0]);
                }
                else if (method.Equals("shutdown", StringComparison.OrdinalIgnoreCase))
                {
                    HandleShutdown(replyTo);
                }
                else
                {
                    RemoteException re = new RemoteException(string.Format(CultureInfo.InvariantCulture, "Unknown message {0}", json));
                    // todo: vlad: fix the hash here
                    this.Publish(replyTo, re.ToHash().ToString());
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Failed to parse message: {0}: {1}", json, ex.ToString());
            }
        }

        private object CheckCredentials(dynamic msg)
        {
            if (!string.IsNullOrEmpty(this.Credentials))
            {
                msg = Decrypt(msg);
                if (msg == null)
                {
                    return null;
                }
            }
            return msg;
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
                            this.Publish(replyTo, payload, () => PostPrepareNetworkChange());
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
                    // DateTime time = rs.Time;
                    // string taskId = rs.AgentTaskId;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "unencrypted", Justification="TODO Not used yet")]
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
            try
            {
                this.Nats.Publish(replyTo, block, payload);
            }
            catch (Exception ex)
            {
                Logger.Error("Error while publishing to nats");
            }
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
                object result = processor.Process(args);
                //return string.Format("{{ \"value\": {0} }}", result);
                return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "value", result } });
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
                return JsonConvert.SerializeObject( new Dictionary<string, object>() { { "exception", ex.ToString() } });
            }
        }

        public static string GenerateAgentTaskId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static void PostPrepareNetworkChange()
        {
            if (Config.Configure != null)
            {
                Logger.Info("Removing settings.json");
                string settingsFile = Config.SettingsFile;
                File.Delete(settingsFile);
            }

            Logger.Info("Halt after networking change");
            System.Diagnostics.Process.Start("shutdown.exe", "/l /y /c /t:0");
        }


        public void HandleShutdown(string replyTo)
        {
            Logger.Info("Shutting down {0} connection", Config.MessageBus.ToUpper(CultureInfo.InvariantCulture));
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
            if (msg == null)
            {
                throw new ArgumentNullException("msg");
            }

            if (msg["session_id"] == null)
            {
                Logger.Info("Missing session ID in {0}", msg.ToString());
                return null;
            }

            if (msg["encrypted_data"] == null)
            {
                Logger.Info("Missing encrypted data in {0}", msg.ToString());
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

        public static void LogEncryptionError(Exception ex)
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
                ""session_id"" => " + sessionId+ @",
                ""encrypted_data"" => encryption_handler.encrypt(payload)""}";

            return payload;
        }


        public Dictionary<string, object> SessionReplyMap { get; private set; }

        public Dictionary<string, object> Sessions { get; private set; }

        public string Credentials { get; set; }

        public int NatsFailCount { get; set; }

        public bool RestartingAgent { get; set; }

        public Collection<string> LongRunningAgentTask { get; private set; }

        public Collection<HandlerResult> Results { get; private set; }

        public Mutex Lock { get; set; }

        public HeartbeatProcessor HeartBeat { get; set; }

        public string SmtpPort { get; set; }

        public string SmtpPassword { get; set; }

        public string SmtpUser { get; set; }

        public bool ProcessAlerts { get; set; }

        public string BaseDir { get; set; }

        public Uri NatsUri { get; set; }

        public string AgentId { get; set; }

        public AlertProcessor Processor { get; set; }

        public HeartbeatProcessor HeartBeatProcessor { get; set; }

    }
}
