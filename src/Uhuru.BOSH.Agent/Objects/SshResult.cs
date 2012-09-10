using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Uhuru.BOSH.Agent.Objects
{
    public class SshResult
    {
        [JsonProperty("command")]
        public string Command
        {
            get;
            set;
        }

        [JsonProperty("status")]
        public string Status
        {
            get;
            set;
        }

        [JsonProperty("ip")]
        public string IP
        {
            get;
            set;
        }
    }
}
