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
using System.Diagnostics;


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

                    byte[] cmdAndLength = new byte[3];
                    var l1 = client.client.GetStream().Read(cmdAndLength, 0, cmdAndLength.Length);
                    var msgLen = BitConverter.ToUInt16(cmdAndLength.Skip(1).ToArray(), 0);

                    byte[] buffer = new byte[msgLen];
                    var l2 = client.client.GetStream().Read(buffer, 0, buffer.Length);

                    var salt = Encoding.Unicode.GetString(buffer);
                    client.salt = salt;

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
