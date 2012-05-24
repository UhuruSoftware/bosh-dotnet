using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent;
using Uhuru.BOSH.Agent.Objects;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    public class StateTest
    {

        [TestMethod]
        public void TC001_TestStateNoConfigFile()
        {
            //Arrange
            string randomString = Guid.NewGuid().ToString();

            //Act
            State testState = new State(randomString);

            //Assert
            Assert.IsNull(testState.Job);
            
        }


        [TestMethod]
        public void TC002_TestGetIps()
        {
            //Arrange
            string configFileLocation = @"E:\_work\bosh-dotnet\src\Uhuru.BOSH.Test\Assets\apply_spec.yml";
            

            //Act
            State testState = new State(configFileLocation);

            //Assert
            List<string> ips = testState.GetIps().ToList();
        }

        [TestMethod]
        public void TC003_TestJob()
        {
            //Arrange
            string configFileLocation = @"E:\_work\bosh-dotnet\src\Uhuru.BOSH.Test\Assets\apply_spec.yml";
            State testState = new State(configFileLocation);
            
            //Act
            Job currentJob = testState.Job;

            //Assert
            Assert.AreEqual("micro", currentJob.Name);
            Assert.AreEqual("0.5-dev", currentJob.Version);
            Assert.AreEqual("d5941f9a113489e8bc14adb61d3bb619cd9566bd", currentJob.Sha1);
            Assert.AreEqual("micro", currentJob.Template);
            Assert.AreEqual("da4dea43-fa56-4a4a-bb18-dba8961d0f6f", currentJob.Blobstore_id);
            
        }
    }
}
