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

            Globals.serverPort = 5003;

            if (socketType_i == "c")
            {
                Console.WriteLine("Enter server IP: ");
                Globals.serverIP = Console.ReadLine()!;

                Console.WriteLine("Enter server port: ");
                string serverPort_i = Console.ReadLine()!;
                Globals.serverPort = Convert.ToUInt16(serverPort_i);

                client = new Client();

                client.LoopConnect();

                do
                {
                    Console.WriteLine("Enter data to send to the server: ");
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
                Console.WriteLine("Server port: " + Globals.serverPort);
                server.Start();
                Console.WriteLine("Server started");
            }
            else
                Console.WriteLine("Invalid option");

            Console.ReadLine();

            return 0;

            /*Contact contact1 = new Contact(Guid.NewGuid());
            Contact contact2 = new Contact(Guid.NewGuid());

            Client client = new Client();
            client.GenerateNewKey();
            Console.WriteLine("CLIENTS KEY IS " + Convert.ToBase64String(client.key));

            Chat chat = new Chat();
            chat.name = "badlands";
            chat.AddMember(contact1);
            chat.AddMember(contact2);

            Chat chat2 = new Chat();
            chat2.name = "the chat";
            chat2.AddMember(contact1);
            chat2.AddMember(contact2);
            chat2.AddMember(new Contact(Guid.NewGuid()));

            client.AddChat(chat);
            client.AddChat(chat2);

            Console.WriteLine("Addded chat to client with 2 members");

            for (int i = 0; i < client.chats.Count; i++)
            {
                Console.WriteLine("CHAT " + client.chats[i].name);

                foreach (var c in client.chats[i].members)
                {
                    Console.WriteLine(c.Key);
                }
            }

            return 0;*/
        }
    }
}