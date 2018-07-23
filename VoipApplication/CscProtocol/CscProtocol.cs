using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace cscprotocol
{


    //Client-server communication protocol :)
    class CscProtocol
    {
        //conversion from any object to byte array
        //object need to be tagged with the Serializable
        enum DataType { UserData, FriendsUserData }


        public static byte[] Serialize(object anySerializableObject)
        {
            using (var memoryStream = new MemoryStream())
            {
                (new BinaryFormatter()).Serialize(memoryStream, anySerializableObject);
                return memoryStream.ToArray();
            }
        }

        public static object DeserializeWithLengthInfo(byte[] message)
        {
            var lengthArray = message.Skip(1).Take(2).ToArray();
            var length = BitConverter.ToUInt16(lengthArray, 0);


            using (var memoryStream = new MemoryStream(message.Skip(lengthArray.Length + 1).Take(length).ToArray()))
                return (new BinaryFormatter()).Deserialize(memoryStream);
        }

        public static object DeserializeWithoutLenghtInfo(byte[] message)
        {
            using (var memoryStream = new MemoryStream(message))
                return (new BinaryFormatter()).Deserialize(memoryStream);
        }




        #region ServerMethods

        public CscUserData ReadUserData(byte[] message)
        {
            var result = DeserializeWithLengthInfo(message) as CscUserData;

            if (result == null)
                throw new NullReferenceException();
            else
                return result;
        }

        //user pasujacy
        public byte[] CreateSearchUserDataResponse(CscUserMainData searchUserData)
        {
            var searchUserAsBytes = CscProtocol.Serialize(searchUserData);
            var message = new byte[3 + searchUserAsBytes.Length];
            message[0] = 3;
            BitConverter.GetBytes((UInt16)searchUserAsBytes.Length).CopyTo(message, 1);
            searchUserAsBytes.CopyTo(message, 3);

            return message;
        }

        //dowolny uzytkownik z listy ulubionych
        public byte[] CreateFriendUserDataMessage(CscUserMainData friendsUserData)
        {
            var friendsAsBytes = CscProtocol.Serialize(friendsUserData);
            var message = new byte[3 + friendsAsBytes.Length];
            message[0] = 9;
            BitConverter.GetBytes((UInt16)friendsAsBytes.Length).CopyTo(message, 1);
            friendsAsBytes.CopyTo(message, 3);

            return message;
        }

        //uzytkownik online
        public byte[] CreateOnlineUserDataMessage(CscUserMainData friendsUserData)
        {
            var users = CscProtocol.Serialize(friendsUserData);
            var message = new byte[3 + users.Length];
            message[0] = 8;
            BitConverter.GetBytes((UInt16)users.Length).CopyTo(message, 1);
            users.CopyTo(message, 3);

            return message;
        }
        //uzytkownik offline
        public byte[] CreateOfflineUserDataMessage(CscUserMainData friendsUserData)
        {
            var users = CscProtocol.Serialize(friendsUserData);
            var message = new byte[3 + users.Length];
            message[0] = 7;
            BitConverter.GetBytes((UInt16)users.Length).CopyTo(message, 1);
            users.CopyTo(message, 3);

            return message;
        }

        public byte[] CreateUserProfileMessage(CscUserMainData userMainData)
        {
            var users = CscProtocol.Serialize(userMainData);
            var message = new byte[3 + users.Length];
            message[0] = 6;
            BitConverter.GetBytes((UInt16)users.Length).CopyTo(message, 1);
            users.CopyTo(message, 3);

            return message;
        }


        public byte[] CreateConfirmMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                var mainMessage = Encoding.Unicode.GetBytes(message);
                UInt16 messageLength = (UInt16)mainMessage.Length;
                var lenghtBytes = BitConverter.GetBytes(messageLength);


                byte[] result = new byte[mainMessage.Length + lenghtBytes.Length + 1];
                result[0] = 12;

                lenghtBytes.CopyTo(result, 1);
                mainMessage.CopyTo(result, lenghtBytes.Length + 1);

                return result;
            }
            else
            { throw new ArgumentNullException("Message cannot be null"); }
        }


        public byte[] CreateErrorMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                var mainMessage = Encoding.Unicode.GetBytes(message);
                UInt16 messageLength = (UInt16)mainMessage.Length;
                var lenghtBytes = BitConverter.GetBytes(messageLength);

                byte[] result = new byte[mainMessage.Length + lenghtBytes.Length + 1];
                result[0] = 13;

                lenghtBytes.CopyTo(result, 1);
                mainMessage.CopyTo(result, lenghtBytes.Length + 1);

                return result;
            }
            else
            { throw new ArgumentNullException("Message cannot be null"); }
        }

        public byte[] CreateSaltMessage(string message)
        {
            var mainMessage = Encoding.Unicode.GetBytes(message);
            UInt16 messageLength = (UInt16)mainMessage.Length;
            var lenghtBytes = BitConverter.GetBytes(messageLength);

            byte[] result = new byte[mainMessage.Length + lenghtBytes.Length + 1];
            result[0] = 1;

            lenghtBytes.CopyTo(result, 1);
            mainMessage.CopyTo(result, lenghtBytes.Length + 1);

            return result;
        }
        public byte[] CreateSaltMessage(byte[] mainMessage)
        {
            UInt16 messageLength = (UInt16)mainMessage.Length;
            var lenghtBytes = BitConverter.GetBytes(messageLength);

            byte[] result = new byte[mainMessage.Length + lenghtBytes.Length + 1];
            result[0] = 1;

            lenghtBytes.CopyTo(result, 1);
            mainMessage.CopyTo(result, lenghtBytes.Length + 1);

            return result;
        }
        public byte[] CreateDiffieHellmanMessage(string message)
        {
            var mainMessage = Encoding.Unicode.GetBytes(message);
            UInt16 messageLength = (UInt16)mainMessage.Length;
            var lenghtBytes = BitConverter.GetBytes(messageLength);

            byte[] result = new byte[mainMessage.Length + lenghtBytes.Length + 1];
            result[0] = 0;

            lenghtBytes.CopyTo(result, 1);
            mainMessage.CopyTo(result, lenghtBytes.Length + 1);

            return result;
        }


        #endregion





        #region Client methods

        public static string ParseConfirmMessage(byte[] message)
        {
            var lenght = BitConverter.ToUInt16(message.Skip(1).Take(2).ToArray(), 0);

            string text = Encoding.Unicode.GetString(message.Skip(3).Take(lenght).ToArray());
            return text;
        }

        public byte[] CreateLoginMessage(CscUserData userData)
        {
            var mainMessage = Serialize(userData);
            UInt16 mainMessageLength = (UInt16)mainMessage.Length;


            byte[] lenghtBytes = BitConverter.GetBytes(mainMessageLength);
            byte[] fullData = new byte[1 + lenghtBytes.Length + mainMessageLength];
            fullData[0] = 0;
            lenghtBytes.CopyTo(fullData, 1);
            mainMessage.CopyTo(fullData, lenghtBytes.Length + 1);

            return fullData;
        }

        public byte[] CreateRegistrationMessage(CscUserData userData)
        {
            var mainMessage = Serialize(userData);
            UInt16 mainMessageLength = (UInt16)mainMessage.Length;


            byte[] lenghtBytes = BitConverter.GetBytes(mainMessageLength);
            byte[] fullData = new byte[1 + lenghtBytes.Length + mainMessageLength];
            fullData[0] = 2;
            lenghtBytes.CopyTo(fullData, 1);
            mainMessage.CopyTo(fullData, lenghtBytes.Length + 1);

            return fullData;
        }


        public byte[] CreateStartingInfoRequest()
        {
            byte[] result = new byte[3];
            result[0] = 1;
            result[1] = 0;
            result[2] = 0;

            return result;
        }

        public byte[] CreateRefreshRequest()
        {
            byte[] result = new byte[3];
            result[0] = 3;
            result[1] = 0;
            result[2] = 0;

            return result;
        }
        public byte[] CreateChangeEmailMessage(CscUserData userData)
        {
            var mainMessage = Serialize(userData);
            UInt16 mainMessageLength = (UInt16)mainMessage.Length;


            byte[] lenghtBytes = BitConverter.GetBytes(mainMessageLength);
            byte[] fullData = new byte[1 + lenghtBytes.Length + mainMessageLength];
            fullData[0] = 6;
            lenghtBytes.CopyTo(fullData, 1);
            mainMessage.CopyTo(fullData, lenghtBytes.Length + 1);

            return fullData;
        }

        public byte[] CreateChangePasswordMessage(CscPasswordData userData)
        {
            var mainMessage = Serialize(userData);
            UInt16 mainMessageLength = (UInt16)mainMessage.Length;

            byte[] lenghtBytes = BitConverter.GetBytes(mainMessageLength);
            byte[] fullData = new byte[1 + lenghtBytes.Length + mainMessageLength];
            fullData[0] = 7;
            lenghtBytes.CopyTo(fullData, 1);
            mainMessage.CopyTo(fullData, lenghtBytes.Length + 1);

            return fullData;
        }

        public byte[] CreateSearchUserRequest(string message)
        {
            var mainMessage = Encoding.Unicode.GetBytes(message);
            UInt16 messageLength = (UInt16)mainMessage.Length;
            var lenghtBytes = BitConverter.GetBytes(messageLength);

            byte[] result = new byte[mainMessage.Length + lenghtBytes.Length + 1];
            result[0] = 8;

            lenghtBytes.CopyTo(result, 1);
            mainMessage.CopyTo(result, lenghtBytes.Length + 1);

            return result;
        }
        public byte[] CreateAddUserToFriendsListDataMessage(CscChangeFriendData userData)
        {
            var mainMessage = Serialize(userData);
            UInt16 mainMessageLength = (UInt16)mainMessage.Length;

            byte[] lenghtBytes = BitConverter.GetBytes(mainMessageLength);
            byte[] fullData = new byte[1 + lenghtBytes.Length + mainMessageLength];
            fullData[0] = 4;
            lenghtBytes.CopyTo(fullData, 1);
            mainMessage.CopyTo(fullData, lenghtBytes.Length + 1);

            return fullData;
        }

        #endregion
    }
}
