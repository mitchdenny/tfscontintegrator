namespace ContinuousIntegrator
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.m_ServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.m_ServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // m_ServiceProcessInstaller
            // 
            this.m_ServiceProcessInstaller.Password = null;
            this.m_ServiceProcessInstaller.Username = null;
            // 
            // m_ServiceInstaller
            // 
            this.m_ServiceInstaller.DisplayName = "Continuous Integrator";
            this.m_ServiceInstaller.ServiceName = "ContinuousIntegrator";
            this.m_ServiceInstaller.ServicesDependedOn = new string[] {
        "w3svc"};
            this.m_ServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.m_ServiceProcessInstaller,
            this.m_ServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller m_ServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller m_ServiceInstaller;
    }
}