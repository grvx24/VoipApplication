using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using VoIP_Server;
using System.Threading;
using System.Diagnostics;

namespace VoIP_Client
{
    public class CallingService
    {
        private class Commands
        {
            public static string Invite = "INVITE";
            public static string Ack = "ACK";
            public static string Reject = "REJECT";
            public static string Bye = "BYE";
            public static string Busy = "BUSY";
            public static string Cancel = "CANCEL";
        }

        public TcpClient ConnectedClient { get; private set; }

        public delegate void CallAlert(string name);
        public delegate void EndCallAlert();
        public delegate void ErrorAlert(string name);
        public delegate void VoiceSending();
        public delegate void VoiceSending2(IPEndPoint iPEndPoint);
        public delegate void UdpListening(IPEndPoint endPoint);

        public event CallAlert IncomingCallEvent;
        public event CallAlert MakeCallEvent;
        public event CallAlert TalkEvent;
        public event EndCallAlert EndCall;
        public event ErrorAlert ErrorEvent;

        public event VoiceSending2 UDPSenderStart;
        public event VoiceSending UDPSenderStop;
        public event UdpListening UDPListenerStart;



        private TcpClient hostTcpClient;
        private TcpListener tcpListener;


        public volatile bool connected = false;
        public volatile bool isListening = false;
        public volatile bool isBusy = false;

        public IPEndPoint localEndPoint;

        public CallingService(IPEndPoint endPoint)
        {
            localEndPoint = endPoint;
        }


        public async void StartListening()
        {
            tcpListener = new TcpListener(localEndPoint);

            tcpListener.Start();
            isListening = true;

            try
            {
                while (isListening)
                {
                    var client = await tcpListener.AcceptTcpClientAsync();
                    if (ConnectedClient == null)
                    {
                        ConnectedClient = client;
                    }
                    var t = Task.Run(() => HandleConnection(client));
                }
            }
            catch (Exception e)
            {

                if (tcpListener != null)
                {
                    tcpListener.Stop();
                }

                ConnectedClient = null;
                isBusy = false;
                isListening = false;

                ErrorEvent.Invoke(e.Message);

            }
        }

        public void StopListening()
        {
            ConnectedClient = null;
            tcpListener.Stop();
            tcpListener = null;
            isListening = false;
        }


        private void HandleConnection(TcpClient client)
        {
            StreamReader streamReader = new StreamReader(client.GetStream());
            StreamWriter streamWriter = new StreamWriter(client.GetStream())
            {
                AutoFlush = true
            };

            try
            {
                if (isBusy || isCalling)
                {
                    //odrzuć połączenie
                    streamWriter.WriteLine(Commands.Busy);
                }
                else
                {
                    isBusy = true;
                    //akceptuj połączenie
                    while (isBusy)
                    {
                        var message = streamReader.ReadLine();

                        if (message.StartsWith(Commands.Invite))
                        {
                            var msgAfterSplit = message.Split(':');
                            IncomingCallEvent.Invoke(msgAfterSplit[2]);
                            remoteEndPointToSendVoice = new IPEndPoint(IPAddress.Parse(msgAfterSplit[1]),localEndPoint.Port);
                        }

                        if (message.StartsWith(Commands.Cancel))
                        {
                            if (EndCall != null)
                            {
                                EndCall.Invoke();
                            }

                            if (UDPSenderStop != null)
                            {
                                UDPSenderStop.Invoke();
                            }

                            break;
                        }

                        if (message.StartsWith(Commands.Bye))
                        {
                            if (EndCall != null)
                            {
                                EndCall.Invoke();
                            }

                            if (UDPSenderStop != null)
                            {
                                UDPSenderStop.Invoke();
                            }

                            break;
                        }

                    }
                }
                if (ConnectedClient == client)
                {
                    ConnectedClient = null;
                    isBusy = false;
                }

                client.GetStream().Close();
                client.Close();


            }
            catch (Exception e)
            {
                //disconnected
                if (ConnectedClient == client)
                {
                    ConnectedClient = null;
                    isBusy = false;
                }

                if (client != null)
                {
                    client.Close();
                }

                ErrorEvent.Invoke(e.Message);
            }


        }


        //receiver methods

        public void AnswerCall()
        {
            if (ConnectedClient != null)
            {
                StreamWriter streamWriter = new StreamWriter(ConnectedClient.GetStream())
                {
                    AutoFlush = true
                };

                streamWriter.WriteLine(Commands.Ack);
                Trace.WriteLine("Ack");

                if (UDPSenderStart != null)
                {
                    UDPSenderStart.Invoke(remoteEndPointToSendVoice);
                }

                if (UDPListenerStart != null)
                {
                    UDPListenerStart.Invoke(localEndPoint);
                }

            }


        }

        public void RejectCall()
        {
            if (ConnectedClient != null)
            {
                StreamWriter streamWriter = new StreamWriter(ConnectedClient.GetStream())
                {
                    AutoFlush = true
                };

                streamWriter.WriteLine(Commands.Reject);

                ConnectedClient.GetStream().Close();
                ConnectedClient.Close();
                ConnectedClient = null;
                isBusy = false;


            }


        }

        //sender fields

        public volatile bool isCalling = false;
        private IPEndPoint remoteEndPointToSendVoice;
        //sender methods

        public async void MakeCall(IPEndPoint endPoint,string userName,string receiverName)
        {
            Trace.WriteLine("username: " + userName);
            Trace.WriteLine("receiver: " + receiverName);


            if (!isCalling)
            {
                remoteEndPointToSendVoice = endPoint;

                isCalling = true;

                if (MakeCallEvent != null)
                {
                    MakeCallEvent.Invoke("Dzwonię do: "+receiverName);
                }

                try
                {
                    Connect(endPoint);
                    string localIp = (((IPEndPoint)hostTcpClient.Client.LocalEndPoint).Address.ToString());
                    SendText(Commands.Invite + ":" +localIp +":"+ userName);


                    while (isCalling)
                    {
                        var msg = await ReceiveTextAsync();

                        if (msg.StartsWith(Commands.Busy))
                        {
                            ErrorEvent.Invoke("Zajęte");
                        }
                        else if (msg.StartsWith(Commands.Reject))
                        {
                            BreakCall();
                        }
                        else if (msg.StartsWith(Commands.Ack))
                        {
                            if (UDPSenderStart != null)
                            {
                                UDPSenderStart.Invoke(remoteEndPointToSendVoice);
                            }

                            if (UDPListenerStart != null)
                            {
                                UDPListenerStart.Invoke(localEndPoint);
                            }

                            //Akceptacja połączenia - zmiana gui
                            if (TalkEvent != null)
                            {
                                TalkEvent.Invoke(receiverName);
                            }
                        }
                        else
                        {
                            ErrorEvent.Invoke(msg);
                        }
                    }


                }
                catch (Exception e)
                {
                    isCalling = false;

                    if (ErrorEvent != null)
                    {
                        ErrorEvent.Invoke(e.Message);
                    }

                    if (EndCall != null)
                    {
                        EndCall.Invoke();
                    }

                }
            }

        }

        public void BreakCall(bool sendCancelCommand = false)
        {
            if (sendCancelCommand)
            {
                SendText(Commands.Cancel);
                Trace.WriteLine(Commands.Cancel);
            }


            if (hostTcpClient != null)
            {
                if (hostTcpClient.Connected)
                {
                    hostTcpClient.GetStream().Close();
                    hostTcpClient.Close();
                }
            }


            if (EndCall != null)
            {
                EndCall.Invoke();
            }

            isCalling = false;

            
        }

        private void Connect(IPEndPoint endPoint)
        {
            hostTcpClient = new TcpClient();
            var result = hostTcpClient.BeginConnect(endPoint.Address, endPoint.Port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));

            if (!success)
            {
                if (EndCall != null)
                {
                    EndCall.Invoke();
                }
                throw new Exception("Przekroczono czas połączenia.");
            }

            connected = true;

        }




        private void SendText(string text)
        {
            try
            {
                if (hostTcpClient.Connected)
                {
                    StreamWriter streamWriter = new StreamWriter(hostTcpClient.GetStream())
                    {
                        AutoFlush = true
                    };

                    streamWriter.WriteLine(text);
                }
            }
            catch (Exception)
            {
                return;
            }


        }

        private async Task<string> ReceiveTextAsync()
        {
            StreamReader streamReader = new StreamReader(hostTcpClient.GetStream());

            var text = await streamReader.ReadLineAsync();
            return text;

        }

        public static string GetLocalIPAddressString()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
