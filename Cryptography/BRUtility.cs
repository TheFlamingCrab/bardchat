#region usings
using System;
using System.Text;
using System.Linq;

using Org.BouncyCastle.Crypto.Digests;
#endregion

namespace bardchat
{
    internal static class BRUtility
    {
        public static Random random = new Random();

        public static string HashText(string text, string salt)
        {
            Sha3Digest hashAlgorithm = new Sha3Digest(512);
            byte[] input = Encoding.ASCII.GetBytes((text + "BARDCODE" + salt));

            hashAlgorithm.BlockUpdate(input, 0, input.Length);

            byte[] result = new byte[64];

            hashAlgorithm.DoFinal(result, 0);

            string hashString = Convert.ToBase64String(result);

            return hashString;
        }

        public static string RandomStringInt(int length)
        {
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public class HexadecimalEncoding
        {
            public static string ToHexString(string str)
            {
                var sb = new StringBuilder();

                var bytes = Encoding.ASCII.GetBytes(str);

                foreach (var t in bytes)
                {
                    sb.Append(t.ToString("X2"));
                }

                Console.WriteLine(sb.ToString());

                return sb.ToString();
            }

            public static string FromHexString(string hexString)
            {
                var bytes = new byte[hexString.Length / 2];

                for (var i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
                }

                return Encoding.ASCII.GetString(bytes);
            }
        }
    }
}