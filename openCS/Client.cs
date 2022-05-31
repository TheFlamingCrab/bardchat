#region usings
using System.Security.Cryptography;

using System.Text;

using System.Net;
using System.Net.Sockets;

using System.Threading.Tasks;
#endregion

namespace bardchat
{
    internal sealed class Client
    {
        public List<Chat> chats { get; private set; }

        public byte[] id { get; private set; } = new byte[64];

        private RSACryptoServiceProvider rsa;
        private RSAParameters serverKey;

        private Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public Client()
        {
            chats = new List<Chat>();
        }

        private byte[] RSAEncrypt(byte[] data)
        {
            byte[] result = new byte[data.Length];
            rsa.Encrypt(result, false);
            return result;
        }

        private byte[] RSADecrypt(byte[] data)
        {
            byte[] result = new byte[data.Length];
            rsa.Decrypt(data, false);
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
            byte[] buffer = Encoding.ASCII.GetBytes(data);//BRC2.EncodeText(data, "hello", 556, 22);

            _clientSocket.Send(buffer);

            byte[] receiveBuffer = new byte[_clientSocket.ReceiveBufferSize];
            int rec = _clientSocket.Receive(receiveBuffer);

            byte[] resp = new byte[rec];
            Array.Copy(receiveBuffer, resp, rec);

            string response = Encoding.ASCII.GetString(resp);

            Console.WriteLine(response);

            if (response == "RCV")
            {
                Console.WriteLine("SERVER HAS RECEIVED THE DATA");
            }
        }
    }
}