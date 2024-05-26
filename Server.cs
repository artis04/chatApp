using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Threading;

public class ConnectedClient
{
    public IPAddress Ip { get; }    // User ip address
    public int ReceivePort { get; } // Port on which user received data
    public int SendPort { get; }    // Port on which user sends data
    public string Username { get; set; }    // Users username
    public string Colour { get; }   // Users chat colour

    public ConnectedClient(IPAddress ip, int senPort, int recPort, string username, string colour)
    {
        this.Ip = ip;
        this.ReceivePort = recPort;
        this.SendPort = senPort;
        this.Username = username;
        this.Colour = colour;
    }
}

public class Server
{
    private static string colourId = "0";
    static List<ConnectedClient> clientList = new List<ConnectedClient>();
    private static List<ServerAcknowledgmentItem> acknowledgmentList = new List<ServerAcknowledgmentItem>();
    private static int serverPort = 12345;

    static async Task StartUdpListener()
    {
        Console.WriteLine($"UDP Listener started on port {serverPort}");

        var udpClient = new UdpClient(serverPort);
        while (true)
        {
            var receivedResults = await udpClient.ReceiveAsync();
            string receivedMessage = Encoding.ASCII.GetString(receivedResults.Buffer);
            var endPoint = receivedResults.RemoteEndPoint;
            // Console.WriteLine(receivedMessage);

            if (receivedMessage.StartsWith(@"$\u-conn"))
            {
                colourText("Received connection request", "darkgreen");

                int portLength = receivedMessage.Substring(9).IndexOf("-");
                int receivingPort = Int32.Parse(receivedMessage.Substring(9, portLength));
                string username = receivedMessage.Substring(10+portLength);

                ConnectedClient userRequest = get_client(endPoint.Address, endPoint.Port);
                string userColour = "";
                if (userRequest == null) {
                    userColour = colourString(colourId);
                    ConnectedClient newClient = new ConnectedClient(endPoint.Address, endPoint.Port, receivingPort, username, userColour);
                    clientList.Add(newClient);   
                    colourId = (Int32.Parse(colourId) + 1).ToString();
                } else {
                    // Change the username
                    foreach (var client in clientList)
                    {
                        if (client.Ip.Equals(endPoint.Address) && client.SendPort.Equals(endPoint.Port))
                        {
                            client.Username = username;
                            userColour = client.Colour;
                        }
                    }
                }

                Byte[] messageBytes = Encoding.ASCII.GetBytes(@"$\u-anss" + userColour);   
                // await udpClient.SendAsync(messageBytes, messageBytes.Length, clientEndPoint);  
                _ = sendUdp(endPoint.Address.ToString(), endPoint.Port, messageBytes);
                
                colourText($"Accepted incoming request. Added user [{username}]", "green");
            }
            else if (receivedMessage.StartsWith(@"\U-mid-"))  // New message received
            {
                string messageId = receivedMessage.Substring(7, 36);
                string messageContent = receivedMessage.Substring(44);
                ConnectedClient receivedClient = get_client(endPoint.Address, endPoint.Port);
                if (receivedClient == null)
                {
                    continue; // User might have been discarded
                }
                string message_username = receivedClient.Username;
                string databaseResponse = await Database.append_message(messageContent, message_username, messageId);
                Byte[] messageBytes = Encoding.ASCII.GetBytes(@"$\u-ack-message-" + messageId);   

                _ = sendUdp(endPoint.Address.ToString(), endPoint.Port, messageBytes);

                if (databaseResponse == "messageExists") {  // Repeated message, sending confirmation!
                    // var clientEndPoint = new IPEndPoint(endPoint.Address, endPoint.Port);
                    
                    // udpClient.Send(messageBytes, messageBytes.Length, clientEndPoint);

                }
                else
                {
                    // Send message to everyone on the client list
                    colourText($"Received chat message ({messageId})", "yellow");

                    string messageToSend = $"{receivedClient.Colour}[{message_username}] - {messageContent}\u001b[0m";
                    messageToSend = $"\\U-reid-{messageId}-{messageToSend}";
                    

                    byte[] messageToSendBytes = Encoding.ASCII.GetBytes(messageToSend);
                    foreach (var client in clientList)
                    {

                        ServerAcknowledgmentItem clientAcknowledgmentItem = new ServerAcknowledgmentItem(messageId, messageToSendBytes, client.Ip.ToString(), client.SendPort, client.ReceivePort);
                        acknowledgmentList.Add(clientAcknowledgmentItem);
                    }
                }
            }
            else if (receivedMessage.StartsWith(@"\u-REC-ack-"))
            {
                string ackMessageId = receivedMessage.Substring(11, 36);
                ackDestroy(ackMessageId, endPoint.Address.ToString(), endPoint.Port);
            }
            
        }
        
        // }
    }

    private static void ackDestroy(string id, string clientIp, int clientPort)
    {
        foreach(var acknowledgmentItem in acknowledgmentList)
        {
            // Console.WriteLine(acknowledgmentItem.ClientIp + " : " + clientIp);
            // Console.WriteLine(acknowledgmentItem.SendingPort + " : " + clientPort);
            // Console.WriteLine(acknowledgmentItem.Id + " : " + id);

            if (acknowledgmentItem.ClientIp.Equals(clientIp) && 
                acknowledgmentItem.SendingPort.Equals(clientPort) && 
                acknowledgmentItem.Id.Equals(id))
            {
                acknowledgmentItem.Dispose();
                acknowledgmentList.Remove(acknowledgmentItem);
                break;
            }
        }
    }

    private static ConnectedClient get_client(IPAddress ipAddress, int port) {
        foreach (var client in clientList) {
            
            if (client.Ip.Equals(ipAddress) && client.SendPort == port)
            {
                return client;
            }
        }
        return null;
    }

    public static void Main1()
    {
        
        Database.initialise_db();
        Task listener = StartUdpListener();
        // StartUdpListener().Wait();
        while(true)
        {}
    }

    private static void colourText(string text, string colour) {
        string colourCode = colourString(colour);
        Console.Write(colourCode);
        Console.Write(text);
        Console.WriteLine("\u001b[0m");
    }

    private static string colourString(string colour) {
        string colourCode = "";
        switch(colour) {
            case "red":
            case "0":
                colourCode = "\u001b[31m";
                break;
            
            case "green":
            case "1":
                colourCode= "\u001b[32m";
                break;

            case "yellow":
            case "2":
                colourCode = "\u001b[33m";
                break;

            case "blue":
            case "3":
                colourCode = "\u001b[34m";
                break;

            case "magenta":
            case "4":
                colourCode = "\u001b[35m";
                break;

            case "cyan":
            case "5":
                colourCode = "\u001b[36m";
                break;

            case "white":
            case "6":
                colourCode = "\u001b[37m";
                break;

            case "darkgreen":
                colourCode = "\u001b[38;5;28m";
                break;

        }

        return colourCode;
    }

    private static async Task sendUdp(string ipAddress, int endPointPort, Byte[] bytes) 
    {
        // Console.WriteLine("SENDING OUT: " + Encoding.ASCII.GetString(bytes));
        using UdpClient udpClient = new UdpClient();
        var client = get_client(IPAddress.Parse(ipAddress), endPointPort);
        // Console.WriteLine("Connection udp sent to: " + ipAddress + ":" + client.ReceivePort);
        try 
        {
            await udpClient.SendAsync(bytes, bytes.Length, ipAddress, client.ReceivePort);

        }
        catch (Exception ex)
        {
            colourText("Error sending UDP packet: " + ex.Message, "red");
        }
    }

    public class ServerAcknowledgmentItem
    {
        private bool disposed = false;
        public string Id { get; }
        public int tries { get; set; } = 0;
        private static int timeout { get; } = 10000;
        private Byte[] MessageBytes { get; }

        public string ClientIp { get; }
        public int SendingPort { get; }
        public int ReceivingPort { get; }

        private System.Timers.Timer timer;

        public ServerAcknowledgmentItem(string ackId, Byte[] message, string clientIp, int sendingPort, int receivingPort)
        {
            this.Id = ackId;
            this.MessageBytes = message;
            this.SendingPort = sendingPort;
            this.ReceivingPort = receivingPort;
            this.ClientIp = clientIp;
            
            _ = sendUdp(this.ClientIp, this.SendingPort, MessageBytes);

            this.timer = new System.Timers.Timer(timeout);
            this.timer.Elapsed += OnTimerElapsed;
            this.timer.AutoReset = true;
            this.timer.Enabled = true;

        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _ = sendUdp(this.ClientIp, this.SendingPort, MessageBytes);
            this.increaseTry();
        }

        private void increaseTry() {
            tries++;
            // Dispose itself?

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if(disposing)
                {
                    this.timer.Enabled = false;
                }

                disposed = true;
            }
        }

        // Destructor 
        ~ServerAcknowledgmentItem()
        {
            Dispose(false);
        }
    }
}

