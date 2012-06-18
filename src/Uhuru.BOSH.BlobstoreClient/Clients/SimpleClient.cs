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
    using System.Collections.Specialized;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class SimpleClient : BaseClient
    {
        protected string endpoint;
        protected string authorizationHeader;

        public SimpleClient(dynamic options)
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
            string ret = endpoint + "/resources";
            if (id == null)
            {
                return ret;
            }
            else
            {
                return ret + "/" + id;
            }
        }


        // More info here: http://stackoverflow.com/questions/566462/upload-files-with-httpwebrequest-multipart-form-data
        public static string HttpUploadFile(string url, FileInfo file, string paramName, string contentType, string authorization)
        {
            string boundary = Guid.NewGuid().ToString("N");
            byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";
            request.Headers[HttpRequestHeader.Authorization] = authorization;

            // diable this to allow streaming big files, without beeing out of memory.
            request.AllowWriteStreamBuffering = false;

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, paramName, file, contentType);
            byte[] headerBytes = Encoding.UTF8.GetBytes(header);
            byte[] trailerBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

            request.ContentLength = boundaryBytes.Length + headerBytes.Length + trailerBytes.Length + file.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                requestStream.Write(headerBytes, 0, headerBytes.Length);
                
                FileStream fileStream = file.OpenRead();

                // fileStream.CopyTo(requestStream, 1024 * 1024);

                int bufferSize = 1024 * 1024;

                byte[] buffer = new byte[bufferSize];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    requestStream.Write(buffer, 0, bytesRead);
                    requestStream.Flush();
                }
                fileStream.Close();

                
                requestStream.Write(trailerBytes, 0, trailerBytes.Length);
                requestStream.Close();

            }

            using (var respnse = request.GetResponse())
            {
                Stream responseStream = respnse.GetResponseStream();
                StreamReader responseReader = new StreamReader(responseStream);
                return responseReader.ReadToEnd();
            }
        }


        public override string CreateFile(FileInfo contentsFilePath)
        {
            try
            {
                return HttpUploadFile(url(null), contentsFilePath, "content", "application/octet-stream", authorizationHeader);

                //using (var client = new WebClient())
                //{
                //    client.Headers[HttpRequestHeader.Authorization] = authorizationHeader;
                //    client.Headers[HttpRequestHeader.ContentType] = "application/octet-stream";

                //    var response = client.UploadFile(url(null), "POST", contentsFilePath.FullName);
                    
                //    return Encoding.ASCII.GetString(response);
                //}
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
