using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using VoipApplication;

namespace VoIP_Server
{
    /// <summary>
    /// Interaction logic for UsersDBControl.xaml
    /// </summary>
    public partial class UsersDBControl : UserControl
    {
        DatabaseManager databaseManager = new DatabaseManager();
        List<Users> usersList;

        public UsersDBControl()
        {
            InitializeComponent();
            using (VoiceChatDBEntities db = new VoiceChatDBEntities())
            {
                usersList = db.Users.ToList();
            }
                
        }

        public void RefreshDataGrid()
        {
            using (VoiceChatDBEntities db = new VoiceChatDBEntities())
            {
                usersList = db.Users.ToList();
                UsersDataGrid.ItemsSource = usersList;
            }
        }
        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            DB_AddUserWindow window = new DB_AddUserWindow
            {
                UserControl = this
            };
            window.Show();
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedRow = (Users)UsersDataGrid.SelectedItem;
                usersList.Remove(selectedRow);
                databaseManager.DeleteUser(selectedRow);
                RefreshDataGrid();
            }
            catch (Exception)
            {

                MessageBox.Show("Wybierz rekord do usunięcia");
            }

        }

        private void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRow = (Users)UsersDataGrid.SelectedItem;
            if(selectedRow!=null)
            {
                DB_EditUser window = new DB_EditUser(selectedRow)
                {
                    UserControl = this,
                };
                window.Show();
            }
            else
            {
                MessageBox.Show("Wybierz rekord do edycji");
            }

        }
    }
}
