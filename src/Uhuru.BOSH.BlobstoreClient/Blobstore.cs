// -----------------------------------------------------------------------
// <copyright file="BlolbstoreClient.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.BlobstoreClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Uhuru.BOSH.BlobstoreClient.Clients;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Blobstore
    {

        public static IClient CreateClient(string provider, dynamic options)
        {
            
            switch (provider)
            {
                case "simple":
                    return new SimpleClient(options);

                case "local":
                    return new LocalClient(options);

                case "s3":
                    return new AmazonS3Client(options);

                case "atmos":
                    return new AtmosClient(options);

                default:
                    throw new ArgumentException("provider", "Invalid client provider");
            }

            
        }

    }
}
