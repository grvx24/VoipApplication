using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using cscprotocol;
using System.Net;
using codecs;
using NAudio.Wave;
using System.Diagnostics;
using System.Media;
using System.ComponentModel;
using System.Text;

namespace VoIP_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ClientMainWindow : Window
    {
        static FriendsList friendsListGrid;
        static ClientSettingsUserControl settingsGrid;
        static GuideUserControl guideGrid;

        static Button lastClickedTab = null;

        CallingService callingService;
        static int port = 7999;

        Client client;
        public ClientMainWindow(Client client, bool createNewInstance)
        {
            callingService = new CallingService(new IPEndPoint(client.LocalIP, port));

            friendsListGrid = new FriendsList(client, callingService);
            settingsGrid = new ClientSettingsUserControl(client, callingService, this);
            guideGrid = new GuideUserControl();

            if (!createNewInstance)
            {
                this.client = client;

                client.AddItemEvent += AddOnlineUser;
                client.RemoveItemEvent += RemoveOnlineUser;

                client.AddFriendEvent += AddFriendToObsCollection;
                client.RemoveFriendEvent += RemoveFriendToObsCollection;

                client.ConnectionLostEvent += LeaveServer;
                //client.SetProfileText += UpdateProfileEmail;
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

                this.client.AddFriendEvent += AddFriendToObsCollection;
                this.client.RemoveFriendEvent += RemoveFriendToObsCollection;

                this.client.AddItemEvent += AddOnlineUser;
                this.client.RemoveItemEvent += RemoveOnlineUser;

                this.client.ConnectionLostEvent += LeaveServer;
                //this.client.SetProfileText += UpdateProfileEmail;
                this.client.AddSearchEvent += AddSearchUser;
            }

            InitializeComponent();
            client.StartListening();

            InitCallingService();

            client.GetBasicInfo();
            UserEmailLabel.Text = client.UserProfile.Email;//n !!!!
            if (System.IO.File.Exists("phone_sound.wav"))
            {
                player = new SoundPlayer("phone_sound.wav");
            }

            CustomUserControl.Content = friendsListGrid;
            lastClickedTab = FriendsButton;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (callingService.isBusy)
            {
                callingService.BreakCall(true);
            }

            base.OnClosed(e);

            callingService.StopListening();
            callingService.DisposeTcpListener();
        }


        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (lastClickedTab != null)
            {
                lastClickedTab.Background = Brushes.DarkGreen;
            }
            lastClickedTab = sender as Button;
            lastClickedTab.Background = Brushes.Purple;

            CustomUserControl.Content = settingsGrid;
        }

        //public void UpdateProfileEmail(CscUserMainData profile)
        //{
        //    Dispatcher.Invoke(new Action(() =>
        //    {
        //        UserEmailLabel.Text = profile.Email;
        //    }));
        //}

        private void AddFriendToObsCollection(CscUserMainData friendData)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                friendData.IsNotFriend = false;
                friendData.CanBeRemoved = true;
                client.FriendsList.Add(friendData);
            }
            ));
        }

        private void RemoveFriendToObsCollection(CscUserMainData userToRemove)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                try
                {
                    client.FriendsList.Remove(client.FriendsList.FirstOrDefault(u => u.Id == userToRemove.Id));
                }
                catch (Exception)
                { return; }
            }
            ));
        }

        private void FriendsButton_Click(object sender, RoutedEventArgs e)
        {
            if (lastClickedTab != null)
            {
                lastClickedTab.Background = Brushes.DarkGreen;
            }
            lastClickedTab = sender as Button;
            lastClickedTab.Background = Brushes.Purple;

            CustomUserControl.Content = friendsListGrid;
        }

        private void GuideButton_Click(object sender, RoutedEventArgs e)
        {
            if (lastClickedTab != null)
            {
                lastClickedTab.Background = Brushes.DarkGreen;
            }
            lastClickedTab = sender as Button;
            lastClickedTab.Background = Brushes.Purple;

            CustomUserControl.Content = guideGrid;
        }

        public void LeaveServer()
        {
            MessageBox.Show("Połączenie z serwerem zostało zerwane");

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                callingService.DisposeTcpListener();
                if (callingService.isBusy)
                {
                    callingService.RejectCall();
                    callingService.BreakCall();
                }
                client.Disconnect();
                ConnectionWindow loginWindow = new ConnectionWindow();
                loginWindow.Show();
                this.Close();
            }));
        }

        public void DebugConsole(string msg)
        {
            MessageBox.Show(msg);
        }

        public void RemoveOnlineUser(CscUserMainData user)
        {
            Dispatcher.Invoke(new Action(() => client.onlineUsers.Remove(user)));

            //var friend = client.FriendsList.Where(u => u.Id == user.Id).FirstOrDefault();
            //if (friend != null)
            //{
            //    CscUserMainData newUser = new CscUserMainData() { Email = friend.Email, Id = friend.Id, Ip = "", FriendName = friend.FriendName, Status = 0 };
            //    Dispatcher.Invoke(new Action(() =>
            //    {
            //        client.FriendsList.Remove(friend);
            //        client.FriendsList.Add(newUser);
            //    }
            //    ));
            //}
        }

        public void AddOnlineUser(CscUserMainData user)
        {
            if (!String.IsNullOrEmpty(user.FriendName))
            { user.CanBeRemoved = true; }
            else
            { user.IsNotFriend = true; }


            Dispatcher.Invoke(new Action(() => client.onlineUsers.Add(user)));

            //var friend = client.FriendsList.Where(u => u.Id == user.Id).FirstOrDefault();
            //if (friend != null)
            //{
            //    CscUserMainData newUser = new CscUserMainData() { Email = friend.Email, Id = friend.Id, Ip = friend.Ip, FriendName = friend.FriendName, Status = 1 };
            //    Dispatcher.Invoke(new Action(() =>
            //    {
            //        client.FriendsList.Remove(friend);
            //        client.FriendsList.Add(newUser);
            //    }  
            //    ));                
            //}
        }
        public void AddSearchUser(CscUserMainData user)
        {
            if (!String.IsNullOrEmpty(user.FriendName))
            { user.CanBeRemoved = true; }
            else
            { user.IsNotFriend = true; }

            Dispatcher.Invoke(new Action(() => client.SearchedUsers.Add(user)));
        }


        private void LogOutButton_Click(object sender, RoutedEventArgs e)
        {
            callingService.DisposeTcpListener();
            if (callingService.isBusy)
            {
                callingService.RejectCall();
                callingService.BreakCall();
            }
            client.Disconnect();
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
        private volatile bool micOn = true;

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
            callingService.ErrorEvent += TraceLogShow;
            callingService.BusyUserInfoEvent += MsgBoxShow;
            callingService.StartListening();
        }

        private void MsgBoxShow(string msg)
        {
            MessageBox.Show(msg);
        }

        private void TraceLogShow(string msg)
        {
            Trace.WriteLine(msg);
        }

        void NetworkChatPanel_Disposed(object sender, EventArgs e)
        {
            Disconnect();
        }


        private void Connect(IPEndPoint endPoint, int inputDeviceNumber, INetworkChatCodec codec)
        {
            try
            {
                waveIn = new WaveIn
                {
                    BufferMilliseconds = 100,
                    DeviceNumber = inputDeviceNumber,
                    WaveFormat = codec.RecordFormat
                };

                if (callingService.EncryptedCallSender || callingService.EncryptedCallReceiver)
                {
                    waveIn.DataAvailable += WaveIn_DataAvailableEncrypted;
                }
                else
                {
                    waveIn.DataAvailable += WaveIn_DataAvailable;
                }
                waveIn.StartRecording();
            }
            catch (Exception)
            {
                return;
            }

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

            if (callingService.EncryptedCallSender || callingService.EncryptedCallReceiver)
            {
                ThreadPool.QueueUserWorkItem(this.ListenerThreadEncrypted, state);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(this.ListenerThread, state);
            }
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
                HideIncomingCallWindow();
                //Disconnected
            }
        }

        private void ListenerThreadEncrypted(object state)
        {
            ListenerThreadState listenerThreadState = (ListenerThreadState)state;
            IPEndPoint endPoint = listenerThreadState.EndPoint;
            //string key = "dnnnnnnnnnnnnnn1dnnnnnnnnnnnnnn1";//klucz ustalony przez DH jako string lub byte []
            try
            {
                while (isListening)
                {
                    Thread.Sleep(10);

                    if (udpListener.Available > 0)
                    {
                        byte[] b = this.udpListener.Receive(ref endPoint);

                        var decrypted = new cscprotocol.CscAes(callingService.DHKey).DecryptBytes(b,800);
                        byte[] decoded = listenerThreadState.Codec.Decode(decrypted, 0, decrypted.Length);//!! odbieranie dzwieku

                        //Trace.WriteLine("Received: "+Encoding.ASCII.GetString(decrypted));

                        waveProvider.AddSamples(decoded, 0, decoded.Length);
                    }
                }
                isListening = false;
                udpListener.Close();
            }
            catch (Exception)
            {
                isListening = false;
                HideIncomingCallWindow();
                //Disconnected
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
                        waveIn.DataAvailable -= WaveIn_DataAvailableEncrypted;
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
            if (micOn)
            {
                byte[] encoded = codec.Encode(e.Buffer, 0, e.BytesRecorded);
                udpSender.Send(encoded, encoded.Length);
            }
        }

        void WaveIn_DataAvailableEncrypted(object sender, WaveInEventArgs e)
        {
            //$$$ szyfrowanie tutaj
            if (micOn)
            {
                byte[] encoded = codec.Encode(e.Buffer, 0, e.BytesRecorded);//!!! wysylanie dzwieku
                //string key = "dnnnnnnnnnnnnnn1dnnnnnnnnnnnnnn1";//klucz ustalony przez DH jako string lub byte []
                
                var encrypted = new cscprotocol.CscAes(callingService.DHKey).EncryptBytes(encoded);
               
                udpSender.Send(encrypted, encrypted.Length);
                
                //długość próbki dźwięku po zakodowaniu wynosi 800!!!
                //przy odebraniu bajtów pierwsze 800 jest od dźwięku, reszta to śmieci z wypełnienia do pełnego bloku
                //Trace.WriteLine(encrypted.Length);
                //Trace.WriteLine(encoded.Length);

            }
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (waveIn != null && connected)
            {
                if (!micOn)
                {
                    micOn = !micOn;
                    MuteButton.Background = Brushes.Blue;
                    MuteButton.Content = "Wł.";
                    MuteButton.ToolTip = "Mikrofon włączony";
                    BitmapImage image = new BitmapImage(new Uri("/VoipApplication;component/mic_on.png", UriKind.Relative));
                    Mic_image.Source = image;
                }
                else
                {
                    micOn = !micOn;
                    MuteButton.Background = Brushes.Red;
                    MuteButton.Content = "Wył.";
                    MuteButton.ToolTip = "Mikrofon wyłączony";
                    BitmapImage image = new BitmapImage(new Uri("/VoipApplication;component/mic_off.png", UriKind.Relative));
                    Mic_image.Source = image;
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



        SoundPlayer player;

        private void ShowIncomingCallWindow(string username)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                IncomingCallGrid.Visibility = Visibility.Visible;
                RejectButton.Visibility = Visibility.Visible;
                BreakCallButton.Visibility = Visibility.Hidden;
                CallInfoLabel.Content = username;


                micOn = true;
                MuteButton.Content = "on";
                MuteButton.Background = Brushes.Blue;
                BitmapImage image = new BitmapImage(new Uri("/VoipApplication;component/mic_on.png", UriKind.Relative));
                Mic_image.Source = image;

                if (player != null)
                    player.PlayLooping();
            }));
        }

        private void HideIncomingCallWindow()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (player != null)
                    player.Stop();

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


                micOn = true;
                MuteButton.Content = "on";
                MuteButton.Background = Brushes.Blue;
                BitmapImage image = new BitmapImage(new Uri("/VoipApplication;component/mic_on.png", UriKind.Relative));
                Mic_image.Source = image;

            }));
        }

        private void CallingWindowToTalkingWindow(string text)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                AnswerButton.Visibility = Visibility.Hidden;
                BreakCallButton.Visibility = Visibility.Visible;
                BreakCallButton.Content = "Zakończ";

                if (text == null)
                {
                    text = "";
                }
                CallInfoLabel.Content = text;
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
            player.Stop();

            callingService.AnswerCall();
            AnswerButton.Visibility = Visibility.Hidden;
            RejectButton.Content = "Zakończ";
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            player.Stop();

            callingService.RejectCall();
            AnswerButton.Visibility = Visibility.Visible;
            RejectButton.Content = "Odrzuć";
            HideIncomingCallWindow();
            Disconnect();
            micOn = true;
        }

        private void BreakCall_Click(object sender, RoutedEventArgs e)
        {
            callingService.BreakCall(true);
            Disconnect();
            IncomingCallGrid.Visibility = Visibility.Hidden;
            micOn = true;
        }
    }
}
