using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VoIP_Server.Server
{
    /// <summary>
    /// Interaction logic for SettingsUserControl.xaml
    /// </summary>
    public partial class SettingsUserControl : UserControl
    {

        public SettingsUserControl()
        {
            InitializeComponent();

            IpAdressesComboBox.ItemsSource = GetIpAddresses();
            IpAdressesComboBox.SelectedIndex = 0;
            
        }

        public IPAddress GetSelectedIp()
        {
            var ipString = IpAdressesComboBox.SelectedItem.ToString();

            return IPAddress.Parse(ipString.Split(new string[] { ":/" }, StringSplitOptions.None)[1]);
        }

        public int GetPort()
        {
            return Convert.ToInt32(ServerPortTextBox.Text);
        }



        private string[] GetIpAddresses()
        {
            List<string> interfaces = new List<string>();

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            var interfaceName = ni.Name + ":/" + ip.Address.ToString();
                            interfaces.Add(interfaceName);
                        }
                    }
                }
                
            }

            return interfaces.ToArray();

            //IPAddress[] ipv4Addresses = Array.FindAll(
            //    Dns.GetHostEntry(string.Empty).AddressList,
            //    a => a.AddressFamily == AddressFamily.InterNetwork);

            //return ipv4Addresses.Reverse().ToArray();
        }

        private void IpAdressesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
