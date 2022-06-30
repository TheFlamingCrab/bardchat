#region usings
using System.Security.Cryptography;

using System.Text;

using System.Net;
using System.Net.Sockets;

using System.Threading.Tasks;

using System.Xml.Serialization;
#endregion

namespace bardchat
{
    internal sealed class Client
    {
        public List<Chat> chats { get; private set; }

        public byte[] id { get; private set; } = new byte[64];

        private bool hasServerKey = false;
        private RSACryptoServiceProvider rsa;

        private string lastCommand = string.Empty;

        private Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public Client()
        {
            chats = new List<Chat>();

            rsa = new RSACryptoServiceProvider();
        }

        private byte[] RSAEncrypt(byte[] data)
        {
            byte[] result = new byte[data.Length];
            result = rsa.Encrypt(data, false);
            return result;
        }

        private byte[] RSADecrypt(byte[] data)
        {
            byte[] result = new byte[data.Length];
            result = rsa.Decrypt(data, false);
            return result;
        }

        public void GenerateNewId()
        {
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            
            rng.GetBytes(id);

            Console.WriteLine(id);
        }

        public void LoopConnect()
        {
            int attempts = 0;

            while (!_clientSocket.Connected)
            {
                try
                {
                    attempts++;

                    _clientSocket.Connect(Globals.serverIP, Globals.serverPort);
                }
                catch
                {
                    Console.Clear();
                    Console.WriteLine("Connection attempts: " + attempts);
                }
            }

            Console.WriteLine("Connected");
        }

        public void AddChat(Chat chat) => chats.Add(chat);

        public void RemoveChat(Chat chat) => chats.Remove(chat);

        public void Disconnect()
        {
            _clientSocket.Disconnect(false);
        }

        public void SendInitRequest(Guid id)
        {
            Send("INIT:" + id);
        }

        public void Send(string data)
        {
            lastCommand = data;

            byte[] buffer = Encoding.ASCII.GetBytes(data);//BRC2.EncodeText(data, "hello", 556, 22);

            if (hasServerKey)
            {
                buffer = RSAEncrypt(buffer);
                Console.WriteLine("Encrypted");
            }

            //TODO: this takes up a lot of space on the heap, fix it if possible

            byte[] tmpBuffer = new byte[buffer.Length + 1];

            Buffer.BlockCopy(buffer, 0, tmpBuffer, 1, buffer.Length);

            if (hasServerKey)
                tmpBuffer[0] = (byte)'D';
            else
                tmpBuffer[0] = (byte)'N';
            
            Console.WriteLine(tmpBuffer.Length);
            _clientSocket.Send(tmpBuffer);

            byte[] receiveBuffer = new byte[_clientSocket.ReceiveBufferSize];
            int rec = _clientSocket.Receive(receiveBuffer);

            byte[] resp = new byte[rec];
            Array.Copy(receiveBuffer, resp, rec);

            string response = Encoding.ASCII.GetString(resp);

            Console.WriteLine(response);

            if (lastCommand.StartsWith("GKEY"))
            {
                rsa.FromXmlString(response);
                Console.WriteLine(rsa.ToXmlString(false));
                hasServerKey = true;
                Console.WriteLine("RECEIVED SERVER KEY");
            }

            if (response == "RCV")
            {
                Console.WriteLine("SERVER HAS RECEIVED THE DATA");
            }
        }
    }
}