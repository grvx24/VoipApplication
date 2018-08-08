using cscprotocol;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace VoIP_Client
{
    /// <summary>
    /// Interaction logic for DB_AddUserWindow.xaml
    /// </summary>
    public partial class FriendsListEditWindow : Window
    {
        DataGrid parentUsersGrid;
        Client client;
        CscUserMainData friendData;
        bool changeName;

        public FriendsListEditWindow(DataGrid usersGrid, Client client, CscUserMainData userdata, bool changeName)
        {
            this.client = client;
            parentUsersGrid = usersGrid;
            friendData = userdata;
            InitializeComponent();
            if(userdata.Email.Length>=20)
            {
                EmailTextBlock.FontSize = 20;
            }else
            {
                EmailTextBlock.FontSize = 25;
            }
            EmailTextBlock.Text = userdata.Email;
            this.changeName = changeName;

            if (changeName)
            {
                UsernameTextBox.Text = friendData.FriendName;
                EditFavouriteUserButton.Content = "Edytuj";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        { this.Close(); }

        private void EditFavouriteUserButton_Click(object sender, RoutedEventArgs e)
        {
            client.SearchedUsers.Clear();

            if (changeName)
            {
                if (UsernameTextBox.Text != string.Empty)
                {
                    client.SendRemoveUserFromFriendsListDataRequestEncrypted(new CscChangeFriendData { Id = friendData.Id, FriendName = UsernameTextBox.Text });
                    client.SendAddUserToFriendsListDataRequestEncrypted(new CscChangeFriendData { Id = friendData.Id, FriendName = UsernameTextBox.Text });
                    Trace.WriteLine("Zmieniono nazwę");
                }
            }
            else
            {
                if (UsernameTextBox.Text != string.Empty)
                {//dodaj tego usera do naszych znajomych      
                    client.SendAddUserToFriendsListDataRequestEncrypted(new CscChangeFriendData { Id = friendData.Id, FriendName = UsernameTextBox.Text });
                    Trace.WriteLine("Dodano do znajomych");
                    friendData.CanBeRemoved = true;
                    friendData.IsNotFriend = false;
                }
            }


            //if (IsOurFriend)
            //{
            //    if (UsernameTextBox.Text != string.Empty)
            //    {
            //        client.SendRemoveUserFromFriendsListDataRequest(new CscChangeFriendData { Id = friendData.Id, FriendName = UsernameTextBox.Text });
            //        //client.SearchedUsers.Clear();
            //        client.SendAddUserToFriendsListDataRequest(new CscChangeFriendData { Id = friendData.Id, FriendName = UsernameTextBox.Text });
            //    }
            //    else
            //    { //usun tego usera z ulubionych
            //        client.SendRemoveUserFromFriendsListDataRequest(new CscChangeFriendData { Id = friendData.Id, FriendName = UsernameTextBox.Text });
            //    }
            //}
            //else
            //{

            //}

            //pobranie odpowiedniego okna jeszcze raz zeby FriendName w grid sie odświerzyło na zmienione !!!!            
            try
            {
                client.SendRefreshRequest();
                switch (client.LastBookmark)
                {
                    case LastBookmarkEnum.online:
                        {
                            parentUsersGrid.DataContext = client.GetOnlineUsers();
                            break;
                        }
                    case LastBookmarkEnum.friendslist:
                        {
                            parentUsersGrid.DataContext = client.GetFriendsList();
                            break;
                        }
                    case LastBookmarkEnum.search:
                        {
                            client.SendSearchUserRequestEncrypted(client.LastSearchText);
                            parentUsersGrid.DataContext = client.GetSearchUsers();
                            break;
                        }
                }
                Close();
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message); }

            Close();
        }
    }
}
