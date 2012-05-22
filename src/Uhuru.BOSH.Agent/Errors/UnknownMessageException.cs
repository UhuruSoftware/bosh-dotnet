// -----------------------------------------------------------------------
// <copyright file="UnknownMessageException.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Errors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Unknown Message BOSH exception.
    /// </summary>
    [Serializable]
    public class UnknownMessageException : BoshException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownMessageException"/> class.
        /// </summary>
        public UnknownMessageException() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownMessageException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public UnknownMessageException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownMessageException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public UnknownMessageException(string message, Exception inner) : base(message, inner) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownMessageException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="info"/> parameter is null. </exception>
        ///   
        /// <exception cref="T:System.Runtime.Serialization.SerializationException">The class name is null or <see cref="P:System.Exception.HResult"/> is zero (0). </exception>
        protected UnknownMessageException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
