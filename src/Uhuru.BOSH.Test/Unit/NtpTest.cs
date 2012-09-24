using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass, DeploymentItem("log4net.config"), DeploymentItem("unity.config")]
    public class NtpTest
    {
        [TestMethod, TestCategory("Unit"), Timeout(20000)]
        public void TC001_TestNtp()
        {
            //Arrange
            string timeServer = "time.windows.com";

            //Act
            Ntp currnetNtp = Ntp.GetNtpOffset(timeServer);

            //Assert
            Assert.AreEqual(null, currnetNtp.Message);
            Assert.AreNotEqual(0, currnetNtp.Offset);
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC002_InvalidNtpServer()
        {
            //Arrange
            string invalidTimeServer = "time.invalidTime.itis";

            //Act
            Ntp currentNtp = Ntp.GetNtpOffset(invalidTimeServer);

            //Assert
            Assert.IsNotNull(currentNtp.Message);
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC003_NullNtpServer()
        {
            //Arrange
            Exception expected = null;
            Ntp currentNtp = null;

            //Act
            try
            {
                currentNtp = Ntp.GetNtpOffset(null);
            }
            catch (Exception ex)
            {
                expected = ex;
            }

            //Assert
            Assert.IsNull(currentNtp);
            Assert.IsNotNull(expected);
            Assert.IsInstanceOfType(expected, typeof(ArgumentNullException));
        }
    }
}
