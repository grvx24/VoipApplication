using CPOL;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VoIP_Server
{
    public class ConnectedUsers
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public int Status { get; set; }
        public string Ip { get; set; }
        public TcpClient Client { get; set; }
        public string Salt { get; set; }
        public DiffieHellman DH { get; set; }
        public cscprotocol.CscAes AES { get; set; }

        public ConcurrentStack<ConnectedUsers> NewOnlineUsers = new ConcurrentStack<ConnectedUsers>();
        public ConcurrentStack<ConnectedUsers> UsersToRemove = new ConcurrentStack<ConnectedUsers>();

    }
}
