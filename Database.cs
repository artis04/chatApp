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

class Database
{
    private static string connectionString = "Data Source=database.db";
    public static void initialise_db() 
    {

        // Create a connection to the database
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            // Create a command to execute SQL queries
            using (var command = connection.CreateCommand())
            {
                // Example query: create a table
                command.CommandText = "CREATE TABLE IF NOT EXISTS chat (Id INTEGER PRIMARY KEY, username TEXT, message TEXT, messageId TEXT)";
                command.ExecuteNonQuery();

            }
            connection.Close();
        }

    }

    public static async Task<string> append_message(string message, string username, string messageId) {
        using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();

            using (var transaction = connection.BeginTransaction())
            {

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT id FROM chat WHERE messageId=@messageId";
                    command.Parameters.AddWithValue("@messageId", messageId);

                    using (var reader = command.ExecuteReader())
                    {
                        // Console.WriteLine("NOW:");
                        // Console.WriteLine(reader.Read());
                        if (reader.Read())
                        {
                            return "messageExists";
                        }
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO chat (username, message, messageId) VALUES (@username, @message, @messageId)";
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@message", message);
                    command.Parameters.AddWithValue("@messageId", messageId);
                    await command.ExecuteNonQueryAsync();
                    
                }

                transaction.Commit();
            }
            

            await connection.CloseAsync();
        }

        // return lastInsertedId;
        return "inserted";
    }

}

// Example query: insert data into the table
                    // command.CommandText = "INSERT INTO MyTable (Name) VALUES ('John')";
                    // command.ExecuteNonQuery();

                    // Example query: select data from the table
                    // command.CommandText = "SELECT * FROM MyTable";
                    // using (var reader = command.ExecuteReader())
                    // {
                    //     while (reader.Read())
                    //     {
                    //         int id = reader.GetInt32(0); // Assuming the first column is Id
                    //         string name = reader.GetString(1); // Assuming the second column is Name
                    //         Console.WriteLine($"Id: {id}, Name: {name}");
                    //     }
                    // }