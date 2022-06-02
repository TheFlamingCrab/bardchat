#region usings
using System.Text;

using System.Net;
using System.Net.Sockets;

using System.Xml.Serialization;

using System.Security.Cryptography;
#endregion

namespace bardchat
{
    internal sealed class Server
    {
        byte[] _buffer;
        
        private List<Socket> _clientSockets = new List<Socket>();

        // List of users currently online
        private SortedSet<byte[]> _currentClients = new SortedSet<byte[]>();

        private short _backlog;
        private Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private RSACryptoServiceProvider rsa;
        private string privKey;
        private string publKey;

        public Server(short _backlog)
        {
            this._backlog = _backlog;
            rsa = new RSACryptoServiceProvider(2048);
            privKey = rsa.ToXmlString(true);
            publKey = rsa.ToXmlString(false);
            Console.WriteLine(rsa.ToXmlString(false));
        }

        public void Start()
        {
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, Globals.serverPort));
            _serverSocket.Listen(_backlog);

            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            Socket socket = _serverSocket.EndAccept(ar);
            _clientSockets.Add(socket);
            Console.WriteLine(this._clientSockets.Count);

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

                byte[] result = HandleData(dataBuffer);
                Console.WriteLine(Encoding.ASCII.GetString(result));

                // fix this
                byte[] resp = result;
                socket.BeginSend(resp, 0, resp.Length, SocketFlags.None, new AsyncCallback(SendCallBack), socket);

                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Client disconnected");
                Console.WriteLine(this._clientSockets.Count);
                socket.Close();
                socket.Dispose();
            }
        }

        private void SendCallBack(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState!;
            socket.EndSend(ar);
        }

        private byte[] HandleData(byte[] data)
        {
            string text = Encoding.ASCII.GetString(data);

            char c = text[0];

            if (c == 'D')
            {   
                data = data[1..];
                Console.WriteLine("DATA LENGTH IS " + data.Length);
                byte[] decryptBuffer = rsa.Decrypt(data, false);
                text = Encoding.ASCII.GetString(decryptBuffer);
                Console.WriteLine(text);
            }
            else if (c == 'N')
                text = text[1..];
            else
            {
                Console.WriteLine("Invalid char code at index 0");
                return Encoding.ASCII.GetBytes("INV");
            }

            string instruction = text[0..4];
            Console.WriteLine(instruction);
            
            // Is 6 instead of 5 becuase instruction and parameter is seperated by a colon

            byte[] parameter = default!;
            int index = -1;
            if (data.Length > 6)
            {
                parameter = data[6..];

                //TODO: MAKE THIS MORE EFFICIENT
                //THIS IS VERY SLOW, FIX THIS
                index = _currentClients.ToList().IndexOf(parameter);
            }

            byte[] returnValue = Array.Empty<byte>();

            switch (instruction)
            {
                // Initialise a conversation
                // Return Socket information on success, otherwise return NTFN (user not found)
                case "INIT":
                    IPEndPoint socketEndPoint = (_clientSockets[index].RemoteEndPoint as IPEndPoint)!;
                    string address = socketEndPoint.Address.ToString();
                    string port = socketEndPoint.Port.ToString();

                    Console.WriteLine(index);

                    returnValue = Encoding.ASCII.GetBytes($"{address}:{port}");
                    break;
                // Register on the server (anonymous registration)
                // Return a receive message
                case "REGG":
                    Console.WriteLine("REGISTERING");

                    _currentClients.Add(parameter);

                    returnValue = Encoding.ASCII.GetBytes("RCV");
                    break;
                // Returns the servers public key
                case "GKEY":
                    string xmlString = rsa.ToXmlString(false);
                    return Encoding.ASCII.GetBytes(xmlString);

                default:
                    return Encoding.ASCII.GetBytes("INV");
            }

            return returnValue;
        }
    }
}