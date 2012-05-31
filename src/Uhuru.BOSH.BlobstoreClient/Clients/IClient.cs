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

        string Create(object contents);

        object Get(string id);

        void Get(string id, string outpuFile);

        void Delete(string id);
    }
}
