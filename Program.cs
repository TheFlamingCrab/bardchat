namespace bardchat
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("Client or server? (c or s)");
            string socketType_i = Console.ReadLine()!;

            Client client;
            Server server;

            if (socketType_i == "c")
            {
                Console.WriteLine("Enter server IP");
                Globals.serverIP = Console.ReadLine()!;

                Console.WriteLine("Enter server port");
                string serverPort_i = Console.ReadLine()!;
                Globals.serverPort = Convert.ToUInt16(serverPort_i);

                client = new Client();

                do
                {
                    Console.WriteLine("Enter request for server");
                    string request_i = Console.ReadLine()!;

                    client?.Send(request_i);
                }
                while 
                    (true);
            }
            else if (socketType_i == "s")
            {
                Console.WriteLine("Starting server...");
                server = new Server(5);
                server.Start();
                Console.WriteLine("Server started.");
            }
            else
            {
                Console.WriteLine("That is not a valid input");
            }

            Console.ReadLine();

            return 0;
        }
    }
}