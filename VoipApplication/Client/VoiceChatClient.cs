using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using codecs;

namespace VoIP_Client
{
    public class VoiceChatClient
    {
        #region NetworkProperties
        private UdpClient udpClient;
        private UdpClient udpListener;
        private IPEndPoint localEndPoint;
        private IPEndPoint remoteEndPoint;
        int listenPort;
        public INetworkChatCodec codec = new G722ChatCodec();

        public delegate void DisposeAll();
        public event DisposeAll DisposeAllEvent;

        //public delegate void DebugConsole(string msg);
        //public event DebugConsole DebugConsoleEvent;

        public bool IsUdpConnected { get; private set; }
        public bool IsListening { get; private set; }

        #endregion

        #region AudioProperties

        private WaveIn sourceSound;
        private WaveOut receivedSound;
        private BufferedWaveProvider waveProvider;

        public int InputAudioDevice { get; set; }
        public int OutputAudioDevice { get; set; }

        #endregion

        CancellationTokenSource udpListeningToken = new CancellationTokenSource();


        #region Constructor
        public VoiceChatClient(int port)
        {
            try
            {
                this.listenPort = port;
                udpClient = new UdpClient(AddressFamily.InterNetwork);
                udpListener = new UdpClient(port);
                //localEndPoint = GetHostEndPoint();   

            }
            catch (SocketException e)
            {
                throw e;
            }
            
        }

        public VoiceChatClient(IPAddress ip,int port)
        {
            try
            {
                this.listenPort = port;
                udpClient = new UdpClient(ip.AddressFamily);
                udpListener = new UdpClient(port);
                //localEndPoint = GetHostEndPoint();   

            }
            catch (SocketException e)
            {
                throw e;
            }

        }

        #endregion


        private void Init()
        {
            waveProvider = new BufferedWaveProvider(new WaveFormat(8000, 16, WaveIn.GetCapabilities(OutputAudioDevice).Channels));
            receivedSound = new WaveOut();
            receivedSound.Init(waveProvider);
            receivedSound.Play();
        }

        public void Connect(IPEndPoint iPEndPoint)
        {
            udpClient = new UdpClient(AddressFamily.InterNetwork);
            udpClient.Connect(iPEndPoint);
            IsUdpConnected = true;
        }
        
        public void Disconnect()
        {
            if(IsUdpConnected)
            {
                IsUdpConnected = false;
                sourceSound.DataAvailable -= WaveIn_DataAvailable;
                sourceSound.StopRecording();
                receivedSound.Stop();


                udpListener.Dispose();
                udpClient.Dispose();
                sourceSound.Dispose();
                receivedSound.Dispose();

                codec.Dispose();

            }

            IsListening = false;
        }

        public void StartRecording(int inputDeviceNumber)
        {
            sourceSound = new WaveIn
            {
                BufferMilliseconds = 100,
                DeviceNumber = inputDeviceNumber,
                WaveFormat = codec.RecordFormat
            };
            sourceSound.DataAvailable += WaveIn_DataAvailable;
            sourceSound.StartRecording();
        }


        #region FunctionsOnOtherThreads
        private void UdpReceiveThread()
        {
            IsListening = true;

            var remoteEP = new IPEndPoint(IPAddress.Any, 2999);

            try
            {
                //udpListener = new UdpClient(listenPort);
                while (IsListening)
                {
                    var b = udpListener.Receive(ref remoteEP);
                    byte[] decoded = codec.Decode(b, 0, b.Length);
                    waveProvider.AddSamples(decoded, 0, decoded.Length);
                }
            }

            catch (SocketException)
            {
                return;
            }
            catch (Exception)
            {
                return;
            }
        }
        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                byte[] encoded = codec.Encode(e.Buffer, 0, e.BytesRecorded);
                udpClient.Send(encoded, encoded.Length);
            }
            catch (Exception)
            {
                return;
            }


        }

        public void StartUdpListeningThread()
        {

            Init();
            Task.Run(()=>UdpReceiveThread(), udpListeningToken.Token);
        }


        #endregion

    }
}
