using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent;
using System.Yaml;
using System.IO;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    public class ConfigTest
    {
        [TestMethod]
        public void TC001_LoadConfigFirstRun()
        {
            //Arrange
            string configFile = @"E:\_work\bosh-dotnet\src\Uhuru.BOSH.Test\Resources\settings.json";
            YamlNode root = null;

            //Act
            using (TextReader textReader = new StreamReader(configFile))
            {
                 YamlNode[] nodes = YamlNode.FromYaml(textReader);
                root = nodes[0];
            }
            Config.Setup(new YamlMapping(), true);

            //Assert
            
        }

        [TestMethod]
        public void TC002_LoadConfigExistingFile()
        {
            //Arrange
            string configFile = @"E:\_work\bosh-dotnet\src\Uhuru.BOSH.Test\Resources\settings.json";
            YamlNode root = null;

            //Act
            using (TextReader textReader = new StreamReader(configFile))
            {
                YamlNode[] nodes = YamlNode.FromYaml(textReader);
                root = nodes[0];
            }
            Config.Setup(root,false);

            //Assert
        }


    }
}
