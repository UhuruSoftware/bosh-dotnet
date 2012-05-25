// -----------------------------------------------------------------------
// <copyright file="Ntp.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Uhuru.Utilities;

    /// <summary>
    /// A class the connects to a specified time server and returns the offset
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ntp", Justification = "FxCop Bug")]
    public class Ntp
    {
        private DateTime currentTime;
        private double offset;
        private string message;

        /// <summary>
        /// Gets the current time.
        /// </summary>
        public DateTime CurrentTime
        {
            get
            {
                return currentTime;
            }
            private set
            {
                currentTime = value;
            }
        }

        /// <summary>
        /// Gets the offset.
        /// </summary>
        public double Offset
        {
            get
            {
                return offset;
            }
            private set
            {
                offset = value;
            }
        }

        /// <summary>
        /// Gets the connection error message.
        /// </summary>
        public string Message
        {
            get
            {
                return message;
            }
            internal set
            {
                message = value;
            }
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="Ntp"/> class from being created.
        /// </summary>
        private Ntp()
        {

        }

        /// <summary>
        /// Gets the NTP offset using the default time server.
        /// </summary>
        /// <returns></returns>
        public static Ntp GetNtpOffset()
        {
            //TODO detect timeserver
            return GetNtpOffset("time.windows.com");
        }

        /// <summary>
        /// Gets the NTP offset from a specified time server.
        /// </summary>
        /// <param name="timeServer">The time server.</param>
        /// <returns></returns>
        public static Ntp GetNtpOffset(string timeServer)
        {
            Ntp currentNtp = new Ntp();
            try
            {
                NTPClient ntpClient = new NTPClient(timeServer);
                ntpClient.Connect(false);
                currentNtp.offset = ntpClient.LocalClockOffset;
                currentNtp.currentTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Logger.Error("Error while retrieving ntp information", ex);
                currentNtp.message = ex.Message;
            }
            return currentNtp;
            
        }
    }
}
