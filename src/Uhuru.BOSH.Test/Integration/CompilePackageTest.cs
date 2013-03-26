using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent;
using System.IO;
using System.Threading;
using Uhuru.NatsClient;
using Newtonsoft.Json;
using Uhuru.BOSH.BlobstoreClient;
using Uhuru.BOSH.BlobstoreClient.Clients;
using System.Reflection;

namespace Uhuru.BOSH.Test.Integration
{
    [TestClass]
    [DeploymentItem("NLog.config")]
    [DeploymentItem("unity.config")]
    public class CompilePackageTest
    {

        private static string configFile = @"Uhuru.BOSH.Test.Resources.settings.json";
        private static string packageFile = @"Uhuru.BOSH.Test.Resources.package.tgz";

        [TestMethod]
        public void TC001_TestCompilePackage()
        {
            byte[] packageBits;
            using (var ms = new MemoryStream())
            {
                Assembly.GetExecutingAssembly().GetManifestResourceStream(packageFile).CopyTo(ms);
                packageBits = ms.ToArray();
            }

            //Arrange
            string fileContent = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(configFile)).ReadToEnd();
            Config.Setup(JsonConvert.DeserializeObject(fileContent), false);
            
            //ConfigBlobStore
            Config.Configure = true;
            Config.BlobstoreProvider = Config.Settings["blobstore"]["provider"].Value;
            Config.BlobstoreOptions = Config.Settings["blobstore"]["options"];

            
            IClient bclient = Blobstore.CreateClient(Config.BlobstoreProvider, Config.BlobstoreOptions);
            string packageId = bclient.Create(packageBits);
            

            //Act
            Handler.Start();
            Thread.Sleep(6000);

            string requestMessage = "{\"method\":\"compile_package\",\"arguments\":[\""+packageId+"\",\"60209d197f9f93a338c93af3228739cdfbd38992\",\"postgresql\",\"5.1\",{}],\"reply_to\":\"director.6d811790-5ceb-4251-93d8-5efa8bbf3bac.5de47a59-21ce-4db6-a7b9-5012a6cdf3bb\"}";

            ManualResetEvent resposeReceived = new ManualResetEvent(false);
            string response = null;

            var natsClient = NatsClient.ReactorFactory.GetReactor(typeof(NatsClient.Reactor));
            natsClient.Start(Config.MessageBus);
            
            natsClient.Subscribe("director.6d811790-5ceb-4251-93d8-5efa8bbf3bac.5de47a59-21ce-4db6-a7b9-5012a6cdf3bb", delegate(string msg, string reply, string subject)
            {
                response = msg;
                resposeReceived.Set();
            });
            
            natsClient.Publish("agent.07dcbefe-e4fc-48b9-97c1-1752d5c1b6df", new SimpleCallback(MessageCallback), requestMessage);

            resposeReceived.WaitOne(4000);

            Assert.AreNotEqual(response, null);

            bclient.Delete(packageId);
        }

        void MessageCallback()
        {
            Console.WriteLine("message sent");
        }

    }
}
