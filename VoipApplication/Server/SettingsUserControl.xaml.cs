using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            return IpAdressesComboBox.SelectedItem as IPAddress;
        }

        public int GetPort()
        {
            return Convert.ToInt32(ServerPortTextBox.Text);
        }



        private IPAddress[] GetIpAddresses()
        {
            IPAddress[] ipv4Addresses = Array.FindAll(
                Dns.GetHostEntry(string.Empty).AddressList,
                a => a.AddressFamily == AddressFamily.InterNetwork);

            return ipv4Addresses.Reverse().ToArray();
        }





    }
}
