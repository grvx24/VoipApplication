using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cscprotocol
{
    [Serializable]
    public class CscChangeFriendData
    {
        public int Id { get; set; }
        public string FriendName { get; set; }
    }
}
