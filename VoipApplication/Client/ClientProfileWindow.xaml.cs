using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
using VoIP_Server;
using cscprotocol;

namespace VoIP_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ClientProfileWindow : Window
    {
        CallingService callingService;
        Client client;
        public ClientProfileWindow(CallingService callingService, Client client)
        {
            InitializeComponent();
            this.callingService = callingService;
            this.client = client;
            UserEmailLabel.Text = client.UserProfile.Email;//n
            EmailTextBox.Text = client.UserProfile.Email;//n
        }

        private void UpdateProfileEmail(CscUserMainData profile)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                UserEmailLabel.Text = profile.Email;
            }));
        }
        private void ClearPasswordFields()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                ChangeEmailPasswordBox.Password = string.Empty;
                OldPasswordBox.Password = string.Empty;
                NewPasswordBox.Password = string.Empty;
            }));
        }

        public void DebugConsole(string msg)
        { MessageBox.Show(msg); }


        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        { Close(); }

        private void EmailButton_Click(object sender, RoutedEventArgs e)
        {
            var NewEmail = EmailTextBox.Text;
            CscUserData userData = new CscUserData()
            {
                Email = NewEmail,
                Password = CscSHA512Generator.get_SHA512_hash_as_string(CscSHA512Generator.get_SHA512_hash_as_string(ChangeEmailPasswordBox.Password) + client.salt)
            };
            client.SendChangeEmailRequest(userData);

            var WaitForMessageTask = Task.Run(() => WaitForEmailMessage(NewEmail));
        }

        private void PasswordButton_Click(object sender, RoutedEventArgs e)
        {
            var OldPassword = OldPasswordBox.Password;
            var NewPassword = NewPasswordBox.Password;
            CscPasswordData passwordData = new CscPasswordData()
            {
                OldPassword = CscSHA512Generator.get_SHA512_hash_as_string(CscSHA512Generator.get_SHA512_hash_as_string(OldPassword) + client.salt),
                NewPassword = CscSHA512Generator.get_SHA512_hash_as_string(NewPassword)
            };
            client.SendChangePasswordRequest(passwordData);

            var WaitForMessageTask = Task.Run(() => WaitForPasswordMessage());
            //sprawdz czy stare haslo jest poprawne
            //jesli tak wyslij komunikat o zmiane hasla ze starego na nowe
        }
        private void WaitForEmailMessage(string email)
        {
            while (client.LastConfirmMessage == string.Empty && client.LastErrorMessage == string.Empty)
            { }
            if (client.LastConfirmMessage != string.Empty)
            {
                MessageBox.Show(client.LastConfirmMessage);
                client.LastConfirmMessage = string.Empty;
                client.UserProfile.Email = email;
                UpdateProfileEmail(client.UserProfile);
            }
            else
            {
                MessageBox.Show(client.LastErrorMessage);
                client.LastErrorMessage = string.Empty;
            }
            ClearPasswordFields();
        }
        private void WaitForPasswordMessage()
        {
            while (client.LastConfirmMessage == string.Empty && client.LastErrorMessage == string.Empty)
            { }
            if (client.LastConfirmMessage != string.Empty)
            {
                MessageBox.Show(client.LastConfirmMessage);
                client.LastConfirmMessage = string.Empty;
            }
            else
            {
                MessageBox.Show(client.LastErrorMessage);
                client.LastErrorMessage = string.Empty;
            }
            ClearPasswordFields();
        }
    }
}
