// -----------------------------------------------------------------------
// <copyright file="SimpleClient.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.BlobstoreClient.Clients
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Net;
    using Uhuru.BOSH.BlobstoreClient.Errors;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class SimpleClient : BaseClient
    {
        protected string endpoint;
        protected WebHeaderCollection headers;

        public SimpleClient(dynamic options)
            : base((object)options)
        {
            this.headers = new WebHeaderCollection();

            endpoint = options["enpoint"];

            string user = options["user"];
            string password = options["password"];

            if (user != null && password != null)
            {
                headers[HttpRequestHeader.Authorization] = "Basic " +
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(user + ":" + password)).Trim();
            }
        }


        public override void Delete(string id)
        {
            try
            {
                var request = HttpWebRequest.Create(url(id));
                request.Method = "DELETE";
                request.Headers = this.headers;

                using (var reponse = request.GetResponse())
                {
                    if ((reponse as HttpWebResponse).StatusCode != HttpStatusCode.OK)
                    {
                        throw new BlobstoreException("Could not delete object");
                    }
                }
            }
            catch (BlobstoreException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new BlobstoreException("Could not delete object", e);
            }
        }


        protected string url(string id)
        {
            string ret = endpoint + "/resources";
            if (id != null)
            {
                return ret;
            }
            else
            {
                return ret + "/" + id;
            }
        }

        public override string CreateFile(FileInfo contentsFilePath)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers = this.headers;

                    var response = client.UploadFile(url(null), "POST", contentsFilePath.FullName);
                    return Encoding.ASCII.GetString(response);
                }
            }
            catch (Exception e)
            {
                throw new BlobstoreException("Could not create object", e);
            }
        }

        public override void GetFile(string id, FileInfo outpuFile)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers = this.headers;
                    client.DownloadFile(url(id), outpuFile.FullName);
                }
            }
            catch (Exception e)
            {
                throw new BlobstoreException("Could not fetch object", e);
            }
        }

    }
}
