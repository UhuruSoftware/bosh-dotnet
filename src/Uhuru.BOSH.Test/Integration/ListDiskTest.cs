using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent.Message;
using Uhuru.BOSH.Agent;
using Newtonsoft.Json;

namespace Uhuru.BOSH.Test.Integration
{

    [TestClass]
    public class ListDiskTest
    {
        static string settings = @"{
  ""disks"": {
    ""system"": 0,
    ""ephemeral"": 1,
    ""persistent"": {
      ""1"": 0,
      ""2"": 1
    }
  }
}";

        [TestMethod, TestCategory("Integration"), Timeout(30000), DeploymentItem("log4net.config"), DeploymentItem("unity.config")]
        public void TC001_ListDiskProcessTest()
        {
            // Arrange
            Config.Settings = JsonConvert.DeserializeObject(settings);
            object actual = null;
            ListDisk listDisk = new ListDisk();
            List<string> disks = null;

            // Act
            actual = listDisk.Process(null);

            // Arrange
            Assert.IsNotNull(actual);
            disks = JsonConvert.DeserializeObject<List<string>>(actual.ToString());
            Assert.AreNotEqual(0, disks.Count);
            Assert.IsTrue(disks.Contains("0") || disks.Contains("1"));
        }
    }
}
