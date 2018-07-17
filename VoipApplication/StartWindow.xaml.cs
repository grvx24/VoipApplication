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
using VoIP_Client;

namespace VoIP_Server
{
    /// <summary>
    /// Interaction logic for StartWindow.xaml
    /// </summary>
    public partial class StartWindow : Window
    {
        public StartWindow()
        {
            InitializeComponent();
        }

        private void StartServer_Click(object sender, RoutedEventArgs e)
        {
            ServerMainWindow window = new ServerMainWindow();
            window.Show();
            Close();
        }

        private void StartClient_Click(object sender, RoutedEventArgs e)
        {
            ConnectionWindow window = new ConnectionWindow();
            window.Show();
            Close();
        }
    }
}
