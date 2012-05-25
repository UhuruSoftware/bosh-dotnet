using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    public class NtpTest
    {
        [TestMethod]
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

        [TestMethod]
        public void TC002_InvalidNtpServer()
        {
            //Arrange
            string invalidTimeServer = "time.invalidTime.itis";

            //Act
            Ntp currentNtp = Ntp.GetNtpOffset(invalidTimeServer);

            //Assert
            Assert.IsNotNull(currentNtp.Message);
        }
    }
}
