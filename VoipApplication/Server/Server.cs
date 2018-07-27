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
            //UsersToRemove = new ConcurrentStack<ConnectedUsers>();
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

        private IQueryable<CscUserMainData> FindFriendsForUser(string email)
        {
            VoiceChatDBEntities serverDB = new VoiceChatDBEntities();//n chyba lepiej lokalny kontekst, co? !!!
            var user = serverDB.Users.FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                throw new NullReferenceException("UserId is null!");
            }

            var friendsQueryResult = from f in serverDB.FriendsList
                                     join u in serverDB.Users on f.FriendId equals u.UserId
                                     where f.UserId == user.UserId
                                     select new CscUserMainData { Id = f.FriendId, FriendName = f.FriendName, Email = u.Email, Status = 0, Ip = "none" };
            return friendsQueryResult;
        }

        private void SendFriendsList(TcpClient client, string email)
        {
            var friendsQueryResult = FindFriendsForUser(email);
            foreach (var friend in friendsQueryResult)
            {
                CscUserMainData friendsUserData = new CscUserMainData()
                {
                    FriendName = friend.FriendName,
                    Email = friend.Email,
                    Id = friend.Id,
                    Status = OnlineUsers.Any(u => u.Id == friend.Id) ? 1 : 0,
                    Ip = OnlineUsers.Any(u => u.Id == friend.Id) ? OnlineUsers.FirstOrDefault(u => u.Id == friend.Id).Ip : "none"
                };

                //var message = cscProtocol.CreateNewFriendUserDataMessage(friendsUserData);
                var message = cscProtocol.CreateUnifiedUserDataMessage(friendsUserData);
                client.GetStream().Write(message, 0, message.Length);
            }
        }

        private void SendSearchList(TcpClient client, int connectedUserID, string text)
        {
            VoiceChatDBEntities serverDB = new VoiceChatDBEntities();//n chyba lepiej lokalny kontekst, co? !!!
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

                var message = cscProtocol.CreateSearchUserDataResponse(searchedUserData);
                client.GetStream().Write(message, 0, message.Length);
            }
        }

        private void SendOnlineUsersList(TcpClient client, int connectedUserID)
        {
            VoiceChatDBEntities serverDB = new VoiceChatDBEntities();//n chyba lepiej lokalny kontekst, co? !!!
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
                //var message = cscProtocol.CreateOnlineUserDataMessage(userData);
                var message = cscProtocol.CreateUnifiedUserDataMessage(userData);
                client.GetStream().Write(message, 0, message.Length);
            }
        }

        private void SendOnlineUser(TcpClient client, ConnectedUsers newUser, string receiverEmail)
        {
            VoiceChatDBEntities serverDB = new VoiceChatDBEntities();//n chyba lepiej lokalny kontekst, co? !!!
            var friendsQueryResult = FindFriendsForUser(receiverEmail);
            CscUserMainData userData = new CscUserMainData()
            {
                Email = newUser.Email,
                Id = newUser.Id,
                Status = 1,
                Ip = newUser.Ip,
                FriendName = friendsQueryResult.Any(u => u.Id == newUser.Id) ? friendsQueryResult.FirstOrDefault(u => u.Id == newUser.Id).FriendName : string.Empty,
            };
            //var message = cscProtocol.CreateOnlineUserDataMessage(userData);
            var message = cscProtocol.CreateUnifiedUserDataMessage(userData);
            client.GetStream().Write(message, 0, message.Length);
        }

        private void SendOfflineUser(TcpClient client, ConnectedUsers newUser, string receiverEmail)
        {
            VoiceChatDBEntities serverDB = new VoiceChatDBEntities();//n chyba lepiej lokalny kontekst, co? !!!
            var friendsQueryResult = FindFriendsForUser(receiverEmail);
            CscUserMainData userData = new CscUserMainData()
            {
                Email = newUser.Email,
                Id = newUser.Id,
                Status = 0,
                Ip = newUser.Ip,
                FriendName = friendsQueryResult.Any(u => u.Id == newUser.Id) ? friendsQueryResult.FirstOrDefault(u => u.Id == newUser.Id).FriendName : string.Empty,
            };
            //var message = cscProtocol.CreateOfflineUserDataMessage(userData);
            var message = cscProtocol.CreateUnifiedUserDataMessage(userData);
            client.GetStream().Write(message, 0, message.Length);
        }

        private void SendNewFriendUser(TcpClient client, CscChangeFriendData friendData)
        {
            VoiceChatDBEntities serverDB = new VoiceChatDBEntities();//n chyba lepiej lokalny kontekst, co? !!!
            var friend = serverDB.Users.FirstOrDefault(u => u.UserId == friendData.Id);
            CscUserMainData userData = new CscUserMainData()
            {
                Email = friend.Email,
                Id = friend.UserId,
                Status = OnlineUsers.Any(u => u.Id == friend.UserId) ? 1 : 0,
                Ip = OnlineUsers.Any(u => u.Id == friend.UserId) ? OnlineUsers.FirstOrDefault(u => u.Id == friend.UserId).Ip : "none",
                FriendName = friendData.FriendName
            };
            //var message = cscProtocol.CreateNewFriendUserDataMessage(userData);
            var message = cscProtocol.CreateUnifiedUserDataMessage(userData);
            client.GetStream().Write(message, 0, message.Length);
        }

        private void SendNoFriendAnymoreUser(TcpClient client, CscChangeFriendData friendData)
        {
            VoiceChatDBEntities serverDB = new VoiceChatDBEntities();//n chyba lepiej lokalny kontekst, co? !!!
            var friend = serverDB.Users.FirstOrDefault(u => u.UserId == friendData.Id);
            CscUserMainData userData = new CscUserMainData()
            {
                Email = friend.Email,
                Id = friend.UserId,
                Status = OnlineUsers.Any(u => u.Id == friend.UserId) ? 1 : 0,
                Ip = OnlineUsers.Any(u => u.Id == friend.UserId) ? OnlineUsers.FirstOrDefault(u => u.Id == friend.UserId).Ip : "none",
                FriendName = ""
            };
            //var message = cscProtocol.CreateNoFriendAnymoreUserDataMessage(userData);
            var message = cscProtocol.CreateUnifiedUserDataMessage(userData);
            client.GetStream().Write(message, 0, message.Length);
        }

        private void SendProfileInfo(ConnectedUsers user)
        {
            CscUserMainData userData = new CscUserMainData() { Email = user.Email, Id = user.Id, Status = 1, Ip = user.Ip, FriendName = "" };
            var message = cscProtocol.CreateUserProfileMessage(userData);
            user.Client.GetStream().Write(message, 0, message.Length);
        }

        private void ExecuteCSCCommand(ConnectedUsers connectedUser, byte cmdNumber, byte[] receivedMessage)
        {//niee liczac diffiehellmana to tutaj bedzie trzeba odszyfrowac receivedMessage przed przekazaniem dalej
            switch (cmdNumber)
            {
                //Logowanie
                case 0:
                    {
                        var userData = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserData;
                        //tu bedzie trza jeszcze odszyfrowac itd, póki co jest jawnie
                        //ServerConsoleWriteEvent.Invoke("Próba zalogowania: " + userData.Email + " : " + userData.Password);

                        var onlineUser = OnlineUsers.Where(e => e.Email == userData.Email).FirstOrDefault();

                        if (onlineUser != null)
                        {
                            var error = cscProtocol.CreateErrorMessage("Podany użytkownik jest już zalogowany!");
                            connectedUser.Client.GetStream().Write(error, 0, error.Length);
                            return;
                        }
                        var localServerDB = new VoiceChatDBEntities();//n odswierzenie kontekstu BD zeby zmienione hasla userów dzialaly
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

                                var buffer = cscProtocol.CreateConfirmMessage("Witaj na serwerze :)");
                                connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);

                                ServerConsoleWriteEvent.Invoke("Udane logowanie: " + userData.Email + " : " + userData.Password);
                            }
                            else
                            {
                                ServerConsoleWriteEvent.Invoke("Nieudane logowanie: " + userData.Email + " : " + userData.Password + " =/= " + hashWithSalt);
                                var buffer = cscProtocol.CreateErrorMessage("Błędne dane logowania. Hasło niepoprawne.");
                                connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                            }
                        }
                        else
                        {
                            ServerConsoleWriteEvent.Invoke("Nieudane logowanie: " + userData.Email);
                            var buffer = cscProtocol.CreateErrorMessage("Błędne dane logowania. Taki użytkownik nie istnieje.");
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                        break;
                    }

                case 1:
                    {//first login message
                        ConnectedUsers currUser = OnlineUsers.Where(t => t.Client == connectedUser.Client).FirstOrDefault();
                        //Dane użytkownika
                        SendProfileInfo(currUser);

                        //Friends
                        SendFriendsList(connectedUser.Client, currUser.Email);

                        //All online users
                        SendOnlineUsersList(connectedUser.Client, connectedUser.Id);

                        break;
                    }

                //Rejestracja
                case 2:
                    {
                        var userToRegister = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserData;

                        if (!EmailValidator.IsValid(userToRegister.Email))
                        {
                            var response = cscProtocol.CreateErrorMessage("Adres e-mail niepoprawny.");
                            connectedUser.Client.GetStream().Write(response, 0, response.Length);
                            break;
                        }
                        var localServerDB = new VoiceChatDBEntities();//n odswierzenie kontekstu BD zeby zmienione hasla userów dzialaly
                        var userWithThisMail = localServerDB.Users.Where(e => e.Email == userToRegister.Email).FirstOrDefault();

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
                    }

                case 3://odświeżanie listy online
                    {
                        if (!connectedUser.NewOnlineUsers.IsEmpty && connectedUser.Id != -1)
                        {
                            foreach (var item in connectedUser.NewOnlineUsers)
                            {
                                SendOnlineUser(connectedUser.Client, item, connectedUser.Email);
                            }
                            connectedUser.NewOnlineUsers.Clear();
                        }

                        if (!connectedUser.UsersToRemove.IsEmpty && connectedUser.Id != -1)
                        {
                            foreach (var item in connectedUser.UsersToRemove)
                            {
                                SendOfflineUser(connectedUser.Client, item, connectedUser.Email);
                            }
                            connectedUser.UsersToRemove.Clear();
                        }
                        //n !!!!! trzeba tez wyslac dane odnosnie friendlisty
                        break;
                    }

                case 4://dodanie usera do ulubionych
                    {
                        var userData = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscChangeFriendData;
                        ServerConsoleWriteEvent.Invoke("Dodawanie usera " + userData.Id + " jako " + userData.FriendName + " do ulubionych usera " + connectedUser.Id);

                        serverDB = new VoiceChatDBEntities();//n odswierzenie kontekstu BD zeby zmienione hasla userów dzialaly
                        serverDB.FriendsList.Add(new FriendsList { FriendName = userData.FriendName, UserId = connectedUser.Id, FriendId = userData.Id });
                        serverDB.SaveChanges();

                        //Friends
                        ConnectedUsers currUser = OnlineUsers.Where(t => t.Client == connectedUser.Client).FirstOrDefault();
                        //SendFriendsList(connectedUser.Client, currUser.Email);
                        SendNewFriendUser(connectedUser.Client, userData);
                        break;
                    }

                case 5://usuniecie usera z ulubionych
                    {
                        var userData = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscChangeFriendData;

                        serverDB = new VoiceChatDBEntities();//n odswierzenie kontekstu BD zeby zmienione hasla userów dzialaly
                        serverDB.FriendsList.Remove(serverDB.FriendsList.FirstOrDefault(u => u.FriendId == userData.Id && u.UserId == connectedUser.Id));
                        serverDB.SaveChanges();
                        ServerConsoleWriteEvent.Invoke("Usuniecie usera " + userData.Id + " z ulubionych usera " + connectedUser.Id + " zakonczone.");

                        //Friends
                        ConnectedUsers currUser = OnlineUsers.Where(t => t.Client == connectedUser.Client).FirstOrDefault();
                        //SendFriendsList(connectedUser.Client, currUser.Email);
                        SendNoFriendAnymoreUser(connectedUser.Client, userData);
                        break;
                    }

                case 6://zmiana adresu email dla polaczonego usera
                    {
                        var userData = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscUserData;
                        //ServerConsoleWriteEvent.Invoke("Próba zmiany adresu e-mail dla UserID" + connectedUser.Id + " z " + connectedUser.Email + " na " + userData.Email);

                        if (!EmailValidator.IsValid(userData.Email))
                        {
                            ServerConsoleWriteEvent.Invoke("Nieudana próba zmiany adresu e-mail dla UserID" + connectedUser.Id + " z " + connectedUser.Email + " na " + userData.Email);
                            var buffer = cscProtocol.CreateErrorMessage("Adres email niepoprawny!");
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                            break;
                        }

                        //serverDB = new VoiceChatDBEntities();//n odswierzenie kontekstu BD zeby zmienione hasla userów dzialaly
                        var queryResult = serverDB.Users.FirstOrDefault(u => u.UserId == connectedUser.Id);
                        var passwordFromDB = queryResult.Password;
                        var hashWithSalt = (CscSHA512Generator.get_SHA512_hash_as_string(passwordFromDB + connectedUser.Salt));
                        bool goodPasswordAndEmailUnused = true;
                        if (!(hashWithSalt == userData.Password))
                        {
                            goodPasswordAndEmailUnused = false;
                            ServerConsoleWriteEvent.Invoke("Nieudana próba zmiany adresu e-mail dla UserID" + connectedUser.Id + " z " + connectedUser.Email + " na " + userData.Email);
                            var buffer = cscProtocol.CreateErrorMessage("Hasło niepoprawne!");
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                            break;
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

                            ServerConsoleWriteEvent.Invoke("Udana zmiana adresu e-mail dla UserID" + connectedUser.Id + " z " + connectedUser.Email + " na " + userData.Email);
                            var buffer = cscProtocol.CreateConfirmMessage("Adres e-mail został zmieniony.");
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            ServerConsoleWriteEvent.Invoke("Nieudana próba zmiany adresu e-mail dla UserID" + connectedUser.Id + " z " + connectedUser.Email + " na " + userData.Email);
                            var buffer = cscProtocol.CreateErrorMessage("Podany adres email jest już zajęty!");
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                        break;
                    }

                case 7://zmiana hasla dla polaczonego usera
                    {
                        var userData = CscProtocol.DeserializeWithoutLenghtInfo(receivedMessage) as CscPasswordData;
                        ServerConsoleWriteEvent.Invoke("Próba zmiany hasla dla UserID" + connectedUser.Id);

                        //serverDB = new VoiceChatDBEntities();//n odswierzenie kontekstu BD zeby zmienione hasla userów dzialaly
                        var queryResult = serverDB.Users.FirstOrDefault(u => u.UserId == connectedUser.Id);
                        var passwordFromDB = queryResult.Password;
                        var hashWithSalt = (CscSHA512Generator.get_SHA512_hash_as_string(passwordFromDB + connectedUser.Salt));

                        if (!(hashWithSalt == userData.OldPassword))
                        {
                            ServerConsoleWriteEvent.Invoke("Nieudana próba zmiany hasla dla UserID" + connectedUser.Id);
                            var buffer = cscProtocol.CreateErrorMessage("Hasło niepoprawne!");
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            queryResult.Password = userData.NewPassword;
                            serverDB.SaveChanges();

                            ServerConsoleWriteEvent.Invoke("Udana zmiana hasla dla UserID" + connectedUser.Id + " z " + passwordFromDB + " na " + userData.NewPassword);
                            var buffer = cscProtocol.CreateConfirmMessage("Hasło zostało zmienione.");
                            connectedUser.Client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                        break;
                    }

                case 8://wyszukiwanie userow ktorych email zawiera podana fraze
                    {
                        var text = Encoding.Unicode.GetString(receivedMessage.ToArray());
                        ServerConsoleWriteEvent.Invoke("Prośba od userID " + connectedUser.Id + " o userów zawierających w adresie e-mail frazę '" + text + "'");
                        SendSearchList(connectedUser.Client, connectedUser.Id, text);
                        break;
                    }

                case 234:
                    {
                        var DHclientdata = Encoding.Unicode.GetString(receivedMessage.ToArray());
                        ServerConsoleWriteEvent.Invoke("Otrzymano od klienta " + connectedUser.Id + " dane algorytmu DiffieHellmana: " + DHclientdata);
                        connectedUser.DH.HandleResponse(DHclientdata);
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

            var encryptedsalt = new CscAes(user.DH.Key).EncryptStringToBytes(salt);//zamiana tablicy bajtow spowrotem na string w celu szyfrowania

            //var msg = cscProtocol.CreateSaltMessage(salt);
            //var msg = cscProtocol.CreateSaltMessage(Encoding.Unicode.GetString(encryptedsalt.ToArray()));
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

                //odbieranie DiffieHellmana od klienta
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

                ////////////////////////////////////
                //odtad wszystko co odbieramy powinno juz byc zaszyfrowane
                ////////////////////////////////////

                user.Salt = SendSalt(user);//wysyłanie soli
                ServerConsoleWriteEvent.Invoke("Wysłano sól " + user.Salt);
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
                RemoveOfflineUserEvent.Invoke(user);
                foreach (var onlineUser in OnlineUsers)
                {
                    onlineUser.UsersToRemove.Push(user);
                }
                //UsersToRemove.Push(user);

                user.Client.Close();

            }
            catch (Exception e)
            {
                ServerConsoleWriteEvent.Invoke("Użytkownik: " + user.Email + " Komunikat: " + e.Message);

                RemoveOfflineUserEvent.Invoke(user);
                foreach (var onlineUser in OnlineUsers)
                {
                    onlineUser.UsersToRemove.Push(user);
                }
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
