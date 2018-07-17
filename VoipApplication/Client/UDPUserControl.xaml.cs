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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using NAudio.Wave;
using NAudio.Wave.Compression;
using System.Threading;
using codecs;

namespace VoIP_Client
{
    /// <summary>
    /// Interaction logic for UDPUserControl.xaml
    /// </summary>
    public partial class UDPUserControl : UserControl
    {
        private WaveIn waveIn;
        private UdpClient udpListener;
        private UdpClient udpSender;
        private IWavePlayer waveOut;
        private BufferedWaveProvider waveProvider;
        public INetworkChatCodec codec = new G722ChatCodec();
        private volatile bool connected;


        public UDPUserControl()
        {
            udpListener = new UdpClient(1024);
            InitializeComponent();
            PopulateInputDevicesCombo();
            ThreadPool.QueueUserWorkItem(ListenerThread2, null);
        }

        private void PopulateInputDevicesCombo()
        {
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                var capabilities = WaveIn.GetCapabilities(n);
                this.DeviceComboBox.Items.Add(capabilities.ProductName);
            }
            if (DeviceComboBox.Items.Count > 0)
            {
                DeviceComboBox.SelectedIndex = 0;
            }
        }

        void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] encoded = codec.Encode(e.Buffer, 0, e.BytesRecorded);
            udpSender.Send(encoded, encoded.Length);
        }

        private void Connect(IPEndPoint endPoint, int inputDeviceNumber, INetworkChatCodec codec)
        {
            waveIn = new WaveIn();
            waveIn.BufferMilliseconds = 50;
            waveIn.DeviceNumber = inputDeviceNumber;
            waveIn.WaveFormat = codec.RecordFormat;
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.StartRecording();

            udpSender = new UdpClient();


            //udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //udpListener.Client.Bind(endPoint);

            udpSender.Connect(endPoint);

            waveOut = new WaveOut();
            waveProvider = new BufferedWaveProvider(codec.RecordFormat);
            waveOut.Init(waveProvider);
            waveOut.Play();

            connected = true;
            //ListenerThreadState state = new ListenerThreadState() { Codec = codec, EndPoint = endPoint };
            //można zastąpić to jakimś Task
            //ThreadPool.QueueUserWorkItem(ListenerThread, state);
            
        }

        private void Disconnect()
        {
            connected = false;
        }

        class ListenerThreadState
        {
            public IPEndPoint EndPoint { get; set; }
            public INetworkChatCodec Codec { get; set; }
        }

        private void ListenerThread(object state)
        {
            ListenerThreadState listenerThreadState = (ListenerThreadState)state;
            //IPEndPoint endPoint = listenerThreadState.EndPoint;

            var remoteEP = new IPEndPoint(IPAddress.Any, 1024);

            try
            {
                while (connected)
                {
                    byte[] b = udpListener.Receive(ref remoteEP);
                    byte[] decoded = listenerThreadState.Codec.Decode(b, 0, b.Length);
                    waveProvider.AddSamples(decoded, 0, decoded.Length);
                }
            }
            catch (SocketException)
            {
                // usually not a problem - just means we have disconnected
            }
        }

        private void ListenerThread2(object state)
        {
            var remoteEP = new IPEndPoint(IPAddress.Loopback, 1024);

            try
            {
                while (true)
                {
                    byte[] b = this.udpListener.Receive(ref remoteEP);
                    byte[] decoded = codec.Decode(b, 0, b.Length);
                    waveProvider.AddSamples(decoded, 0, decoded.Length);
                }
            }
            catch (SocketException)
            {
                // usually not a problem - just means we have disconnected
                return;
            }
        }

        private void CallButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!connected)
                {
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(IPTextBox.Text), int.Parse(PortTextBox.Text));
                    int inputDeviceNumber = DeviceComboBox.SelectedIndex;
                    Connect(endPoint, inputDeviceNumber, codec);
                    CallButton.Content = "Disconnect";
                }
                else
                {
                    Disconnect();
                    CallButton.Content = "Connect";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            
        }
    }
}
