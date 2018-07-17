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

namespace VoIP_Client
{

    //Cała klasa jest do naprawy :)


    public static class CallCommands
    {
        public static string INVITE = "INVITE:";
        public static string CANCEL = "CANCEL";
        public static string ACK = "ACK";
        public static string BYE = "BYE";
        public static string OK = "OK:";
    }

    public class CallingEventArgs : EventArgs
    {
        public string Nick { get; set; }
        public string Ip { get; set; }
        public bool Encrypted{ get; set; }


    }

    public enum CallState { Waiting, Answer, Reject, }
    public class CallingService
    {
        TcpListener listener;
        public string Ip { get; private set; }
        public int Port { get; private set; }

        TcpClient tcpClient;
        bool isListening = false;
        public bool isCalling = false;
        public bool isBusy = false;
        bool waitingForAnswer = false;
        bool isTalking = false;

        TcpClient currentClient;

        public VoiceChatClient chatClient;

        public CallState callState = CallState.Waiting;

        //events
        public delegate void CallInfo(object sender, CallingEventArgs e, TcpClient client);
        public event CallInfo OnCallingEvent;

        public delegate void Debug(string msg);
        public event Debug InfoEvent;


        public CallingService(IPAddress ip,int listenPort)
        {
            chatClient = new VoiceChatClient(ip,listenPort);

            Port = listenPort;
            listener = new TcpListener(ip, listenPort);
            Ip = ip.ToString();


        }


        public CallingService(int listenPort)
        {
            chatClient = new VoiceChatClient(listenPort);

            Port = listenPort;
            var host=Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if(ip.AddressFamily==AddressFamily.InterNetwork)
                {
                    listener = new TcpListener(ip, listenPort);
                    Ip = ip.ToString();
                }
            }

            
        }

        public void EndConnection()
        {
            try
            {
                if (currentClient != null)
                {
                    StreamWriter writer = new StreamWriter(currentClient.GetStream());
                    writer.WriteLine(CallCommands.BYE);
                    isBusy = false;
                    isCalling = false;
                    currentClient.Dispose();
                }
                InfoEvent.Invoke("...");
            }
            catch (Exception)
            {
                InfoEvent.Invoke("...");
                isBusy = false;
                isCalling = false;
                currentClient.Close();
                currentClient.Dispose();
                return;
            }

        }

        public async Task<bool> Call(UserProfile userProfile, IPEndPoint ipendpoint)
        {
            if(InfoEvent!=null)
            {
                InfoEvent.Invoke("Trwa łączenie...");
            }

            callState = CallState.Waiting;
            tcpClient = new TcpClient();
            var t= await Task.Run(() => MakeCall(userProfile, ipendpoint));

            if (t)
            {
                chatClient.StartRecording(0);
                if (InfoEvent != null)
                    InfoEvent.Invoke("Trwa rozmowa");

                return true;
            }

            else
            {
                if (InfoEvent != null)
                    InfoEvent.Invoke("Odrzucono");

                return false;
            }
                


            
        }

        public void CancelCall()
        {
            chatClient.Disconnect();
            waitingForAnswer = false;
            isCalling = false;
            isBusy = false;
            
        }

        public void StopListening()
        {
            listener.Server.Dispose();
        }

        public void SendCancel()
        {
            if(currentClient!=null)
            {
                using (StreamWriter writer = new StreamWriter(currentClient.GetStream())
                {
                    AutoFlush = true
                })
                {
                    writer.WriteLine(CallCommands.CANCEL);
                }
            }
        }

        private void ProcessCurrentConversation(object obj)
        {

            try
            {
                var client = obj as TcpClient;

                NetworkStream networkStream = client.GetStream();
                StreamReader reader = new StreamReader(networkStream);
                StreamWriter writer = new StreamWriter(networkStream)
                {
                    AutoFlush = true
                };

                while (isTalking)
                {
                    var request = reader.ReadLine();

                    if (request.StartsWith(CallCommands.CANCEL))
                    {
                        isTalking = false;
                        isBusy = false;
                        chatClient.Disconnect();
                        return;
                    }

                    if (request.StartsWith(CallCommands.BYE))
                    {
                        isTalking = false;
                        isBusy = false;
                        chatClient.Disconnect();
                        return;
                    }
                }
            }
            catch (Exception)
            {
                isTalking = false;
                isBusy = false;
                chatClient.Disconnect();
                return;
            }

        }

        private bool MakeCall(UserProfile userProfile, IPEndPoint ipendpoint)
        {

            isBusy = true;
            try
            {
                tcpClient.Connect(ipendpoint);


                NetworkStream networkStream = tcpClient.GetStream();
                StreamReader reader = new StreamReader(networkStream);
                StreamWriter writer = new StreamWriter(networkStream)
                {
                    AutoFlush = true
                };

                if (userProfile != null)
                {
                    var text = CallCommands.INVITE +userProfile.Email+":" +userProfile.UserIp + ":" + userProfile.UserPort.ToString() + ":" + userProfile.Status;
                    
                    //wlaczenie udp
                    chatClient.StartUdpListeningThread();
                    writer.WriteLine(text);
                    isCalling = true;
                }

                //reader.BaseStream.ReadTimeout = 5000;
                while (isCalling)
                {
                    var response = reader.ReadLine();

                    if (response.StartsWith(CallCommands.CANCEL))
                    {
                        chatClient.Disconnect();
                        isCalling = false;
                        isBusy = false;
                        return false;
                    }

                    if(response.StartsWith(CallCommands.OK))
                    {
                        var msg = response.Split(new char[] { ':' });
                        var receivedIP = msg[1];

                        chatClient.Connect(new IPEndPoint(IPAddress.Parse(receivedIP), 2999));
                        writer.WriteLine(CallCommands.ACK);
                        isCalling = false;
                        isTalking = true;
                        ThreadPool.QueueUserWorkItem(ProcessCurrentConversation, tcpClient);
                        return true;
                    }

                }
                tcpClient.Close();
                isBusy = false;
                return false;
                
            }
            catch (Exception)
            {
                InfoEvent.Invoke("Odrzucono");
                chatClient.Disconnect();
                tcpClient.Close();
                isBusy = false;
                return false;
            }

            


        }


        public async void ListenIncomingCalls()
        {
            isListening = true;
            listener.Start();

            while (isListening)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    currentClient = client;
                    var t= Task.Run(() => HandleConnection(currentClient));
                }
                catch (Exception)
                {
                    return;
                }


            }

        }


        private async void HandleConnection(TcpClient client)
        {
            try
            {

                NetworkStream networkStream = client.GetStream();
                StreamReader reader = new StreamReader(networkStream);
                StreamWriter writer = new StreamWriter(networkStream)
                {
                    AutoFlush = true
                };


                if (isBusy)
                {
                    writer.WriteLine(CallCommands.CANCEL);
                    client.Dispose();
                    return;
                }

                string ip = "";

                while (isListening)
                {
                    var request = await reader.ReadLineAsync();

                    if(request!=null)
                    {
                        if (request.StartsWith(CallCommands.CANCEL))
                        {
                            isBusy = false;
                            chatClient.Disconnect();
                        }

                        if (request.StartsWith(CallCommands.BYE))
                        {
                            isBusy = false;
                            chatClient.Disconnect();
                        }

                        if (request.StartsWith(CallCommands.ACK))
                        {
                            chatClient.Connect(new IPEndPoint(IPAddress.Parse(ip), 2999));
                        }


                        if (request.StartsWith(CallCommands.INVITE) && !isBusy)
                        {
                            var message = request.Split(new char[] { ':' });

                            if (!string.IsNullOrEmpty(message[2]))
                            {
                                var name = message[1];
                                ip = message[2];
                                isBusy = true;
                                OnCallingEvent.Invoke(this, new CallingEventArgs() { Nick = name, Ip = ip, Encrypted = false }, client);
                                waitingForAnswer = true;

                                while (waitingForAnswer)
                                {
                                    if(callState == CallState.Answer)
                                    {
                                        chatClient.StartUdpListeningThread();

                                        writer.WriteLine(CallCommands.OK + Ip);//powinien odsylac swoje ip
                                        waitingForAnswer = false;
                                        callState = CallState.Waiting;
                                    }
                                    if(callState == CallState.Reject)
                                    {
                                        writer.WriteLine(CallCommands.CANCEL);

                                        isBusy = false;
                                        waitingForAnswer = false;
                                        callState = CallState.Waiting;
                                        chatClient.Disconnect();
                                        client.GetStream().Close();
                                        client.Dispose();
                                        return;
                                    }
                                }
                            }
                        }

                    }
                    
                }
                
            }

            catch (Exception e)
            {
                
                chatClient.Disconnect();
                EndConnection();

                return;

            }
        }



    }
}
