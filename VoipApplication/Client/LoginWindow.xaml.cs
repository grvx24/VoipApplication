using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using System.Windows.Shapes;
using VoIP_Server;
using cscprotocol;
using VoipApplication;

namespace VoIP_Client
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        Client client;
        CscProtocol protocol;
        public LoginWindow(Client client)
        {
            InitializeComponent();
            this.client = client;
            protocol = new CscProtocol();
        }
        

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EmailValidator.IsValid(EmailTextBox.Text))
            {
                MessageBox.Show("Adres email niepoprawny!");
                return;
            }

            try
            {
                CscUserData userData = new CscUserData() { Email = EmailTextBox.Text,
                    Password = CscSHA512Generator.get_SHA512_hash_as_string(CscSHA512Generator.get_SHA512_hash_as_string(PasswordTextBox.Password) + client.salt) };
                var bytesToSend = protocol.CreateLoginMessage(userData);
                client.SendBytes(bytesToSend);
                client.UserProfile.Email = EmailTextBox.Text;//n

                var response = client.ReceiveBytes();
                if(response[0]==12)
                {
                    var length=BitConverter.ToInt16(response.Skip(1).Take(2).ToArray(),0);
                    var message = response.Skip(3).ToArray();
                    MessageBox.Show(Encoding.Unicode.GetString(message, 0, length));


                    //Create main window
                    client.initialized = true;
                    ClientMainWindow mainWindow = new ClientMainWindow(client, false);
                    mainWindow.Show();
                    this.Close();
                }
                if (response[0] == 13)
                {
                    var length = BitConverter.ToInt16(response.Skip(1).Take(2).ToArray(), 0);
                    var message = response.Skip(3).ToArray();
                    MessageBox.Show(Encoding.Unicode.GetString(message, 0, length));
                    return;
                }
                else
                {
                    //MessageBox.Show(Encoding.Unicode.GetString(response));
                }


            }
            catch(SocketException ex)
            {
                MessageBox.Show("Nastąpiło rozłączenie z serwerem: " + ex.Message);
                client.Disconnect();
                ConnectionWindow window = new ConnectionWindow();
                window.Show();
                Close();
            }
            catch (IOException)
            {
                MessageBox.Show("Nie otrzymano odpowiedzi od serwera");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wystąpił nieznany błąd: "+ex.Message);
            }

        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            client.Disconnect();
            ConnectionWindow window = new ConnectionWindow();
            window.Show();
            Close();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow window = new RegisterWindow(client);
            window.Show();
            Close();
        }
    }
}
