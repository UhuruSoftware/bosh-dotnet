// -----------------------------------------------------------------------
// <copyright file="Ntp.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Globalization;
    using System.Net.Sockets;
    using System.Threading;
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
            foreach (dynamic ntpServer in Config.Settings["ntp"])
            {
                return Ntp.GetNtpOffset(Convert.ToString(ntpServer.Value).Trim());
            }

            return new Ntp() { Message = "bad ntp server" };
        }

        /// <summary>
        /// Gets the NTP offset from a specified time server.
        /// </summary>
        /// <param name="timeServer">The time server.</param>
        /// <returns></returns>
        public static Ntp GetNtpOffset(string timeserver)
        {
            if (string.IsNullOrEmpty(timeserver))
            {
                throw new ArgumentNullException("timeserver");
            }

            Logger.Debug("Retrieving NTP information from {0}", timeserver);

            int retryCount = 5;
            Ntp currentNtp = new Ntp();
            while (retryCount > 0)
            {
                try
                {
                    NtpClient ntpClient = new NtpClient(timeserver);
                    ntpClient.Connect(false);
                    currentNtp.offset = ntpClient.LocalClockOffset;
                    currentNtp.currentTime = DateTime.Now;
                    break;
                }
                catch (SocketException se)
                {
                    Logger.Error("Error while retrieving ntp information: {0}", se.ToString());
                    currentNtp.message = se.Message;
                    retryCount--;
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while retrieving ntp information: {0}", ex.ToString());
                    currentNtp.message = ex.Message;
                }
            }
            return currentNtp;
            
        }

        public static void SetTime(double timeOffset)
        {
            Logger.Debug("Updating time");

            Uhuru.BOSH.Agent.NativeMethods.Systemtime st;

            DateTime trts = DateTime.Now.AddMilliseconds(timeOffset);
            st.year = (short)trts.Year;
            st.month = (short)trts.Month;
            st.dayOfWeek = (short)trts.DayOfWeek;
            st.day = (short)trts.Day;
            st.hour = (short)trts.Hour;
            st.minute = (short)trts.Minute;
            st.second = (short)trts.Second;
            st.milliseconds = (short)trts.Millisecond;

            Uhuru.BOSH.Agent.NativeMethods.SetLocalTime(ref st);

            Logger.Debug("Updated local time: {0}", DateTime.Now.ToString(CultureInfo.InvariantCulture));
        }
    }
}
