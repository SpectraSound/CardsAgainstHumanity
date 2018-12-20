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
        public List<string> chosenWhiteCards = new List<string>();
        public Dictionary<int,string> clientAndCard = new Dictionary<int,string>();
        public List<int> playerPoints = new List<int>();

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
            
            

            while (isRunning)
            {

                try
                {

                    int numberOfBytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, numberOfBytesRead);
                    string[] command = receivedMessage.ToLower().Split(':');
                    string shownMessage = receivedMessage.Substring(command[0].Length);
                    byte[] sentMessage = Encoding.UTF8.GetBytes($"\n{GetClientIP(client)}: \n{shownMessage}");
                    switch (command[0])
                    {
                        case "all":
                            Console.WriteLine($"\n{GetClientIP(client)}: {receivedMessage}");
                            foreach (var person in receivedClients)
                            {
                                person.GetStream().Write(sentMessage, 0, sentMessage.Length);
                            }
                            break;

                        case "client":
                            Console.WriteLine($"\n{GetClientIP(client)}: {receivedMessage}");
                            foreach (var person in receivedClients)
                            {
                                if (command[1] == ((IPEndPoint)person.Client.RemoteEndPoint).Address.ToString())
                                {

                                    person.GetStream().Write(sentMessage, 0, sentMessage.Length);
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

                            clientAndCard.Add(receivedClients.IndexOf(client), shownMessage);
                            chosenWhiteCards.Add(shownMessage);
                            if (clientAndCard.Count < receivedClients.Count)
                            {
                                Console.WriteLine("\n Received an answer\nWait for all white cards to be chosen\n");
                            }
                            if (clientAndCard.Count == receivedClients.Count)
                            {
                                Console.WriteLine("\nAll white cards have been chosen\n");
                                ChosenWhiteCards(chosenWhiteCards);
                            }
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
                for (int player = 0; player < receivedClients.Count; player++)
                {
                    playerPoints.Add(0);
                    string playerNumber = $"\n You are player {player}\n";
                    byte[] playerNumberBytes = Encoding.UTF8.GetBytes(playerNumber);
                    receivedClients[player].GetStream().Write(playerNumberBytes, 0, playerNumberBytes.Length);
                }
                

                
                gameStart = true;
            }
        }


        public void SendMessages(string input)
        {
            switch (input.Trim().ToLower())
            {
                case "next round":
                    string blackCard = $"\nThe Chosen Black Card Is\n\n{ChooseBlackCard()}\n";
                    
                    Console.WriteLine();
                    Console.WriteLine(blackCard);

                    byte[] blackCardBytes = Encoding.UTF8.GetBytes(blackCard);


                    for (int client = 0; client < receivedClients.Count; client++)
                    {
                            if (receivedClients[client].Connected)
                            {
                                string whiteCards = ChooseWhiteCards();
                                byte[] whiteCardsBytes = Encoding.UTF8.GetBytes(whiteCards);
                                receivedClients[client].GetStream().Write(blackCardBytes, 0, blackCardBytes.Length);
                                receivedClients[client].GetStream().Write(whiteCardsBytes, 0, whiteCardsBytes.Length);
                            }
                    }
                    break;

                case "choose card":
                    foreach (var whiteCard in chosenWhiteCards)
                    {
                        Console.WriteLine($"At index {chosenWhiteCards.IndexOf(whiteCard)}:  {whiteCard}");
                    }
                    Console.WriteLine();
                    Console.WriteLine("Choose a card index:");
                    int chosenIndex = int.Parse(Console.ReadLine());
                    string scoreBoard = "\nScoreBoard:\n";

                    foreach (var player in clientAndCard)
                    {
                        if (clientAndCard.ContainsValue(chosenWhiteCards[chosenIndex]) && player.Value == chosenWhiteCards[chosenIndex])
                        {
                            playerPoints[player.Key] += 1;
                            foreach (var client in receivedClients)
                            {
                                string pointMessage = $"player {player.Key} got the point";
                                byte[] pointMessageBytes = Encoding.UTF8.GetBytes(pointMessage);
                                client.GetStream().Write(pointMessageBytes, 0, pointMessageBytes.Length);
                            }
                        }

                        for (int client = 0; client < receivedClients.Count; client++)
                        {
                            for (int i = 0; i < receivedClients.Count - 1; i++)
                            {
                                scoreBoard += $"\nPlayer {client} Has:  {playerPoints[client]} points";

                            }
                            byte[] scoreBoardBytes = Encoding.UTF8.GetBytes(scoreBoard);
                            receivedClients[client].GetStream().Write(scoreBoardBytes, 0, scoreBoardBytes.Length);
                        }
                        

                        

                        if (playerPoints[player.Key] == pointsToWin)
                        {
                            foreach (var client in receivedClients)
                            {
                                string gameOver = $"\nPlayer {player.Key} has won the game!!!!";
                                byte[] gameOverBytes = Encoding.UTF8.GetBytes(gameOver);
                                client.GetStream().Write(gameOverBytes, 0, gameOverBytes.Length);
                            }
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


        public List<string> ChosenWhiteCards(List<string> whiteCards)
        {
            List<string> chosenWhiteCards = whiteCards;
            return chosenWhiteCards;
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
                byte[] buffer = Encoding.UTF8.GetBytes(text);

                switch (text.ToLower())
                {
                    case "choose":
                        ChooseWhiteCard(whiteCards, stream);
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
            byte[] buffer = new byte[2560];
            int numberOfBytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, numberOfBytesRead);
            whiteCards = receivedMessage.Split('<');
            Console.WriteLine($"\nCarCzar: \n{receivedMessage}");
            Console.WriteLine();
            for (int whiteCard = 0; whiteCard < whiteCards.Length; whiteCard++)
            {
                Console.WriteLine($"nr {whiteCard}:{whiteCards[whiteCard]}");
            }
            
                
            
        }

        public void ChooseWhiteCard(string[] whiteCards, NetworkStream stream)
        {
            Console.WriteLine("Type the number of the card:\n");
            int index = int.Parse(Console.ReadLine());
            string chosenCard = $"choose:{whiteCards[index]}";
            byte[] chosenCardBytes = Encoding.UTF8.GetBytes(chosenCard);
            stream.Write(chosenCardBytes, 0, chosenCardBytes.Length);
        }
    }
}
