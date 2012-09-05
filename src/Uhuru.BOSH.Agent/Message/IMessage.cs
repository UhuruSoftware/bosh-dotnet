using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.BOSH.Agent.Message
{
    public interface IMessage
    {
        bool IsLongRunning();

        object Process(dynamic args);
    }
}
