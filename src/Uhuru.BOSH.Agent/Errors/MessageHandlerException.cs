// -----------------------------------------------------------------------
// <copyright file="MessageHandlerException.cs" company="">
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
    /// Message handler BOSH exception.
    /// </summary>
    [Serializable]
    public class MessageHandlerException : BoshException
    {
        string blob = null;

        /// <summary>
        /// Gets the BLOB.
        /// </summary>
        public string Blob
        {
            get
            {
                return blob;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerException"/> class.
        /// </summary>
        public MessageHandlerException() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public MessageHandlerException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public MessageHandlerException(string message, Exception inner) : base(message, inner) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="blob">The BLOB.</param>
        public MessageHandlerException(string message, string blob) : base(message) { this.blob = blob; }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="blob">The BLOB.</param>
        public MessageHandlerException(string message, Exception inner, string blob) : base(message, inner) { this.blob = blob; }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="info"/> parameter is null. </exception>
        ///   
        /// <exception cref="T:System.Runtime.Serialization.SerializationException">The class name is null or <see cref="P:System.Exception.HResult"/> is zero (0). </exception>
        protected MessageHandlerException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="info"/> parameter is a null reference (Nothing in Visual Basic). </exception>
        ///   
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Read="*AllFiles*" PathDiscovery="*AllFiles*"/>
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="SerializationFormatter"/>
        ///   </PermissionSet>
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("blob", blob);
        }
    }
}
