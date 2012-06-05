// -----------------------------------------------------------------------
// <copyright file="BaseClient.cs" company="">
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
    using Uhuru.BOSH.BlobstoreClient.Errors;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class BaseClient : IClient
    {

        protected object options;

        protected BaseClient(object options)
        {
            this.options = options;
        }

        public virtual string Create(string contents)
        {
            try
            {
                string tempFile = TempPath();

                File.WriteAllText(tempFile, contents);

                return CreateFile(new FileInfo(tempFile));
            }
            catch (BlobstoreException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new BlobstoreException("Failed to create object", e);
            }
        }

        public virtual string Create(FileInfo contentsFilePath)
        {
            return CreateFile(contentsFilePath);
        }

        public virtual string Get(string id)
        {
            try
            {
                string tempFile = TempPath();

                GetFile(id, new FileInfo(tempFile));

                return File.ReadAllText(tempFile);
            }
            catch (BlobstoreException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new BlobstoreException("Failed to create object", e);
            }
        }

        public virtual void Get(string id, FileInfo outpuFile)
        {
            GetFile(id, outpuFile);         
        }

        public virtual void Delete(string id)
        {
            throw new NotImplementedException();
        }


        // Creates the blob from a file.
        public virtual string CreateFile(FileInfo contentsFilePath)
        {
            throw new NotImplementedException();
        }

        // Gets the blob from a file.
        public virtual void GetFile(string id, FileInfo outpuFile)
        {
            throw new NotImplementedException();
        }

        protected string TempPath()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                File.Delete(path);
            }
            catch{}

            return path;
        }

    }
}
