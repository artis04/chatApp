using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq; // Enumerable
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Timers;

public class Client
{
    private static int sentMessages = 0;
    private static string message = "";
    private static string clientColour = "";
    private static bool connected = false;
    private static List<string> displayedMessages = new List<string>();
    private static List<AcknowledgmentItem> acknowledgmentList = new List<AcknowledgmentItem>();
    private static string serverIp = "127.0.0.1";
    private static int serverPort = 12345;
    private static int clientReceivingPort;
    private static int clientSendingPort = 0;

    public static Task Main1()
    {
        Task listener = ReceiveMessages();

        Console.Write("Enter your username: ");
        string username = Console.ReadLine();

        string connectMessage = $"$\\u-conn-{clientReceivingPort}-{username}";
        Byte[] connectMessageBytes = Encoding.ASCII.GetBytes(connectMessage);

        AcknowledgmentItem connectionAcknowledgment = new AcknowledgmentItem("connect", connectMessageBytes);
        acknowledgmentList.Add(connectionAcknowledgment);
        
        while (!connected) {}

        Console.Write("\n\n\n\n");
        Console.Write("Message: ");

        while (true)
        {
            Console.Write(clientColour);
            ConsoleKeyInfo keyinfo = Console.ReadKey();

            if (keyinfo.Key.ToString() == "Backspace") {
                Console.Write(" ");
                Console.SetCursorPosition(Console.CursorLeft-1, Console.CursorTop);
                continue;
            } else if (keyinfo.Key.ToString() != "Enter") {
                message += keyinfo.KeyChar;
                continue;
            }

            Console.Write("\u001b[0m");

            Console.Write(new string(' ', Console.WindowWidth));

            if (!string.IsNullOrEmpty(message))
            {
                string messageId = Guid.NewGuid().ToString();
                string messageToSend = $"\\U-mid-{messageId}-{message}";
                Byte[] messageBytes = Encoding.ASCII.GetBytes(messageToSend);

                AcknowledgmentItem messageAcknowledgment = new AcknowledgmentItem(messageId, messageBytes);
                acknowledgmentList.Add(messageAcknowledgment);

                message = "";
            }
        }
    }

    static async Task ReceiveMessages()
    {
        var udpClient = new UdpClient(0);
        clientReceivingPort = ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;
        Console.WriteLine($"UDP Listener started on port {clientReceivingPort}");

        while (true)
        {
            var receivedBytes = await udpClient.ReceiveAsync();

            string receivedMessage = Encoding.ASCII.GetString(receivedBytes.Buffer);
            // Console.WriteLine("Received data: " + receivedMessage);

            if (!connected)
            {
                if (receivedMessage.StartsWith(@"$\u-anss")) {

                    ackDestroy("connect");

                    Console.Write("\u001b[32mConnection was established!\u001b[0m");
                    clientColour = receivedMessage.Substring(8);
                    connected = true;

                }
            }
            else
            {
                if (receivedMessage.StartsWith("$\\u-ack-message-"))
                {
                    string messageId = receivedMessage.Substring(16);
                    ackDestroy(messageId);
                }
                else if (receivedMessage.StartsWith(@"\U-reid-")) 
                {
                    string receivingMessageId = receivedMessage.Substring(8, 36);
                    string receivedMessageContent = receivedMessage.Substring(45);

                    string messageToSend = $"\\u-REC-ack-{receivingMessageId}";
                    Byte[] messageBytes = Encoding.ASCII.GetBytes(messageToSend);
                    _ = sendUdp(serverIp, serverPort, messageBytes);

                    // udpClient.Send(messageBytes, messageBytes.Length, endPoint);

                    if (displayedMessages.Contains(receivingMessageId)) {
                        // Send response, do nothing
                    }
                    else
                    {
                        // Send response, update dsplayedMessages list, update chat

                        displayedMessages.Add(receivingMessageId);

                        Console.SetCursorPosition(0, Console.CursorTop-2);
                        Console.Write($"{receivedMessageContent}");


                        Console.SetCursorPosition(0, Console.CursorTop + 1);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, Console.CursorTop + 1);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.Write($"\n\u001b[0mMessage: {clientColour}{message}");
                    }

                }
            }
        }
    }

    private static void ackDestroy(string id)
    {
        int index = 0;
        foreach(var acknowledgmentItem in acknowledgmentList)
        {
            index ++;
            if (acknowledgmentItem.Id.Equals(id)){
                acknowledgmentItem.Dispose();
                acknowledgmentList.Remove(acknowledgmentItem);
                break;
            }
        }
    }
    private static async Task sendUdp(string ipAddress, int port, Byte[] bytes) 
    {
        // sentMessages ++;
        // if (sentMessages <=2) {
        //     return;
        // }
        // sentMessages = 0;
        
        UdpClient udpClient;
        if (clientSendingPort == 0) {
            udpClient = new UdpClient(0);
            clientSendingPort = ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;
        } else {
            udpClient = new UdpClient(clientSendingPort);
        }
        
        try 
        {
            await udpClient.SendAsync(bytes, bytes.Length, ipAddress, port);

        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending UDP packet: " + ex.Message);
        }
        udpClient.Close();
    }

    public class AcknowledgmentItem : IDisposable
    {
        private bool disposed = false;
        public string Id { get; }
        public int tries { get; set; } = 0;
        private Byte[] MessageBytes { get; }

        private System.Timers.Timer timer;

        public AcknowledgmentItem(string id, Byte[] message)
        {
            Id = id;
            MessageBytes = message;

            _ = sendUdp(serverIp, serverPort, MessageBytes);

            this.timer = new System.Timers.Timer(5000);
            this.timer.Elapsed += OnTimerElapsed;
            this.timer.AutoReset = true;
            this.timer.Enabled = true;

        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _ = sendUdp(serverIp, serverPort, MessageBytes);
            this.increaseTry();
        }

        private void increaseTry() {
            this.tries++;
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
        ~AcknowledgmentItem()
        {
            Dispose(false);
        }


    }

}
