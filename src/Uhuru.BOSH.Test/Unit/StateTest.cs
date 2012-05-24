using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent;
using Uhuru.BOSH.Agent.Objects;
using System.IO;
using System.Yaml;
using Uhuru.BOSH.Agent.Errors;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    public class StateTest
    {
        private static string yamlLocation = @"E:\_work\bosh-dotnet\src\Uhuru.BOSH.Test\Assets\apply_spec.yml";

        [TestMethod]
        public void TC001_TestStateNoConfigFile()
        {
            //Arrange
            string randomString = Guid.NewGuid().ToString();

            //Act
            State testState = new State(randomString);

            //Assert
            Assert.IsNull(testState.Job);
            Assert.Equals(0, testState.Networks.Count);
            
        }


        [TestMethod]
        public void TC002_TestGetIps()
        {
            //Arrange
            State testState = new State(yamlLocation);

            //Act
            List<string> ips = testState.GetIps().ToList();

            //Assert
            Assert.AreEqual(1, ips.Count);
            Assert.AreEqual(ips[0], "127.0.0.1");
            
        }

        [TestMethod]
        public void TC003_TestJob()
        {
            //Arrange
            State testState = new State(yamlLocation);
            
            //Act
            Job currentJob = testState.Job;

            //Assert
            Assert.AreEqual("micro", currentJob.Name);
            Assert.AreEqual("0.5-dev", currentJob.Version);
            Assert.AreEqual("d5941f9a113489e8bc14adb61d3bb619cd9566bd", currentJob.Sha1);
            Assert.AreEqual("micro", currentJob.Template);
            Assert.AreEqual("da4dea43-fa56-4a4a-bb18-dba8961d0f6f", currentJob.Blobstore_id);
            
        }

        [TestMethod]
        public void TC004_TestValue()
        {
            //Arrange
            State testState = new State(yamlLocation);

            //Act
            string deployment = ((dynamic)testState.GetValue("deployment")).Value;

            //Assert
            Assert.AreEqual("micro", deployment);

        }

        [TestMethod]
        public void TC005_TestNetworks()
        {
            //Arrange
            State testState = new State(yamlLocation);

            //Act
            Network network = testState.Networks.First();

            //Assert
            Assert.AreEqual("127.0.0.1", network.Ip);
            Assert.AreEqual("local", network.Name);

        }

        [TestMethod]
        public void TC006_TestWrite()
        {
            //Arrange
            string newFile = yamlLocation.Replace("apply_spec.yml", "apply_spec_test.yml");
            State testState = new State(yamlLocation);
            State newTestState = new State(newFile);
            StateException exception = null;
            YamlNode[] data;
            using (TextReader textReader = new StreamReader(yamlLocation))
            {
                data = YamlNode.FromYaml(textReader);
            }

            //Act
            try
            {
                newTestState.Write(data[0]);
            }
            catch (StateException stateException)
            {
                exception = stateException;
            }

            //Assert
            Assert.IsNull(exception);
            Assert.AreEqual(newTestState.Job.Name, testState.Job.Name);
        }


        
    }
}
