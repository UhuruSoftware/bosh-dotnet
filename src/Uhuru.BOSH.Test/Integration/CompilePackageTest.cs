using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent;
using System.Yaml;
using System.IO;
using System.Threading;
using Uhuru.NatsClient;
using Newtonsoft.Json;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    [DeploymentItem("log4net.config")]
    [DeploymentItem("unity.config")]
    public class CompilePackageTest
    {
        private static string configFile = @"C:\vcap\bosh\settings.json";
        [TestMethod]
        public void TC001_TestCompilePackage()
        {
            //Arrange
            string fileContent = File.ReadAllText(configFile);
            Config.Setup(JsonConvert.DeserializeObject(fileContent), false);

            //ConfigBlobStore
            Config.Configure = true;
            Config.BlobstoreProvider = Config.Settings["blobstore"]["plugin"].Value;
            Config.BlobstoreOptions = Config.Settings["blobstore"]["properties"];
            
            //Act
            Handler.Start();
            Thread.Sleep(6000);

            string requestMessage = "{\"method\":\"compile_package\",\"arguments\":[\"b2e43311-e80f-41f2-9da9-3268b8cc9a82\",\"60209d197f9f93a338c93af3228739cdfbd38992\",\"postgresql\",\"5.1\",{}],\"reply_to\":\"director.6d811790-5ceb-4251-93d8-5efa8bbf3bac.5de47a59-21ce-4db6-a7b9-5012a6cdf3bb\"}";
            
            var natsClient = NatsClient.ReactorFactory.GetReactor(typeof (NatsClient.Reactor));
            natsClient.Start(Config.MessageBus);
            natsClient.Publish("agent.07dcbefe-e4fc-48b9-97c1-1752d5c1b6df", new SimpleCallback(MessageCallback), requestMessage);

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        void MessageCallback()
        {
            Console.WriteLine("message sent");
        }

    }
}
