using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent.Message;
using Uhuru.BOSH.Agent;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass, DeploymentItem("log4net.config"), DeploymentItem("unity.config")]
    public class DiskUtilTest
    {
        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC001_GetDiskIndexForMountPointTest()
        {
            // Arrange
            string mountPoint = @"c:\";

            // Act
            int id = DiskUtil.GetDiskIndexForMountPoint(mountPoint);

            // Assert
            Assert.AreNotEqual(-1, id);
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC002_GetDiskIndexForDiskIdTest()
        {
            // Arrange
            int diskId = 0;

            // Act
            int index = DiskUtil.GetDiskIndexForDiskId(diskId);

            // Assert
            Assert.AreNotEqual(int.MinValue, index);
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC003_IsMountPointTest()
        {
            // Arrange
            string mountPoint = @"c:\";

            // Act
            bool isMountPoint = DiskUtil.IsMountPoint(mountPoint);

            // Assert
            Assert.IsTrue(isMountPoint);
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC004_GetVolumeDeviceIdTest()
        {
            // Arrange
            string mountPoint = @"c:\";

            // Act
            string volumeDeviceId = DiskUtil.GetVolumeDeviceId(mountPoint);

            // Assert
            Assert.IsNotNull(volumeDeviceId);
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC005_DiskHasPartitionTest()
        {
            // Arrange
            int diskId = 0;

            // Act
            bool hasPartition = DiskUtil.DiskHasPartition(diskId);

            // Assert
            Assert.IsTrue(hasPartition);
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC006_GetUsageTest()
        {
            // Arrange
            Dictionary<string, object> usage = new Dictionary<string, object>();
            Config.BaseDir = @"c:\vcap";

            // Act
            usage = DiskUtil.GetUsage;

            // Assert
            Assert.AreNotEqual(0 ,(int)usage["system"]);
        }
    }
}
