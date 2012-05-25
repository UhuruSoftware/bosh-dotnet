// -----------------------------------------------------------------------
// <copyright file="DummyPlatform.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Platforms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Uhuru.BOSH.Agent.Providers;

    /// <summary>
    /// Dummy platform
    /// </summary>
    public class DummyPlatform : IPlatform
    {
        public void MountPersistentDisk(string cid)
        {
            throw new NotImplementedException();
        }

        public void UpdateLogging()
        {
        }

        public void UpdatePasswords(List<string> settings)
        {
            throw new NotImplementedException();
        }

        public string LookupDiskByCid(string cid)
        {
            throw new NotImplementedException();
        }

        public string GetDataDiskDeviceName()
        {
            return string.Empty;
        }

        public void SettupNetworking()
        {
            throw new NotImplementedException();
        }
    }
}
