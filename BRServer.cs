#region usings
using System.Text;

using System.Net;
using System.Net.Sockets;
#endregion

namespace bardchat
{
    internal sealed class BRServer
    {
        byte[] _buffer;
        
        private List<Socket> _clientSockets = new List<Socket>();

        private short backlog;
        private Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    
        public BRServer(short backlog)
        {
            this.backlog = backlog;
        }

        public void Start()
        {
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, Globals.serverPort));
            _serverSocket.Listen(backlog);

            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            Socket socket = _serverSocket.EndAccept(ar);
            _clientSockets.Add(socket);

            Console.WriteLine("Client Connected");

            _buffer = new byte[socket.ReceiveBufferSize];

            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState!;

            try
            {
                int received = socket.EndReceive(ar);
                byte[] dataBuffer = new byte[received];
                Array.Copy(_buffer, dataBuffer, received);

                string text = Encoding.ASCII.GetString(dataBuffer);

                int result = HandleData(text);

                byte[] resp = Encoding.ASCII.GetBytes("RCV");
                socket.BeginSend(resp, 0, resp.Length, SocketFlags.None, new AsyncCallback(SendCallBack), socket);

                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            }
            catch
            {
                Console.WriteLine("Client disconnected");
                socket.Close();
                socket.Dispose();
            }
        }

        private void SendCallBack(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState!;
            socket.EndSend(ar);
        }

        private int HandleData(string data)
        {
            Console.WriteLine(data);
            return 0;
        }
    }
}