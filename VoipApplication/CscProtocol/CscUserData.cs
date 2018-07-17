using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoIP_Server
{
    [Serializable]
    public class CscUserData
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
