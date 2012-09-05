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
            string mountPoint = @"c:\";

            // Act
            int id = DiskUtil.GetDiskIndexForMountPoint(mountPoint);

            // Assert
            Assert.AreEqual(0, id);
        }

        [TestMethod]
        public void TC002_GetDiskIdForScsiDeviceIdTest()
        {
            // Arrange
            int diskId = 0;

            // Act
            int index = DiskUtil.GetDiskIndexForDiskId(diskId);

            // Assert
            Assert.AreEqual(diskId, index);
        }
    }
}
