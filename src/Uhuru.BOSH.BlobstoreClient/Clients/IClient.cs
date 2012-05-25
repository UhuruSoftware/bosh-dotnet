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

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public interface IClient
    {
        //class Client
        //  def create(contents)
        //  end

        //  def get(id, file = nil)
        //  end

        //  def delete(id)
        //  end
        //end

        string Create(object contents);
        object Get(object id, object file);
        void Delete(string id);
    }
}
