using System;
using System.Collections.Generic;
using System.Text;

namespace ContinuousIntegrator
{
    public class BuildRequest
    {
        private string m_TeamProject;

        public string TeamProject
        {
            get { return this.m_TeamProject; }
            set { this.m_TeamProject = value; }
        }

        private string m_BuildType;

        public string BuildType
        {
            get { return this.m_BuildType; }
            set { this.m_BuildType = value; }
        }

        private string m_BuildMachine;

        public string BuildMachine
        {
            get { return this.m_BuildMachine; }
            set { this.m_BuildMachine = value; }
        }

        public override bool Equals(object obj)
        {
            BuildRequest request = obj as BuildRequest;
            if (request == null) return false;

            if ((this.TeamProject == request.TeamProject) &&
                (this.BuildType == request.BuildType) &&
                (this.BuildMachine == request.BuildMachine))
            {
                return true;
            }

            return false;
        }
    }
}
