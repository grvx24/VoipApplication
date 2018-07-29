using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
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
using System.Windows.Threading;
using VoIP_Server;
using VoIP_Server.Client;
using cscprotocol;
using VoipApplication;
using NAudio.Wave;

namespace VoIP_Client
{
    public partial class ClientSettingsUserControl : UserControl
    {
        CallingService callingService;
        ObservableCollection<CscUserMainData> obsCollection = new ObservableCollection<CscUserMainData>();
        Client client;
        Window parentWindow;

        public ClientSettingsUserControl(Client client, CallingService callingService, Window parentWindow)
        {
            this.client = client;
            this.parentWindow = parentWindow;
            this.callingService = callingService;

            InitializeComponent();
            PopulateInputDevicesCombo();
            //(parentWindow as ClientMainWindow).UserEmailLabel.Text = client.UserProfile.Email;//to robi fajny blad aplikacji przydatny do zrobienia poprawnego wylogowania usera po craschu apki
            EmailTextBox.Text = client.UserProfile.Email;//n
        }

        private void PopulateInputDevicesCombo()
        {
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                var capabilities = WaveIn.GetCapabilities(n);
                InputDeviceComboBox.Items.Add(capabilities.ProductName);

            }
            if (InputDeviceComboBox.Items.Count > 0)
            { InputDeviceComboBox.SelectedIndex = 0; }
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

        private void EmailButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EmailValidator.IsValid(EmailTextBox.Text))
            {
                MessageBox.Show("Adres email niepoprawny!");
                return;
            }

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
                (parentWindow as ClientMainWindow).UpdateProfileEmail(client.UserProfile);

                MessageBox.Show("Zmiana adresu email wymaga ponownego zalogowania się.");

                Dispatcher.Invoke(() =>
                {
                    callingService.DisposeTcpListener();
                    client.Disconnect();
                    ConnectionWindow loginWindow = new ConnectionWindow();
                    loginWindow.Show();
                    parentWindow.Close();
                }
                );
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
