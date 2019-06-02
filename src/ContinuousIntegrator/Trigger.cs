using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace ContinuousIntegrator
{
    public class Trigger
    {
        private string m_ChangePath;

        [XmlAttribute()]
        public string ChangePath
        {
            get { return this.m_ChangePath; }
            set { this.m_ChangePath = value; }
        }

        private string m_BuildType;

        [XmlAttribute()]
        public string BuildType
        {
            get { return this.m_BuildType; }
            set { this.m_BuildType = value; }
        }

        private string m_BuildMachine;

        [XmlAttribute()]
        public string BuildMachine
        {
            get { return this.m_BuildMachine; }
            set { this.m_BuildMachine = value; }
        }

        private string m_TeamProject;

        [XmlIgnore()]
        public string TeamProject
        {
            get { return this.m_TeamProject; }
            set { this.m_TeamProject = value; }
        }

        public bool Evaluate(string serverItem)
        {
            string lowerCaseServerItem = serverItem.ToLower();
            string lowerCaseChangePath = this.ChangePath.ToLower();

            if (lowerCaseServerItem.StartsWith(lowerCaseChangePath + "/"))
            {
                return true;
            }

            return false;
        }
    }
}
