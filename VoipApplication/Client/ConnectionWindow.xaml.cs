using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;


namespace VoIP_Client
{
    /// <summary>
    /// Interaction logic for ConnectionWindow.xaml
    /// </summary>
    public partial class ConnectionWindow : Window
    {

        Client client = new Client();
        public ConnectionWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                IPAddress ip = IPAddress.Parse(IPTextBox.Text);
                int port = Int32.Parse(PortTextBox.Text);
                client.Connect(ip, port);

                if (client.IsConnected)
                {
                    LoginWindow window = new LoginWindow(client);
                    window.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Nie można się połączyć!");
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
    }
}
