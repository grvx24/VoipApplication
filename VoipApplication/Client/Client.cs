using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using VoIP_Server;
using System.Collections.ObjectModel;
using cscprotocol;
using System.Diagnostics;
using CPOL;

namespace VoIP_Client
{
    public enum LastBookmarkEnum { search, online, friendslist}
    //Communication with Server
    public class Client
    {
        CscProtocol protocol = new CscProtocol();
        //public CscUserData UserProfile { get; set; }
        public CscUserMainData UserProfile { get; set; }
        public TcpClient client;
        IPAddress serverIpAddress;
        int port;
        bool isConnected = false;
        bool isListening = false;
        public bool initialized = false;
        public string salt { get; set; }
        public string LastConfirmMessage { get; set; }
        public string LastErrorMessage { get; set; }
        public string LastSearchText { get; set; }
        public LastBookmarkEnum LastBookmark { get; set; }
        public DiffieHellman DH { get; set; }
        public CscAes AES { get; set; }


        public IPAddress LocalIP { get; set; }

        //public delegate void ProfileDelegate(CscUserMainData profile);
        //public event ProfileDelegate SetProfileText;

        public delegate void ChangeItem(CscUserMainData data);
        public event ChangeItem RemoveItemEvent;
        public event ChangeItem AddItemEvent;
        public event ChangeItem AddFriendEvent;
        public event ChangeItem RemoveFriendEvent;
        public event ChangeItem AddSearchEvent;

        public Client()
        {
            client = new TcpClient();
            UserProfile = new CscUserMainData();//n
            LastConfirmMessage = string.Empty;
            LastErrorMessage = string.Empty;
        }


        public delegate void ConnectionLost();
        public event ConnectionLost ConnectionLostEvent;

        public IPAddress IPAddress { get => serverIpAddress; set => serverIpAddress = value; }
        public int Port { get => port; set => port = value; }
        public bool IsConnected { get => isConnected; set => isConnected = value; }

        public ObservableCollection<CscUserMainData> FriendsList = new ObservableCollection<CscUserMainData>();
        public ObservableCollection<CscUserMainData> onlineUsers = new ObservableCollection<CscUserMainData>();
        public ObservableCollection<CscUserMainData> SearchedUsers = new ObservableCollection<CscUserMainData>();

        public ObservableCollection<CscUserMainData> GetFriendsList()
        {//n !!! tu mozna zrobic przypisywanie statusu usera online ofline przed przekazaniem tej listy do GUI
            if (FriendsList != null)
            {
                return FriendsList;
            }
            else
            {
                throw new NullReferenceException("FriendsList is null");
            }
        }

        public ObservableCollection<CscUserMainData> GetOnlineUsers()
        {
            if (onlineUsers != null)
            {
                return onlineUsers;
            }
            else
            {
                throw new NullReferenceException("OnlineUsers list is null");
            }
        }
        public ObservableCollection<CscUserMainData> GetSearchUsers()
        {
            if (SearchedUsers != null)
            {
                return SearchedUsers;
            }
            else
            {
                throw new NullReferenceException("OnlineUsers list is null");
            }
        }

        //wysyła prośbę o aktualizację użytkowników
        public void SendRefreshRequest()
        {
            var msg = protocol.CreateRefreshRequest();
            client.GetStream().Write(msg, 0, msg.Length);
        }

        public void GetBasicInfo()
        {
            var msg = protocol.CreateStartingInfoRequest();
            client.GetStream().Write(msg, 0, msg.Length);
        }

        public void SendChangeEmailRequestEncrypted(CscUserData userData)
        {
            var msg = protocol.CreateChangeEmailRequestEncrypted(userData, DH.Key);
            client.GetStream().Write(msg, 0, msg.Length);
        }

        public void SendChangePasswordRequestEncrypted(CscPasswordData userData)
        {
            var msg = protocol.CreateChangePasswordRequestEncrypted(userData, DH.Key);
            client.GetStream().Write(msg, 0, msg.Length);
        }

        public void SendSearchUserRequest(string text)
        {
            var msg = protocol.CreateSearchUserRequestEncrypted(text, DH.Key);
            client.GetStream().Write(msg, 0, msg.Length);
        }

        public void SendAddUserToFriendsListDataRequest(CscChangeFriendData friendData)
        {
            var msg = protocol.CreateAddUserToFriendsListRequestEncrypted(friendData, DH.Key);
            client.GetStream().Write(msg, 0, msg.Length);
        }

        public void SendRemoveUserFromFriendsListDataRequest(CscChangeFriendData friendData)
        {
            var msg = protocol.CreateRemoveUserFromFriendsListRequestEncrypted(friendData, DH.Key);
            client.GetStream().Write(msg, 0, msg.Length);
        }


        public void Connect(IPAddress ip, int port)
        {
            IPAddress = ip;
            this.Port = port;

            client.Connect(ip, port);

            if (client.Connected)
            {
                var c = client.Client;
                LocalIP = ((IPEndPoint)c.LocalEndPoint).Address;
                isConnected = true;
            }
        }

        public void StartListening()
        {
            isListening = true;
            Task.Run(() => Listening());
            Trace.WriteLine("TcpListening");
        }

        private void ExecuteCSCCommand(byte commandNumber, byte[] receivedMessage)
        {
            switch (commandNumber)
            {
                case 3://odebranie kontaktow pasujacych do frazy wpisanej w pole search
                    {
                        var searchUser = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserMainData;
                        if (searchUser.Email != UserProfile.Email)
                        {
                            if (AddSearchEvent != null)
                                AddSearchEvent.Invoke(searchUser);
                        }
                        break;
                    }

                case 4://n !!! odebranie zunifikowanego info o zmianie stanu kontaktu
                    {
                        var changedUser = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserMainData;
                        var online = onlineUsers.FirstOrDefault(u => u.Id == changedUser.Id);
                        var friend = FriendsList.FirstOrDefault(u => u.Id == changedUser.Id);
                        try
                        {
                            if (RemoveItemEvent != null)
                                RemoveItemEvent.Invoke(online);
                        }
                        catch (Exception) { }

                        try
                        {
                            if (RemoveFriendEvent != null)
                                RemoveFriendEvent.Invoke(friend);
                        }
                        catch (Exception) { }


                        try
                        {
                            if (changedUser.FriendName != string.Empty)
                            {
                                if (AddFriendEvent != null)
                                    AddFriendEvent.Invoke(changedUser);
                            }
                        }
                        catch (Exception) { }

                        try
                        {
                            if (changedUser.Status > 0 && changedUser.Email != UserProfile.Email)
                            {
                                if (AddItemEvent != null)
                                { AddItemEvent.Invoke(changedUser); }
                            }
                        }
                        catch (Exception) { }

                        break;
                    }

                //case 6:
                //    {
                //        var profile = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserMainData;
                //        UserProfile = profile;
                //        SetProfileText.Invoke(profile);
                //        break;
                //    }
                /*
            case 7:
                var onlineUserToRemove = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserMainData;
                var toRemove = onlineUsers.Where(i => i.Id == onlineUserToRemove.Id).FirstOrDefault();

                if (toRemove != null)
                {
                    Trace.WriteLine("Usuwanie: " + toRemove.Email);

                    if (RemoveItemEvent != null)
                        RemoveItemEvent.Invoke(toRemove);
                }
                break;


            case 8:
                {
                    var onlineUser = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserMainData;
                    if (onlineUser.Email != UserProfile.Email)

                        if (AddItemEvent != null)
                            AddItemEvent.Invoke(onlineUser);
                    break;
                }

            case 9:
                {
                    var friendData = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserMainData;
                    if (AddFriendEvent != null)
                        AddFriendEvent.Invoke(friendData);
                    //friendsUserDatas.Add(friendData);
                    break;
                }

            case 10:
                {
                    var friendData = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserMainData;
                    if (RemoveFriendEvent != null)
                        RemoveFriendEvent.Invoke(friendData);
                    //onlineUsers.Remove(userToRemove);
                    break;
                }*/

                case 12:
                    {
                        //var message = CscProtocol.ParseConfirmMessageAndDecrypt(receivedMessage);//n czy to tu musi byc parsowane? chyba nie

                        if (Encoding.ASCII.GetString(receivedMessage, 0, receivedMessage.Count()) == "ONLINE_USERS_OK")
                        {
                            Environment.Exit(0);
                        }
                        else
                        {
                            //LastConfirmMessage = message;
                            LastConfirmMessage = Encoding.ASCII.GetString(receivedMessage, 0, receivedMessage.Count());
                        }
                        break;
                    }

                case 13:
                    {
                        //var message = CscProtocol.ParseConfirmMessage(receivedMessage);
                        LastErrorMessage = Encoding.ASCII.GetString(receivedMessage, 0, receivedMessage.Count());
                        break;
                    }

                default:
                    break;
            }
        }

        private void Listening()
        {
            try
            {
                while (isListening)
                {
                    if (!client.Connected)
                    {
                        ConnectionLostEvent.Invoke();
                    }

                    byte[] cmdAndLength = new byte[3];
                    //protokoly
                    var l1 = client.GetStream().Read(cmdAndLength, 0, cmdAndLength.Length);
                    var msgLen = BitConverter.ToUInt16(cmdAndLength.Skip(1).ToArray(), 0);

                    byte[] buffer = new byte[msgLen];
                    var l2 = client.GetStream().Read(buffer, 0, buffer.Length);

                    if (buffer != null)
                    {
                    var decrypted = AES.DecrypBytesFromBytes(buffer);
                        ExecuteCSCCommand(cmdAndLength[0], decrypted);
                    }
                }
            }
            catch (Exception)
            {
                Disconnect();
                ConnectionLostEvent.Invoke();
            }
        }

        public void SendBytes(byte[] message)
        {
            try
            { client.GetStream().Write(message, 0, message.Length); }
            catch (Exception e)
            { throw e; }
        }

        public byte[] ReceiveBytes()
        {
            byte[] request = new byte[3];
            var l1 = client.GetStream().Read(request, 0, request.Length);

            var msgLen = BitConverter.ToUInt16(request.Skip(1).ToArray(), 0);

            byte[] buffer = new byte[msgLen];
            var l2 = client.GetStream().Read(buffer, 0, buffer.Length);

            byte[] full = new byte[3 + msgLen];
            System.Buffer.BlockCopy(request, 0, full, 0, request.Length);
            System.Buffer.BlockCopy(buffer, 0, full, request.Length, buffer.Length);
            return full;
        }

        //public void SendText(string msg)//n !!! to chyba nie jest potrzebne
        //{
        //    StreamWriter writer = new StreamWriter(client.GetStream());
        //    writer.AutoFlush = true;
        //    writer.WriteLine(msg);
        //}

        //public string ReceiveText()//n !!! to chyba nie jest potrzebne
        //{
        //    StreamReader reader = new StreamReader(client.GetStream());
        //    var result = reader.ReadLine();
        //    return result;
        //}

        //public async Task<string> ReceiveTextAsync()//n !!! to chyba nie jest potrzebne
        //{
        //    StreamReader reader = new StreamReader(client.GetStream());
        //    var result = await reader.ReadLineAsync();
        //    return result;
        //}

        public void Disconnect()
        {
            if (client.Connected)
                client.Close();
        }
    }
}
