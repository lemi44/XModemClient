using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XModemClient
{
    public class Port
    {
        public string PortName { get; set; }
        public string InstanceName { get; set; }
        public Port(string PortName, string InstanceName)
        {
            this.PortName = PortName;
            this.InstanceName = InstanceName;
        }
    }
}
