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
    public class UnknownInfrastructure : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownInfrastructure"/> class.
        /// </summary>
        /// <param name="infrastuctureName">Name of the infrastructure.</param>
        public UnknownInfrastructure(string infrastructureName)
            : base(string.Format(CultureInfo.InvariantCulture, "infrastructure {0} not found", infrastructureName))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownInfrastructure"/> class.
        /// </summary>
        /// <param name="ex">The inner exception.</param>
        public UnknownInfrastructure(Exception ex)
            : base("Error retrieving infrastructure", ex)
        {
        }
    }
}
