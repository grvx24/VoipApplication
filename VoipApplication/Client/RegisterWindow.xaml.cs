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
using VoIP_Server;
using cscprotocol;
using VoipApplication;

namespace VoIP_Client
{
    /// <summary>
    /// Interaction logic for RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {

        Client client;
        CscProtocol protocol = new CscProtocol();

        public RegisterWindow(Client client)
        {
            InitializeComponent();
            this.client = client;
        }


        private void Register()
        {
            CscUserData userData = new CscUserData()
            {
                Email = RegisterEmailTextBox.Text,
                Password = CscSHA512Generator.get_SHA512_hash_as_string(RegisterPasswordTextBox.Password)
            };
            var bytesToSend = protocol.CreateRegistrationMessageEncrypted(userData, client.DH.Key);
            client.SendBytes(bytesToSend);

            var response = client.ReceiveBytes();
            var messageEncrypted = response.Skip(3).ToArray();
            var msg = client.AES.DecryptStringFromBytes(messageEncrypted);
            if (response[0] == 12)
            {
                MessageBox.Show(msg);
            }
            if (response[0] == 13)
            {
                MessageBox.Show(msg);
                return;
            }
            else
            {
                //MessageBox.Show(Encoding.Unicode.GetString(response));
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EmailValidator.IsValid(RegisterEmailTextBox.Text))
            {
                MessageBox.Show("Adres email niepoprawny!");
                return;
            }
            if(!PasswordValidator.ValidatePassword(RegisterPasswordTextBox.Password))
            {
                MessageBox.Show("Hasło jest za słabe!");
                return;
            }
            Register();
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow window = new LoginWindow(client);
            window.Show();
            Close();
        }
    }
}
