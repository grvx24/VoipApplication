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
using CPOL;
using cscprotocol;

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
                    //odbieranie DiffieHellmana od serwera

                    byte[] cmdAndLength = new byte[3];
                    var l1 = client.client.GetStream().Read(cmdAndLength, 0, cmdAndLength.Length);
                    var msgLen = BitConverter.ToUInt16(cmdAndLength.Skip(1).ToArray(), 0);

                    byte[] buffer = new byte[msgLen];
                    var l2 = client.client.GetStream().Read(buffer, 0, buffer.Length);

                    var DHdata = Encoding.Unicode.GetString(buffer);

                    DiffieHellman DHclient = new DiffieHellman(256).GenerateResponse(DHdata);

                    //////////////////////////
                    //wysylanie DiffieHellmana do serwera
                    
                    var mainMessage = Encoding.Unicode.GetBytes(DHclient.ToString());

                    UInt16 messageLength = (UInt16)mainMessage.Length;
                    var lenghtBytes = BitConverter.GetBytes(messageLength);

                    byte[] result = new byte[mainMessage.Length + lenghtBytes.Length + 1];
                    result[0] = 234;//wazne ustalic kod dla DiffieHellmana od klienta od serwera

                    lenghtBytes.CopyTo(result, 1);
                    mainMessage.CopyTo(result, lenghtBytes.Length + 1);

                    client.client.GetStream().Write(result, 0, result.Length);

                    ////////////////////////////////////
                    //odtad wszystko co odbieramy powinno juz byc zaszyfrowane
                    ////////////////////////////////////

                    var aes = new CscAes(DHclient.Key);

                    cmdAndLength = new byte[3];
                    l1 = client.client.GetStream().Read(cmdAndLength, 0, cmdAndLength.Length);
                    msgLen = BitConverter.ToUInt16(cmdAndLength.Skip(1).ToArray(), 0);

                    buffer = new byte[msgLen];
                    l2 = client.client.GetStream().Read(buffer, 0, buffer.Length);


                    //var salt = Encoding.Unicode.GetString(buffer);
                    //client.salt = salt;
                    client.salt = aes.DecryptStringFromBytes(buffer);
                    //MessageBox.Show("Odebrano sól " + client.salt);
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
