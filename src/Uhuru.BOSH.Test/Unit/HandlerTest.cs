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
    [TestClass, DeploymentItem("NLog.config"), DeploymentItem("unity.config")]
    public class HandlerTest
    {
        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC001_LookupPingTest()
        {
            // Arrange
            string method = "ping";
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(Ping));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC002_LookupApplyTest()
        {
            // Arrange
            string method = "apply";
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(Apply));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC003_LookupStateTest()
        {
            // Arrange
            string method = "state";
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(Uhuru.BOSH.Agent.Message.State));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC004_LookupStartTest()
        {
            // Arrange
            string method = "start";
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(Start));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC005_LookupDrainTest()
        {
            // Arrange
            string method = "drain";
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(Drain));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC006_LookupStopTest()
        {
            // Arrange
            string method = "stop";
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(Stop));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC007_LookupMountDiskTest()
        {
            // Arrange
            string method = "mount_disk";
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(MountDisk));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC008_LookupListDiskTest()
        {
            // Arrange
            string method = "list_disk";
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(ListDisk));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC009_LookupCompilePackageTest()
        {
            // Arrange
            string method = "compile_package";
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(CompilePackage));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC010_LookupUnmountDiskTest()
        {
            // Arrange
            string method = "unmount_disk";
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(UnmountDisk));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC011_LookupSSHTest()
        {
            // Arrange
            string method = "ssh";
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(Ssh));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC012_LookupFetchLogsTest()
        {
            // Arrange
            string method = "fetch_logs";
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(FetchLogs));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC013_LookupTestTest()
        {
            // Arrange
            string method = "test";
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(TestMessage));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC014_LookupMigrateDiskTest()
        {
            // Arrange
            string method = "migrate_disk";
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(MigrateDisk));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC015_LookupPrepareNetworkChangeTest()
        {
            // Arrange
            string method = "prepare_network_change";
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsInstanceOfType(actual, typeof(PrepareNetworkChange));
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC016_LookupInvalidMethodTest()
        {
            // Arrange
            string method = Guid.NewGuid().ToString();
            IMessage actual = null;

            // Act
            actual = Handler.Lookup(method);

            // Assert
            Assert.IsNull(actual);
        }

        [TestMethod, TestCategory("Unit"), Timeout(30000)]
        public void TC017_LookupNullMethodTest()
        {
            // Arrange
            string method = null;
            IMessage actual = null;
            Exception exception = null;

            // Act
            try
            {
                actual = Handler.Lookup(method);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // Assert
            Assert.IsNull(actual);
            Assert.IsNotNull(exception);
            Assert.IsInstanceOfType(exception, typeof(ArgumentNullException));
        }
    }
}
