using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace cscprotocol
{
    public class CscAes
    {
        private const int number_of_bits_in_one_byte = 8;
        private Aes aes_data;
        //public Aes Aes_data {get => aes_data;}
        private CipherMode mode = CipherMode.ECB;
        public CscAes(byte[] Key)
        {
            Validate_key_length(Key.Length);
            aes_data = Aes.Create();
            if(Key.Length == 31)
            {
                byte[] newKey = new byte[32];
                Key.CopyTo(newKey, 0);
                newKey[31] = 0;
                aes_data.KeySize = newKey.Length * number_of_bits_in_one_byte;
                aes_data.Key = newKey;
            }
            else
            {
                aes_data.KeySize = Key.Length * number_of_bits_in_one_byte;
                aes_data.Key = Key;
            }


        }

        public CscAes(string Key)
        {
            Validate_key_length(Key.Length);
            aes_data = Aes.Create();
            byte[] Key_as_bytes_array = Encoding.ASCII.GetBytes(Key);//change string to byte array
            aes_data.KeySize = Key_as_bytes_array.Length * number_of_bits_in_one_byte;
            aes_data.Key = Key_as_bytes_array;
        }
        public byte[] EncryptBytesToBytes(byte[] message_to_encrypt)
        //{ return EncryptStringToBytes(Encoding.Unicode.GetString(message_to_encrypt.ToArray()), mode); }//oryginalnie, ale chyba troche dziwnie
        //{ return EncryptStringToBytes(Encoding.Unicode.GetString(message_to_encrypt), mode); }
        { return EncryptStringToBytes(Encoding.ASCII.GetString(message_to_encrypt), mode); }//serializowane dane logowania dzialaja

        public byte[] EncryptStringToBytes(string message_to_encrypt)
        { return EncryptStringToBytes(message_to_encrypt, mode); }

        public byte[] EncryptStringToBytes(string message_to_encrypt, CipherMode mode)
        {
            // Check arguments.
            if (message_to_encrypt == null || message_to_encrypt.Length <= 0)
                throw new ArgumentNullException("Message is empty.");

            byte[] encrypted;
            aes_data.Mode = mode;

            // Create a decrytor to perform the stream transform.
            ICryptoTransform encryptor = aes_data.CreateEncryptor(aes_data.Key, aes_data.IV);

            // Create the streams used for encryption.
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        //Write all data to the stream.
                        swEncrypt.Write(message_to_encrypt);
                    }
                    encrypted = msEncrypt.ToArray();
                }
            }
            return encrypted;
        }

        public byte[] DecrypBytesFromBytes(byte[] message_to_decrypt)
        {
            //return Encoding.Unicode.GetBytes(DecryptStringFromBytes(message_to_decrypt, mode));//oryginalnie
            return Encoding.ASCII.GetBytes(DecryptStringFromBytes(message_to_decrypt, mode));//serializowane dane logowania dzialaja
        }
        public string DecryptStringFromBytes(byte[] message_to_decrypt)
        {
            return DecryptStringFromBytes(message_to_decrypt, mode);
        }

        public string DecryptStringFromBytes(byte[] message_to_decrypt, CipherMode mode)
        {
            // Check arguments.
            if (message_to_decrypt == null || message_to_decrypt.Length <= 0)
                throw new ArgumentNullException("Message is empty.");

            // Declare the string used to hold
            // the decrypted text.
            string decrypted = null;
            aes_data.Mode = mode;

            // Create a decrytor to perform the stream transform.
            ICryptoTransform decryptor = aes_data.CreateDecryptor(aes_data.Key, aes_data.IV);

            // Create the streams used for decryption.
            using (MemoryStream msDecrypt = new MemoryStream(message_to_decrypt))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        // Read the decrypted bytes from the decrypting stream
                        // and place them in a string.
                        decrypted = srDecrypt.ReadToEnd();
                    }
                }
            }
            return decrypted;
        }
        public static void Validate_key_length(int Key_length)
        {
            AesManaged aesmanaged = new AesManaged();
            KeySizes[] ks = aesmanaged.LegalKeySizes;
            bool is_length_good = false;
            foreach (KeySizes k in ks)
            {
                if ((Key_length * number_of_bits_in_one_byte) >= k.MinSize && (Key_length * number_of_bits_in_one_byte) <= k.MaxSize)
                { is_length_good = true; break; }
            }

            if (is_length_good == false)
            {
                string message = string.Empty;
                message += "Your key length is " + Key_length * number_of_bits_in_one_byte + " bits";
                foreach (KeySizes k in ks)
                {
                    message += "\n\nLegal min key size = " + k.MinSize + " bits";
                    message += "\nLegal max key size = " + k.MaxSize + " bits";
                }
                throw new Exception(message);
            }
        }
        public static void AES_example()
        {
            try
            {
                string key = "dnnnnnnnnnnnnnn1dnnnnnnnnnnnnnn1";//klucz ustalony przez DH jako string lub byte []
                CscAes aes_en = new CscAes(key);
                var ECB_encrypted = aes_en.EncryptStringToBytes("Taka tam wiadomosc odszyfrowana.", CipherMode.ECB);
                var ECB_decrypted = aes_en.DecryptStringFromBytes(ECB_encrypted, CipherMode.ECB);
                Console.WriteLine(ECB_decrypted);
            }
            catch (Exception e)
            { Console.WriteLine(e.Message); }
        }

        public byte[] EncryptBytes(byte[] message)
        {
            // Check arguments.
            if (message == null || message.Length <= 0)
                throw new ArgumentNullException("Message is empty.");

            aes_data.Mode = mode;

            // Create a decrytor to perform the stream transform.
            ICryptoTransform encryptor = aes_data.CreateEncryptor(aes_data.Key, aes_data.IV);

            // Create the streams used for encryption.
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(message, 0, message.Length);
                    csEncrypt.FlushFinalBlock();
                    return msEncrypt.ToArray();
                }
            }
        }

        public byte[] DecryptBytes(byte[] message)
        {
            // Check arguments.
            if (message == null || message.Length <= 0)
                throw new ArgumentNullException("Message is empty.");

            // Declare the string used to hold
            // the decrypted text.
            aes_data.Mode = mode;

            // Create a decrytor to perform the stream transform.
            ICryptoTransform decryptor = aes_data.CreateDecryptor(aes_data.Key, aes_data.IV);

            // Create the streams used for decryption.
            using (MemoryStream msDecrypt = new MemoryStream(message))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                {
                    csDecrypt.Write(message, 0, message.Length);
                    csDecrypt.FlushFinalBlock();
                    return msDecrypt.ToArray();
                }
            }
        }


        public byte[] DecryptBytes(byte[] message, int length)
        {
            // Check arguments.
            if (message == null || message.Length <= 0)
                throw new ArgumentNullException("Message is empty.");

            // Declare the string used to hold
            // the decrypted text.
            aes_data.Mode = mode;

            // Create a decrytor to perform the stream transform.
            ICryptoTransform decryptor = aes_data.CreateDecryptor(aes_data.Key, aes_data.IV);



            // Create the streams used for decryption.
            using (MemoryStream msDecrypt = new MemoryStream(message))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                {
                    csDecrypt.Write(message, 0, message.Length);
                    csDecrypt.FlushFinalBlock();
                    return message.Take(length).ToArray();
                }
            }
        }
    }
}
