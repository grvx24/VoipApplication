﻿using System;
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
        public DB_EditUser(Users user)
        {
            InitializeComponent();
            this.user = user;
            EmailTextBox.Text = user.Email;
            PasswordTextBox.Text = user.Password;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                user.Email = EmailTextBox.Text;
                user.Password = PasswordTextBox.Text;

                manager.EditUser(user);

                if (UserControl != null)
                {
                    UserControl.RefreshDataGrid();
                }

                MessageBox.Show("Zaktualizowano dane użytkownika");

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
