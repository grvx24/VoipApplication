using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using cscprotocol;
using System.Diagnostics;

namespace VoIP_Client
{
    /// <summary>
    /// Interaction logic for FriendsList.xaml
    /// </summary>
    public partial class FriendsList : UserControl
    {
        CallingService callingService;
        Client client;
        public ClientSearchWindow searchWindow;

        static Button lastClickedTab = null;

        public FriendsList(Client client, CallingService callingService)
        {
            this.client = client;
            this.callingService = callingService;
            InitializeComponent();
        }

        private void MSGBoxShow(string msg)
        { MessageBox.Show(msg); }

        private void RowButtonCall_Click(object sender, RoutedEventArgs args)
        {
            if (!callingService.isCalling && !callingService.isBusy)
            {
                CscUserMainData data = ((FrameworkElement)sender).DataContext as CscUserMainData;
                var text = string.Format("{0} - {1} - {2}", data.FriendName, data.Ip, data.Email);

                Trace.WriteLine(text);

                if (data.Status == 1)
                {
                    try
                    {
                        IPEndPoint iPEndPoint =
                            new IPEndPoint(IPAddress.Parse(data.Ip), callingService.localEndPoint.Port);

                        Task.Run(() =>
                        {
                            callingService.EncryptedCallSender = false;
                            callingService.MakeCall(iPEndPoint, client.UserProfile.Email, data.Email);
                        });

                    }
                    catch (Exception ex)
                    { MessageBox.Show(ex.Message); }
                }
                else
                { MessageBox.Show("Użytkownik nie jest dostępny"); }
            }
            else
            { MessageBox.Show("Jesteś w trakcie rozmowy"); }
        }
        private void RowButtonEncryptedCall_Click(object sender, RoutedEventArgs args)
        {
            if (!callingService.isCalling && !callingService.isBusy)
            {
                CscUserMainData data = ((FrameworkElement)sender).DataContext as CscUserMainData;
                var text = string.Format("{0} - {1} - {2}", data.FriendName, data.Ip, data.Email);

                Trace.WriteLine(text);

                if (data.Status == 1)
                {
                    try
                    {
                        IPEndPoint iPEndPoint =
                            new IPEndPoint(IPAddress.Parse(data.Ip), callingService.localEndPoint.Port);

                        Task.Run(() =>
                        {
                            callingService.EncryptedCallSender = true;
                            callingService.MakeCall(iPEndPoint, client.UserProfile.Email, data.Email);
                        });

                    }
                    catch (Exception ex)
                    { MessageBox.Show(ex.Message); }
                }
                else
                { MessageBox.Show("Użytkownik nie jest dostępny"); }
            }
            else
            { MessageBox.Show("Jesteś w trakcie rozmowy"); }
        }

        private void RowButtonRemoveFriend_Click(object sender, RoutedEventArgs args)
        {
            CscUserMainData data = ((FrameworkElement)sender).DataContext as CscUserMainData;

            if (!String.IsNullOrEmpty(data.FriendName))
            {
                client.SendRemoveUserFromFriendsListDataRequestEncrypted(new CscChangeFriendData { Id = data.Id, FriendName = data.FriendName });
                client.SearchedUsers.Remove(data);
                data.FriendName = String.Empty;
                data.CanBeRemoved = false;
                data.IsNotFriend = true;
                client.SearchedUsers.Add(data);
            }
            else
            {
                FriendsListEditWindow window = new FriendsListEditWindow(FriendsListDataGrid, client, data, false);
                window.ShowDialog();
            }
        }

        private void RowButtonEditNickname_Click(object sender, RoutedEventArgs args)
        {
            CscUserMainData data = ((FrameworkElement)sender).DataContext as CscUserMainData;
            if (client.FriendsList.FirstOrDefault(u => u.Id == data.Id) != null)
            {
                FriendsListEditWindow window = new FriendsListEditWindow(FriendsListDataGrid, client, data, true);
                window.ShowDialog();
            }
        }

        private void ClientSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (lastClickedTab != null)
            {
                lastClickedTab.Background = Brushes.LightGreen;
            }
            lastClickedTab = sender as Button;
            lastClickedTab.Background = Brushes.Yellow;

            client.LastBookmark = LastBookmarkEnum.search;
            searchWindow = new ClientSearchWindow(FriendsListDataGrid, client);
            searchWindow.ShowDialog();
        }

        private void OnlineUsersButton_Click(object sender, RoutedEventArgs e)
        {
            if (lastClickedTab != null)
            {
                lastClickedTab.Background = Brushes.LightGreen;
            }
            lastClickedTab = sender as Button;
            lastClickedTab.Background = Brushes.Yellow;

            client.LastBookmark = LastBookmarkEnum.online;
            client.SendRefreshRequest();
            FriendsListDataGrid.DataContext = client.GetOnlineUsers();
        }

        private void AllFriendsButton_Click(object sender, RoutedEventArgs e)
        {
            if (lastClickedTab != null)
            {
                lastClickedTab.Background = Brushes.LightGreen;
            }
            lastClickedTab = sender as Button;
            lastClickedTab.Background = Brushes.Yellow;
            client.LastBookmark = LastBookmarkEnum.friendslist;
            client.SendRefreshRequest();
            FriendsListDataGrid.DataContext = client.GetFriendsList();
        }
    }
}
