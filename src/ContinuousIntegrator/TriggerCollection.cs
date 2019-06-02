using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace ContinuousIntegrator
{
    [XmlRoot("Triggers")]
    public class TriggerCollection : Collection<Trigger>
    {
        public void AddRange(TriggerCollection triggers)
        {
            foreach (Trigger trigger in triggers)
            {
                this.Add(trigger);
            }
        }
    }
}
