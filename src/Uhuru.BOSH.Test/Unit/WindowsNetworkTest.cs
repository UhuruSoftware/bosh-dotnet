using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent.Platforms.Windows;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    public class WindowsNetworkTest
    {
        [TestMethod]
        public void TC001_TestNetwork()
        {
            WindowsNetwork wn = new WindowsNetwork();
            wn.SetupNetwork();
        }
    }
}
