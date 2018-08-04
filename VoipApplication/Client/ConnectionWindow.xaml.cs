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
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Web.Script.Serialization;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace VoIP_Client
{
    /// <summary>
    /// Interaction logic for ConnectionWindow.xaml
    /// </summary>
    public partial class ConnectionWindow : Window
    {
        ObservableCollection<ServerParameters> serversList = new ObservableCollection<ServerParameters>();
        Client client = new Client();
        public ConnectionWindow()
        {
            InitializeComponent();
            LoadServersList();
            ServersList.ItemsSource = serversList;
            this.Closed += new EventHandler(OnCloseWindow);
        }

        private void LoadServersList()
        {
            if (File.Exists("ServersList.json"))
            {
                var lines = File.ReadLines("ServersList.json");
                foreach (var line in lines)
                {
                    var json = new JavaScriptSerializer().Deserialize<ServerParameters>(line);
                    serversList.Add(json);
                }
            }
            else
            { File.Create("ServersList.json"); }
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
                    result[0] = 234;//n !!! wazne ustalic kod dla DiffieHellmana od klienta od serwera

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
                    //MessageBox.Show("Otrzymano sól: " + client.salt);
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


        private void ServersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var serverParameters = ServersList.SelectedItem as ServerParameters;

            if (serverParameters != null)
            {
                NameLabel.Text = serverParameters.Name;
                IPTextBox.Text = serverParameters.IP;
                PortTextBox.Text = serverParameters.Port;
            }




        }

        private void SaveServerButton_Click(object sender, RoutedEventArgs e)
        {
            var ipText = IPTextBox.Text;
            var portText = PortTextBox.Text;

            int port;
            bool isPortCorrect = int.TryParse(portText, out port);

            IPAddress ip;
            bool isIpCorrect = IPAddress.TryParse(ipText, out ip);

            if (String.IsNullOrEmpty(NameLabel.Text))
            {
                MessageBox.Show("Pole nazwa nie może być puste!");
                return;
            }

            if (!isIpCorrect)
            {
                MessageBox.Show("Pole adres ip jest niepoprawne!");
                return;
            }
            if (!isPortCorrect)
            {
                MessageBox.Show("Pole port jest nipoprawne!");
                return;
            }

            ServerParameters serverParameters = new ServerParameters()
            {
                Name = NameLabel.Text,
                IP = ip.ToString(),
                Port = port.ToString()
            };

            serversList.Add(serverParameters);


            var json = new JavaScriptSerializer().Serialize(serverParameters);

            using (FileStream fs = new FileStream("ServersList.json", FileMode.Append))
            {
                var bytes = Encoding.UTF8.GetBytes(json + Environment.NewLine);
                fs.Write(bytes, 0, bytes.Length);

            }

            if (serversList.Count > 0)
            {
                ServersList.SelectedIndex = 0;
            }

        }

        private void OnCloseWindow(object sender, EventArgs e)
        {
            try
            {
                System.IO.File.WriteAllText(@"ServersList.json", string.Empty);
                using (FileStream fs = new FileStream("ServersList.json", FileMode.OpenOrCreate))
                {
                    foreach (var item in serversList)
                    {
                        var json = new JavaScriptSerializer().Serialize(item);

                        {
                            var bytes = Encoding.UTF8.GetBytes(json + Environment.NewLine);
                            fs.Write(bytes, 0, bytes.Length);

                        }
                    }
                }
            }
            catch (Exception)
            {
                return;
            }


        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {

            var serverParameters = ServersList.SelectedItem as ServerParameters;

            serversList.Remove(serverParameters);

            if (serversList.Count > 0)
            {
                ServersList.SelectedIndex = 0;
            }
        }
    }
}
