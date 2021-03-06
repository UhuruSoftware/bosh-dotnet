﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent;
using System.Threading;

namespace Uhuru.BOSH.Test.Integration
{
    [TestClass]
    [DeploymentItem("NLog.config")]
    [DeploymentItem("unity.config")]
    public class MonitTest
    {
        [TestMethod]
        [DeploymentItem(@"Resources\Monit\MonitFile1.xml", "Monit")]
        public void TC002_TestGetVitals()
        {
            //Arrange
            Monit monit = Monit.GetInstance(@"Monit");

            //Act
            Vitals vitals = monit.GetVitals;

            //Assert
            Assert.IsNotNull(vitals.CPU);
            Assert.IsNotNull(vitals.CPU.Sys);
            Assert.IsNotNull(vitals.Disk);
            Assert.IsNotNull(vitals.Load);
            Assert.IsNotNull(vitals.Memory);

        }
    }
}
