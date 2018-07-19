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
        static FriendsList friendsListGrid;


        CallingService callingService;
        Client client;
        public ClientProfileWindow(CallingService callingService, Client client)
        {
            InitializeComponent();
            this.callingService = callingService;
            this.client = client;
            UserEmailLabel.Text = client.UserProfile.Email;//n
        }

        private void UpdateProfileEmail(CscUserMainData profile)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                UserEmailLabel.Text = profile.Email;
            }));

        }

        public void ShowCallingWindow(object sender, CallingEventArgs eventArgs, TcpClient client)
        {
            var callingService = sender as CallingService;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Media.SoundPlayer player = new System.Media.SoundPlayer("telephone_ring.wav");
                var callWindow = new CallingWindow(this.client, callingService, player, eventArgs, this);
                callWindow.NickLabel.Text = eventArgs.Nick;
                callWindow.Show();


            }));
        }

        private void FriendsButton_Click(object sender, RoutedEventArgs e)
        {
        }



        public void LeaveServer()
        {
            MessageBox.Show("Połączenie z serwerem zostało zerwane");

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
               //ConnectionWindow window = new ConnectionWindow();
               //window.Show();
               Close();

            }));

        }

        public void DebugConsole(string msg)
        {
            MessageBox.Show(msg);
        }


        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
