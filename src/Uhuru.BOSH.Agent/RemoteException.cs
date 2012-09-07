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
    using Uhuru.BOSH.BlobstoreClient.Clients;
    using Uhuru.Utilities;
    using Uhuru.BOSH.BlobstoreClient.Errors;
    using Uhuru.BOSH.Agent.Errors;
    using System.Diagnostics;
    using System.Reflection;
    using System.Globalization;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "Keeping name similar to VMWare's code base")]
    public class RemoteException
    {
        private string message;
        private string backtrace;
        private string blob;

        /// <summary>
        /// Gets the message.
        /// </summary>
        public string Message
        {
            get
            {
                return message;
            }

        }

        /// <summary>
        /// Gets the backtrace.
        /// </summary>
        public string Backtrace
        {
            get
            {
                return backtrace;
            }
        }

        /// <summary>
        /// Gets the BLOB.
        /// </summary>
        public string Blob
        {
            get
            {
                return blob;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="backtrace">The backtrace.</param>
        /// <param name="blob">The BLOB.</param>
        public RemoteException(string message, string backtrace, string blob)
        {
            this.message = message;
            this.backtrace = backtrace;
            this.blob = blob;
            
            if (string.IsNullOrEmpty(backtrace))
            {
                StackTrace stackTrace = new System.Diagnostics.StackTrace();
                StackFrame[] stackFrames = stackTrace.GetFrames();
                foreach (StackFrame stackFrame in stackFrames)
                {
                    MethodBase method = stackFrame.GetMethod();
                    this.backtrace = this.backtrace + string.Format(CultureInfo.InvariantCulture, "{0}.{1}", method.DeclaringType.FullName, method.Name) + Environment.NewLine;
                }
                
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public RemoteException(string message) : this(message, null, null)
        {
        }

        /// <summary>
        /// Stores the BLOB.
        /// </summary>
        /// <returns></returns>
        public string StoreBlob()
        {
            string[] bscOptions = Config.BlobstoreOptions.ToArray();
            string bscProvider = Config.BlobstoreProvider;

            Logger.Info("Storring blob");
            IClient blobStore = BlobstoreClient.Blobstore.CreateClient(bscProvider, bscOptions);

            Logger.Info(string.Format(CultureInfo.InvariantCulture, "Uploading blob for {0} to blobstore", message));

            string blobStoreId = null;
            try
            {
                blobStoreId = blobStore.Create(blob);
            }
            catch (Exception ex)
            {
                Logger.Warning(string.Format(CultureInfo.InvariantCulture, "unable to upload blob for {0}", message));
                throw new BlobstoreException(string.Format(CultureInfo.InvariantCulture, "error: unable to upload blob to blobstore."),ex);
            }

            return blobStoreId;
        }

        /// <summary>
        /// Gets the remote exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns></returns>
        public static RemoteException CreateRemoteException(Exception exception)
        {
            string blob = null;
            if (exception.GetType() == typeof(MessageHandlerException))
            {
                blob = (exception as MessageHandlerException).Blob;
            }

            return new RemoteException(exception.Message, exception.StackTrace, blob);
        }


        internal Dictionary<string, object> ToHash()
        {
            throw new NotImplementedException();
        }

        internal static RemoteException From(AgentException aex)
        {
            throw new NotImplementedException();
        }
    }
}
