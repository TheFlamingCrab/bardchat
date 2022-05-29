#region usings
using System.Text;

using System.Net;
using System.Net.Sockets;

using System.Threading.Tasks;
#endregion

namespace bardchat
{
    internal sealed class BRClient
    {
        private Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

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

        public void Disconnect()
        {
            _clientSocket.Disconnect(false);
        }

        public void Send(string data)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            _clientSocket.Send(buffer);

            byte[] receiveBuffer = new byte[_clientSocket.ReceiveBufferSize];
            int rec = _clientSocket.Receive(receiveBuffer);

            byte[] resp = new byte[rec];
            Array.Copy(receiveBuffer, resp, rec);

            Console.WriteLine("Received " + Encoding.ASCII.GetString(resp));
        }
    }
}