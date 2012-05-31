using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent.Message;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    public class DiskUtilTest
    {
        [TestMethod]
        public void TC001_GetVolumeDeviceIdTest()
        {
            // Arrange
            string mountPoint = @"C:\";

            // Act
            int id = DiskUtil.GetDiskIdForMountPoint(mountPoint);

            // Assert
            Assert.AreEqual(0, id);
        }
    }
}
