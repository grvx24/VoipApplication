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
            var bytesToSend = protocol.CreateRegistrationMessage(userData);
            client.SendBytes(bytesToSend);

            var response = client.ReceiveBytes();
            if (response[0] == 12)
            {
                var msg = CscProtocol.ParseConfirmMessage(response);
                MessageBox.Show(msg);
            }
            if (response[0] == 13)
            {
                var msg = CscProtocol.ParseConfirmMessage(response);
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
