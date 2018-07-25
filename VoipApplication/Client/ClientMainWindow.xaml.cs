using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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
using VoIP_Server;
using cscprotocol;
using System.Net;
using codecs;
using NAudio.Wave;

namespace VoIP_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ClientMainWindow : Window
    {
        static FriendsList friendsListGrid;
        static ClientSettingsUserControl settingsGrid;

        CallingService callingService;
        static int port = 7999;

        Client client;
        public ClientMainWindow(Client client, bool createNewInstance)
        {
            callingService = new CallingService(new IPEndPoint(client.LocalIP, port));

            friendsListGrid = new FriendsList(client, callingService, this);
            settingsGrid = new ClientSettingsUserControl(client, callingService, this);

            if (!createNewInstance)
            {
                this.client = client;

                client.AddItemEvent += AddOnlineUser;
                client.RemoveItemEvent += RemoveOnlineUser;

                client.AddFriendEvent += AddFriendToObsCollection;
                client.RemoveFriendEvent += RemoveFriendToObsCollection;

                client.ConnectionLostEvent += LeaveServer;
                client.SetProfileText += UpdateProfileEmail;
                client.AddSearchEvent += AddSearchUser;
            }
            else
            {
                this.client = new Client()
                {
                    client = client.client,
                    LocalIP = client.LocalIP,
                    IsConnected = true,
                    Port = client.Port,
                    IPAddress = client.IPAddress,
                    UserProfile = client.UserProfile
                };

                this.client.AddItemEvent += AddOnlineUser;
                this.client.RemoveItemEvent += RemoveOnlineUser;

                this.client.AddFriendEvent += AddFriendToObsCollection;
                this.client.RemoveFriendEvent += RemoveFriendToObsCollection;

                this.client.ConnectionLostEvent += LeaveServer;
                this.client.SetProfileText += UpdateProfileEmail;
                this.client.AddSearchEvent += AddSearchUser;
            }

            InitializeComponent();
            client.StartListening();

            InitCallingService();

            client.GetBasicInfo();
            UserEmailLabel.Text = client.UserProfile.Email;//n
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            CustomUserControl.Content = settingsGrid;
        }

        public void UpdateProfileEmail(CscUserMainData profile)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                UserEmailLabel.Text = profile.Email;
            }));
        }

        private void AddFriendToObsCollection(CscUserMainData friendData)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                client.FriendsList.Add(friendData);
            }
            ));
        }

        private void RemoveFriendToObsCollection(CscUserMainData userToRemove)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                client.FriendsList.Remove(userToRemove);
            }
            ));
        }

        private void FriendsButton_Click(object sender, RoutedEventArgs e)
        {
            CustomUserControl.Content = friendsListGrid;
        }

        public void LeaveServer()
        {
            MessageBox.Show("Połączenie z serwerem zostało zerwane");

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
               //ConnectionWindow window = new ConnectionWindow();
               //window.Show();
               Close();
            }));
        }

        public void DebugConsole(string msg)
        {
            MessageBox.Show(msg);
        }

        public void RemoveOnlineUser(CscUserMainData user)
        {
            Dispatcher.Invoke(new Action(() => client.onlineUsers.Remove(user)));

            var friend = client.FriendsList.Where(u => u.Id == user.Id).FirstOrDefault();
            if (friend != null)
            {
                CscUserMainData newUser = new CscUserMainData() { Email = friend.Email, Id = friend.Id, Ip = "", FriendName = friend.FriendName, Status = 0 };
                Dispatcher.Invoke(new Action(() => client.FriendsList.Remove(friend)));
                client.FriendsList.Add(newUser);
            }
        }

        public void AddOnlineUser(CscUserMainData user)
        {
            Dispatcher.Invoke(new Action(() => client.onlineUsers.Add(user)));

            var friend = client.FriendsList.Where(u => u.Id == user.Id).FirstOrDefault();
            if (friend != null)
            {
                CscUserMainData newUser = new CscUserMainData() { Email = friend.Email, Id = friend.Id, Ip = friend.Ip, FriendName = friend.FriendName, Status = 1 };
                Dispatcher.Invoke(new Action(() => client.FriendsList.Remove(friend)));
                client.FriendsList.Add(newUser);
            }
        }
        public void AddSearchUser(CscUserMainData user)
        {
            Dispatcher.Invoke(new Action(() => client.SearchedUsers.Add(user)));
        }



        private void LogOutButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectionWindow loginWindow = new ConnectionWindow();
            loginWindow.Show();
            this.Close();

        }


        //Sygnalizacja
        //Dzwonienie i wysyłanie dźwięku


        private WaveIn waveIn;
        private UdpClient udpSender;
        private UdpClient udpListener;
        private IWavePlayer waveOut;
        private BufferedWaveProvider waveProvider;
        private INetworkChatCodec codec = new G722ChatCodec();


        private volatile bool connected = false;
        private volatile bool isListening = false;
        private volatile bool isSendingPackets = true;

        public void InitCallingService()
        {
            this.Closed += new EventHandler(NetworkChatPanel_Disposed);
            callingService.IncomingCallEvent += ShowIncomingCallWindow;
            callingService.MakeCallEvent += ShowCallingWindow;
            callingService.EndCall += HideCallingWindow;
            callingService.EndCall += HideIncomingCallWindow;
            callingService.EndCall += Disconnect;
            callingService.TalkEvent += CallingWindowToTalkingWindow;

            callingService.UDPSenderStart += TurnOnUdpSending;
            callingService.UDPSenderStop += TurnOffUdpSending;
            callingService.UDPListenerStart += StartListening;

            IncomingCallGrid.Visibility = Visibility.Hidden;
            callingService.ErrorEvent += MsgBoxShow;
            callingService.StartListening();
        }

        private void MsgBoxShow(string msg)
        {
            MessageBox.Show(msg);
        }

        void NetworkChatPanel_Disposed(object sender, EventArgs e)
        {
            Disconnect();
        }


        private void Connect(IPEndPoint endPoint, int inputDeviceNumber, INetworkChatCodec codec)
        {
            waveIn = new WaveIn
            {
                BufferMilliseconds = 100,
                DeviceNumber = inputDeviceNumber,
                WaveFormat = codec.RecordFormat
            };
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.StartRecording();

            udpSender = new UdpClient();
            //to wywalilo
            udpSender.Connect(endPoint);


            connected = true;

        }

        private void StartListening(IPEndPoint localEndPoint)
        {
            udpListener = new UdpClient();
            udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpListener.Client.Bind(localEndPoint);

            waveOut = new WaveOut();
            waveProvider = new BufferedWaveProvider(codec.RecordFormat);
            waveOut.Init(waveProvider);
            waveOut.Play();

            ListenerThreadState state = new ListenerThreadState() { Codec = codec, EndPoint = localEndPoint };
            isListening = true;
            ThreadPool.QueueUserWorkItem(this.ListenerThread, state);


        }

        class ListenerThreadState
        {
            public IPEndPoint EndPoint { get; set; }
            public INetworkChatCodec Codec { get; set; }
        }

        private void ListenerThread(object state)
        {
            ListenerThreadState listenerThreadState = (ListenerThreadState)state;
            IPEndPoint endPoint = listenerThreadState.EndPoint;
            try
            {

                while (isListening)
                {
                    Thread.Sleep(10);

                    if (udpListener.Available > 0)
                    {
                        byte[] b = this.udpListener.Receive(ref endPoint);

                        byte[] decoded = listenerThreadState.Codec.Decode(b, 0, b.Length);
                        waveProvider.AddSamples(decoded, 0, decoded.Length);
                    }

                }
                isListening = false;
                udpListener.Close();
            }
            catch (Exception)
            {
                isListening = false;
                // usually not a problem - just means we have disconnected
            }
        }

        private void Disconnect()
        {

            //if(connected)
            {

                Dispatcher.Invoke(new Action(() =>
                {
                    connected = false;

                    if (waveIn != null)
                    {
                        waveIn.DataAvailable -= WaveIn_DataAvailable;
                        waveIn.StopRecording();
                        waveIn.Dispose();
                    }


                    if (udpSender != null)
                    {
                        udpSender.Close();
                    }

                    if (waveOut != null)
                    {
                        waveOut.Stop();
                        waveOut.Dispose();
                    }

                    if (udpListener != null)
                    {
                        udpListener.Close();
                    }

                    this.codec.Dispose();

                }

                ));


            }
        }


        void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (isSendingPackets)
            {
                byte[] encoded = codec.Encode(e.Buffer, 0, e.BytesRecorded);
                udpSender.Send(encoded, encoded.Length);
            }

        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (waveIn != null && connected)
            {
                if (!isSendingPackets)
                {
                    isSendingPackets = !isSendingPackets;
                    MuteButton.Content = "On";
                }
                else
                {
                    isSendingPackets = !isSendingPackets;
                    MuteButton.Content = "Off";
                }
            }
        }


        private void TurnOnUdpSending(IPEndPoint endPoint)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                int inputDeviceNumber = settingsGrid.InputDeviceComboBox.SelectedIndex;
                Connect(endPoint, inputDeviceNumber, codec);
            }
            ));

        }

        private void TurnOffUdpSending()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                Disconnect();
            }
            ));

        }

        private void TurnOnUdpListening()
        {
            StartListening(new IPEndPoint(CallingService.GetLocalIPAddress(), port));
        }

        private void TurnOffUdpListening()
        {
            isListening = false;
        }

        private void UdpListenerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isListening)
            {
                StartListening(new IPEndPoint(CallingService.GetLocalIPAddress(), port));
            }
            else
            {
                isListening = false;
            }

        }

        private void TcpListenerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!callingService.isListening)
            {
                callingService.StartListening();

            }
            else
            {
                callingService.StopListening();
            }

            if (callingService.isListening)
            {
                MessageBox.Show("Nasluchiwanie włączone");
            }
            else
            {
                MessageBox.Show("Nasluchiwanie wyłączone");
            }
        }

        private void ShowIncomingCallWindow(string username)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                IncomingCallGrid.Visibility = Visibility.Visible;
                CallInfoLabel.Content = username+" dzwoni!";
            }));

        }

        private void HideIncomingCallWindow()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                IncomingCallGrid.Visibility = Visibility.Hidden;
                AnswerButton.Visibility = Visibility.Visible;
                RejectButton.Content = "Odrzuć";
            }));
        }

        private void ShowCallingWindow(string text)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                AnswerButton.Visibility = Visibility.Hidden;
                RejectButton.Visibility = Visibility.Hidden;
                IncomingCallGrid.Visibility = Visibility.Visible;
                BreakCallButton.Visibility = Visibility.Visible;
                CallInfoLabel.Content = text;

            }));

        }

        private void CallingWindowToTalkingWindow(string text)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                AnswerButton.Visibility = Visibility.Hidden;
                BreakCallButton.Visibility = Visibility.Visible;
                BreakCallButton.Content = "Zakończ";

                if(text!=null)
                {
                    text = "";
                }
                CallInfoLabel.Content = "Rozmowa z: " + text;

            }));
        }

        private void HideCallingWindow()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                IncomingCallGrid.Visibility = Visibility.Hidden;
                AnswerButton.Visibility = Visibility.Visible;
                RejectButton.Visibility = Visibility.Visible;
                RejectButton.Content = "Odrzuć";
            }));
        }



        private void AnswerButton_Click(object sender, RoutedEventArgs e)
        {
            callingService.AnswerCall();
            AnswerButton.Visibility = Visibility.Hidden;
            RejectButton.Content = "Zakończ";

        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            callingService.RejectCall();
            AnswerButton.Visibility = Visibility.Visible;
            RejectButton.Content = "Odrzuć";
            HideIncomingCallWindow();
            Disconnect();
        }
        
        private void BreakCall_Click(object sender, RoutedEventArgs e)
        {
            callingService.BreakCall(true);
            Disconnect();
            IncomingCallGrid.Visibility = Visibility.Hidden;
        }

        private void GuideButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
