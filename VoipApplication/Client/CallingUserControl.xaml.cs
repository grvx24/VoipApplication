using codecs;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VoipApplication.Client
{
    /// <summary>
    /// Interaction logic for CallingUserControl.xaml
    /// </summary>
    public partial class CallingUserControl : UserControl
    {
        
        //Sygnalizacja
        CallingService callingService = new CallingService(new IPEndPoint(CallingService.GetLocalIPAddress(), port));

        static int port = 7999;

        private WaveIn waveIn;
        private UdpClient udpSender;
        private UdpClient udpListener;
        private IWavePlayer waveOut;
        private BufferedWaveProvider waveProvider;
        private INetworkChatCodec codec = new G722ChatCodec();


        private volatile bool connected = false;
        private volatile bool isListening = false;
        private volatile bool isSendingPackets = true;

        public CallingUserControl()
        {
            InitializeComponent();
            PopulateInputDevicesCombo();
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
            MakingCallGrid.Visibility = Visibility.Hidden;


            callingService.ErrorEvent += MsgBoxShow;
        }

        private void MsgBoxShow(string msg)
        {
            MessageBox.Show(msg);
        }

        void NetworkChatPanel_Disposed(object sender, EventArgs e)
        {
            Disconnect();
        }


        private void PopulateInputDevicesCombo()
        {
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                var capabilities = WaveIn.GetCapabilities(n);
                InputDeviceComboBox.Items.Add(capabilities.ProductName);

            }
            if (InputDeviceComboBox.Items.Count > 0)
            {
                InputDeviceComboBox.SelectedIndex = 0;
            }
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
                Trace.WriteLine("rozlaczenie");
            }
            catch (Exception)
            {
                Trace.WriteLine("rozlaczenie - wyjatek");
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

                    Trace.WriteLine("Usuwanie");

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

            if (isListening)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    UdpListenerButton.Background = Brushes.Green;
                }

                ));

            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    UdpListenerButton.Background = Brushes.Red;
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

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!connected)
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(IpTextBox.Text), int.Parse(PortTextBox.Text));
                int inputDeviceNumber = InputDeviceComboBox.SelectedIndex;
                Connect(endPoint, inputDeviceNumber, codec);
                ConnectButton.Content = "Disconnect";
            }
            else
            {
                Disconnect();
                ConnectButton.Content = "Connect";
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


        private void TurnOnUdpSending()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(IpTextBox.Text), int.Parse(PortTextBox.Text));
                int inputDeviceNumber = InputDeviceComboBox.SelectedIndex;
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

            if (isListening)
            {
                UdpListenerButton.Background = Brushes.Green;
            }
            else
            {
                UdpListenerButton.Background = Brushes.Red;
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
                MakingCallGrid.Visibility = Visibility.Visible;

                CallingLabel.Content = text;

            }));

        }

        private void CallingWindowToTalkingWindow(string text)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                MakingCallGrid.Visibility = Visibility.Visible;
                CallingLabel.Content = "W trakcie rozmowy.";

            }));
        }

        private void HideCallingWindow()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                MakingCallGrid.Visibility = Visibility.Hidden;
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

        private void MakeCall_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(IpTextBox.Text), int.Parse(PortTextBox.Text));

                Task.Run(() =>
                {
                    callingService.MakeCall(iPEndPoint);
                });

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void BreakCall_Click(object sender, RoutedEventArgs e)
        {
            callingService.BreakCall(true);
            Disconnect();
            IncomingCallGrid.Visibility = Visibility.Visible;
        }
    }


}
}
