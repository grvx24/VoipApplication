using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PODlab5
{
    public class CscSHA512Generator
    {
        public static byte[] get_SHA512_hash(string data_as_string)
        {
            byte[] data = Encoding.ASCII.GetBytes(data_as_string);
            SHA512 sha = SHA512Managed.Create();
            return sha.ComputeHash(data);
        }
        public static string get_SHA512_hash_as_string(string data_as_string)
        {
            byte[] data = Encoding.ASCII.GetBytes(data_as_string);
            SHA512 sha = SHA512Managed.Create();
            var hash = sha.ComputeHash(data);
            return Encoding.ASCII.GetString(hash);
        }
        public static string RandomString(int length)
        {
            Random rand = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[rand.Next(s.Length)]).ToArray());
        }
        public static string Get_salt()
        {
            var size_in_bytes = SHA512Managed.Create().HashSize / 8;
            return RandomString(size_in_bytes);
        }
    }
}
