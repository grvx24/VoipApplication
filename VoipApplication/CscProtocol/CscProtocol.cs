using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace cscprotocol
{
    //Client-server communication protocol :)
    class CscProtocol
    {
        //conversion from any object to byte array
        //object need to be tagged with the Serializable
        //enum DataType { UserData, FriendsUserData }

        public static byte[] Serialize(object anySerializableObject)
        {
            using (var memoryStream = new MemoryStream())
            {
                (new BinaryFormatter()).Serialize(memoryStream, anySerializableObject);
                return memoryStream.ToArray();
            }
        }

        //public static object DeserializeWithLengthInfo(byte[] message)
        //{
        //    var lengthArray = message.Skip(1).Take(2).ToArray();
        //    var length = BitConverter.ToUInt16(lengthArray, 0);

        //    using (var memoryStream = new MemoryStream(message.Skip(lengthArray.Length + 1).Take(length).ToArray()))
        //        return (new BinaryFormatter()).Deserialize(memoryStream);
        //}

        public static object DeserializeWithoutLenghtInfo(byte[] message)
        {
            using (var memoryStream = new MemoryStream(message))
                return (new BinaryFormatter()).Deserialize(memoryStream);
        }




        #region ServerMethods

        //public CscUserData ReadUserData(byte[] message)
        //{
        //    var result = DeserializeWithLengthInfo(message) as CscUserData;

        //    if (result == null)
        //        throw new NullReferenceException();
        //    else
        //        return result;
        //}

        //user pasujacy do zapytania wyszukiwania userow
        public byte[] CreateSearchUserDataResponseEncrypted(CscUserMainData searchUserData, byte[] key)
        {
            var searchUserAsBytes = CscProtocol.Serialize(searchUserData);
            var searchUserAsBytesEncrypted = new CscAes(key).EncryptBytesToBytes(searchUserAsBytes);
            var message = new byte[3 + searchUserAsBytesEncrypted.Length];
            message[0] = 3;
            BitConverter.GetBytes((UInt16)searchUserAsBytesEncrypted.Length).CopyTo(message, 1);
            searchUserAsBytesEncrypted.CopyTo(message, 3);
            return message;
        }

        //n zunifikowana zmiana stanu usera !!!!
        public byte[] CreateUnifiedUserDataMessageEncrypted(CscUserMainData userMainData, byte[] key)//CreateFriendUserDataMessage
        {
            var friendsAsBytes = CscProtocol.Serialize(userMainData);
            var friendsAsBytesEncrypted = new CscAes(key).EncryptBytesToBytes(friendsAsBytes);
            var message = new byte[3 + friendsAsBytesEncrypted.Length];
            message[0] = 4;
            BitConverter.GetBytes((UInt16)friendsAsBytesEncrypted.Length).CopyTo(message, 1);
            friendsAsBytesEncrypted.CopyTo(message, 3);
            return message;
        }

        public byte[] CreateConfirmMessageEncrypted(string message, byte[] key)
        {
            if (!string.IsNullOrEmpty(message))
            {
                var mainMessage = new CscAes(key).EncryptStringToBytes(message);
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

        public byte[] CreateErrorMessageEncrypted(string message, byte[] key)
        {
            if (!string.IsNullOrEmpty(message))
            {
                var mainMessage = new CscAes(key).EncryptStringToBytes(message);
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

        //public static string ParseConfirmMessageAndDecrypt(byte[] message, byte[] key)
        //{
        //    var lenght = BitConverter.ToUInt16(message.Skip(1).Take(2).ToArray(), 0);
        //    //string text = Encoding.Unicode.GetString(message.Skip(3).Take(lenght).ToArray());
        //    string text = new CscAes(key).DecryptStringFromBytes(message.Skip(3).Take(lenght).ToArray());
        //    return text;
        //}

        public byte[] CreateLoginMessageEncrypted(CscUserData userData, byte[] key)
        {
            var mainMessagePlain = Serialize(userData);
            var mainMessage = new CscAes(key).EncryptBytesToBytes(mainMessagePlain);
            UInt16 mainMessageLength = (UInt16)mainMessage.Length;


            byte[] lenghtBytes = BitConverter.GetBytes(mainMessageLength);
            byte[] fullData = new byte[1 + lenghtBytes.Length + mainMessageLength];
            fullData[0] = 0;
            lenghtBytes.CopyTo(fullData, 1);
            mainMessage.CopyTo(fullData, lenghtBytes.Length + 1);
            return fullData;
        }

        public byte[] CreateRegistrationMessageEncrypted(CscUserData userData, byte[] key)
        {
            var mainMessagePlain = Serialize(userData);
            var mainMessage = new CscAes(key).EncryptBytesToBytes(mainMessagePlain);
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
        public byte[] CreateChangeEmailRequestEncrypted(CscUserData userData, byte[] key)
        {
            var mainMessagePlain = Serialize(userData);
            var mainMessage = new CscAes(key).EncryptBytesToBytes(mainMessagePlain);
            UInt16 mainMessageLength = (UInt16)mainMessage.Length;

            byte[] lenghtBytes = BitConverter.GetBytes(mainMessageLength);
            byte[] fullData = new byte[1 + lenghtBytes.Length + mainMessageLength];
            fullData[0] = 6;
            lenghtBytes.CopyTo(fullData, 1);
            mainMessage.CopyTo(fullData, lenghtBytes.Length + 1);

            return fullData;
        }

        public byte[] CreateChangePasswordRequestEncrypted(CscPasswordData userData, byte[] key)
        {
            var mainMessagePlain = Serialize(userData);
            var mainMessage = new CscAes(key).EncryptBytesToBytes(mainMessagePlain);
            UInt16 mainMessageLength = (UInt16)mainMessage.Length;

            byte[] lenghtBytes = BitConverter.GetBytes(mainMessageLength);
            byte[] fullData = new byte[1 + lenghtBytes.Length + mainMessageLength];
            fullData[0] = 7;
            lenghtBytes.CopyTo(fullData, 1);
            mainMessage.CopyTo(fullData, lenghtBytes.Length + 1);

            return fullData;
        }

        public byte[] CreateSearchUserRequestEncrypted(string message, byte[] key)
        {
            var mainMessage = new CscAes(key).EncryptStringToBytes(message);
            UInt16 messageLength = (UInt16)mainMessage.Length;
            var lenghtBytes = BitConverter.GetBytes(messageLength);

            byte[] result = new byte[mainMessage.Length + lenghtBytes.Length + 1];
            result[0] = 8;

            lenghtBytes.CopyTo(result, 1);
            mainMessage.CopyTo(result, lenghtBytes.Length + 1);

            return result;
        }

        public byte[] CreateAddUserToFriendsListRequestEncrypted(CscChangeFriendData userData, byte[] key)
        {
            var mainMessagePlain = Serialize(userData);
            var mainMessage = new CscAes(key).EncryptBytesToBytes(mainMessagePlain);
            UInt16 mainMessageLength = (UInt16)mainMessage.Length;

            byte[] lenghtBytes = BitConverter.GetBytes(mainMessageLength);
            byte[] fullData = new byte[1 + lenghtBytes.Length + mainMessageLength];
            fullData[0] = 4;
            lenghtBytes.CopyTo(fullData, 1);
            mainMessage.CopyTo(fullData, lenghtBytes.Length + 1);
            return fullData;
        }
        
        public byte[] CreateRemoveUserFromFriendsListRequestEncrypted(CscChangeFriendData userData, byte[] key)
        {
            var mainMessagePlain = Serialize(userData);
            var mainMessage = new CscAes(key).EncryptBytesToBytes(mainMessagePlain);
            UInt16 mainMessageLength = (UInt16)mainMessage.Length;

            byte[] lenghtBytes = BitConverter.GetBytes(mainMessageLength);
            byte[] fullData = new byte[1 + lenghtBytes.Length + mainMessageLength];
            fullData[0] = 5;
            lenghtBytes.CopyTo(fullData, 1);
            mainMessage.CopyTo(fullData, lenghtBytes.Length + 1);
            return fullData;
        }

        #endregion
    }
}
