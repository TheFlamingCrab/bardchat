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
        private RSAParameters privKey;
        private RSAParameters publKey;
    
        public Server(short _backlog)
        {
            this._backlog = _backlog;
            rsa = new RSACryptoServiceProvider(2048);
            privKey = rsa.ExportParameters(true);
            publKey = rsa.ExportParameters(false);
            Console.WriteLine(GetStringKey(publKey));
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

                string result = HandleData(dataBuffer);
                Console.WriteLine(result);

                byte[] resp = Encoding.ASCII.GetBytes(result);
                socket.BeginSend(resp, 0, resp.Length, SocketFlags.None, new AsyncCallback(SendCallBack), socket);

                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            }
            catch
            {
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

        private string GetStringKey(RSAParameters key)
        {
            StringWriter sw = new StringWriter();
            XmlSerializer xs = new XmlSerializer(typeof(RSAParameters));
            xs.Serialize(sw, key);
            return sw.ToString();
        }

        private string HandleData(byte[] data)
        {
            string text = Encoding.ASCII.GetString(data);

            string instruction = text[0..4];
            Console.WriteLine(instruction);
            // Is 6 instead of 5 becuase instruction and parameter is seperated by a colon

            byte[] parameter = default!;
            int index = -1;
            if (data.Length > 6)
            {
                parameter = data[6..];


                //TODO: MAKE THIS MORE EFFICIENT
                //THIS IS VERY SLOW, FIX THIS ASAP
                index = _currentClients.ToList().IndexOf(parameter);
            }

            string returnValue = string.Empty;

            switch (instruction)
            {
                // Initialise a conversation
                // Return Socket information on success, otherwise return NTFN (user not found)
                case "INIT":
                    IPEndPoint socketEndPoint = (_clientSockets[index].RemoteEndPoint as IPEndPoint)!;
                    string address = socketEndPoint.Address.ToString();
                    string port = socketEndPoint.Port.ToString();

                    Console.WriteLine(index);

                    returnValue = $"{address}:{port}";
                    break;
                // Register on the server (anonymous registration)
                // Return a receive message
                case "REGG":
                    Console.WriteLine("REGISTERING");

                    _currentClients.Add(parameter);

                    returnValue = "RCV";
                    break;
                // Returns the servers public key
                case "GKEY":
                    return GetStringKey(publKey);
            }

            return returnValue;
        }
    }
}