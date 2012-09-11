using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.BOSH.Agent
{
    public class EncryptionHandler
    {
        private string p;
        private string p2;

        public EncryptionHandler(string p, string p2)
        {
            // TODO: Complete member initialization
            this.p = p;
            this.p2 = p2;
        }

        public string SessionId { get; set; }

        internal Dictionary<string, object> Decrypt(object p)
        {
            throw new NotImplementedException();
        }
    }
}
