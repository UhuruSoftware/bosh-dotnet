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
using Uhuru.BOSH.Agent.Objects;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    [DeploymentItem("log4net.config")]
    [DeploymentItem("unity.config")]
    public class HandlerTests
    {
        private static string configFile = @"C:\vcap\bosh\settings.json";
        [TestMethod]
        public void TC001_TestHandler()
        {
            //Arrange
            string fileContent = File.ReadAllText(configFile);
            Config.Setup(JsonConvert.DeserializeObject(fileContent), false);
            
            //ConfigBlobStore
            Config.Configure = true;
            Config.BlobstoreProvider = Config.Settings["blobstore"]["plugin"].Value;
            Config.BlobstoreOptions = Config.Settings["blobstore"]["properties"];
            Monit.GetInstance().Start();
            //Act
            Handler.Start();
            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        [TestMethod]
        public void TC002_TestPing()
        {
            //Arrange
            YamlNode root = null;
            using (TextReader textReader = new StreamReader(configFile))
            {
                YamlNode[] nodes = YamlNode.FromYaml(textReader);
                root = nodes[0];
            }
            Config.Setup(root, false);
            Handler.Start();
            Thread.Sleep(5000);
            string pingMessage = "{\"method\":\"ping\",\"arguments\":[],\"reply_to\":\"director.6d811790-5ceb-4251-93d8-5efa8bbf3bac.5de47a59-21ce-4db6-a7b9-5012a6cdf3bb\"}";

            //Act
            Config.Nats.Publish("agent.2df8bff9-7af3-4aad-9684-6c9fe93ab64e", new SimpleCallback(MessageCallback), pingMessage);
            Thread.Sleep(20000000);
            //Assert
        }

        void MessageCallback()
        {
            Console.WriteLine("iseste");
        }

        [TestMethod]
        public void TC003_test_aa()
        {
            JobManifest testjobManifest = LoadManifest(@"E:\_me\job.MF");
        }

        private static JobManifest LoadManifest(string jobManifestPath)
        {
            string[] fileContent = File.ReadAllLines(jobManifestPath);
            //dynamic job = JsonConvert.DeserializeObject(fileContent);
            JobManifest jobManifest = new JobManifest();


            for (int i = 0; i < fileContent.Length; i++)
            {
                //get name
                if (fileContent[i].StartsWith("name", StringComparison.OrdinalIgnoreCase))
                {
                    jobManifest.Name = fileContent[i].Split(':')[1].Trim();
                }

                if (fileContent[i].StartsWith("templates", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    while (!String.IsNullOrEmpty(fileContent[i]))
                    {
                        jobManifest.AddTemplate(fileContent[i].Split(':')[0].Trim(), fileContent[i].Split(':')[1].Trim());
                        i++;
                    }
                }
                if (fileContent[i].StartsWith("packages", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    while (!String.IsNullOrEmpty(fileContent[i]) || i == fileContent.Length)
                    {
                        jobManifest.AddPackage(fileContent[i].Split('-')[1].Trim());
                        i++;
                    }
                }
            }
            return jobManifest;
        }
    }
}
