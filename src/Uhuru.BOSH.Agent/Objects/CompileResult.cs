using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Uhuru.BOSH.Agent.Objects
{
    public class CompileResult
    {
        public class UploadResult
        {
            [JsonProperty("sha1")]
            public string Sha1 { get; set; }

            [JsonProperty("blobstore_id")]
            public string BlobstoreId { get; set; }

            [JsonProperty("compile_log")]
            public string CompileLogId { get; set; }
        }

        [JsonProperty("result")]
        public UploadResult result;

    }
}
