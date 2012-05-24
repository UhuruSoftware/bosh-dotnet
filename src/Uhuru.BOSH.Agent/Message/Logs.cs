// -----------------------------------------------------------------------
// <copyright file="Logs.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
using YamlDotNet.RepresentationModel;
    using System.Collections;
    using Uhuru.BOSH.BlobstoreClient;
    using Uhuru.BOSH.BlobstoreClient.Clients;
    using Uhuru.Utilities;
    using System.IO;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class FetchLogs : Base
    {
        public static bool LongRunning { get { return true; } }
        public FileMatcher Matcher { get; set; }
        public FileAggregator Aggregator { get; set; }

        string logType;
        ICollection filters;
        object state;

        public FetchLogs(object[] args)
        {
            logType = args[0].ToString();
            filters = (ICollection)args[1]; // TODO: ??? Set.new(args[1])
            state = Config.State; //TODO: Bosh::Agent::Config.state.to_hash
            Matcher = defaultMatcher;
            Aggregator = new FileAggregator();
        }

        public static void Process(string[] args)
        {
            new FetchLogs(args);
        }

        private FileMatcher defaultMatcher
        {
            get
            {
                switch (logType.ToLower())
                {
                    case "job":
                        {
                            return new JobLogMatcher();
                        }
                    case "agent":
                        {
                            return new AgentLogMatcher();
                        }
                    default:
                        {
                            return null;
                        }
                }
            }
        }

        public void Process()
        {
            if (Matcher == null)
            {
                ErrorHandler(String.Format("matcher for {0} logs not found", logType));
            }
            if (Aggregator == null)
            {
                ErrorHandler(String.Format("aggregator for {0} logs not found", logType));
            }

            if (filters != null && filters.Count > 0)
            {
                Matcher.Globs = filterGlobs;
            }
        }

        private IEnumerable<string> filterGlobs
        {
            get
            {
                throw new NotImplementedException();
                ////custom_job_logs = {}

                ////if @state["job"] && @state["job"]["logs"]
                ////  logs_spec = @state["job"]["logs"]

                ////  if logs_spec.is_a?(Hash)
                ////    custom_job_logs = logs_spec
                ////  else
                ////    logger.warn("Invalid format for job logs spec: Hash expected, #{logs_spec.class} given")
                ////    logger.warn("All custom filtering except '--all' thus disabled")
                ////  end
                ////end

                ////predefined = { "all" => "**/*" }

                ////predefined.merge(custom_job_logs).inject([]) do |result, (filter_name, glob)|
                ////  result << glob if @filters.include?(filter_name)
                ////  result
                ////end
            }
        }

        private string UploadTarball(string path)
        {
            string blobstoreId = null;
            try
            {
                string bscOptions = Config.BlobstoreOptions;
                string bscProvider = Config.BlobstoreProvider;

                IClient blobstore = BlobstoreClient.Create(bscProvider, new string[] { bscOptions });
                Logger.Info("Uploading tarball to blobstore");

                using (StreamReader reader = new StreamReader(path))
                {
                    blobstoreId = blobstore.Create(reader);
                }
            }
            catch (Exception e)
            {
                ErrorHandler(String.Format("unable to upload logs to blobstore: {0}", e.Message));
            }
            return blobstoreId;
        }
    }
}
