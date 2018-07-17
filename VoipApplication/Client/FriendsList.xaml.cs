using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using VoIP_Server;
using VoIP_Server.Client;

namespace VoIP_Client
{
    /// <summary>
    /// Interaction logic for FriendsList.xaml
    /// </summary>
    public partial class FriendsList : UserControl
    {
        CallingService callingService;
        ObservableCollection<CscUserMainData> obsCollection=new ObservableCollection<CscUserMainData>();
        Client client;
        Window parentWindow;

        public FriendsList(Client client,CallingService callingService,Window parentWindow)
        {
            this.client = client;
            this.parentWindow = parentWindow;


            this.callingService = callingService;

            //callingService.Users = usersList;
            callingService.InfoEvent += UpdateInfoLabel;
            //callingService.InfoEvent += MSGBoxShow;


            InitializeComponent();

        }

        private void MSGBoxShow(string msg)
        {
            MessageBox.Show(msg);
        }


        private void RowButtonCall_Click(object sender,RoutedEventArgs args)
        {
            if(!callingService.isCalling)
            {

                CscUserMainData data = ((FrameworkElement)sender).DataContext as CscUserMainData;
                var text = string.Format("{0} - {1} - {2}",data.FriendName, data.Ip, data.Email);

                if(data.Status==1)
                {
                    InfoLabel.Content = "Trwa łączenie";
                    IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(data.Ip), 2999);
                    UserProfile userProfile = new UserProfile()
                    {
                        Email = client.UserProfile.Email,
                        Status = 1,
                        Id = client.UserProfile.Id,
                        UserIp = client.UserProfile.Ip,
                        UserPort = 2999
                    };

                    System.Media.SoundPlayer player = new System.Media.SoundPlayer("telephone_ring.wav");
                    var callingWindowSender = new CallingWindowSender(client, callingService, player, parentWindow, new IPEndPoint(IPAddress.Parse(data.Ip), 2999), userProfile);
                    callingWindowSender.NickLabel.Text = (parentWindow as ClientMainWindow).UserEmailLabel.Text;
                    callingWindowSender.AnswerButton.Content = "Trwa łączenie";
                    callingWindowSender.Show();


                    parentWindow.Close();

                    

                }
                else
                {
                    MessageBox.Show("Użytkownik nie jest dostępny");
                }

            }else
            {
                callingService.CancelCall();

            }



            //MessageBox.Show(text);
        }

        public void UpdateInfoLabel(string msg)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                InfoLabel.Content = msg;
            }
            ));

        }

        private void OnlineUsersButton_Click(object sender, RoutedEventArgs e)
        {
            client.SendRefreshRequest();
            FriendsListDataGrid.DataContext = client.GetOnlineUsers();
        }

        private void AllFriendsButton_Click(object sender, RoutedEventArgs e)
        {
            FriendsListDataGrid.DataContext = client.GetFriendsList();
        }
    }
}
