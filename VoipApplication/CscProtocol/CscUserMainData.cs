using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cscprotocol
{
    [Serializable]
    public class CscUserMainData
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FriendName { get; set; }
        public int Status { get; set; }
        public string Ip { get; set; }
        public bool IsNotFriend { get; set; }
        public bool CanBeRemoved { get; set; }
    }
}
