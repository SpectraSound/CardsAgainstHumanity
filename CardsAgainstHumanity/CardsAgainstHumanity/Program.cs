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

        public int pointsToWin;
        public bool gameStart = false;

        public CardCzar(int port) : base(IPAddress.Any, port)
        {
            this.Start();

           
            Console.WriteLine("Awaiting clients");

            AcceptClients(this);

            SetupGame();

            while (gameStart)
            {
                string input = Console.ReadLine();

                SendMessages(input);

            }
        }


        public string ChooseBlackCard()
        {
            IEnumerable<string> blackCards = File.ReadLines(@"C:\Users\SpectraSound\source\repos\CardsAgainstHumanity\BlackCards.txt");
            List<string> randomBlackCards = new List<string>();
            foreach (var blackCard in blackCards)
            {
                randomBlackCards.Add(blackCard);
            }
            Random selectCard = new Random();
            int chosenCard = selectCard.Next(0, randomBlackCards.Count);
            return randomBlackCards[chosenCard];
        }


        public string ChooseWhiteCards()
        {
            int chosenCard;
            string chosenCards = "";
            IEnumerable<string> whiteCards = File.ReadLines(@"C:\Users\SpectraSound\source\repos\CardsAgainstHumanity\WhiteCards.txt");
            List<string> randomWhiteCards = new List<string>();
            foreach (var whiteCard in whiteCards)
            {
                randomWhiteCards.Add(whiteCard);
            }
            Random selectCard = new Random();
            
            for (int i = 0; i < 5; i++)
            {
                chosenCard = selectCard.Next(0, randomWhiteCards.Count);
                chosenCards += $"<{randomWhiteCards[chosenCard]}";
            }
            return chosenCards;
        }


        public async void AcceptClients(TcpListener listener)
        {
            bool isRunning = true;
            while (isRunning)
            {

                TcpClient client = await listener.AcceptTcpClientAsync();

                if (gameStart)
                {
                    NetworkStream disconnectStream = client.GetStream();
                    string disconnectMessage = "\nWe're sorry but a game is already in progress try connecting later.";
                    byte[] disconnectMessageBytes = Encoding.UTF8.GetBytes(disconnectMessage);
                    disconnectStream.Write(disconnectMessageBytes, 0, disconnectMessageBytes.Length);
                    client.Close();
                }
                
                    NetworkStream stream = client.GetStream();
                    
                    Console.WriteLine("You are connected");
                    receivedClients.Add(client);
                    Console.WriteLine(receivedClients.Count);

                    ReceiveMessages(stream, client);
                

            }
        }


        public async void ReceiveMessages(NetworkStream stream, TcpClient client)
        {
            bool isRunning = true;
            byte[] buffer = new byte[256];
            List<string> chosenWhiteCards = new List<string>();

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
                            help(client);

                            break;

                        case "choose":
                            chosenWhiteCards.Add(shownMessage);
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


        public string GetClientIP(TcpClient client)
        {
            string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            return clientIP;
        }


        public void help(TcpClient client)
        {
            string help = "\n To send a message to everyone type <all:> followed by your message." +
            "\n To send a message to a specific person type <client:> <INSERT PERSON IP:> followed by your message" +
            "\n To show the IP address of all connected people type <show:> then press enter" +
            "\n To open this prompt again type <help:> then press enter";

            byte[] helpBuffer = Encoding.UTF8.GetBytes(help);

            client.GetStream().Write(helpBuffer, 0, helpBuffer.Length);

        }


        public void SetupGame()
        {
            Console.WriteLine();
            Console.WriteLine("Choose how many points you need to win:");
            pointsToWin = int.Parse(Console.ReadLine());
            Console.WriteLine();

            Console.WriteLine("start game?");
            string startGame = Console.ReadLine();

            if (startGame.ToLower() == "yes")
            {
                gameStart = true;
            }
        }


        public void SendMessages(string input)
        {
            switch (input.Trim().ToLower())
            {
                case "next round":
                    string blackCard = $"\nThe Chosen Black Card Is\n\n{ChooseBlackCard()}\n";
                    string whiteCards = ChooseWhiteCards();
                    Console.WriteLine();
                    Console.WriteLine(blackCard);

                    byte[] blackCardBytes = Encoding.UTF8.GetBytes(blackCard);
                    byte[] whiteCardsBytes = Encoding.UTF8.GetBytes(whiteCards);

                    foreach (var client in receivedClients)
                    {
                        try
                        {
                            if (client.Connected)
                            {
                                client.GetStream().Write(blackCardBytes, 0, blackCardBytes.Length);
                                client.GetStream().Write(whiteCardsBytes, 0, whiteCardsBytes.Length);
                            }
                        }
                        catch (Exception)
                        {
                            client.Close();
                            Console.WriteLine($"{GetClientIP(client)} Has left.");
                            receivedClients.Remove(client);
                        }
                    }
                    break;



                default:
                    byte[] buffer = Encoding.UTF8.GetBytes(input);

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
                    break;
            }
        }
    }



    class Player : TcpClient
    {
        public string[] whiteCards;
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
                string[] whiteCard = text.Split(':'); 
                byte[] buffer = Encoding.UTF8.GetBytes(text);

                switch (whiteCard[0].ToLower())
                {
                    case "choose:":
                        byte[] chosenCard = Encoding.UTF8.GetBytes($"choose:{whiteCards[int.Parse(whiteCard[1])]}");
                        stream.Write(chosenCard, 0, chosenCard.Length);
                        break;

                    default:
                        stream.Write(buffer, 0, buffer.Length);
                        break;
                }

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
            whiteCards = receivedMessage.Split('<');
            Console.WriteLine($"\nCarCzar: \n{receivedMessage}");
            Console.WriteLine();
            foreach (var whiteCard in whiteCards)
            {
                Console.WriteLine(whiteCard);
            }
        }
    }
}
