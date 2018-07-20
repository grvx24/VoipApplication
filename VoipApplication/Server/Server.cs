using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Timers;
using VoipApplication;
using cscprotocol;

namespace VoIP_Server
{

    public delegate void ServerLogDelegate(string message);
    public class MainServer
    {
        public event ServerLogDelegate ServerConsoleWriteEvent;
        TcpListener listener;
        IPAddress iPAddress;
        int port;

        public ObservableCollection<ConnectedUsers> OnlineUsers { get; private set; }
        public ConcurrentStack<ConnectedUsers> UsersToRemove { get; private set; }

        VoiceChatDBEntities serverDB = new VoiceChatDBEntities();

        private bool running = false;
        public bool Running { get => running; }

        //CSC protocol
        CscProtocol cscProtocol = new CscProtocol();

        public delegate void RemoveOfflineUserDelegate(ConnectedUsers user);
        public event RemoveOfflineUserDelegate RemoveOfflineUserEvent;

        public MainServer()
        {
            OnlineUsers = new ObservableCollection<ConnectedUsers>();
            UsersToRemove = new ConcurrentStack<ConnectedUsers>();
        }

        public void CreateServer(IPAddress iP, int port)
        {
            this.iPAddress = iP;
            this.port = port;
            listener = new TcpListener(iPAddress, port);
        }

        public IPAddress Address
        {
            get { return iPAddress; }
            set
            {
                if (!Running) iPAddress = value;
            }
        }

        public int Port
        {
            get { return port; }
            set
            {
                if (!Running)
                    port = value;
            }
        }

        public async void RunAsync()
        {
            running = true;
            listener.Start();
            ServerConsoleWriteEvent.Invoke("Serwer wystartował - Ip: " + iPAddress.ToString() + " port: " + port);

            while (running)
            {
                try
                {
                    Random random = new Random();
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    var ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                    var connectedClient = new ConnectedUsers() { Email = "Niezalogowany użytkownik" + Guid.NewGuid().GetHashCode(), Id = -1, Ip = ip.ToString(), Status = 0, Client = client };
                    OnlineUsers.Add(connectedClient);

                    ServerConsoleWriteEvent.Invoke(client.Client.RemoteEndPoint.ToString());

                    foreach (var item in OnlineUsers)
                    {
                        item.NewOnlineUsers.Push(connectedClient);
                    }

                    ThreadPool.QueueUserWorkItem(Process, connectedClient);

                    //await Process(connectedClient);

                }
                catch (Exception e)
                {
                    ServerConsoleWriteEvent.Invoke(e.Message);
                }
            }
        }


        private void SendFriendsList(TcpClient client, string email)
        {
            var user = serverDB.Users.FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                throw new NullReferenceException("UserId is null!");
            }

            var friendsQueryResult = from f in serverDB.FriendsList
                                     join u in serverDB.Users on f.FriendId equals u.UserId
                                     where f.UserId == user.UserId
                                     select new { f.UserId, f.FriendName, u.Email, };


            foreach (var friend in friendsQueryResult)
            {
                CscUserMainData friendsUserData = new CscUserMainData() { FriendName = friend.FriendName, Email = friend.Email, Id = friend.UserId, Status = 0, Ip = "none" };
                var message = cscProtocol.CreateFriendUserDataMessage(friendsUserData);
                client.GetStream().Write(message, 0, message.Length);
            }
        }

        private void SendOnlineUsersList(TcpClient client)
        {
            var online = OnlineUsers;

            foreach (var user in online)
            {
                CscUserMainData userData = new CscUserMainData() { Email = user.Email, Id = user.Id, Status = 1, Ip = user.Ip, FriendName = "" };
                var message = cscProtocol.CreateOnlineUserDataMessage(userData);
                client.GetStream().Write(message, 0, message.Length);
            }
        }

        private void SendOnlineUser(TcpClient client, ConnectedUsers newUser)
        {
            CscUserMainData userData = new CscUserMainData() { Email = newUser.Email, Id = newUser.Id, Status = 1, Ip = newUser.Ip, FriendName = "" };
            var message = cscProtocol.CreateOnlineUserDataMessage(userData);
            client.GetStream().Write(message, 0, message.Length);
        }

        private void SendOfflineUser(TcpClient client, ConnectedUsers newUser)
        {
            CscUserMainData userData = new CscUserMainData() { Email = newUser.Email, Id = newUser.Id, Status = 0, Ip = newUser.Ip, FriendName = "" };
            var message = cscProtocol.CreateOfflineUserDataMessage(userData);
            client.GetStream().Write(message, 0, message.Length);
        }

        private void SendProfileInfo(ConnectedUsers user)
        {
            CscUserMainData userData = new CscUserMainData() { Email = user.Email, Id = user.Id, Status = 1, Ip = user.Ip, FriendName = "" };
            var message = cscProtocol.CreateUserProfileMessage(userData);
            user.Client.GetStream().Write(message, 0, message.Length);
        }

        private void ExecuteCSCCommand(ConnectedUsers connectedUser, byte cmdNumber, byte[] receivedMessage)
        {
            switch (cmdNumber)
            {
                //Logowanie
                case 0:
                    {
                        var userData = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserData;
                        //tu bedzie trza jeszcze odszyfrowac itd, póki co jest jawnie
                        ServerConsoleWriteEvent.Invoke("Próba zalogowania: " + userData.Email + " : " + userData.Password);

                        var onlineUser = OnlineUsers.Where(e => e.Email == userData.Email).FirstOrDefault();

                        if (onlineUser != null)
                        {
                            var error = cscProtocol.CreateErrorMessage("Podany użytkownik jest już zalogowany!");
                            connectedUser.Client.GetStream().Write(error, 0, error.Length);
                            return;
                        }

                        var queryResult = serverDB.Users.FirstOrDefault(u => u.Email == userData.Email);
                        var passwordFromDB = queryResult.Password;

                        var hashWithSalt = (CscSHA512Generator.get_SHA512_hash_as_string(passwordFromDB + connectedUser.Salt));

                        if (!(hashWithSalt == userData.Password))
                        {
                            queryResult = null;
                        }

                        if (queryResult != null)
                        {

                            ConnectedUsers user = OnlineUsers.Where(t => t.Client == connectedUser.Client).FirstOrDefault();
                            user.Email = queryResult.Email;
                            user.Id = queryResult.UserId;

                            var buffer = cscProtocol.CreateConfirmMessage("Witaj na serwerze :)");
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);

                            ServerConsoleWriteEvent.Invoke("Udane logowanie: " + userData.Email + " : " + userData.Password);
                        }
                        else
                        {
                            ServerConsoleWriteEvent.Invoke("Nieudane logowanie: " + userData.Email + " : " + userData.Password);
                            var buffer = cscProtocol.CreateErrorMessage("Błędne dane logowania");
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                        break;
                    }

                case 1:

                    ConnectedUsers currUser = OnlineUsers.Where(t => t.Client == connectedUser.Client).FirstOrDefault();
                    //Dane użytkownika
                    SendProfileInfo(currUser);

                    //Friends
                    SendFriendsList(connectedUser.Client, currUser.Email);

                    //All online users
                    SendOnlineUsersList(connectedUser.Client);

                    break;

                //Rejestracja
                case 2:
                    var userToRegister = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserData;

                    var userWithThisMail = serverDB.Users.Where(e => e.Email == userToRegister.Email).FirstOrDefault();

                    if (userWithThisMail != null)
                    {
                        var response = cscProtocol.CreateErrorMessage("Podany adres e-mail już istnieje.");
                        connectedUser.Client.GetStream().Write(response, 0, response.Length);
                    }
                    else
                    {
                        CreateAccount(userToRegister.Email, userToRegister.Password);
                        var response = cscProtocol.CreateConfirmMessage("Rejestracja udana.");
                        connectedUser.Client.GetStream().Write(response, 0, response.Length);
                    }
                    break;

                case 3://odświeżanie listy online
                    if (!connectedUser.NewOnlineUsers.IsEmpty && connectedUser.Id != -1)
                    {
                        foreach (var item in connectedUser.NewOnlineUsers)
                        {
                            SendOnlineUser(connectedUser.Client, item);
                        }

                        connectedUser.NewOnlineUsers.Clear();
                    }

                    if (!connectedUser.UsersToRemove.IsEmpty && connectedUser.Id != -1)
                    {
                        foreach (var item in connectedUser.UsersToRemove)
                        {
                            SendOfflineUser(connectedUser.Client, item);
                        }
                        connectedUser.NewOnlineUsers.Clear();
                    }


                    break;

                case 4:
                    break;

                case 6://zmiana adresu email dla polaczonego usera
                    {
                        var userData = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserData;
                        ServerConsoleWriteEvent.Invoke("Próba zmiany adresu e-mail dla UserID" + connectedUser.Id + " z " + connectedUser.Email + " na " + userData.Email);

                        var queryResult = serverDB.Users.FirstOrDefault(u => u.UserId == connectedUser.Id);
                        var passwordFromDB = queryResult.Password;
                        var hashWithSalt = (CscSHA512Generator.get_SHA512_hash_as_string(passwordFromDB + connectedUser.Salt));
                        bool goodPasswordAndEmailUnused = true;
                        if (!(hashWithSalt == userData.Password))
                        {
                            goodPasswordAndEmailUnused = false;
                        }

                        //sprawdzamy czy taki email jest nieuzywany

                        var queryEmailResult = serverDB.Users.FirstOrDefault(u => u.Email == userData.Email);
                        if (!(queryEmailResult == null))
                        {
                            goodPasswordAndEmailUnused = false;
                        }

                        if (goodPasswordAndEmailUnused == true)
                        {
                            queryResult.Email = userData.Email;
                            serverDB.SaveChanges();

                            var buffer = cscProtocol.CreateConfirmMessage("Adres e-mail został zmieniony.");
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);

                            ServerConsoleWriteEvent.Invoke("Udana zmiana adresu e-mail dla UserID" + connectedUser.Id + " z " + connectedUser.Email + " na " + userData.Email);
                        }
                        else
                        {
                            ServerConsoleWriteEvent.Invoke("Nieudana próba zmiany adresu e-mail dla UserID" + connectedUser.Id + " z " + connectedUser.Email + " na " + userData.Email);
                            var buffer = cscProtocol.CreateErrorMessage("Taki adres e-mail już istnieje w bazie!");
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                        break;
                    }



                default:
                    break;
            }
        }

        private string SendSalt(ConnectedUsers user)
        {
            var salt = CscSHA512Generator.Get_salt();



            var stream = user.Client.GetStream();

            var msg = cscProtocol.CreateSaltMessage(salt);

            stream.Write(msg, 0, msg.Length);

            return salt;

        }


        private async void Process(object obj)
        {
            var user = obj as ConnectedUsers;

            try
            {
                NetworkStream networkStream = user.Client.GetStream();
                //StreamReader reader = new StreamReader(networkStream);
                //StreamWriter writer = new StreamWriter(networkStream)
                //{
                //    AutoFlush = true
                //};

                //wysyłanie soli
                user.Salt = SendSalt(user);


                while (running)
                {


                    byte[] request = new byte[3];
                    var l1 = await networkStream.ReadAsync(request, 0, request.Length);

                    var msgLen = BitConverter.ToUInt16(request.Skip(1).ToArray(), 0);

                    byte[] buffer = new byte[msgLen];
                    var l2 = await networkStream.ReadAsync(buffer, 0, buffer.Length);

                    if (buffer != null)
                    {
                        ExecuteCSCCommand(user, request[0], buffer);
                    }
                }
                RemoveOfflineUserEvent(user);
                //UsersToRemove.Push(user);

                user.Client.Close();

            }
            catch (Exception e)
            {
                ServerConsoleWriteEvent.Invoke("Użytkownik: " + user.Email + " Komunikat: " + e.Message);

                RemoveOfflineUserEvent(user);
                //UsersToRemove.Push(user);

                if (user.Client.Connected)
                {
                    user.Client.Dispose();
                }


            }
        }


        public void StopRunning()
        {
            listener.Stop();
            listener = null;
            running = false;
        }

        public ObservableCollection<ConnectedUsers> GetOnlineClients()
        {
            return OnlineUsers;
        }



        private void CreateAccount(string email, string password)
        {
            Users user = new Users();
            user.Email = email;
            user.Password = password;
            user.RegistrationDate = DateTime.Now;


            serverDB.Users.Add(user);
            serverDB.SaveChanges();

        }




        //public ObservableCollection<ConnectedUsers> GetOnlineClients()
        //{
        //    ObservableCollection<ConnectedUsers> list = new ObservableCollection<ConnectedUsers>();
        //    foreach (var item in OnlineClients)
        //    {
        //        list.Add(item.Value);
        //    }

        //    return list;
        //}

    }
}
