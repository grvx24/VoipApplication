using cscprotocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using VoipApplication;

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

        public FriendsListEditWindow(DataGrid usersGrid, Client client, CscUserMainData userdata,bool changeName)
        {
            this.client = client;
            parentUsersGrid = usersGrid;
            friendData = userdata;
            InitializeComponent();
            EmailTextBlock.Text = userdata.Email;
            this.changeName = changeName;

            if(changeName)
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

            if(changeName)
            {
                if (UsernameTextBox.Text != string.Empty)
                {
                    client.SendRemoveUserFromFriendsListDataRequest(new CscChangeFriendData { Id = friendData.Id, FriendName = UsernameTextBox.Text });
                    client.SendAddUserToFriendsListDataRequest(new CscChangeFriendData { Id = friendData.Id, FriendName = UsernameTextBox.Text });
                    Trace.WriteLine("Zmieniono nazwę");
                }
            }
            else
            {
                if (UsernameTextBox.Text != string.Empty)
                {//dodaj tego usera do naszych znajomych                    
                    client.SendAddUserToFriendsListDataRequest(new CscChangeFriendData { Id = friendData.Id, FriendName = UsernameTextBox.Text });
                    Trace.WriteLine("Dodano do znajomych");
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
                    case "online":
                        {
                            parentUsersGrid.DataContext = client.GetOnlineUsers();
                            break;
                        }
                    case "friends":
                        {
                            parentUsersGrid.DataContext = client.GetFriendsList();
                            break;
                        }
                    case "search":
                        {
                            client.SendSearchUserRequest(client.LastSearchText);
                            parentUsersGrid.DataContext = client.GetSearchUsers();
                            break;
                        }
                }
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            Close();
        }
    }
}
