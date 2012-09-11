using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.BOSH.Agent
{
    [Serializable]
    public class AgentException : Exception
    {
        public AgentException() { }

        public AgentException(String message) : base(message) { }

        public AgentException(string message, Exception inner) : base(message, inner) { }

        protected AgentException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
