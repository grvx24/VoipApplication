using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoIP_Client
{
    public class UserProfile
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string UserIp { get; set; }
        public int UserPort { get; set; }
        public int Status { get; set; }
    }
}
