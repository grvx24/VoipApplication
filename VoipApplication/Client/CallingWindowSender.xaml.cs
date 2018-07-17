using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

namespace VoIP_Client
{
    /// <summary>
    /// Interaction logic for CallingWindowSender.xaml
    /// </summary>
    public partial class CallingWindowSender : Window
    {

        //System.Media.SoundPlayer player;
        CallingService service;
        System.Timers.Timer aTimer = new System.Timers.Timer();
        Window win;
        Client client;

        public CallingWindowSender(Client client, CallingService service, System.Media.SoundPlayer player, Window window, IPEndPoint endPoint, UserProfile profile)
        {
            this.service = service;
            this.win = window;
            this.client = client;
            service.InfoEvent += ChangeInfoText;
            
            InitializeComponent();


            var t = service.Call(profile, endPoint);


        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            service.SendCancel();
            service.StopListening();
            service.chatClient.Disconnect();
            service.EndConnection();

            ClientMainWindow clientMainWindow2 = new ClientMainWindow(this.client,false);
            clientMainWindow2.Show();

            this.Close();
        }

        private void ChangeInfoText(string msg)
        {
            AnswerButton.Content = msg;
        }

    }
}
