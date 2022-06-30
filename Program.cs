namespace bardchat
{
    class Program
    {
        private byte[] bytemessage;
        static int Main(string[] args)
        {
            Console.WriteLine("Client or server? (c or s)");
            string socketType_i = Console.ReadLine()!;

            Client client;
            Server server;

            Globals.serverPort = 5003;

            if (socketType_i == "c")
            {
                Console.WriteLine("Enter server IP");
                Globals.serverIP = Console.ReadLine()!;

                Console.WriteLine("Enter server port");
                string serverPort_i = Console.ReadLine()!;
                Globals.serverPort = Convert.ToUInt16(serverPort_i);

                client = new Client();

                client.LoopConnect();
                Console.WriteLine("Enter username:");
                string username = Console.ReadLine();
                do
                {
                    enterRequest:
                    Console.WriteLine("Enter request for server");
                    string request_i = Console.ReadLine()!;
                    if (request_i == "SEND")
                    {
                        Console.WriteLine("Enter ip address.");
                        string inputtedAddress = Console.ReadLine();
                        /*
                        if (!_currentClients.Contains(inputtedAddress))
                        {
                            Console.WriteLine("IP address is incorrect or not connected.");
                            goto enterRequest;
                        }
                        else
                        {
                        */
                            Console.WriteLine("IP Address is valid. Enter in message:");
                            string message = Console.ReadLine();
                            Console.WriteLine("Encrypted message:");
                            Console.WriteLine(BRHasher.HashText(message, message));
                            Console.WriteLine("Decrypted message:");
                            Console.WriteLine(message);
                            //client?.Send(username, "SENT:"+BRHasher.HashText(message, message).ToString());
                            bytemessage = Encoding.ASCII.GetBytes(message);
                            client?.Send("SENT:"+BRC2.EncodeText(bytemessage, message, 0));
                            goto enterRequest;
                        //}
                    }
                    client?.Send(request_i);
                }
                while
                    (true);
            }
            else if (socketType_i == "s")
            {
                Console.WriteLine("Starting server...");
                server = new Server(5);
                Console.WriteLine(Globals.serverPort);
                server.Start();
                Console.WriteLine("Server started.");
            }
            else
            {
                Console.WriteLine("That is not a valid input");
            }

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