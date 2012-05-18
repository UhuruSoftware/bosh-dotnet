// -----------------------------------------------------------------------
// <copyright file="ProjectInstaller.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.WindowsService
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Configuration;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using Microsoft.Web.Administration;

    /// <summary>
    /// Installer Class for Windows DEA Service
    /// </summary>
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectInstaller"/> class.
        /// </summary>
        public ProjectInstaller()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// When overridden in a derived class, performs the installation.
        /// </summary>
        /// <param name="stateSaver">An <see cref="T:System.Collections.IDictionary"/> used to save information needed to perform a commit, rollback, or uninstall operation.</param>
        /// <exception cref="T:System.ArgumentException">The <paramref name="stateSaver"/> parameter is null. </exception>
        /// Custom install method. Writes configuration values to uhuru.config
        /// <exception cref="T:System.Exception">An exception occurred in the <see cref="E:System.Configuration.Install.Installer.BeforeInstall"/> event handler of one of the installers in the collection.-or- An exception occurred in the <see cref="E:System.Configuration.Install.Installer.AfterInstall"/> event handler of one of the installers in the collection. </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFile", Justification = "No way around this right now.")]
        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Configuration.Install.Installer.AfterInstall"/> event.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Collections.IDictionary"/> that contains the state of the computer after all the installers contained in the <see cref="P:System.Configuration.Install.Installer.Installers"/> property have completed their installations.</param>
        protected override void OnAfterInstall(IDictionary savedState)
        {
            base.OnAfterInstall(savedState);
        }
    }
}
