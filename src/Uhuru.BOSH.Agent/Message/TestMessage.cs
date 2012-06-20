using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Uhuru.BOSH.Agent.Message
{
    public class TestMessage : IMessage
    {

        public bool IsLongRunning()
        {
            return true;
        }

        public string Process(dynamic args)
        {
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
