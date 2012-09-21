using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent.Message;
using Uhuru.BOSH.Agent;
using Uhuru.BOSH.Agent.Errors;
using System.IO;

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

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC007_MountEntryInvalidDiskIdTest()
        {
            // Arrange
            int diskId = int.MinValue;
            string actual = null;

            // Act
            actual = DiskUtil.MountEntry(diskId);

            // Assert
            Assert.IsNull(actual);
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC008_DiskHasPartitionInvalidDiskIdTest()
        {
            // Arrange
            int diskId = int.MinValue;
            Exception expected = null;
            bool hasPartition = false;

            // Act
            try
            {
                hasPartition = DiskUtil.DiskHasPartition(diskId);
            }
            catch (Exception ex)
            {
                expected = ex;
            }

            // Assert
            Assert.IsFalse(hasPartition);
            Assert.IsNotNull(expected);
            Assert.IsInstanceOfType(expected, typeof(MessageHandlerException));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC009_IsMountPointNullPathTest()
        {
            // Arrange
            string mountPoint = null;
            Exception expected = null;
            bool isMountPoint = false;

            // Act
            try
            {
                isMountPoint = DiskUtil.IsMountPoint(mountPoint);
            }
            catch (Exception ex)
            {
                expected = ex;
            }

            // Assert
            Assert.IsFalse(isMountPoint);
            Assert.IsNotNull(expected);
            Assert.IsInstanceOfType(expected, typeof(ArgumentNullException));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC010_IsMountPointInvalidPathTest()
        {
            // Arrange
            string mountPoint = Path.Combine(@"c:\", Guid.NewGuid().ToString());

            // Act
            bool isMountPoint = DiskUtil.IsMountPoint(mountPoint);

            // Assert
            Assert.IsFalse(isMountPoint);
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC011_GetDiskIndexForMountPointInvalidMountPointTest()
        {
            // Arrange
            string mountPoint = Path.Combine(@"c:\", Guid.NewGuid().ToString());
            int id = int.MinValue;
            Exception expected = null;

            // Act
            try
            {
                id = DiskUtil.GetDiskIndexForMountPoint(mountPoint);
            }
            catch (Exception ex)
            {
                expected = ex;
            }
            
            // Assert
            Assert.AreEqual(int.MinValue, id);
            Assert.IsNotNull(expected);
            Assert.IsInstanceOfType(expected, typeof(MessageHandlerException));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC012_GetDiskIndexForMountPointNullMountPointTest()
        {
            // Arrange
            string mountPoint = null;
            int id = int.MinValue;
            Exception expected = null;

            // Act
            try
            {
                id = DiskUtil.GetDiskIndexForMountPoint(mountPoint);
            }
            catch (Exception ex)
            {
                expected = ex;
            }

            // Assert
            Assert.AreEqual(int.MinValue, id);
            Assert.IsNotNull(expected);
            Assert.IsInstanceOfType(expected, typeof(ArgumentNullException));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC013_GetVolumeDeviceIdNullMountPointTest()
        {
            // Arrange
            string mountPoint = null;
            Exception expected = null;
            string volumeDeviceId = null;

            // Act
            try
            {
                volumeDeviceId = DiskUtil.GetVolumeDeviceId(mountPoint);
            }
            catch (Exception ex)
            {
                expected = ex;
            }

            // Assert
            Assert.IsNull(volumeDeviceId);
            Assert.IsNotNull(expected);
            Assert.IsInstanceOfType(expected, typeof(ArgumentNullException));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC014_GetVolumeDeviceIdInvalidMountPointTest()
        {
            // Arrange
            string mountPoint = Path.Combine(@"c:\", Guid.NewGuid().ToString());
            string volumeDeviceId = null;

            // Act
            volumeDeviceId = DiskUtil.GetVolumeDeviceId(mountPoint);

            // Assert
            Assert.IsNotNull(volumeDeviceId);
            Assert.AreEqual(string.Empty, volumeDeviceId);
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC015_GetDiskIndexForDiskIdInvalidDiskIdTest()
        {
            // Arrange
            int diskId = int.MinValue;

            // Act
            int index = DiskUtil.GetDiskIndexForDiskId(diskId);

            // Assert
            Assert.AreEqual(int.MinValue, index);
        }
    }
}
