using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;
using VoIP_Server.Server;
using System.Collections.ObjectModel;
using VoipApplication;

namespace VoIP_Server
{
    public partial class ServerMainWindow : Window
    {
        //front end info
        private enum CurrentPage { OnlineUsers, UsersDB, FriendsDB, ServerLog, Settings, Guide }
        CurrentPage currentPage = CurrentPage.OnlineUsers;

        //network
        MainServer server;
        ObservableCollection<ConnectedUsers> onlineUsers = new ObservableCollection<ConnectedUsers>();

        //ostatnio kliknięty przycisk
        Button lastClickedButton = null;

        //server log
        //Musi być stworzony od początku programu, żeby nie tracić do niego referencji
        static ServerLogControl serverLogControl = new ServerLogControl();
        static SettingsUserControl settingUserControl = new SettingsUserControl();
        public ServerMainWindow()
        {
            server = new MainServer();
            server.ServerConsoleWriteEvent += UpdateServerLog;
            server.ServerMessageBoxShowEvent += ShowAlert;
            server.CreateServer(settingUserControl.GetSelectedIp(), settingUserControl.GetPort());
            InitializeComponent();
            LoadGrid(currentPage);
            onlineUsers = server.GetOnlineClients();
            server.RemoveOfflineUserEvent += RemoveDisconnecteUser;
        }

        private void ShowAlert(string msg)
        {
            settingUserControl.CurrentIPLabel.Content = "";
            MessageBox.Show(msg);
        }
        public void UpdateServerLog(string message)
        {
            string msg = DateTime.Now + ": " + message + Environment.NewLine;
            Dispatcher.Invoke(new Action(() =>
            {
                serverLogControl.ServerConsole.AppendText(msg);
            }));

        }

        private void RunServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!server.Running)
            {
                server.CreateServer(settingUserControl.GetSelectedIp(), settingUserControl.GetPort());
                settingUserControl.CurrentIPLabel.Content = settingUserControl.GetSelectedIp() + ":" + settingUserControl.GetPort();
                server.RunAsync();
            }
            else
            {
                settingUserControl.CurrentIPLabel.Content = "";
                server.StopRunning();
            }

            RunServerButton.Content = server.Running ? "Online" : "Offline";
            RunServerButton.Background = server.Running ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            RunServerButton.BorderBrush = server.Running ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
        }

        private void RemoveDisconnecteUser(ConnectedUsers user)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                onlineUsers.Remove(user);
            }));
        }

        private void LoadGrid(CurrentPage page)
        {
            var localEntities = new VoiceChatDBEntities();//n to chyba sie przyda do odswierzania na serwerze ladnego okienek !!!!!
            currentPage = page;
            //default button color #FF673AB7
            switch (page)
            {
                case CurrentPage.OnlineUsers:
                    var onlineUsersControl = new OnlineUsersControl();
                    onlineUsersControl.OnlineUsersDataGrid.ItemsSource = onlineUsers;

                    CustomUserControl.Content = onlineUsersControl;
                    break;

                case CurrentPage.UsersDB:
                    var allUsers = localEntities.Users.ToList();
                    var userControl = new UsersDBControl(server);
                    userControl.UsersDataGrid.ItemsSource = allUsers;

                    CustomUserControl.Content = userControl;
                    break;

                case CurrentPage.FriendsDB:

                    var friendsControl = new FriendsListControl();
                    friendsControl.FriendsListDataGrid.ItemsSource = localEntities.FriendsList.ToList();
                    CustomUserControl.Content = friendsControl;
                    break;

                case CurrentPage.ServerLog:

                    CustomUserControl.Content = serverLogControl;
                    break;

                case CurrentPage.Settings:
                    CustomUserControl.Content = settingUserControl;
                    break;

                case CurrentPage.Guide:
                    CustomUserControl.Content = new GuideUserControl();
                    break;

                default:
                    break;
            }
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            if (lastClickedButton != null)
            {
                lastClickedButton.Background = Brushes.DarkGreen;
            }

            lastClickedButton = sender as Button;
            lastClickedButton.Background = Brushes.Purple;

            LoadGrid(CurrentPage.UsersDB);
        }

        private void OnlineUsersButton_Click(object sender, RoutedEventArgs e)
        {
            if (lastClickedButton != null)
            {
                lastClickedButton.Background = Brushes.DarkGreen;
            }

            lastClickedButton = sender as Button;
            lastClickedButton.Background = Brushes.Purple;

            LoadGrid(CurrentPage.OnlineUsers);
        }

        private void FriendsListButton_Click(object sender, RoutedEventArgs e)
        {
            if (lastClickedButton != null)
            {
                lastClickedButton.Background = Brushes.DarkGreen;
            }

            lastClickedButton = sender as Button;
            lastClickedButton.Background = Brushes.Purple;

            LoadGrid(CurrentPage.FriendsDB);
        }

        private void ServerLogButton_Click(object sender, RoutedEventArgs e)
        {
            if (lastClickedButton != null)
            {
                lastClickedButton.Background = Brushes.DarkGreen;
            }

            lastClickedButton = sender as Button;
            lastClickedButton.Background = Brushes.Purple;

            if (currentPage != CurrentPage.ServerLog)
                LoadGrid(CurrentPage.ServerLog);
            else
                return;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (MessageBox.Show("Zamknąć aplikację?", "Zamykanie aplikacji serwera", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Environment.Exit(0);
            }
            else
            {
                e.Cancel = true;
                return;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (lastClickedButton != null)
            {
                lastClickedButton.Background = Brushes.DarkGreen;
            }

            lastClickedButton = sender as Button;
            lastClickedButton.Background = Brushes.Purple;

            if (currentPage != CurrentPage.Settings)
                LoadGrid(CurrentPage.Settings);
            else
                return;
        }

        private void GuideButton_Click(object sender, RoutedEventArgs e)
        {
            if (lastClickedButton != null)
            {
                lastClickedButton.Background = Brushes.DarkGreen;
            }
            lastClickedButton = sender as Button;
            lastClickedButton.Background = Brushes.Purple;

            if (currentPage != CurrentPage.Guide)
                LoadGrid(CurrentPage.Guide);
            else
                return;
        }
    }
}
