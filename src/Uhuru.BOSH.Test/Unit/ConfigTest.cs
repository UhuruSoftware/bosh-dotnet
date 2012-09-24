using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent;
using System.IO;
using Newtonsoft.Json;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    [DeploymentItem("log4net.config")]
    [DeploymentItem("unity.config")]
    public class ConfigTest
    {
        [TestMethod]
        [DeploymentItem("Resources\\settings.json")]
        public void TC002_LoadConfigExistingFile()
        {
            //Arrange
            string configFile = @"settings.json";

            //Act
            string fileContent = File.ReadAllText(configFile);

            Config.Setup(JsonConvert.DeserializeObject(fileContent),false);
            Console.WriteLine("");
            //Assert
        }


    }
}
