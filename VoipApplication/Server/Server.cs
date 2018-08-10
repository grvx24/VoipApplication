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
using CPOL;

namespace VoIP_Server
{

    public delegate void ServerLogDelegate(string message);
    public class MainServer
    {
        public event ServerLogDelegate ServerConsoleWriteEvent;
        public event ServerLogDelegate ServerMessageBoxShowEvent;
        TcpListener listener;
        IPAddress iPAddress;
        int port;

        public ObservableCollection<ConnectedUsers> OnlineUsers { get; private set; }

        private bool running = false;
        public bool Running { get => running; }

        //CSC protocol
        CscProtocol cscProtocol = new CscProtocol();

        public delegate void RemoveOfflineUserDelegate(ConnectedUsers user);
        public event RemoveOfflineUserDelegate RemoveOfflineUserEvent;

        public MainServer()
        {
            OnlineUsers = new ObservableCollection<ConnectedUsers>();
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
            try
            {
                running = true;
                listener.Start();
                ServerConsoleWriteEvent.Invoke("Serwer wystartował - Ip: " + iPAddress.ToString() + " port: " + port);
            }
            catch (Exception e)
            {
                running = false;
                ServerConsoleWriteEvent.Invoke("Nie można uruchomić serwera: " + e.Message);
                ServerMessageBoxShowEvent.Invoke("Nie można uruchomić serwera: " + e.Message);
                return;
            }

            while (running)
            {
                try
                {
                    Random random = new Random();
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    var ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                    var connectedClient = new ConnectedUsers()
                    {
                        Email = "Niezalogowany użytkownik" + Guid.NewGuid().GetHashCode(),
                        Id = -1,
                        Ip = ip.ToString(),
                        Status = 0,
                        Client = client
                    };
                    OnlineUsers.Add(connectedClient);
                    ServerConsoleWriteEvent.Invoke(client.Client.RemoteEndPoint.ToString());
                    ThreadPool.QueueUserWorkItem(Process, connectedClient);
                    //await Process(connectedClient);
                }
                catch (Exception e)
                {
                    ServerConsoleWriteEvent.Invoke(e.Message);
                }
            }
        }

        private IQueryable<CscUserMainData> FindFriendsForUser(string email)
        {
            VoiceChatDBEntities serverDB = new VoiceChatDBEntities();
            var user = serverDB.Users.FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                throw new NullReferenceException("UserId is null!");
            }

            var friendsQueryResult = from f in serverDB.FriendsList
                                     join u in serverDB.Users on f.FriendId equals u.UserId//powiazanie ID friendow z ich nazwami
                                     where f.UserId == user.UserId// powiazanie ID glownego usera z obu tabel
                                     select new CscUserMainData { Id = f.FriendId, FriendName = f.FriendName, Email = u.Email, Status = 0, Ip = "none" };
            return friendsQueryResult;
        }

        private void SendFriendsListEncrypted(TcpClient client, string email, byte[] key)
        {
            var friendsQueryResult = FindFriendsForUser(email);
            foreach (var friend in friendsQueryResult)
            {
                SendNewFriendUserEncrypted(client, new CscChangeFriendData { Id = friend.Id, FriendName = friend.FriendName }, key);
            }
        }

        private void SendSearchListEncrypted(TcpClient client, int connectedUserID, string text, byte[] key)
        {
            VoiceChatDBEntities serverDB = new VoiceChatDBEntities();
            var queryResult = serverDB.Users.Where(u => u.Email.Contains(text));
            var friendsQueryResult = FindFriendsForUser(serverDB.Users.FirstOrDefault(u => u.UserId == connectedUserID).Email);

            foreach (var user in queryResult)
            {
                CscUserMainData searchedUserData = new CscUserMainData()
                {
                    FriendName = friendsQueryResult.Any(u => u.Id == user.UserId) ? friendsQueryResult.FirstOrDefault(u => u.Id == user.UserId).FriendName : string.Empty,
                    Email = user.Email,
                    Id = user.UserId,
                    Status = OnlineUsers.Any(u => u.Id == user.UserId) ? 1 : 0,
                    Ip = OnlineUsers.Any(u => u.Id == user.UserId) ? OnlineUsers.FirstOrDefault(u => u.Id == user.UserId).Ip : "none"
                };

                var message = cscProtocol.CreateSearchUserDataResponseEncrypted(searchedUserData, key);
                client.GetStream().Write(message, 0, message.Length);
            }
        }

        private void SendOnlineUsersListEncrypted(TcpClient client, int connectedUserID, byte[] key)
        {
            VoiceChatDBEntities serverDB = new VoiceChatDBEntities();
            var online = OnlineUsers;
            var friendsQueryResult = FindFriendsForUser(serverDB.Users.FirstOrDefault(u => u.UserId == connectedUserID).Email);

            foreach (var user in online)
            {
                CscUserMainData userData = new CscUserMainData()
                {
                    Email = user.Email,
                    Id = user.Id,
                    Status = 1,
                    Ip = user.Ip,
                    FriendName = friendsQueryResult.Any(u => u.Id == user.Id) ? friendsQueryResult.FirstOrDefault(u => u.Id == user.Id).FriendName : string.Empty,
                };
                var message = cscProtocol.CreateUnifiedUserDataMessageEncrypted(userData, key);
                client.GetStream().Write(message, 0, message.Length);
            }
        }

        private void SendOnlineUserEncrypted(TcpClient client, ConnectedUsers newUser, string receiverEmail, byte[] key)
        {
            VoiceChatDBEntities serverDB = new VoiceChatDBEntities();
            var friendsQueryResult = FindFriendsForUser(receiverEmail);
            CscUserMainData userData = new CscUserMainData()
            {
                Email = newUser.Email,
                Id = newUser.Id,
                Status = 1,
                Ip = newUser.Ip,
                FriendName = friendsQueryResult.Any(u => u.Id == newUser.Id) ? friendsQueryResult.FirstOrDefault(u => u.Id == newUser.Id).FriendName : string.Empty,
            };
            var message = cscProtocol.CreateUnifiedUserDataMessageEncrypted(userData, key);
            client.GetStream().Write(message, 0, message.Length);
        }

        private void SendOfflineUserEncrypted(TcpClient client, ConnectedUsers newUser, string receiverEmail, byte[] key)
        {
            VoiceChatDBEntities serverDB = new VoiceChatDBEntities();
            var friendsQueryResult = FindFriendsForUser(receiverEmail);
            CscUserMainData userData = new CscUserMainData()
            {
                Email = newUser.Email,
                Id = newUser.Id,
                Status = 0,
                Ip = newUser.Ip,
                FriendName = friendsQueryResult.Any(u => u.Id == newUser.Id) ? friendsQueryResult.FirstOrDefault(u => u.Id == newUser.Id).FriendName : string.Empty,
            };
            var message = cscProtocol.CreateUnifiedUserDataMessageEncrypted(userData, key);
            client.GetStream().Write(message, 0, message.Length);
        }

        public void SendNewFriendUserEncrypted(TcpClient client, CscChangeFriendData friendData, byte[] key)
        {
            VoiceChatDBEntities serverDB = new VoiceChatDBEntities();
            var friend = serverDB.Users.FirstOrDefault(u => u.UserId == friendData.Id);
            CscUserMainData userData = new CscUserMainData()
            {
                Email = friend.Email,
                Id = friend.UserId,
                Status = OnlineUsers.Any(u => u.Id == friend.UserId) ? 1 : 0,
                Ip = OnlineUsers.Any(u => u.Id == friend.UserId) ? OnlineUsers.FirstOrDefault(u => u.Id == friend.UserId).Ip : "none",
                FriendName = friendData.FriendName
            };
            var message = cscProtocol.CreateUnifiedUserDataMessageEncrypted(userData, key);
            client.GetStream().Write(message, 0, message.Length);
        }

        public void SendNoFriendAnymoreUserEncrypted(TcpClient client, CscChangeFriendData friendData, byte[] key)
        {
            VoiceChatDBEntities serverDB = new VoiceChatDBEntities();
            var friend = serverDB.Users.FirstOrDefault(u => u.UserId == friendData.Id);
            CscUserMainData userData = new CscUserMainData()
            {
                Email = friend.Email,
                Id = friend.UserId,
                Status = OnlineUsers.Any(u => u.Id == friend.UserId) ? 1 : 0,
                Ip = OnlineUsers.Any(u => u.Id == friend.UserId) ? OnlineUsers.FirstOrDefault(u => u.Id == friend.UserId).Ip : "none",
                FriendName = ""
            };
            var message = cscProtocol.CreateUnifiedUserDataMessageEncrypted(userData, key);
            client.GetStream().Write(message, 0, message.Length);
        }

        private void ExecuteCSCCommand(ConnectedUsers connectedUser, byte cmdNumber, byte[] receivedMessage)
        {
            switch (cmdNumber)
            {
                case 0://Logowanie
                    {
                        var userData = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserData;
                        //ServerConsoleWriteEvent.Invoke("Próba zalogowania: " + userData.Email + " : " + userData.Password);

                        var onlineUser = OnlineUsers.Where(e => e.Email == userData.Email).FirstOrDefault();

                        if (onlineUser != null)
                        {
                            var error = cscProtocol.CreateErrorMessageEncrypted("Podany użytkownik jest już zalogowany!", connectedUser.DH.Key);
                            connectedUser.Client.GetStream().Write(error, 0, error.Length);
                            return;
                        }
                        var localServerDB = new VoiceChatDBEntities();
                        var queryResult = localServerDB.Users.FirstOrDefault(u => u.Email == userData.Email);

                        if (queryResult != null)
                        {
                            var passwordFromDB = queryResult.Password;

                            var hashWithSalt = (CscSHA512Generator.get_SHA512_hash_as_string(passwordFromDB + connectedUser.Salt));

                            if (hashWithSalt == userData.Password)
                            {
                                ConnectedUsers user = OnlineUsers.Where(t => t.Client == connectedUser.Client).FirstOrDefault();
                                user.Email = queryResult.Email;
                                user.Id = queryResult.UserId;
                                user.Status = 1;

                                var buffer = cscProtocol.CreateConfirmMessageEncrypted("Witaj na serwerze :)", connectedUser.DH.Key);
                                connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);

                                queryResult.LastLoginDate = DateTime.Now;
                                localServerDB.SaveChanges();

                                ServerConsoleWriteEvent.Invoke("Udane logowanie: " + userData.Email + " : " + userData.Password);

                                foreach (var item in OnlineUsers)
                                { item.NewOnlineUsers.Push(connectedUser); }

                                foreach (var singleUser in OnlineUsers)
                                { SendRefreshResponseEncrypted(singleUser, singleUser.DH.Key); }
                            }
                            else
                            {
                                ServerConsoleWriteEvent.Invoke("Nieudane logowanie: " + userData.Email);
                                var buffer = cscProtocol.CreateErrorMessageEncrypted("Błędne dane logowania.", connectedUser.DH.Key);
                                connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                            }
                        }
                        else
                        {
                            ServerConsoleWriteEvent.Invoke("Nieudane logowanie: " + userData.Email);
                            var buffer = cscProtocol.CreateErrorMessageEncrypted("Błędne dane logowania. Taki użytkownik nie istnieje.", connectedUser.DH.Key);
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                        break;
                    }

                case 1://first login message
                    {
                        ConnectedUsers currUser = OnlineUsers.Where(t => t.Client == connectedUser.Client).FirstOrDefault();

                        //Friends
                        SendFriendsListEncrypted(connectedUser.Client, currUser.Email, connectedUser.DH.Key);

                        //All online users
                        SendOnlineUsersListEncrypted(connectedUser.Client, connectedUser.Id, connectedUser.DH.Key);
                        break;
                    }
                
                case 2://Rejestracja
                    {
                        var userToRegister = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserData;

                        if (!EmailValidator.IsValid(userToRegister.Email))
                        {
                            var response = cscProtocol.CreateErrorMessageEncrypted("Adres e-mail niepoprawny.", connectedUser.DH.Key);
                            connectedUser.Client.GetStream().Write(response, 0, response.Length);
                            break;
                        }
                        var localServerDB = new VoiceChatDBEntities();
                        var userWithThisMail = localServerDB.Users.Where(e => e.Email == userToRegister.Email).FirstOrDefault();

                        if (userWithThisMail != null)
                        {
                            var response = cscProtocol.CreateErrorMessageEncrypted("Podany adres e-mail już istnieje.", connectedUser.DH.Key);
                            connectedUser.Client.GetStream().Write(response, 0, response.Length);
                        }
                        else
                        {
                            CreateAccount(userToRegister.Email, userToRegister.Password);
                            var response = cscProtocol.CreateConfirmMessageEncrypted("Rejestracja udana.", connectedUser.DH.Key);
                            connectedUser.Client.GetStream().Write(response, 0, response.Length);
                        }
                        break;
                    }

                case 3://odswiezanie listy online i friend
                    {
                        SendRefreshResponseEncrypted(connectedUser, connectedUser.DH.Key);
                        break;
                    }

                case 4://dodanie usera do ulubionych
                    {
                        var userData = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscChangeFriendData;
                        ServerConsoleWriteEvent.Invoke("Dodawanie usera " + userData.Id + " jako " + userData.FriendName + " do ulubionych usera " + connectedUser.Id);

                        var localServerDB = new VoiceChatDBEntities();
                        localServerDB.FriendsList.Add(new FriendsList { FriendName = userData.FriendName, UserId = connectedUser.Id, FriendId = userData.Id });
                        localServerDB.SaveChanges();

                        //Friends
                        ConnectedUsers currUser = OnlineUsers.Where(t => t.Client == connectedUser.Client).FirstOrDefault();
                        SendNewFriendUserEncrypted(connectedUser.Client, userData, connectedUser.DH.Key);
                        break;
                    }

                case 5://usuniecie usera z ulubionych
                    {
                        var userData = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscChangeFriendData;

                        var localServerDB = new VoiceChatDBEntities();
                        localServerDB.FriendsList.Remove(localServerDB.FriendsList.FirstOrDefault(u => u.FriendId == userData.Id && u.UserId == connectedUser.Id));
                        localServerDB.SaveChanges();
                        ServerConsoleWriteEvent.Invoke("Usuniecie usera " + userData.Id + " z ulubionych usera " + connectedUser.Id + " zakonczone.");

                        //Friends
                        ConnectedUsers currUser = OnlineUsers.Where(t => t.Client == connectedUser.Client).FirstOrDefault();
                        //SendFriendsList(connectedUser.Client, currUser.Email);
                        SendNoFriendAnymoreUserEncrypted(connectedUser.Client, userData, connectedUser.DH.Key);
                        break;
                    }

                case 6://zmiana adresu email dla polaczonego usera
                    {
                        var userData = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserData;

                        if (!EmailValidator.IsValid(userData.Email))
                        {
                            ServerConsoleWriteEvent.Invoke("Nieudana próba zmiany adresu e-mail dla UserID" + connectedUser.Id + " z " + connectedUser.Email + " na " + userData.Email);
                            var buffer = cscProtocol.CreateErrorMessageEncrypted("Adres email niepoprawny!", connectedUser.DH.Key);
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                            break;
                        }

                        var localServerDB = new VoiceChatDBEntities();
                        var queryResult = localServerDB.Users.FirstOrDefault(u => u.UserId == connectedUser.Id);
                        var passwordFromDB = queryResult.Password;
                        var hashWithSalt = (CscSHA512Generator.get_SHA512_hash_as_string(passwordFromDB + connectedUser.Salt));
                        bool goodPasswordAndEmailUnused = true;
                        if (!(hashWithSalt == userData.Password))
                        {
                            goodPasswordAndEmailUnused = false;
                            ServerConsoleWriteEvent.Invoke("Nieudana próba zmiany adresu e-mail dla UserID" + connectedUser.Id + " z " + connectedUser.Email + " na " + userData.Email);
                            var buffer = cscProtocol.CreateErrorMessageEncrypted("Hasło niepoprawne!", connectedUser.DH.Key);
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                            break;
                        }

                        //sprawdzamy czy taki email jest nieuzywany
                        var queryEmailResult = localServerDB.Users.FirstOrDefault(u => u.Email == userData.Email);
                        if (!(queryEmailResult == null))
                        {
                            goodPasswordAndEmailUnused = false;
                        }

                        if (goodPasswordAndEmailUnused == true)
                        {
                            queryResult.Email = userData.Email;
                            localServerDB.SaveChanges();

                            ServerConsoleWriteEvent.Invoke("Udana zmiana adresu e-mail dla UserID" + connectedUser.Id + " z " + connectedUser.Email + " na " + userData.Email);
                            var buffer = cscProtocol.CreateConfirmMessageEncrypted("Adres e-mail został zmieniony.", connectedUser.DH.Key);
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            ServerConsoleWriteEvent.Invoke("Nieudana próba zmiany adresu e-mail dla UserID" + connectedUser.Id + " z " + connectedUser.Email + " na " + userData.Email);
                            var buffer = cscProtocol.CreateErrorMessageEncrypted("Podany adres email jest już zajęty!", connectedUser.DH.Key);
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                        break;
                    }

                case 7://zmiana hasla dla polaczonego usera
                    {
                        var userData = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscPasswordData;
                        ServerConsoleWriteEvent.Invoke("Próba zmiany hasla dla UserID" + connectedUser.Id);

                        var localServerDB = new VoiceChatDBEntities();
                        var queryResult = localServerDB.Users.FirstOrDefault(u => u.UserId == connectedUser.Id);
                        var passwordFromDB = queryResult.Password;
                        var hashWithSalt = (CscSHA512Generator.get_SHA512_hash_as_string(passwordFromDB + connectedUser.Salt));

                        if (!(hashWithSalt == userData.OldPassword))
                        {
                            ServerConsoleWriteEvent.Invoke("Nieudana próba zmiany hasla dla UserID" + connectedUser.Id);
                            var buffer = cscProtocol.CreateErrorMessageEncrypted("Hasło niepoprawne!", connectedUser.DH.Key);
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            queryResult.Password = userData.NewPassword;
                            localServerDB.SaveChanges();

                            ServerConsoleWriteEvent.Invoke("Udana zmiana hasla dla UserID" + connectedUser.Id + " z " + passwordFromDB + " na " + userData.NewPassword);
                            var buffer = cscProtocol.CreateConfirmMessageEncrypted("Hasło zostało zmienione.", connectedUser.DH.Key);
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                        break;
                    }

                case 8://wyszukiwanie userow ktorych email zawiera podana fraze
                    {
                        var text = Encoding.ASCII.GetString(receivedMessage.ToArray());
                        ServerConsoleWriteEvent.Invoke("Prośba od userID " + connectedUser.Id + " o userów zawierających w adresie e-mail frazę '" + text + "'");
                        SendSearchListEncrypted(connectedUser.Client, connectedUser.Id, text, connectedUser.DH.Key);
                        break;
                    }

                case 234:
                    {//to nigdy sie nie zdaza gdy jest polaczenie juz szyfrowane
                        var DHclientdata = Encoding.Unicode.GetString(receivedMessage.ToArray());
                        ServerConsoleWriteEvent.Invoke("Otrzymano od klienta " + connectedUser.Id + " dane algorytmu Diffie-Hellmana");
                        connectedUser.DH.HandleResponse(DHclientdata);
                        break;
                    }

                default:
                    break;
            }
        }

        private void SendRefreshResponseEncrypted(ConnectedUsers connectedUser, byte[] key)
        {//info odnosnie friendow jest juz zawarte w tych komunikatach
            if (!connectedUser.UsersToRemove.IsEmpty && connectedUser.Id != -1)
            {
                foreach (var item in connectedUser.UsersToRemove)
                {
                    SendOfflineUserEncrypted(connectedUser.Client, item, connectedUser.Email, key);
                }
                connectedUser.UsersToRemove.Clear();
            }

            if (!connectedUser.NewOnlineUsers.IsEmpty && connectedUser.Id != -1)
            {
                foreach (var item in connectedUser.NewOnlineUsers)
                {
                    SendOnlineUserEncrypted(connectedUser.Client, item, connectedUser.Email, key);
                }
                connectedUser.NewOnlineUsers.Clear();
            }            
        }

        private string SendSaltEncrypted(ConnectedUsers user)
        {
            var salt = CscSHA512Generator.Get_salt();
            var stream = user.Client.GetStream();
            var encryptedsalt = user.AES.EncryptStringToBytes(salt);
            var msg = cscProtocol.CreateSaltMessage(encryptedsalt);
            stream.Write(msg, 0, msg.Length);
            return salt;
        }
        private string SendDiffieHellman(ConnectedUsers user)
        {
            var DHdata = user.DH.ToString();
            var stream = user.Client.GetStream();
            var msg = cscProtocol.CreateDiffieHellmanMessage(DHdata);
            stream.Write(msg, 0, msg.Length);
            return DHdata;
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

                user.DH = new DiffieHellman(256).GenerateRequest();
                SendDiffieHellman(user);
                
                {//odbieranie DiffieHellmana od klienta
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

                ////////////////////////////////////
                //odtad wszystko co odbieramy powinno juz byc zaszyfrowane
                ////////////////////////////////////
                user.AES = new CscAes(user.DH.Key);
                user.Salt = SendSaltEncrypted(user);
                ServerConsoleWriteEvent.Invoke("Wysłano sól do użytkownika ID: " + user.Id);
                while (running)
                {
                    byte[] request = new byte[3];
                    var l1 = await networkStream.ReadAsync(request, 0, request.Length);

                    var msgLen = BitConverter.ToUInt16(request.Skip(1).ToArray(), 0);

                    byte[] buffer = new byte[msgLen];
                    var l2 = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                    try
                    {
                        if (buffer != null)
                        {
                            var decrypted = user.AES.DecrypBytesFromBytes(buffer);
                            ExecuteCSCCommand(user, request[0], decrypted);
                        }
                    }
                    catch(Exception)
                    {
                        if (buffer != null)
                        {
                            ExecuteCSCCommand(user, request[0], buffer);
                        }
                    }
                }
                RemoveOfflineUserEvent.Invoke(user);
                foreach (var singleUser in OnlineUsers)
                {
                    singleUser.UsersToRemove.Push(user);
                    SendRefreshResponseEncrypted(singleUser, singleUser.DH.Key);
                }
                user.Client.Close();
            }
            catch (Exception e)
            {
                ServerConsoleWriteEvent.Invoke("Użytkownik: " + user.Email + " Komunikat: " + e.Message);

                RemoveOfflineUserEvent.Invoke(user);
                foreach (var onlineUser in OnlineUsers)
                {
                    onlineUser.UsersToRemove.Push(user);
                    SendRefreshResponseEncrypted(onlineUser, onlineUser.DH.Key);
                }

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

        public ObservableCollection<ConnectedUsers> GetOnlineClients()//n !!! po oc nam ta metoda skoro pole i tak jest publiczne?
        {
            return OnlineUsers;
        }

        private void CreateAccount(string email, string password)
        {
            var localServerDB = new VoiceChatDBEntities();
            Users user = new Users();
            user.Email = email;
            user.Password = password;
            user.RegistrationDate = DateTime.Now;

            localServerDB.Users.Add(user);
            localServerDB.SaveChanges();
        }
    }
}
