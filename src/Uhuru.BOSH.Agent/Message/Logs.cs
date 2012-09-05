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
//using YamlDotNet.RepresentationModel;
    using System.Collections;
    using Uhuru.BOSH.BlobstoreClient;
    using Uhuru.BOSH.BlobstoreClient.Clients;
    using Uhuru.Utilities;
    using System.IO;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class FetchLogs : Base, IMessage
    {
        public static bool longRunning { get { return true; } }
        public FileMatcher matcher { get; set; }
        public FileAggregator aggregator { get; set; }

        string logType;
        ICollection filters;
        object state;

        public FetchLogs()
        {
         
        }

        private FileMatcher defaultMatcher
        {
            get
            {
                switch (logType.ToLower())
                {
                    case "job":
                        {
                            return new JobLogMatcher(BaseDir);
                        }
                    case "agent":
                        {
                            return new AgentLogMatcher(BaseDir);
                        }
                    default:
                        {
                            return null;
                        }
                }
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
            Logger.Info("Uploading tarball");
            string blobstoreId = null;
            try
            {
               dynamic bscOptions = Config.BlobstoreOptions;
                string bscProvider = Config.BlobstoreProvider;

                Logger.Info("Uploading tarball to blobstore");
                IClient blobstore = Blobstore.CreateClient(bscProvider,  bscOptions );
                
                blobstoreId = blobstore.Create(new FileInfo(path));
            }
            catch (Exception e)
            {
                ErrorHandler(String.Format("unable to upload logs to blobstore: {0}", e.Message));
            }
            return blobstoreId;
        }

        /// <summary>
        /// Determines whether [is long running].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is long running]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsLongRunning()
        {
           return true;
        }

        /// <summary>
        /// Processes the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public object Process(dynamic args)
        {
            Logger.Info(string.Format("Processing {0} ", args.ToString()));
            logType = args[0].ToString();
            filters = (ICollection)args[1]; // TODO: ??? Set.new(args[1])
            state = Config.State.ToHash(); //TODO: Bosh::Agent::Config.state.to_hash
            matcher = defaultMatcher;
            aggregator = new FileAggregator();
            aggregator.Matcher = matcher;

            if (matcher == null)
            {
                ErrorHandler(String.Format("matcher for {0} logs not found", logType));
            }
            if (aggregator == null)
            {
                ErrorHandler(String.Format("aggregator for {0} logs not found", logType));
            }

            if (filters != null && filters.Count > 0)
            {
                matcher.Globs = filterGlobs;
            }
            
            string tarballPath = aggregator.GenerateTarball();

            string blobstoreId = UploadTarball(tarballPath);

            return new Dictionary<string, string>() { { "blobstore_id", blobstoreId } };
        }
    }
}
