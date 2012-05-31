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
    public class BlobstoreClient
    {

        public static IClient Create(string provider, dynamic options)
        {
            switch (provider)
            {
                case "simple":
                    return new SimpleClient(options);
                    break;

                case "local":
                    return new LocalClient(options);
                    break;

                case "s3":
                    return new AmazonS3Client(options);
                    break;

                case "atmos":
                    return new AtmosClient(options);
                    break;

                default:
                    throw new ArgumentException("privider", "Invalid client provider");
            }

            
        }

    }
}
