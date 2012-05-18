// -----------------------------------------------------------------------
// <copyright file="BoshAgentWindowsService.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.WindowsService
{
    using System.ServiceProcess;

    /// <summary>
    /// The Windows Service hosting the DEA.
    /// </summary>
    public partial class BoshAgentWindowsService : ServiceBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoshAgentWindowsService"/> class.
        /// </summary>
        public BoshAgentWindowsService()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        internal void Start()
        {
            this.AutoLog = true;
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Start command is sent to the service by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically). Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override void OnStart(string[] args)
        {
            this.Start();
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Stop command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service stops running.
        /// </summary>
        protected override void OnStop()
        {
        }
    }
}
