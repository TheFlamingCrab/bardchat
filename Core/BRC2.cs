#region usings
using System;
using System.IO;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Crypto.Digests;
using System.Collections.Generic;
using System.Security.Cryptography;
#endregion

namespace bardchat
{
    using static BRUtility;
    
    internal static class BRC2
    {
        static int hashAmount = 1;

        public static void TestLine()
        {
            while (true)
            {
                try
                {
                    Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);

                    Console.Write("Encode or decode? (e/d) ");
                    string input = Console.ReadLine();

                    if (input == "e" || input == "encode")
                    {
                        Console.Write("Text: ");
                        string textToEncode = Console.ReadLine();
                        Console.Write("Password: ");
                        string password = Console.ReadLine();
                        int iv = random.Next(0, int.MaxValue - ((int)Math.Ceiling((decimal)(textToEncode.Length / 64))));
                        Console.WriteLine(iv);
                        byte[] encodedBrCode = BRC2.EncodeText(textToEncode, password, iv, 44);
                        // THE LAST NUMBER MUST BE A MULTIPLE OF 22
                        Console.Write("Full save path: ");
                        string savePath = Console.ReadLine();

                        File.WriteAllBytes(savePath, encodedBrCode);
                    }
                    else if (input == "d" || input == "decode")
                    {
                        Console.Write("Full path to save encoded file: ");
                        string fileToDecode = Console.ReadLine();

                        byte[] bytes = File.ReadAllBytes(fileToDecode);

                        Console.Write("password: ");
                        string password = Console.ReadLine();
                        Console.Write("IV: ");
                        int iv = Convert.ToInt32(Console.ReadLine());
                        string decodedText = BRC2.DecodeText(bytes, password, iv);
                        Console.WriteLine(decodedText + " ");
                    }
                    else
                    {
                        Console.WriteLine("I don't understand that");
                    }
                }
                catch (Exception e)
                {
                  Console.WriteLine(e.Message);
                }
            }
        }

        public static string DecodeText(byte[] bytes, string password, int iv)
        {
            int b = 88;

            Console.WriteLine("Decrypting...");
            Console.WriteLine("IV is " + iv);

            int blockCount = (int)Math.Ceiling(((decimal)bytes.Length / (decimal)(b)));

            byte[] text = bytes;

            List<byte[]> blocks = new List<byte[]>();

            for (int i = 0; i < blockCount; i++)
            {
                if (text[0..].Length < b)
                {
                    blocks.Add(text);
                    break;
                }

                blocks.Add(text[0..b]);
                text = text[b..];
            }

            string result = "";

            string masterKey = "";

            for (int i = 0; i < blocks.Count; i++)
            {
                string decrypted = DecodeBlock(blocks[i], (password + (iv + i).ToString()));

                int dl = decrypted.Length;

                result += decrypted[0..(dl-4)];

                masterKey += decrypted[(dl-4)..];
            }

            int counter = 0;

            char[] preMasteredChars = result.ToCharArray();

            string masterHash = HashText(masterKey, ("masterkey" + iv));

            for (int i = 0; i < result.Length; i++)
            {
                preMasteredChars[i] = ((char)Convert.ToByte((Convert.ToInt32(preMasteredChars[i]) ^
                    masterHash[counter % masterHash.Length]) % 256));

                counter++;
            }

            result = new string(preMasteredChars);

            result = result[(result.IndexOf((char)25))..(result.IndexOf((char)26))];

            return result;
        }

        private static string DecodeBlock(byte[] bytes, string password)
        {
            int hashAmount = 1;

            string returnValue = "";

            List<byte> newBytes = new List<byte>();
            newBytes = bytes.ToList();

            int counter = 0;

            string hashedPassword = HashText(password, "!@BARD#$" + password + "!@CODE#$");

            char[] hashedPasswordArray = hashedPassword.ToCharArray();

            if (password != "")
            {
                for (int j = 0; j < hashAmount; j++)
                {
                    for (int i = 0; i < hashedPasswordArray.Length; i++)
                    {
                        hashedPasswordArray[i] = ((char)Convert.ToByte((Convert.ToInt32(hashedPasswordArray[i]) ^
                            hashedPasswordArray[counter % hashedPasswordArray.Length]) % 256));

                        counter++;
                    }

                    counter = 0;

                    hashedPassword = HashText(hashedPassword, "!@BARD#$" + hashedPassword + "!@CODE#$");
                    hashedPasswordArray = hashedPassword.ToCharArray();
                }
            }

            hashedPassword = new string(hashedPasswordArray);

            if (password != "")
            {
                for (int i = 0; i < newBytes.Count; i++)
                {
                    int tempInt = Convert.ToInt32(newBytes[i]) -
                        hashedPassword[counter % hashedPassword.Length];

                    if (tempInt < 0)
                    {
                        tempInt = 256 + tempInt;
                    }

                    newBytes[i] = Convert.ToByte(tempInt);

                    counter++;
                }
            }

            byte[] newBytesList = newBytes.ToArray();

            int length = newBytesList.Length;

            for (int i = 0; i < length / 8; i++)
            {
                string binary = "";

                byte[] currentOctet = newBytesList[(i * 8)..((i + 1) * 8)];

                foreach (var v in currentOctet)
                {
                    binary += DecimalToBinary(Convert.ToInt32(Convert.ToString(v, 2), 2), 8);
                }

                returnValue += Decode(binary);
            }

            if ((int)returnValue[returnValue.Length - 1] == 3)
            {
                returnValue = returnValue[0..(returnValue.Length - 1)];
            }

            string recordString = returnValue;

            return returnValue;
        }

        public static byte[] EncodeText(string input, string password, int iv, int fBlockSize)
        {
            // FBLOCKSIZE MUST BE A MULTIPLE OF 22

            if (fBlockSize < 22)
                Console.WriteLine("Block size must be at least 22");

            Console.WriteLine("Encrypting...");

            Console.WriteLine("IV is " + iv);

            int b = 18;

            int fBlockCount = (int)Math.Ceiling((decimal)(input.Length + 2) / (decimal)(fBlockSize));

            string text = input;

            int pos = random.Next(0, ((fBlockSize * fBlockCount) - (text.Length) - 2));

            string fullBlock = GetRandomAsciiString(pos)
                + (char)25
                + input
                + (char)26
                + GetRandomAsciiString((fBlockSize * fBlockCount) - (text.Length) - 2 - pos);

            text = fullBlock;

            int blockCount = (int)Math.Ceiling(((decimal)text.Length / (decimal)(b)));

            // Generate master key

            string masterKey = "";

            for (int i = 0; i < blockCount; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    int n = RandomNumberGenerator.GetInt32(1, 256);
                    masterKey += (char)n;
                }
            }

            string masterHash = HashText(masterKey, ("masterkey" + iv));

            int counter = 0;

            char[] preMasteredChars = text.ToCharArray();

            for (int i = 0; i < text.Length; i++)
            {
                preMasteredChars[i] = ((char)Convert.ToByte((Convert.ToInt32(preMasteredChars[i]) ^
                    masterHash[counter % masterHash.Length]) % 256));

                counter++;
            }

            text = new string(preMasteredChars);

            List<string> blocks = new List<string>();

            for (int i = 0; i < blockCount; i++)
            {
                if (text[0..].Length < b)
                {
                    blocks.Add(text);
                    break;
                }

                blocks.Add(text[0..b]);
                text = text[b..];
            }

            List<byte> bytes = new List<byte>();

            for (int i = 0; i < blocks.Count; i++)
            {
                bytes.AddRange(EncodeBlock((blocks[i] + masterKey[(i*4)..((i+1)*4)]), (password + (iv + i).ToString())));
            }

            return bytes.ToArray();
        }

        private static byte[] EncodeBlock(string input, string password)
        {
            int length = input.Length;

            string tempStr = input;

            string text = tempStr;

            List<byte> returnValue = new List<byte>();

            List<string> twins = new List<string>();

            int index = 0;

            if (text.Length % 2 != 0)
                index = text.Length / 2 + 1;
            else
                index = text.Length / 2;

            for (int i = 0; i < index; i++)
            {
                if (text.Length % 2 != 0 && text.Length < 2)
                {
                    twins.Add(text + (char)03);
                }
                else
                {
                    twins.Add(text[0..2]);
                    text = text[2..];
                }
            }

            foreach (var v in twins)
            {
                returnValue.AddRange(Encode(v));
            }

            int counter = 0;

            string hashedPassword = HashText(password, "!@BARD#$" + password + "!@CODE#$");

            char[] hashedPasswordArray = hashedPassword.ToCharArray();

            if (password != "")
            {
                for (int j = 0; j < hashAmount; j++)
                {
                    for (int i = 0; i < hashedPasswordArray.Length; i++)
                    {
                        hashedPasswordArray[i] = ((char)Convert.ToByte((Convert.ToInt32(hashedPasswordArray[i]) ^
                            hashedPasswordArray[counter % hashedPasswordArray.Length]) % 256));

                        counter++;
                    }

                    counter = 0;

                    hashedPassword = HashText(hashedPassword, "!@BARD#$" + hashedPassword + "!@CODE#$");
                    hashedPasswordArray = hashedPassword.ToCharArray();
                }
            }

            hashedPassword = new string(hashedPasswordArray);

            if (password != "")
            {
                for (int i = 0; i < returnValue.Count; i++)
                {
                    returnValue[i] = (Convert.ToByte((Convert.ToInt32(returnValue[i]) +
                        hashedPassword[counter % hashedPassword.Length]) % 256));

                    counter++;
                }
            }

            return returnValue.ToArray();
        }

        private static string Decode(string binary)
        {
            List<int> nullByteList = new List<int>();
            List<int> bytes = new List<int>();

            bytes.Add(Convert.ToChar(Convert.ToInt32(GetSector(binary, 0, 4, 2, 4, 8), 2)));
            bytes.Add(Convert.ToChar(Convert.ToInt32(GetSector(binary, 2, 4, 2, 4, 8), 2)));
            bytes.Add(Convert.ToChar(Convert.ToInt32(GetSector(binary, 4, 4, 2, 4, 8), 2)));
            bytes.Add(Convert.ToChar(Convert.ToInt32(GetSector(binary, 6, 4, 2, 4, 8), 2)));

            nullByteList.Add(Convert.ToChar(Convert.ToInt32(GetSector(binary, 0, 0, 2, 4, 8), 2)));
            nullByteList.Add(Convert.ToChar(Convert.ToInt32(GetSector(binary, 2, 0, 2, 4, 8), 2)));

            string blue = GetSector(binary, 4, 0, 2, 2, 8);

            string lime = GetSector(binary, 4, 2, 2, 2, 8);

            string purple = GetSector(binary, 6, 2, 2, 2, 8);

            string orange = GetSector(binary, 6, 0, 2, 1, 8);

            string magenta = GetSector(binary, 6, 1, 2, 1, 8);

            bytes[Convert.ToInt32(orange, 2)] -= Convert.ToInt32(purple, 2);

            for (int i = 0; i < bytes.Count; i++)
            {
                bytes[i] -= (((Convert.ToInt32(magenta, 2) + i) % 4) + 1) * Convert.ToInt32(lime, 2);
            }

            for (int i = 0; i < bytes.Count; i++)
            {
                if (bytes[i] < 0)
                {
                    bytes[i] = 256 + bytes[i];
                }
            }

            List<int> newByteList = new List<int>();

            for (int i = 0; i < bytes.Count; i++)
            {
                if (bytes[i] != (((nullByteList[0]) + Convert.ToInt32(blue, 2))) % 256 &&
                    bytes[i] != (((nullByteList[1]) + Convert.ToInt32(blue, 2))) % 256)
                    newByteList.Add(bytes[i]);
            }

            string returnValue = "";

            foreach (var v in newByteList)
            {
                returnValue += (char)v;
            }

            return returnValue;
        }

        private static byte[] Encode(string text)
        {
            if (text.Length != 2)
                return null;

            string binary = "";

            for (int i = 0; i < 64; i++)
            {
                binary += "0";
            }

            List<int> bytes = new List<int>();

            int blue = random.Next(0, 16);

            int[] order = new int[] { 1, 2, 3, 4 };

            order.Shuffle();

            List<int> nullByteList = new List<int>();

            List<int> newByteList = new List<int>();

            int counter = 0;

            for (int i = 0; i < text.Length; i++)
            {
                bytes.Add((char)text[i]);
            }

            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] > text.Length)
                {
                    int nullByte = Convert.ToInt32(GetRandomNullByte(8), 2);

                    nullByteList.Add(nullByte);

                    nullByte += blue;

                    newByteList.Add(nullByte);
                }
                else
                {
                    newByteList.Add(bytes[counter]);
                    counter++;
                }
            }

            int lime = random.Next(0, 16);
            int magenta = random.Next(0, 4);

            for (int i = 0; i < newByteList.Count; i++)
            {
                newByteList[i] += (((magenta + i) % 4) + 1) * lime;
            }

            int orange = random.Next(0, text.Length);
            int purple = random.Next(0, 16);

            newByteList[orange] += purple;

            for (int i = 0; i < newByteList.Count; i++)
            {
                newByteList[i] = newByteList[i] % 256;
            }

            SetSector(ref binary, 0, 4, 2, 4, 8, DecimalToBinary(newByteList[0], 8));
            SetSector(ref binary, 2, 4, 2, 4, 8, DecimalToBinary(newByteList[1], 8));
            SetSector(ref binary, 4, 4, 2, 4, 8, DecimalToBinary(newByteList[2], 8));
            SetSector(ref binary, 6, 4, 2, 4, 8, DecimalToBinary(newByteList[3], 8));

            SetSector(ref binary, 0, 0, 2, 4, 8, DecimalToBinary(nullByteList[0], 8));
            SetSector(ref binary, 2, 0, 2, 4, 8, DecimalToBinary(nullByteList[1], 8));

            SetSector(ref binary, 4, 0, 2, 2, 8, DecimalToBinary(blue, 4));

            SetSector(ref binary, 4, 2, 2, 2, 8, DecimalToBinary(lime, 4));

            SetSector(ref binary, 6, 2, 2, 2, 8, DecimalToBinary(purple, 4));

            SetSector(ref binary, 6, 0, 2, 1, 8, DecimalToBinary(orange, 2));

            SetSector(ref binary, 6, 1, 2, 1, 8, DecimalToBinary(magenta, 2));

            List<byte> returnByteList = new List<byte>();

            for (int i = 0; i < 8; i++)
            {
                returnByteList.Add(Convert.ToByte(Convert.ToInt32(binary[0..8], 2)));
                binary = binary[8..];
            }

            return returnByteList.ToArray();
        }

        private static string GetRandomNullByte(int length)
        {
            Random random = new Random();

            string result = "";

            int randomInt = random.Next(198, 246);

            return DecimalToBinary(randomInt, 8);
        }

        private static string DecimalToBinary(int number, int digits)
        {
            string tempStr = Convert.ToString(number, 2);

            while (tempStr.Length < digits)
            {
                tempStr = "0" + tempStr;
            }

            return tempStr;
        }

        private static void PrintCharArray(string text)
        {
            for (int i = 0; i < 8; i++)
            {
                Console.WriteLine(text[(i * 8)..(i * 8 + 8)]);
            }
        }

        private static void ChangeCharInString(ref string text, int position, char newValue)
        {
            char[] chars = text.ToCharArray();

            chars[position] = newValue;

            text = new string(chars);
        }

        private static void SetSector(ref string binary, int xPos, int yPos, int xDim, int yDim, int binarySideLength, string newBinary)
        {
            string value = "";
            int counter = 0;

            int start = (yPos * (binarySideLength)) + xPos;

            for (int i = 0; i < yDim; i++)
            {
                for (int j = 0; j < xDim; j++)
                {
                    ChangeCharInString(ref binary, start + (i * (binarySideLength)) + j, newBinary[counter]);
                    counter++;
                }
            }
        }

        private static string GetSector(string binary, int xPos, int yPos, int xDim, int yDim, int binarySideLength)
        {
            string value = "";

            int start = (yPos * (binarySideLength)) + xPos;

            for (int i = 0; i < yDim; i++)
            {
                for (int j = 0; j < xDim; j++)
                {
                    int pos = start + (i * (binarySideLength)) + j;

                    value += binary[pos];
                }
            }

            return value;
        }

        private static string GetRandomAsciiString(int length)
        {
            if (length < 0)
            {
                return "";
            }

            char[] chars = new char[length];

            for (int i = 0; i < length; i++)
            {
                int c = RandomNumberGenerator.GetInt32(1, 256);

                chars[i] = (char)c;
            }

            return new string(chars);
        }
    }

    internal static class BRExtensionMethods
    {
        public static string ToAsciiString(this byte[] bytes)
        {
            char[] chars = new char[bytes.Length];

            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i] = (char)bytes[i];
            }

            return new string(chars);
        }

        public static byte[] ToByteArray(this string s)
        {
            byte[] bytes = new byte[s.Length];

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)s[i];
            }

            return bytes;
        }

        public static void Shuffle<T>(this Random rng, T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static string Offset(this string text, int amount)
        {
            char[] offsetString = text.ToCharArray();

            for (int i = 0; i < text.Length; i++)
            {
                int charInt = (Convert.ToInt32(text[i]) + amount);
                if (charInt < 0)
                    charInt = 256 + charInt;

                charInt = charInt % 256;

                offsetString[i] = (char)charInt;
            }

            return new string(offsetString);
        }
    }

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
