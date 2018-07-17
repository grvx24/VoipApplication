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
    /// Interaction logic for CallingWindow.xaml
    /// </summary>
    public partial class CallingWindow : Window
    {
        //System.Media.SoundPlayer player;
        CallingService service;
        System.Timers.Timer aTimer = new System.Timers.Timer();
        CallingEventArgs eventArgs;
        Window win;
        Client client;
        public CallingWindow(Client client ,CallingService service, System.Media.SoundPlayer player, CallingEventArgs eventArgs, Window window)
        {
            this.client = client;
            win = window;
            this.eventArgs = eventArgs;
            this.service = service;
            //this.player = player;
            InitializeComponent();
            //player.Play();
            RejectionTimer();
        }


        private void AnswerButton_Click(object sender, RoutedEventArgs e)
        {
            service.callState = CallState.Answer;
            aTimer.Stop();


            service.chatClient.StartRecording(0);

            AnswerButton.IsHitTestVisible = false;
            AnswerButton.Content = "W trakcie rozmowy";
            RejectButton.Content = "Zakończ";

            Dispatcher.Invoke(new Action(() =>
           {
               win.Close();
           }));

            //player.Stop();
            //player.Dispose();
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            service.SendCancel();
            service.StopListening();
            service.chatClient.Disconnect();
            service.EndConnection();


            ClientMainWindow clientMainWindow2 = new ClientMainWindow(this.client,false);
            clientMainWindow2.Show();



            //player.Stop();
            //player.Dispose();
            this.Close();
        }

        private void RejectionTimer()
        {
            aTimer.Elapsed += new System.Timers.ElapsedEventHandler((object source, System.Timers.ElapsedEventArgs e) => {   
            Application.Current.Dispatcher.BeginInvoke(new Action(() => RejectButton_Click(null, null)));
                aTimer.Dispose();
            });
            aTimer.Interval = 5000;
            aTimer.Enabled = true;
        }

    }
}
