// -----------------------------------------------------------------------
// <copyright file="BlolbstoreException.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.BlobstoreClient.Errors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;


    [Serializable]
    public class BlobstoreException : Exception
    {
        public BlobstoreException() { }
        public BlobstoreException(string message) : base(message) { }
        public BlobstoreException(string message, Exception inner) : base(message, inner) { }
        protected BlobstoreException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
