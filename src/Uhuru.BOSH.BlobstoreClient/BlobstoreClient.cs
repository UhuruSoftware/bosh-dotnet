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

        //class Client

        //  PROVIDER_MAP = {
        //    "simple" => SimpleBlobstoreClient,
        //    "s3" => S3BlobstoreClient,
        //    "atmos" => AtmosBlobstoreClient,
        //    "local" => LocalClient
        //  }

        //  def self.create(provider, options = {})
        //    p = PROVIDER_MAP[provider]
        //    if p
        //      p.new(options)
        //    else
        //      providers = PROVIDER_MAP.keys.sort.join(", ")
        //      raise "Invalid client provider, available providers are: #{providers}"
        //    end
        //  end
        //end

        public static IClient Create(string provider, string[] options)
        {
            return new SimpleClient();
        }

    }
}
