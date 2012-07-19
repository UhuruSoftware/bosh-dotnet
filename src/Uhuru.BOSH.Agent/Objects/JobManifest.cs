using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.BOSH.Agent.Objects
{
    public class JobManifest
    {
        public JobManifest()
        {
            Templates = new Dictionary<string, string>();
            Packages = new List<string>();
        }

        public string Name
        {
            get;
            set;
        }

        public Dictionary<string, string> Templates
        {
            get;
            set;
        }

        public List<string> Packages
        {
            get;
            set;
        }
    }
}
