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
    using System.Collections.ObjectModel;

    /// <summary>
    /// Dummy platform
    /// </summary>
    public class DummyPlatform : IPlatform
    {
        public void MountPersistentDisk(int diskId)
        {
            throw new NotImplementedException();
        }

        public void UpdateLogging()
        {
        }

        public void UpdatePasswords(Collection<string> settings)
        {
            throw new NotImplementedException();
        }

        public string LookupDiskByCid(string cid)
        {
            throw new NotImplementedException();
        }

        public void SetupNetworking()
        {
            
        }

        public string GetDataDiskDeviceName
        {
            get { return null; }
        }
    }
}
