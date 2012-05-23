using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uhuru.BOSH.Agent;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    public class StateTest
    {
        [TestMethod]
        public void TestGetIps()
        {
            State testState = new State(@"E:\_work\bosh-dotnet\src\Uhuru.BOSH.Test\Assets\apply_spec.yml");

            List<string> ips = testState.GetIps().ToList();
        }
    }
}
