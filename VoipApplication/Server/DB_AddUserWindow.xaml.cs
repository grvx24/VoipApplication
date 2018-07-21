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
    /// Interaction logic for DB_AddUserWindow.xaml
    /// </summary>
    public partial class DB_AddUserWindow : Window
    {
        DatabaseManager manager = new DatabaseManager();
        public UsersDBControl UserControl { get; set; }

        public DB_AddUserWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
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
                if (!(queryEmailResult == null))
                {
                    MessageBox.Show("Podany adres email jest już zajęty!");
                    return;
                }

                var user = new Users() { Email = EmailTextBox.Text,
                    Password = CscSHA512Generator.get_SHA512_hash_as_string(PasswordTextBox.Password),
                    RegistrationDate = DateTime.Now,LastLoginDate=null };

                manager.AddUser(user);

                EmailTextBox.Clear();
                PasswordTextBox.Clear();

                if(UserControl != null)
                {
                    UserControl.RefreshDataGrid();
                }

                MessageBox.Show("Pomyślnie dodano użytkownika");

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
    }
}
