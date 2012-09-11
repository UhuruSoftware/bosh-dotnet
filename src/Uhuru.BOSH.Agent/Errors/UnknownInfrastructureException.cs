using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Uhuru.BOSH.Agent.Errors
{
    /// <summary>
    /// Exception for an unknown infrastructure
    /// </summary>
    [Serializable]
    public class UnknownInfrastructureException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownInfrastructureException"/> class.
        /// </summary>
        /// <param name="infrastuctureName">Name of the infrastructure.</param>
        public UnknownInfrastructureException(string infrastructureName)
            : base(string.Format(CultureInfo.InvariantCulture, "infrastructure {0} not found", infrastructureName))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownInfrastructureException"/> class.
        /// </summary>
        /// <param name="ex">The inner exception.</param>
        public UnknownInfrastructureException(Exception ex)
            : base("Error retrieving infrastructure", ex)
        {
        }

        protected UnknownInfrastructureException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public UnknownInfrastructureException() { }

        public UnknownInfrastructureException(string message, Exception inner) : base(message, inner) { }
    }
}
