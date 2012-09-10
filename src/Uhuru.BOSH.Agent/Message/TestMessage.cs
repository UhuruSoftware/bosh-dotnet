using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Uhuru.BOSH.Agent.Message
{
    /// <summary>
    /// Test message
    /// </summary>
    public class TestMessage : IMessage
    {

        /// <summary>
        /// Determines whether the message [is long running].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is long running]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsLongRunning()
        {
            return true;
        }

        /// <summary>
        /// Processes the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public object Process(dynamic args)
        {
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
