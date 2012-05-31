using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent;
using System.Yaml;
using System.IO;
using System.Threading;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    [DeploymentItem("log4net.config")]
    public class HandlerTests
    {
        private static string configFile = @"E:\_work\bosh-dotnet\src\Uhuru.BOSH.Test\Resources\settings.json";
        [TestMethod]
        public void TC001_TestHandler()
        {
            //Arrange
            YamlNode root = null;
            using (TextReader textReader = new StreamReader(configFile))
            {
                YamlNode[] nodes = YamlNode.FromYaml(textReader);
                root = nodes[0];
            }
            Config.Setup(root, false);

            //Act
            Handler.Start();
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
