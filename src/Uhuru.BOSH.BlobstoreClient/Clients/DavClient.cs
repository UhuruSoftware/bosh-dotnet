// -----------------------------------------------------------------------
// <copyright file="DavClient.cs" company="">
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
    using System.Collections.Specialized;
    using System.Security.Cryptography;
    using System.Threading;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DavClient : BaseClient
    {
        protected string endpoint;
        protected string authorizationHeader;

        public DavClient(dynamic options)
            : base((object)options)
        {
            endpoint = (string)options["endpoint"].Value;

            string user = options["user"].Value;
            string password = options["password"].Value;

            if (user != null && password != null)
            {
                authorizationHeader = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(user + ":" + password)).Trim();
            }
            else
            {
                authorizationHeader = "";
            }
        }


        public override void Delete(string id)
        {
            try
            {
                var request = HttpWebRequest.Create(url(id));
                request.Method = "DELETE";
                request.Headers[HttpRequestHeader.Authorization] = authorizationHeader;

                using (var reponse = request.GetResponse())
                {
                    if ((reponse as HttpWebResponse).StatusCode != HttpStatusCode.NoContent)
                    {
                        throw new BlobstoreException("Could not delete object: Http Code/Description: " + (reponse as HttpWebResponse).StatusCode.ToString() + "/" + (reponse as HttpWebResponse).StatusDescription);
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
            byte[] bucket = 
                new SHA1CryptoServiceProvider()
                .ComputeHash(Encoding.UTF8.GetBytes(id))
                .Take(1).ToArray();

            var prefix = BitConverter.ToString(bucket).ToLower().Trim(new char[] { '-' });

            return endpoint + "/" + prefix + "/" + id;
        }


        // More info here: http://stackoverflow.com/questions/566462/upload-files-with-httpwebrequest-multipart-form-data
        public static string HttpUploadFile(string url, FileInfo file, string paramName, string contentType, string authorization)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.ContentType = contentType;
            request.Method = "PUT";
            request.Headers[HttpRequestHeader.Authorization] = authorization;
            // request.ReadWriteTimeout = 1000 * 1000;
            request.Timeout = 1000 * 1000;

            // disable this to allow streaming big files, without being out of memory.
            request.AllowWriteStreamBuffering = false;

            request.ContentLength = file.Length;

            using (Stream requestStream = request.GetRequestStream())
            {

                using (FileStream fileStream = file.OpenRead())
                {                  
                    int bufferSize = 1024 * 1024;

                    byte[] buffer = new byte[bufferSize];
                    int bytesRead = 0;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        requestStream.Write(buffer, 0, bytesRead);
                        requestStream.Flush();
                    }

                    // fileStream.CopyTo(requestStream, 1024 * 1024);
                }

            }

            using (var respnse = request.GetResponse())
            {
                Stream responseStream = respnse.GetResponseStream();
                StreamReader responseReader = new StreamReader(responseStream);
                return responseReader.ReadToEnd();
            }
        }


        public override string CreateFile(string id, FileInfo contentsFilePath)
        {
            try
            {
                if (id == null) {
                    id = Guid.NewGuid().ToString();
                }

                HttpUploadFile(url(id), contentsFilePath, "content", "application/octet-stream", authorizationHeader);

                // This method will load all contents in RAM
                //using (var client = new WebClient())
                //{
                //    client.Headers[HttpRequestHeader.Authorization] = authorizationHeader;
                //    client.Headers[HttpRequestHeader.ContentType] = "application/octet-stream";

                //    var response = client.UploadFile(url(id), "PUT", contentsFilePath.FullName);
                //}

                return id;
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
                    client.Headers[HttpRequestHeader.Authorization] = authorizationHeader;

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
