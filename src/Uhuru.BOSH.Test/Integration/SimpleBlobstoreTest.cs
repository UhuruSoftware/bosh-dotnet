using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.BlobstoreClient;
using Uhuru.BOSH.BlobstoreClient.Clients;
using System.Yaml;
using System.IO;
using Uhuru.BOSH.BlobstoreClient.Errors;

namespace Uhuru.BOSH.Test.Integration
{
    [TestClass]
    public class SimpleBlobstoreTest
    {


        string yamlOptions =@"
{
    ""blobstore"":
    {
        ""plugin"":""simple"",
        ""properties"":
        {
            ""endpoint"":""http://10.0.3.30:25251"",
            ""user"":""agent"",
            ""password"":""agent""
        }
    }
}";

        string yamlOptions2 = @"
{
    ""blobstore"":
    {
        ""plugin"":""simple"",
        ""properties"":
        {
            ""endpoint"":""http://127.0.0.1:9999"",
            ""user"":""admin"",
            ""password"":""admin""
        }
    }
}";

        string content = "tralalalalalalal";

        [TestMethod]
        public void CreateContentTest()
        {
            dynamic ynode = YamlNode.FromYaml(yamlOptions).FirstOrDefault();


            IClient sc = Blobstore.CreateClient("simple", ynode["blobstore"]["properties"]);

            string objectId = sc.Create(content);

            string pulledContent = sc.Get(objectId);
            Assert.AreEqual(content, pulledContent);

            sc.Delete(objectId);
        }

        [TestMethod]
        public void CreateFileTest()
        {            
            FileInfo fileToUpload = new FileInfo(Path.GetTempFileName());
            FileInfo downloadedFile = new System.IO.FileInfo(Path.GetTempFileName());

            // Set the file size
            using (var fstream = fileToUpload.OpenWrite())
            {
                fstream.SetLength(1024 * 1024 * 500);
            }

            dynamic ynode = YamlNode.FromYaml(yamlOptions).FirstOrDefault();

            IClient sc = Blobstore.CreateClient("simple", ynode["blobstore"]["properties"]);

            string objectId = sc.Create(fileToUpload);

            sc.Get(objectId, downloadedFile);

            Assert.IsTrue(fileToUpload.Length == downloadedFile.Length);

            fileToUpload.Delete();
            downloadedFile.Delete();

            sc.Delete(objectId);
        }

        [TestMethod]
        [ExpectedException(typeof(BlobstoreException), "Could not delete object")]
        public void InvalidDeleteTest()
        {
            dynamic ynode = YamlNode.FromYaml(yamlOptions).FirstOrDefault();

            IClient sc = Blobstore.CreateClient("simple", ynode["blobstore"]["properties"]);

            sc.Delete("test");
        }

    }
}
