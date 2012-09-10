using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uhuru.BOSH.Agent.Message
{
    /// <summary>
    /// Interface used for Messages
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// Determines whether the message [is long running].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is long running]; otherwise, <c>false</c>.
        /// </returns>
        bool IsLongRunning();

        /// <summary>
        /// Processes the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        object Process(dynamic args);
    }
}
