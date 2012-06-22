using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent;
using System.Yaml;
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
        public void TC001_LoadConfigFirstRun()
        {
            //Arrange
            string configFile = @"E:\_work\bosh-dotnet\src\Uhuru.BOSH.Test\Resources\settings.json";
            YamlNode root = null;

            //Act
            //string fileContent = File.ReadAllText(configFile);
            

            Config.Setup(new Newtonsoft.Json.Linq.JObject(), true);

            //Assert
            
        }

        [TestMethod]
        public void TC002_LoadConfigExistingFile()
        {
            //Arrange
            string configFile = @"E:\_work\bosh-dotnet\src\Uhuru.BOSH.Test\Resources\settings.json";
            YamlNode root = null;

            //Act
            string fileContent = File.ReadAllText(configFile);

            Config.Setup(JsonConvert.DeserializeObject(fileContent),false);
            Console.WriteLine("");
            //Assert
        }


    }
}
