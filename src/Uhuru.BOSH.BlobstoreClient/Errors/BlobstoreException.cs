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

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class BlobstoreException : Exception 
    {

        public BlobstoreException() : base()
        {

        }

        public BlobstoreException(string message, Exception innerException) : base(message,innerException)
        {
        
        }
            //class BlobstoreError < StandardError; end
            //class NotFound < BlobstoreError; end


    }
}
