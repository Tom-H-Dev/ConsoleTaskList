using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;

namespace CommandTaskList
{

    //TODO: Use try catch to see check if something is broken or not
    /*
        For each user create new table named the username.
        For login get table data from username.
        Disable usernames that contain a list of banned wordes like DROP, DELETE etc.
     
     
     */


    class Program
    {
        private static bool logedin = false;
        static string connString = "Host=localhost;Username=postgres;Password=12345;Database=postgres";

        public static string[] userCommands = { "c-help", "c-create task", "c-get list", "c-get task", "c-delete task", "c-logout", "c-clear", "c-exit" };

        private static int questionAmount = 0;
        private static int userID;

        static void Main(string[] args)
        {

            Task.Run(() => Start()).GetAwaiter().GetResult();
        }

        static async Task Start()
        {
            //make a login Cycle
            Console.WriteLine(">>>Welcome to the CommandTaskList!");
            //Would you like to login or register?
            Console.WriteLine(">>>Would you like to login or register?");
            Console.WriteLine(">>>To login type \"c-login\", and to register type \"c-register\"");
            string userAccountInput = Console.ReadLine();

            switch (userAccountInput)
            {
                case "c-login":
                    LoginSquence();
                    break;
                case "c-register":
                    RegisterAccountSequence();
                    break;
                default:
                    Console.WriteLine("<!>The command you entered was not found");
                    Start();
                    break;
            }

        }

        static async Task LoginSquence()
        {
            Console.WriteLine(">>>Please enter your email");
            string email = Console.ReadLine();
            Console.WriteLine(">>>Please enter your password");

            //TODO: Need to comment and understand
            var pass = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    Console.Write("\b \b");
                    pass = pass[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    pass += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);
            Console.WriteLine("");

            //TODO: Remove testing only
            Console.WriteLine(pass);


            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open(); // Open the connection

                    // SQL query to get both the email and hash
                    string query = "SELECT username, hash, user_id, questionamount FROM users WHERE LOWER(username) = LOWER(@Email)";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        // Add the email parameter to prevent SQL injection
                        cmd.Parameters.AddWithValue("Email", email);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Retrieve email and hash from the database
                                string storedEmail = reader.GetString(0);
                                string hashString = reader.GetString(1); // Assuming hash is stored as a hex string
                                // Convert the hex string to byte[]
                                byte[] storedHash = HexStringToByteArray(hashString);

                                // Hash the input password
                                byte[] inputHash = GetHash(pass);

                                // Compare email and password hash
                                if (storedEmail.Equals(email, StringComparison.OrdinalIgnoreCase) && CompareHashes(storedHash, inputHash))
                                {
                                    Console.WriteLine("Email and password are correct.");
                                    userID = reader.GetInt32(2);
                                    questionAmount = reader.GetInt32(3);
                                    logedin = true;
                                    WelcomeUserText("login");
                                }
                                else
                                {
                                    Console.WriteLine("Invalid email or password.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Email not found.");
                                LoginSquence();
                            }
                        }
                    }
                }
            }
            catch (Npgsql.PostgresException ex)
            {
                // Handle PostgreSQL-specific exceptions
                Console.WriteLine($"PostgreSQL error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            //Send data to php server to check
            //Do not check here to protect a bit more against hackers

            //Response if correct
            //userToken = "Token from server"
            //GetUserCommandInput();


            //Response if not correct


        }

        static async Task RegisterAccountSequence()
        {
            Console.WriteLine(">>>Please enter your email");
            string email = Console.ReadLine();
            Console.WriteLine(">>>Please enter password");

            //TODO: Need to comment and understand
            var pass = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    Console.Write("\b \b");
                    pass = pass[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    pass += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);
            Console.WriteLine("");

            //TODO: Remove testing only
            Console.WriteLine(pass);
            string hashVal = GetHashString(pass);
            Console.WriteLine(hashVal);
            logedin = true;


            try
            {
                // Open the connection
                await using var conn = new NpgsqlConnection(connString);
                conn.Open();

                // SQL select statement to check if the user already exists
                var selectCommand = @"SELECT * FROM users, user_id WHERE username = @username";

                await using (var cmd = new NpgsqlCommand(selectCommand, conn))
                {
                    cmd.Parameters.AddWithValue("@username", email);

                    // Execute the query
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            Console.WriteLine("User with this email already exists.");
                            Start();
                            return; // Exit the method to prevent further execution
                        }
                    } // Reader is disposed and closed here

                    // If no user was found, proceed to insert
                    Console.WriteLine("No user found with this email. Proceeding to insert.");

                    var insertCommand = @"INSERT INTO users (username, hash, questionamount) VALUES (@username, @hash, @questionamount) RETURNING user_id";

                    await using (var insertCmd = new NpgsqlCommand(insertCommand, conn))
                    {
                        // Assuming 'hashVal' is already defined
                        insertCmd.Parameters.AddWithValue("@username", email);
                        insertCmd.Parameters.AddWithValue("@hash", hashVal);
                        insertCmd.Parameters.AddWithValue("@questionamount", 0);
                        questionAmount = 0;
                        var userId = await insertCmd.ExecuteScalarAsync();
                        userID = Convert.ToInt32(userId);
                        // Execute the insert command
                        await insertCmd.ExecuteNonQueryAsync();
                        Console.WriteLine("New user inserted successfully.");
                        WelcomeUserText("register");
                    }



                }
            }
            catch (Npgsql.PostgresException ex)
            {
                Console.WriteLine($"PostgreSQL error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }


            //Check if register is successful and then continue
        }



        // Method to compute the SHA256 hash of a given string
        public static byte[] GetHash(string inputString)
        {
            // Create a new instance of the SHA256 hashing algorithm
            using (HashAlgorithm algorithm = SHA256.Create())
                // Convert the input string to a byte array and compute the hash
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        static bool CompareHashes(byte[] hash1, byte[] hash2)
        {
            if (hash1.Length != hash2.Length) return false;

            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i]) return false;
            }
            return true;
        }

        // Convert a hex string to a byte array
        static byte[] HexStringToByteArray(string hex)
        {
            int length = hex.Length;
            byte[] bytes = new byte[length / 2];

            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        // Method to convert the byte array hash to a hexadecimal string
        public static string GetHashString(string inputString)
        {
            // Create a new StringBuilder to store the hexadecimal string
            StringBuilder sb = new StringBuilder();

            // Get the byte array hash by calling the GetHash method
            foreach (byte b in GetHash(inputString))
                // Convert each byte to a two-character hexadecimal string and append to the StringBuilder
                sb.Append(b.ToString("X2"));

            // Return the complete hexadecimal string
            return sb.ToString();
        }



        static private void WelcomeUserText(string l_callType)
        {
            Console.Clear();
            if (logedin)
            {
                switch (l_callType)
                {
                    case "register":
                        Console.WriteLine(">>>Welcome \"X\" to the Console Task List.");
                        break;
                    case "login":
                        Console.WriteLine(">>>Welcome back \"X\".");
                        break;
                    default:
                        Console.WriteLine("<!>l_ calltype was wrong, is was entered: " + l_callType);
                        break;
                }
                try
                {
                    using (var conn = new NpgsqlConnection(connString))
                    {
                        conn.Open(); // Open the connection

                        // SQL query to get both the email and hash
                        string query = "SELECT questionAmount FROM users WHERE user_id = @user_id";

                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            // Add the email parameter to prevent SQL injection
                            cmd.Parameters.AddWithValue("user_id", userID);

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    if (reader["questionamount"] != DBNull.Value)
                                    {
                                        int questionNumber = reader.GetOrdinal("questionamount");
                                        questionAmount = reader.GetInt32(questionNumber);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Email not found.");
                                }
                            }
                        }
                    }
                }
                catch (Npgsql.PostgresException ex)
                {
                    // Handle PostgreSQL-specific exceptions
                    Console.WriteLine($"PostgreSQL error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Handle other exceptions
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }


                GetUserCommandInput();
            }
            else
            {
                Console.WriteLine(">>>You are not logged in!");
                Task.Run(() => Start()).GetAwaiter().GetResult();
            }
        }

        static public void GetUserCommandInput()
        {
            Console.WriteLine(">>>Please enter a command! If you need help type c-help");
            Console.WriteLine(">>>To write a command use the prefix \"c-\"");
            Console.WriteLine("_____________________________________________");
            Console.WriteLine("");
            string userInput = Console.ReadLine();
            bool clearConsole = false;
            if (logedin)
            {
                switch (userInput)
                {
                    case "c-help":
                        CommandHelp();
                        break;
                    case "c-get list":
                        CommandGetTaskList();
                        break;
                    case "c-get task":
                        CommandGetTask();
                        break;
                    case "c-delete task":
                        CommandDeleteTask();
                        break;
                    case "c-clear":
                        clearConsole = true;
                        CommandClear();
                        break;
                    case "c-create task":
                        CommandCreateTask();
                        break;
                    case "c-exit":

                        break;
                    default:
                        Console.WriteLine("<!>The command you entered was not found");
                        SetBlankLine();
                        GetUserCommandInput();
                        break;
                }
            }
            else
            {
                Console.WriteLine(">>>You are not logged in!");
                Task.Run(() => Start()).GetAwaiter().GetResult();
            }
            if (!clearConsole)
                SetBlankLine();
            clearConsole = false;

        }
        static void SetBlankLine()
        {
            Console.WriteLine("");
            Console.WriteLine("");
        }

        static void CommandHelp()
        {
            Console.WriteLine("");
            Console.WriteLine(">>>All the commands that you can execute are:");
            foreach (string com in userCommands)
            {
                Console.WriteLine(com);
            }
            SetBlankLine();
            GetUserCommandInput();
        }

        static void CommandClear()
        {
            Console.Clear();
            GetUserCommandInput();
        }

        static void CommandCreateTask()
        {
            if (logedin)
            {
                Console.WriteLine(">>>Plaese enter the task name");
                string taskNameInput = Console.ReadLine();
                Console.WriteLine(">>>Please enter a description for the task");
                string taskDescriptionInput = Console.ReadLine();
                questionAmount++;
                string taskNumber = "task" + questionAmount;
                //string createTaskQuerry = $"ALTER TABLE users ADD {taskNumber} TEXT, ADD {taskNumber + "description"} TEXT";
                string createTaskQuerry = $"ALTER TABLE users ADD {taskNumber} TEXT, ADD {taskNumber}_description TEXT";

                //string updateTaskQuerry = $"UPDATE users SET {taskNumber} = {taskNameInput}, {taskNumber + "description"} = {taskDescriptionInput},questionamount = {questionAmount} WHERE user_id = {userID}";
                string updateTaskQuerry = $"UPDATE users SET {taskNumber} = '{taskNameInput}', " +
                                          $"{taskNumber}_description = '{taskDescriptionInput}', " +
                                          $"questionamount = {questionAmount} WHERE user_id = {userID}";


                try
                {
                    using (var connection = new NpgsqlConnection(connString))
                    {
                        connection.Open();
                        using (var transaction = connection.BeginTransaction())
                        using (var command = new NpgsqlCommand())
                        {
                            command.Connection = connection;
                            command.Transaction = transaction;

                            try
                            {
                                // Step 1: Add new column
                                command.CommandText = createTaskQuerry;
                                command.ExecuteNonQuery();

                                // Step 2: Update the new column
                                command.CommandText = updateTaskQuerry;
                                command.ExecuteNonQuery();

                                // Commit the transaction
                                transaction.Commit();

                                SetBlankLine();
                                GetUserCommandInput();
                            }
                            catch (Exception ex)
                            {
                                // Rollback on error
                                transaction.Rollback();
                                Console.WriteLine($"An error occurred: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Npgsql.PostgresException ex)
                {
                    // Handle PostgreSQL-specific exceptions
                    Console.WriteLine($"PostgreSQL error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Handle other exceptions
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine(">>>You are not logged in!");
                Start();
            }
        }

        static void CommandGetTaskList()
        {
            if (logedin)
            {
                string[] tasksOnDatabase = new string[questionAmount];
                Console.WriteLine(">>>Get list");

            }
            else
            {
                Console.WriteLine(">>>You are not logged in!");
                Start();

            }
        }

        static void CommandGetTask()
        {
            if (logedin)
            {
                Console.WriteLine(">>>What task do you want to see?");
                Console.WriteLine(">>>Enter like \"c-task {task name}\"");
                string userTaskInput = Console.ReadLine();
            }
            else
            {
                Console.WriteLine(">>>You are not logged in!");
                Start();
            }
        }

        static void CommandDeleteTask()
        {
            if (logedin)
            {
                Console.WriteLine(">>>What task do you want to delete?");
                Console.WriteLine(">>>Enter like \"c-delete {task name}\"");
                string userTaskInput = Console.ReadLine();
            }
            else
            {
                Console.WriteLine(">>>You are not logged in!");
                Start();
            }
        }

        static void CommandLogout()
        {

            //TODO: Logout

            Console.Clear();
            Console.WriteLine(">>>Logout successful");
            Console.WriteLine("");
            logedin = false;
            Start();
        }


        static void CommandExit()
        {
            if (logedin)
            {
                //TOOD: Logout functionality
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine(">>>You are not logged in!");
                Start();
            }
        }
    }
}