using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.BOSH.Agent.Objects
{
    public class HandlerResult
    {
        public string Result
        {
            get;
            set;
        }

        public DateTime Time
        {
            get;
            set;
        }

        public string AgentTaskId
        {
            get;
            set;
        }
    }
}
