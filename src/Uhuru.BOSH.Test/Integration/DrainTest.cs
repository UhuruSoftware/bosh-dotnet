using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Uhuru.BOSH.Agent;
using System.Threading;
using Uhuru.BOSH.Agent.Message;
using Newtonsoft.Json;

namespace Uhuru.BOSH.Test.Integration
{
    [TestClass]
    [DeploymentItem("log4net.config")]
    [DeploymentItem("unity.config")]
    public class DrainTest
    {
        private static string configFile = @"C:\vcap\bosh\settings.json";

        [TestMethod]
        public void TC001_TestDrainStatus()
        {
            //Arrange
            string fileContent = File.ReadAllText(configFile);
            Config.Setup(JsonConvert.DeserializeObject(fileContent), false);
            string argsString = "[\"status\"]";
            dynamic args = JsonConvert.DeserializeObject(argsString); 
            //ConfigBlobStore
            Config.Configure = true;
            Config.BlobstoreProvider = Config.Settings["blobstore"]["plugin"].Value;
            Config.BlobstoreOptions = Config.Settings["blobstore"]["properties"];
            Monit.GetInstance().Start();

            //Act
            Drain drain = new Drain();
            object drainResult = drain.Process(args);

            //Assert
            Assert.AreEqual(drainResult.ToString(), "0");
        }

        [TestMethod]
        public void TC002_TestDrainShutdown()
        {
            //Arrange
            string fileContent = File.ReadAllText(configFile);
            Config.Setup(JsonConvert.DeserializeObject(fileContent), false);
            string argsString = "[\"shutdown\"]";
            dynamic args = JsonConvert.DeserializeObject(argsString);
            //ConfigBlobStore
            Config.Configure = true;
            Config.BlobstoreProvider = Config.Settings["blobstore"]["plugin"].Value;
            Config.BlobstoreOptions = Config.Settings["blobstore"]["properties"];
            Config.Nats = new NatsClient.Reactor();
            Uri natsUri = new Uri(Config.MessageBus);
            Config.Nats.Start(natsUri);
            Monit.GetInstance().Start();

            //Act
            Drain drain = new Drain();
            object drainResult = drain.Process(args);

            //Assert
            Assert.AreEqual(drainResult.ToString(), "0");
        }


        [TestMethod]
        public void TC003_TestDrainUpdate()
        {
            //Arrange
            string fileContent = File.ReadAllText(configFile);
            Config.Setup(JsonConvert.DeserializeObject(fileContent), false);
            string argsString = "[\"update\",{\"deployment\":\"uhuru-cf-mitza\",\"release\":{\"name\":\"uhuru-cf\",\"version\":\"86.11-dev\"},\"job\":{\"name\":\"win_dea\",\"release\":\"uhuru-cf\",\"template\":\"win_dea\",\"version\":\"0.2-dev\",\"sha1\":\"cea000d7e5611f3fff6ddbf46163b6a4b025664b\",\"blobstore_id\":\"c9034ac9-ffc1-4aff-b17a-9be8533a6bfa\"},\"index\":0,\"networks\":{\"default\":{\"ip\":\"10.0.3.137\",\"netmask\":\"255.255.0.0\",\"cloud_properties\":{\"name\":\"VM Network\"},\"default\":[\"dns\",\"gateway\"],\"dns\":[\"10.0.0.10\",\"8.8.8.8\"],\"gateway\":\"10.0.0.1\"}},\"resource_pool\":{\"name\":\"windowspool\",\"cloud_properties\":{\"ram\":1000,\"cpu\":1,\"disk\":10192},\"stemcell\":{\"name\":\"test-stemcell\",\"version\":\"0.1.4\"}},\"packages\":{\"win_dea1\":{\"name\":\"win_dea\",\"version\":\"0.1-dev.1\",\"sha1\":\"8a90c0c416601e90bbe6a177d79d1efc2e47d526\",\"blobstore_id\":\"ba697254-60d5-4360-816d-e4dd5d3b8d02\"}},\"persistent_disk\":0,\"configuration_hash\":\"f7452dda13a19bc46a15d725e0151007ae194e2b\",\"properties\":{\"domain\":\"vcap.me\",\"env\":{},\"networks\":{\"apps\":\"default\",\"management\":\"default\"},\"nats\":{\"user\":\"nats\",\"password\":\"nats\",\"address\":\"10.0.3.136\",\"port\":4222},\"ccdb\":{\"user\":\"ccadmin\",\"password\":\"ccadmin\",\"address\":\"10.0.3.136\",\"port\":5524,\"dbname\":\"appcloud\",\"databases\":[{\"tag\":\"cc\",\"name\":\"appcloud\"}],\"roles\":[{\"tag\":\"admin\",\"name\":\"ccadmin\",\"password\":\"ccadmin\"}]},\"cc\":{\"srv_api_uri\":\"http://api.vcap.me\",\"password\":\"aaauuLaap44jadlas2l312lk\",\"token\":\"aaaf4eaa8c1758f66d5cb7adcb24adb9d7\",\"use_nginx\":false,\"allow_registration\":true,\"uaa\":{\"enabled\":false,\"resource_id\":\"cloud_controller\",\"client_secret\":\"aaaNxRJcx6cpv\"},\"admins\":[\"dev@cloudfoundry.org\"]},\"router\":{\"status\":{\"port\":8080,\"user\":\"vcap\",\"password\":\"vcap\"}},\"dashboard\":{\"users\":[[\"dash\",\"admin\"]]},\"dea\":{\"max_memory\":4096},\"nfs_server\":{\"address\":\"10.0.3.136\",\"network\":\"10.0.0.0/16\"},\"opentsdb\":{\"address\":\"10.0.3.136\",\"port\":4242},\"service_plans\":{\"mysql\":{\"free\":{\"job_management\":{\"high_water\":1400,\"low_water\":100},\"configuration\":{\"allow_over_provisioning\":true,\"capacity\":200,\"max_db_size\":128,\"max_long_query\":3,\"max_long_tx\":0,\"max_clients\":20}}},\"postgresql\":{\"free\":{\"job_management\":{\"high_water\":1400,\"low_water\":100},\"configuration\":{\"capacity\":200,\"max_db_size\":128,\"max_long_query\":3,\"max_long_tx\":30,\"max_clients\":20}}},\"mongodb\":{\"free\":{\"job_management\":{\"high_water\":3000,\"low_water\":100},\"configuration\":{\"allow_over_provisioning\":true,\"capacity\":200,\"quota_files\":4,\"max_clients\":500}}},\"redis\":{\"free\":{\"job_management\":{\"high_water\":1400,\"low_water\":100},\"configuration\":{\"capacity\":200,\"max_memory\":16,\"max_swap\":32,\"max_clients\":500}}},\"rabbit\":{\"free\":{\"job_management\":{\"low_water\":100,\"high_water\":1400},\"configuration\":{\"max_memory_factor\":0.5,\"max_clients\":512,\"capacity\":200}}}},\"mysql_gateway\":{\"check_orphan_interval\":7200,\"token\":\"0xfeedface\"},\"mysql_node\":{\"production\":true,\"password\":\"aaa13djkas\"},\"redis_gateway\":{\"token\":\"0xfeedface\",\"check_orphan_interval\":7200},\"redis_node\":{\"command_rename_prefix\":\"sample\"},\"mongodb_gateway\":{\"check_orphan_interval\":7200,\"token\":\"0xfeedface\"},\"mongodb_node\":{},\"uhurufs_gateway\":{\"check_orphan_interval\":7200,\"token\":\"0xfeedface\"},\"uhurufs_node\":{\"statusport\":12345,\"basedir\":\"c:\\\\vcap\\\\store\\\\uhurufs\",\"capacity\":200},\"rabbit_gateway\":{\"token\":\"AAA430A8BAED490B1240338DA34B10D\"},\"rabbit_node\":{},\"postgresql_gateway\":{\"check_orphan_interval\":7200,\"token\":\"aaaeAh4BXFBXwLrrWJCpQTfeDnaCn7m\"},\"postgresql_node\":{\"production\":true},\"uaa\":{},\"win_dea\":{\"localroute\":\"10.0.3.30\",\"fillerport\":12345}}}]";
            dynamic args = JsonConvert.DeserializeObject(argsString);
            //ConfigBlobStore
            Config.Configure = true;
            Config.BlobstoreProvider = Config.Settings["blobstore"]["plugin"].Value;
            Config.BlobstoreOptions = Config.Settings["blobstore"]["properties"];
            Config.Nats = new NatsClient.Reactor();
            Uri natsUri = new Uri(Config.MessageBus);
            Config.Nats.Start(natsUri);
            Monit.GetInstance().Start();

            //Act
            Drain drain = new Drain();
            object drainResult = drain.Process(args);

            //Assert
            Assert.AreEqual(drainResult.ToString(), "0");
        }
    }
}
