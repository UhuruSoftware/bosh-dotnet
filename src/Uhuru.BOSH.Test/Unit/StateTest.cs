using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent;
using Uhuru.BOSH.Agent.Objects;
using System.IO;
using Uhuru.BOSH.Agent.Errors;
using Newtonsoft.Json.Linq;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    [DeploymentItem("Assets\\state.yml")]
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
            Assert.AreEqual(0, testState.Networks.Count);
            
        }


        [TestMethod]
        public void TC002_TestGetIps()
        {
            //Arrange
            State testState = new State("state.yml");

            //Act

            string test = GetRubyObject(testState.GetValue("properties"));
            

            List<string> ips = testState.GetIPs.ToList();

            //Assert
            Assert.AreEqual(1, ips.Count);
            Assert.AreEqual(ips[0], "10.0.3.132");
            
        }

        public string GetRubyObject(dynamic jsonProperty)
        {
            StringBuilder currentObject = new StringBuilder();
            if (jsonProperty.GetType() == typeof(JObject))
            {
                currentObject.Append("{");
            }
            if ((jsonProperty as JContainer).Children().Count() != 0)
            {
             
                foreach (var child in jsonProperty.Children())
                {
                    if (child.GetType() == typeof(JValue))
                    {
                        string childValue = child.ToString();
                        
                        //Escaping \ character
                        childValue = childValue.Replace(@"\", @"\\");
                        return "\"" + childValue + "\"";
                    }
                    if (child.GetType() == typeof(JProperty))
                    {
                        if (currentObject.ToString() != "{")
                            currentObject.Append(", ");
                        currentObject.Append(child.Name + ": ");
                        currentObject.Append(GetRubyObject(child));

                    }

                    //TODO IMPROVE JARAY
                    if (child.GetType() == typeof(JArray))
                        return "{}";
                    if (child.GetType() == typeof(JObject))
                    {
                        currentObject.Append(GetRubyObject(child));
                    }
                }
               
            }
            if (jsonProperty.GetType() == typeof(JObject))
            {
                currentObject.Append("}");
            }
            return currentObject.ToString();
        }

        [TestMethod]
        public void TC003_TestJob()
        {
            //Arrange
            State testState = new State("state.yml");
            
            //Act
            Job currentJob = testState.Job;

            //Assert
            Assert.AreEqual("win_dea", currentJob.Name);
            Assert.AreEqual("0.2-dev", currentJob.Version);
            Assert.AreEqual("cea000d7e5611f3fff6ddbf46163b6a4b025664b", currentJob.SHA1);
            Assert.AreEqual("win_dea", currentJob.Template);
            Assert.AreEqual("c9034ac9-ffc1-4aff-b17a-9be8533a6bfa", currentJob.BlobstoreId);
            
        }

        [TestMethod]
        public void TC004_TestValue()
        {
            //Arrange
            State testState = new State("state.yml");

            //Act
            string deployment = ((dynamic)testState.GetValue("deployment")).Value;

            //Assert
            Assert.AreEqual("uhuru-cf", deployment);

        }

        [TestMethod]
        public void TC005_TestNetworks()
        {
            //Arrange
            State testState = new State("state.yml");

            //Act
            Network network = testState.Networks.First();

            //Assert
            Assert.AreEqual("10.0.3.132", network.IP);
            Assert.AreEqual("default", network.Name);

        }

        [TestMethod]
        public void TC006_TestWrite()
        {
            //Arrange
            string newFile = "state_test.yml";
            State testState = new State("state.yml");
            State newTestState = new State(newFile);
            StateException exception = null;

            //Act
            try
            {
                newTestState.Write(testState.ToHash());
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
