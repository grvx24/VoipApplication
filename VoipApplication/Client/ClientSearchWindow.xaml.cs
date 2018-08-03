using cscprotocol;
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
using VoipApplication;

namespace VoIP_Client
{
    /// <summary>
    /// Interaction logic for DB_AddUserWindow.xaml
    /// </summary>
    public partial class ClientSearchWindow : Window
    {
        DataGrid parentUsersGrid;
        Client client;
        public ClientSearchWindow(DataGrid usersGrid, Client client)
        {
            parentUsersGrid = usersGrid;
            this.client = client;
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        { this.Close(); }

        private void SearchUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (EmailTextBox.Text.Length > 2)
            {
                client.SearchedUsers.Clear();
                client.SendSearchUserRequest(EmailTextBox.Text);
                client.LastSearchText = EmailTextBox.Text;
                try
                {
                    parentUsersGrid.DataContext = client.GetSearchUsers();
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            { MessageBox.Show("Podaj minimum 3 znaki."); }
        }
    }
}
