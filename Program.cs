namespace bardchat
{
    class Program
    {
        static int Main(string[] args)
        {
            Globals.serverIP = "127.0.0.1";
            Globals.serverPort = 5002;

            BRServer server = new BRServer(5);
            server.Start();
            Console.WriteLine("Started server!");

            BRClient client1 = new BRClient();
            client1.LoopConnect();
            client1.Send("hello from 1");

            BRClient client2 = new BRClient();
            client2.LoopConnect();
            client2.Send("hello from 2");

            Console.ReadKey();

            return 0;
        }
    }
}