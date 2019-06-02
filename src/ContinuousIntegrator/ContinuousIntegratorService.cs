using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;

namespace ContinuousIntegrator
{
    partial class ContinuousIntegratorService : ServiceBase
    {
        public ContinuousIntegratorService()
        {
            InitializeComponent();
        }

        private ChangesetWatcher m_Watcher = null;

        protected override void OnStart(string[] args)
        {
            this.m_Watcher = new ChangesetWatcher();
            this.m_Watcher.Start();
        }

        protected override void OnStop()
        {
            this.m_Watcher.Stop();
        }
    }
}
