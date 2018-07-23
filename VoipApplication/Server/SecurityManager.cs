using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cscprotocol;

namespace VoIP_Server
{
    class SecurityManager
    {
        public bool Authenticate(CscUserData receivedData, CscUserData userFromDatabase)
        {
            if (receivedData.Email != userFromDatabase.Email)
            {
                return false;
            }

            if (receivedData.Password != userFromDatabase.Password)
            {
                return false;
            }

            return true;
        }

    }
}
