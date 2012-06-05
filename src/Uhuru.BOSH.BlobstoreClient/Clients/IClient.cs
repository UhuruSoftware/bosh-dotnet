// -----------------------------------------------------------------------
// <copyright file="Client.cs" company="">
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

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public interface IClient
    {
        //IClient();

        //IClient(object options);

        // Create a new blob and returns the blob ID
        string Create(string contents);
        
        // Creates a new blob from file and returns the blob ID
        string Create(FileInfo contentsFilePath);

        // Gets the blob content
        string Get(string id);

        // Get the blob from the ID and writes the output to a file
        void Get(string id, FileInfo outputFile);

        // Deletes a blob
        void Delete(string id);
    }
}
