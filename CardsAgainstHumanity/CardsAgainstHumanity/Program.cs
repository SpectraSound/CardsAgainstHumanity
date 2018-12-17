using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace CardsAgainstHumanity
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress[] localIP = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress addr in localIP)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    Console.WriteLine(addr);
                }
            }

            Console.WriteLine("Will you be player or Cardczar: 'P' or 'C' ");
            string answer = Console.ReadLine();

            switch (answer.ToLower())
            {
                case "c":
                    Console.Write("Enter Port: ");
                    CardCzar cardCzar = new CardCzar(int.Parse(Console.ReadLine()));
                    break;

                case "p":
                    Player player = new Player();
                    break;
                default:
                    break;
            }
        }
    }

    class CardCzar : TcpListener
    {
        public List<TcpClient> receivedClients = new List<TcpClient>();

        public string help =
                                "\n To send a message to everyone type <all:> followed by your message." +
                                "\n To send a message to a specific person type <client:> <INSERT PERSON IP:> followed by your message" +
                                "\n To show the IP address of all connected people type <show:> then press enter" +
                                "\n To open this prompt again type <help:> then press enter";



        public string GetClientIP(TcpClient client)
        {
            string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            return clientIP;
        }

        public CardCzar(int port) : base(IPAddress.Any, port)
        {
            this.Start();

            Console.WriteLine();
            Console.WriteLine("Awaiting client");

            AcceptClients(this);



            bool ConnectionOn = true;

            while (ConnectionOn)
            {
                Console.Write("\nYou: ");
                string text = Console.ReadLine();
                byte[] buffer = Encoding.UTF8.GetBytes(text);

                foreach (var client in receivedClients)
                {
                    try
                    {
                        if (client.Connected)
                        {
                            client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                    }
                    catch (Exception)
                    {
                        client.Close();
                        Console.WriteLine($"{GetClientIP(client)} Has left.");
                        receivedClients.Remove(client);
                    }

                }

            }
        }

        public async void AcceptClients(TcpListener listener)
        {
            bool isRunning = true;
            while (isRunning)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                receivedClients.Add(client);

                NetworkStream stream = client.GetStream();
                byte[] helpBytes = Encoding.UTF8.GetBytes(help);
                Console.WriteLine("You are connected");
                client.GetStream().Write(helpBytes, 0, helpBytes.Length);
                ReceiveMessages(stream, client);
            }
        }

        public async void ReceiveMessages(NetworkStream stream, TcpClient client)
        {
            bool isRunning = true;
            byte[] buffer = new byte[256];

            while (isRunning)
            {

                try
                {

                    int numberOfBytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, numberOfBytesRead);
                    string[] command = receivedMessage.ToLower().Split(':');
                    string shownMessage = receivedMessage.Substring(command[0].Length);
                    byte[] sendMessage = Encoding.UTF8.GetBytes($"\n{GetClientIP(client)}: \n{shownMessage}");
                    switch (command[0])
                    {
                        case "all":
                            Console.WriteLine($"\n{GetClientIP(client)}: {receivedMessage}");
                            foreach (var person in receivedClients)
                            {
                                person.GetStream().Write(sendMessage, 0, sendMessage.Length);
                            }
                            break;

                        case "client":
                            Console.WriteLine($"\n{GetClientIP(client)}: {receivedMessage}");
                            foreach (var person in receivedClients)
                            {
                                if (command[1] == ((IPEndPoint)person.Client.RemoteEndPoint).Address.ToString())
                                {

                                    person.GetStream().Write(sendMessage, 0, sendMessage.Length);
                                }
                            }
                            break;

                        case "show":
                            Console.WriteLine($"\n{GetClientIP(client)}: {receivedMessage}");
                            foreach (var person in receivedClients)
                            {
                                byte[] personIP = Encoding.UTF8.GetBytes(GetClientIP(person));
                                client.GetStream().Write(personIP, 0, personIP.Length);
                            }
                            break;

                        case "help":
                            byte[] helpBytes = Encoding.UTF8.GetBytes(help);
                            client.GetStream().Write(helpBytes, 0, helpBytes.Length);

                            break;

                        default:
                            Console.WriteLine($"\n{GetClientIP(client)}: {receivedMessage}");
                            break;
                    }


                }
                catch (Exception)
                {
                    Console.WriteLine($"{GetClientIP(client)} has left");
                    byte[] clientLeft = Encoding.UTF8.GetBytes($"{GetClientIP(client)} has left");
                    foreach (var person in receivedClients)
                    {
                        person.GetStream().Write(clientLeft, 0, clientLeft.Length);
                    }
                    client.Close();
                    receivedClients.Remove(client);
                    break;
                }

            }
        }
    }



    class Player : TcpClient
    {
        public Player()
        {
            Console.Write("Enter Destination IP: ");
            IPAddress ip = IPAddress.Parse(Console.ReadLine());
            Console.Write("Enter port: ");
            int port = int.Parse(Console.ReadLine());
            IPEndPoint destinationEndPoint = new IPEndPoint(ip, port);

            this.Connect(destinationEndPoint);
            NetworkStream stream = this.GetStream();
            Console.WriteLine("You are connected");

            bool connectionOn = true;

            while (connectionOn)
            {

                ReceiveMessage(stream);
                Console.Write("You: ");
                string text = Console.ReadLine();
                byte[] buffer = Encoding.UTF8.GetBytes(text);

                stream.Write(buffer, 0, buffer.Length);

                if (text.ToLower() == "break")
                {
                    connectionOn = false;
                }

            }
            stream.Close();
            this.Close();

        }

        public async void ReceiveMessage(NetworkStream stream)
        {
            byte[] buffer = new byte[256];
            int numberOfBytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, numberOfBytesRead);
            Console.WriteLine($"\nServer: {receivedMessage}");
        }
    }
}
