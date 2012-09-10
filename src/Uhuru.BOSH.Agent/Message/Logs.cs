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
    using System.Collections;
    using Uhuru.BOSH.BlobstoreClient;
    using Uhuru.BOSH.BlobstoreClient.Clients;
    using Uhuru.Utilities;
    using System.IO;
    using System.Globalization;

    /// <summary>
    /// Grab Logs message
    /// </summary>
    public class FetchLogs :  IMessage
    {

        private FileMatcher matcher;
        private FileAggregator aggregator;

        /// <summary>
        /// Gets or sets the file matcher.
        /// </summary>
        /// <value>
        /// The file matcher.
        /// </value>
        public FileMatcher Matcher {
            get
            {
                return matcher;
            }
            set
            {
                matcher = value;
            } 
        }

        /// <summary>
        /// Gets or sets the file aggregator.
        /// </summary>
        /// <value>
        /// The file aggregator.
        /// </value>
        public FileAggregator Aggregator
        {
            get
            {
                return aggregator;
            }
            set
            {
                aggregator = value;
            }
        }

        string logType;
        ICollection filters;

        /// <summary>
        /// Initializes a new instance of the <see cref="FetchLogs"/> class.
        /// </summary>
        public FetchLogs()
        {
        }

        private FileMatcher defaultMatcher
        {
            get
            {
                switch (logType.ToUpperInvariant())
                {
                    case "JOB":
                        {
                            return new JobLogMatcher(BaseMessage.BaseDir);
                        }
                    case "AGENT":
                        {
                            return new AgentLogMatcher(BaseMessage.BaseDir);
                        }
                    default:
                        {
                            return null;
                        }
                }
            }
        }

        //TODO Jira UH-1175
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private IEnumerable<string> filterGlobs
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static string UploadTarball(string path)
        {
            Logger.Info("Uploading tarball");
            string blobstoreId = null;
            try
            {
                dynamic bscOptions = Config.BlobstoreOptions;
                string bscProvider = Config.BlobstoreProvider;

                Logger.Info("Uploading tarball to blobstore");
                IClient blobstore = Blobstore.CreateClient(bscProvider, bscOptions);

                blobstoreId = blobstore.Create(new FileInfo(path));
            }
            catch (Exception e)
            {
                BaseMessage.ErrorHandler(String.Format(CultureInfo.InvariantCulture, "unable to upload logs to blobstore: {0}", e.Message));
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
            filters = (ICollection)args[1]; 
            matcher = defaultMatcher;
            aggregator = new FileAggregator();
            aggregator.Matcher = matcher;

            if (matcher == null)
            {
                BaseMessage.ErrorHandler(String.Format(CultureInfo.InvariantCulture, "matcher for {0} logs not found", logType));
            }
            if (aggregator == null)
            {
                BaseMessage.ErrorHandler(String.Format(CultureInfo.InvariantCulture, "aggregator for {0} logs not found", logType));
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
