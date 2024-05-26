using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

using System.Linq; // Enumerable
using System.Threading;
using System.Threading.Tasks;
// Database
// using Miscrosoft.Data.Sqlite;

using Microsoft.Data.Sqlite;

namespace csharpudp
{
    class Program
    {
        // private static int initial_pos_y = 1;
        // private string server_ip = "87.110.174.25";
        static void Main(string[] args)
        {
            string identificator = Environment.GetEnvironmentVariable("id");  // 1 - Receiver, 2 - Sender

            if (identificator == "1") {
                _ = Client.Main1();

                return;
            }


            // Server side  
            Server.Main1();

           
        }


        public static void colourText(string text, string colour) {
            string colourCode = "";
            switch(colour) {
                case "red":
                    colourCode = "\u001b[31m";
                    break;
                
                case "green":
                    colourCode= "\u001b[32m";
                    break;

                case "yellow":
                    colourCode = "\u001b[33m";
                    break;

                case "blue":
                    colourCode = "\u001b[34m";
                    break;

                case "magenta":
                    colourCode = "\u001b[35m";
                    break;

                case "cyan":
                    colourCode = "\u001b[36m";
                    break;

                case "white":
                    colourCode = "\u001b[37m";
                    break;

                case "darkgreen":
                    colourCode = "\u001b[38;5;28m";
                    break;

            }

            Console.Write(colourCode);
            Console.Write(text);
            Console.WriteLine("\u001b[0m");
        }
    }
}