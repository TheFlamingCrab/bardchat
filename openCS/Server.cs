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
        private Dictionary<byte[], (byte[], IPAddress, int)> _currentClients = new Dictionary<byte[], (byte[], IPAddress, int)>();
        
        private short _backlog;
        private Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private RSACryptoServiceProvider _rsa;
        private string _privKey;
        private string _publKey;
        private string message;
        private byte[] bytemessage;
        private static readonly byte[] MSG_RECEIVE = Encoding.ASCII.GetBytes("RCV");
        private static readonly byte[] MSG_INVALID = Encoding.ASCII.GetBytes("INV");
        private static readonly byte[] MSG_AGREE = Encoding.ASCII.GetBytes("AGR");
        private static readonly byte[] MSG_REFUSE = Encoding.ASCII.GetBytes("REF");
        private static readonly byte[] USER_NOT_FOUND = Encoding.ASCII.GetBytes("UNF");

        public Server(short _backlog)
        {
            this._backlog = _backlog;
            _rsa = new RSACryptoServiceProvider(2048);
            _privKey = _rsa.ToXmlString(true);
            _publKey = _rsa.ToXmlString(false);
            Console.WriteLine(_rsa.ToXmlString(false));
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

                IPEndPoint socketEndPoint = (socket.RemoteEndPoint as IPEndPoint)!;
                byte[] result = HandleData(dataBuffer, socketEndPoint.Address, socketEndPoint.Port);
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

        private byte[] HandleData(byte[] data, IPAddress address, int port)
        {
            Console.WriteLine(address);
            Console.WriteLine(port);

            string text = Encoding.ASCII.GetString(data);

            char c = text[0];

            if (c == 'D')
            {   
                data = data[1..];
                Console.WriteLine("DATA LENGTH IS " + data.Length);
                byte[] decryptBuffer = _rsa.Decrypt(data, false);
                text = Encoding.ASCII.GetString(decryptBuffer);
                Console.WriteLine(text);
            }
            else if (c == 'N')
                text = text[1..];
            else
            {
                Console.WriteLine("Invalid char code at index 0");
                return MSG_INVALID;
            }

            string instruction = text[0..4];
            Console.WriteLine(instruction);

            byte[] parameter = default!;
            int index = -1;

            if (data.Length > 6)
            {
                parameter = Encoding.ASCII.GetBytes(text[5..]);

                //TODO: MAKE THIS MORE EFFICIENT
                //THIS IS VERY SLOW, FIX THIS
                index = _currentClients.Keys.ToList().IndexOf(parameter);
            }

            byte[] returnValue = Array.Empty<byte>();

            switch (instruction)
            {
                // Authenticates a user anonymously
                case "AUTH":
               		Console.WriteLine(BRHasher.HashText("hello", "hello"));
					return Encoding.ASCII.GetBytes("hello");

                // Initialise a conversation
                // Return Socket information on success, otherwise return UNF (user not found)
                case "SENT":
                    Console.WriteLine(text);
                    message.Replace("SENT:", "").ToString();
                    Console.WriteLine(message);
                    bytemessage = Encoding.ASCII.GetBytes(message);
                    message = BRC2.DecodeText(bytemessage, message, 0);
                    return Encoding.ASCII.GetBytes($"{address}:{port} typed {message}");
                case "INIT":
                    Console.WriteLine(index);
                    
					// Temporary return

                    returnValue = Encoding.ASCII.GetBytes($"{address}:{port}");
                    break;
                // Agree on a symmetric key to use for further communication
                case "AGRE":
                    //TODO: This is bad, like really bad, please do something about it
                    // most likely vulnerable to IP spoofing :/

                    Console.WriteLine("PARAMETER LENGTH IS : " + parameter.Length);

                    bool hasPermission = false;

                    byte[] id = default!;
                    IPAddress dictionaryAddress = default!;
                    int dictionaryPort = default!;

                    foreach(var k in _currentClients)
                    {
                        if (k.Value.Item2.ToString() == address.ToString() && k.Value.Item3 == port)
                        {
                            Console.WriteLine(k.Value.Item2);
                            Console.WriteLine(address);
                            Console.WriteLine(k.Value.Item3);
                            Console.WriteLine(port);
                            hasPermission = true;
                            id = k.Key;
                            dictionaryAddress = k.Value.Item2;
                            dictionaryPort = k.Value.Item3;
                        }
                    }
                    
                    Console.WriteLine("PERMISSION : " + hasPermission);

                    if (hasPermission)
                        _currentClients[id] = (parameter, dictionaryAddress, dictionaryPort);

                    foreach( var v in _currentClients)
                    {
                        Console.WriteLine("KEY : " + v.Key);
                        Console.WriteLine("SYMMETRIC KEY : ");
                        for (int i = 0; i < v.Value.Item1.Length; i++)
                        {
                            Console.Write((char)v.Value.Item1[i]);
                        }
                        Console.WriteLine();
                    }
                    if (hasPermission)
                        // agree to key update
                        returnValue = MSG_AGREE;
                    else
                        // refuse key update
                        returnValue = MSG_REFUSE;

                    break;

                // Register on the server (anonymous registration)
                // Return a receive message
                case "REGG":
                    Console.WriteLine("REGISTERING");

                    _currentClients[parameter] = (null, address, port)!;

                    returnValue = MSG_RECEIVE;
                    break;
                // Returns the servers public key
                case "GKEY":
                    string xmlString = _rsa.ToXmlString(false);
                    return Encoding.ASCII.GetBytes(xmlString);

                default:
                    return MSG_INVALID;
            }

            return returnValue;
        }
    }
}