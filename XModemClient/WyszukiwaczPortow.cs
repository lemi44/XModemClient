using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace XModemClient
{
    public class WyszukiwaczPortow
    {
        private ManagementObjectSearcher searcher { get; set; }

        /// <summary>
        /// Key - PortName,
        /// Value - InstanceName
        /// </summary>
        private List<Port> listaPortow;
        public WyszukiwaczPortow()
        {
            listaPortow = new List<Port>();
            /*
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\WMI",
                    "SELECT * FROM MSSerial_PortName");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    slownikPortow.Add(queryObj["PortName"].ToString(), queryObj["InstanceName"].ToString());
                }
            }
            catch (ManagementException e)
            {
                MessageBox.Show("An error occurred while querying for WMI data: " + e.Message);
            }
            */
            try
            {
                foreach(string s in SerialPort.GetPortNames())
                {
                    listaPortow.Add(new Port(s, s));
                }
            }
            catch (Win32Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        public List<Port> Porty
        {
            get { return listaPortow; }
        }

    }
}
