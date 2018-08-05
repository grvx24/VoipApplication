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

namespace VoIP_Server
{
    /// <summary>
    /// Interaction logic for DB_EditUser.xaml
    /// </summary>
    public partial class DB_EditUser : Window
    {
        DatabaseManager manager = new DatabaseManager();
        public UsersDBControl UserControl { get; set; }
        public Users user;
        private MainServer server;
        public DB_EditUser(Users user, MainServer server)
        {
            InitializeComponent();
            this.user = user;
            EmailTextBox.Text = user.Email;
            //PasswordTextBox.Text = user.Password;
            this.server = server;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        { Close(); }
        

        private void EditEmailButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EmailValidator.IsValid(EmailTextBox.Text))
                {
                    MessageBox.Show("Adres email niepoprawny!");
                    return;
                }

                VoiceChatDBEntities serverDB = new VoiceChatDBEntities();
                var queryEmailResult = serverDB.Users.FirstOrDefault(u => u.Email == EmailTextBox.Text);
                if (queryEmailResult != null)
                {
                    MessageBox.Show("Podany adres email jest już zajęty!");
                    return;
                }
                if (server.OnlineUsers.Any(u => u.Id == user.UserId))
                {
                    MessageBox.Show("Nie można edytować użytkownika, który jest online!");
                    return;
                }

                user.Email = EmailTextBox.Text;
                manager.EditUser(user);

                //n !!! trzeba wyslac wszystkim userom online info, zeby zaktualizowali sobie tego usera
                var onlineUsers = server.OnlineUsers;
                foreach (var singleUser in onlineUsers)
                {
                    server.SendNewFriendUserEncrypted(singleUser.Client,
                        new cscprotocol.CscChangeFriendData { Id = user.UserId }, singleUser.DH.Key);
                }

                if (UserControl != null)
                {
                    UserControl.RefreshDataGrid();
                }

                MessageBox.Show("Zaktualizowano dane użytkownika.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void EditPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PasswordValidator.ValidatePassword(PasswordTextBox.Password))
                {
                    MessageBox.Show("Hasło jest za słabe!");
                    return;
                }

                VoiceChatDBEntities serverDB = new VoiceChatDBEntities();
                var queryEmailResult = serverDB.Users.FirstOrDefault(u => u.Email == EmailTextBox.Text);
                if (server.OnlineUsers.Any(u => u.Id == user.UserId))
                {
                    MessageBox.Show("Nie można edytować użytkownika, który jest online!");
                    return;
                }
                
                user.Password = CscSHA512Generator.get_SHA512_hash_as_string(PasswordTextBox.Password);

                manager.EditUser(user);

                if (UserControl != null)
                {
                    UserControl.RefreshDataGrid();
                }

                MessageBox.Show("Zaktualizowano dane użytkownika.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
