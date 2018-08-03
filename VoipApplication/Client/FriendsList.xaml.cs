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
using System.Diagnostics;
using System.Data;

namespace VoIP_Client
{
    /// <summary>
    /// Interaction logic for FriendsList.xaml
    /// </summary>
    public partial class FriendsList : UserControl
    {
        CallingService callingService;
        ObservableCollection<CscUserMainData> obsCollection = new ObservableCollection<CscUserMainData>();
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
            //try { searchWindow.Hide(); } catch (Exception) { }

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
        private void RowButtonEdit_Click(object sender, RoutedEventArgs args)
        {
            //try { searchWindow.Hide(); } catch (Exception) { }

            CscUserMainData data = ((FrameworkElement)sender).DataContext as CscUserMainData;

            if (client.FriendsList.FirstOrDefault(u => u.Id == data.Id) != null)
            {
                client.SendRemoveUserFromFriendsListDataRequest(new CscChangeFriendData { Id = data.Id, FriendName = data.FriendName });
                //window = new FriendsListEditWindow(FriendsListDataGrid, client, data, true);
            }
            else
            {
                FriendsListEditWindow window = new FriendsListEditWindow(FriendsListDataGrid, client, data,false);
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

        //public void UpdateInfoLabel(string msg)// n !!!! czy to jest potrzebne w ogole?
        //{
        //    Dispatcher.Invoke(new Action(() =>
        //    {
        //        InfoLabel.Content = msg;
        //    }
        //    ));
        //}

        private void ClientSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (lastClickedTab != null)
            {
                lastClickedTab.Background = Brushes.LightGreen;
            }
            lastClickedTab = sender as Button;
            lastClickedTab.Background = Brushes.Yellow;

            client.LastBookmark = "search";
            //if (searchWindow == null)
            { searchWindow = new ClientSearchWindow(FriendsListDataGrid, client); }
            searchWindow.ShowDialog();
        }

        private void OnlineUsersButton_Click(object sender, RoutedEventArgs e)
        {
            //try { searchWindow.Hide(); } catch (Exception) { }

            if (lastClickedTab != null)
            {
                lastClickedTab.Background = Brushes.LightGreen;
            }
            lastClickedTab = sender as Button;
            lastClickedTab.Background = Brushes.Yellow;


            client.LastBookmark = "online";
            client.SendRefreshRequest();
            FriendsListDataGrid.DataContext = client.GetOnlineUsers();
        }

        private void AllFriendsButton_Click(object sender, RoutedEventArgs e)
        {
            //try { searchWindow.Hide(); } catch (Exception) { }

            if (lastClickedTab != null)
            {
                lastClickedTab.Background = Brushes.LightGreen;
            }
            lastClickedTab = sender as Button;
            lastClickedTab.Background = Brushes.Yellow;
            client.LastBookmark = "friends";
            client.SendRefreshRequest();
            FriendsListDataGrid.DataContext = client.GetFriendsList();
        }
    }
}
