// -----------------------------------------------------------------------
// <copyright file="RemoteException.cs" company="Uhuru Software, Inc.">
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "Keeping name similar to VMWare's code base")]
    public class RemoteException
    {
        private string p;

        public RemoteException(string p)
        {
            // TODO: Complete member initialization
            this.p = p;
        }
    ////attr_reader :message, :backtrace, :blob

        public RemoteException(string message, string backtrace=null, string blob=null)
        {
          this.Message = message;
          this.Backtrace = backtrace == null ? Environment.StackTrace : backtrace;
          this.Blob = blob;
        }

    ////# Stores the blob in the configured blobstore
    ////#
    ////# @return [String] blobstore id of the stored object, or an error
    ////# string which can be displayed instead of the blob
    ////def store_blob
    ////  bsc_options  = Bosh::Agent::Config.blobstore_options
    ////  bsc_provider = Bosh::Agent::Config.blobstore_provider

    ////  blobstore = Bosh::Blobstore::Client.create(bsc_provider, bsc_options)

    ////  logger.info("Uploading blob for '#{@message}' to blobstore")

    ////  blobstore_id = nil
    ////  blobstore_id = blobstore.create(@blob)

    ////  blobstore_id
    ////rescue Bosh::Blobstore::BlobstoreError => e
    ////  logger.warn("unable to upload blob for '#{@message}'")
    ////  "error: unable to upload blob to blobstore: #{e.message}"
    ////end

    ////# Returns a hash of the [RemoteException] suitable to convert to json
    ////#
    ////# @return [Hash] [RemoteException] represented as a [Hash]
    ////def to_hash
    ////  hash = {:message => @message}
    ////  hash[:backtrace] = @backtrace
    ////  hash[:blobstore_id] = store_blob if @blob
    ////  {:exception => hash}
    ////end

    ////def logger
    ////  Bosh::Agent::Config.logger
    ////end

    ////# Helper class method that creates a [Bosh::Agent::RemoteException]
    ////# from an [Exception]
    ////#
    ////# @return [Bosh::Agent::RemoteException]
    ////def self.from(exception)
    ////  blob = nil
    ////  if exception.instance_of?(Bosh::Agent::MessageHandlerError)
    ////    blob = exception.blob
    ////  end
    ////  self.new(exception.message, exception.backtrace, blob)
    ////end

        internal Dictionary<string, object> ToHash()
        {
            throw new NotImplementedException();
        }

        internal static RemoteException From(AgentException ex)
        {
            throw new NotImplementedException();
        }

        public string Message { get; set; }

        public string Backtrace { get; set; }

        public string Blob { get; set; }
    }
}
