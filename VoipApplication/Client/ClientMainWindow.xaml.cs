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
    public partial class ClientMainWindow : Window
    {
        static FriendsList friendsListGrid;


        CallingService callingService;
        Client client;
        public ClientMainWindow(Client client, bool createNewInstance)
        {
            callingService = new CallingService(client.LocalIP, 2999);


            friendsListGrid = new FriendsList(client,callingService,this);

            if (!createNewInstance)
            {
                this.client = client;

                client.AddItemEvent += AddOnlineUser;
                client.RemoveItemEvent += RemoveOnlineUser;

                client.AddFriendEvent += AddFriendToObsCollection;
                client.RemoveFriendEvent += RemoveFriendToObsCollection;

                client.ConnectionLostEvent += LeaveServer;
                client.SetProfileText += UpdateProfileEmail;
            }else
            {
                this.client = new Client() { client = client.client, LocalIP = client.LocalIP, IsConnected = true,
                    Port = client.Port, IPAddress = client.IPAddress, UserProfile = client.UserProfile };

                this.client.AddItemEvent += AddOnlineUser;
                this.client.RemoveItemEvent += RemoveOnlineUser;

                this.client.AddFriendEvent += AddFriendToObsCollection;
                this.client.RemoveFriendEvent += RemoveFriendToObsCollection;

                this.client.ConnectionLostEvent += LeaveServer;
                this.client.SetProfileText += UpdateProfileEmail;
            }



            InitializeComponent();
            callingService.OnCallingEvent += ShowCallingWindow;
            callingService.ListenIncomingCalls();

            client.StartListening();

            client.GetBasicInfo();

        }

        private void UpdateProfileEmail(CscUserMainData profile)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                UserEmailLabel.Text = profile.Email;
            }));
            
        }

        private void AddFriendToObsCollection(CscUserMainData friendData)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                client.FriendsList.Add(friendData);
            }
            ));
        }

        private void RemoveFriendToObsCollection(CscUserMainData userToRemove)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                client.FriendsList.Remove(userToRemove);
            }
            ));
        }

        public void ShowCallingWindow(object sender, CallingEventArgs eventArgs,TcpClient client)
        {
            var callingService = sender as CallingService;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Media.SoundPlayer player = new System.Media.SoundPlayer("telephone_ring.wav");
                var callWindow = new CallingWindow(this.client,callingService, player, eventArgs, this);
                callWindow.NickLabel.Text = eventArgs.Nick;
                callWindow.Show();


            }));

        }

        private void FriendsButton_Click(object sender, RoutedEventArgs e)
        {
            CustomUserControl.Content = friendsListGrid;
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
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

        public void RemoveOnlineUser(CscUserMainData user)
        {
            Dispatcher.Invoke(new Action(() => client.onlineUsers.Remove(user)));

            var friend = client.FriendsList.Where(u => u.Id == user.Id).FirstOrDefault();
            if(friend!=null)
            {
                CscUserMainData newUser = new CscUserMainData() { Email = friend.Email, Id = friend.Id, Ip = "", FriendName = friend.FriendName, Status = 0 };
                Dispatcher.Invoke(new Action(() => client.FriendsList.Remove(friend)));
                client.FriendsList.Add(newUser);
            }

            


        }

        public void AddOnlineUser(CscUserMainData user)
        {
            Dispatcher.Invoke(new Action(() => client.onlineUsers.Add(user)));

            var friend = client.FriendsList.Where(u => u.Id == user.Id).FirstOrDefault();
            if (friend != null)
            {
                CscUserMainData newUser = new CscUserMainData() { Email = friend.Email, Id = friend.Id, Ip = friend.Ip, FriendName = friend.FriendName, Status = 1 };
                Dispatcher.Invoke(new Action(() => client.FriendsList.Remove(friend)));
                client.FriendsList.Add(newUser);
            }

        }

        private void LogOut(object sender, RoutedEventArgs e)
        {

        }

    }
}
