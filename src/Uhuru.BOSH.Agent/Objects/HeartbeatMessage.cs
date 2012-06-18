using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Uhuru.BOSH.Agent.Objects
{
    public class HeartbeatMessage
    {
        [JsonProperty("job")]
        public string Job { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("job_state")]
        public string JobState { get; set; }

        [JsonProperty("vitals")]
        public Vitals Vitals { get; set; }

        [JsonProperty("ntp")]
        public NtpMessage NtpMsg { get; set; }

        public class NtpMessage
        {
            [JsonProperty("offset")]
            public string Offset { get; set; }

            [JsonProperty("timestamp")]
            public string Timestamp { get; set; }
        }
    }
}
