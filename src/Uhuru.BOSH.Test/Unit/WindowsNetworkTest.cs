using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent.Platforms.Windows;
using Uhuru.BOSH.Agent;
using System.IO;
using Newtonsoft.Json;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    public class WindowsNetworkTest
    {
        [TestMethod]
        [DeploymentItem("Resources\\settings.json")]
        [DeploymentItem("Resources\\settings.json", "Bosh")]
        public void TC001_TestNetwork()
        {
            string fileContent = File.ReadAllText("settings.json");
            Config.Setup(JsonConvert.DeserializeObject(fileContent), false); 
            WindowsNetwork.SetupNetwork();
        }
    }
}
