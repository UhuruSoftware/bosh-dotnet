using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    public class RemoteExceptionTest
    {
        [TestMethod]
        public void TC001_OnlyMessage()
        {
            //Arrange
            string message = "this is a message";
            
            //Act
            RemoteException remoteException = new RemoteException(message);

            //Assert
            Assert.IsFalse(string.IsNullOrEmpty(remoteException.Backtrace));
        }

        [TestMethod]
        public void TC002_TestException()
        {
            //Arrange
            string message = "this is a message";
            string blob = "blob";
            string stacktrace = "this is a stacktrace";

            //Act
            RemoteException remoteException = new RemoteException(message, stacktrace, blob);

            //Assert
            Assert.AreEqual(message, remoteException.Message);
            Assert.AreEqual(blob, remoteException.Blob);
            Assert.AreEqual(stacktrace, remoteException.Backtrace);

        }

        [TestMethod]
        public void TC003_TestStatic()
        {
            //Arrange
            string message = "my special exception";
            Exception exception = new Exception(message);

            //Act
            RemoteException remoteException = RemoteException.CreateRemoteException(exception);

            //Assert
            Assert.AreEqual(message, remoteException.Message);
        }
    }
}
