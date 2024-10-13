﻿using System;
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
        //private static var connString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";
        private static bool logedin = false;
        static string connString = "Host=localhost;Username=postgres;Password=12345;Database=postgres";

        public static string[] userCommands = { "c-help", "c-create task", "c-get list", "c-get task", "c-delete task", "c-logout", "c-clear", "c-exit" };

        static void Main(string[] args)
        {
            //make a login Cycle
            Console.WriteLine(">>>Welcome to the CommandTaskList!");
            //Would you like to login or register?
            Console.WriteLine(">>>Would you like to login or register?");
            Console.WriteLine(">>>To login type \"c-login\", and to register type \"c-register\"");
            Task.Run(() => Start()).GetAwaiter().GetResult();
        }

        static async Task Start()
        {
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
                    string query = "SELECT username, hash FROM users WHERE LOWER(username) = LOWER(@Email)";

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
                var selectCommand = @"SELECT * FROM users WHERE username = @username";

                await using (var cmd = new NpgsqlCommand(selectCommand, conn))
                {
                    cmd.Parameters.AddWithValue("@username", email);

                    // Execute the query
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            Console.WriteLine("User with this email already exists.");
                            LoginSquence();
                            return; // Exit the method to prevent further execution
                        }
                    } // Reader is disposed and closed here

                    // If no user was found, proceed to insert
                    Console.WriteLine("No user found with this email. Proceeding to insert.");

                    var insertCommand = @"INSERT INTO users (username, hash) VALUES (@username, @hash)";

                    await using (var insertCmd = new NpgsqlCommand(insertCommand, conn))
                    {
                        // Assuming 'hashVal' is already defined
                        insertCmd.Parameters.AddWithValue("@username", email);
                        insertCmd.Parameters.AddWithValue("@hash", hashVal);

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

            }
            else
            {
                Console.WriteLine(">>>You are not logged in!");
                Task.Run(() => Start()).GetAwaiter().GetResult();
            }
        }

        static void CommandGetTaskList()
        {
            if (logedin)
            {
                Console.WriteLine(">>>Get list");

            }
            else
            {
                Console.WriteLine(">>>You are not logged in!");
                Task.Run(() => Start()).GetAwaiter().GetResult();
            }
        }

        static void CommandGetTask()
        {
            if (logedin)
            {
                Console.WriteLine(">>>What task do you want to see?");
                Console.WriteLine(">>>Enter like \"Task {task name}\"");
                string userTaskInput = Console.ReadLine();
            }
            else
            {
                Console.WriteLine(">>>You are not logged in!");
                Task.Run(() => Start()).GetAwaiter().GetResult();
            }
        }

        static void CommandDeleteTask()
        {
            if (logedin)
            {
                Console.WriteLine(">>>What task do you want to delete?");
                Console.WriteLine(">>>Enter like \"Delete {task name}\"");
                string userTaskInput = Console.ReadLine();
            }
            else
            {
                Console.WriteLine(">>>You are not logged in!");
                Task.Run(() => Start()).GetAwaiter().GetResult();
            }
        }

        static void CommandLogout()
        {

            //TODO: Logout

            Console.Clear();
            Console.WriteLine(">>>Logout successful");
            Console.WriteLine("");
            logedin = false;
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
                Task.Run(() => Start()).GetAwaiter().GetResult();
            }
        }


        #region API Calls
    }

    #endregion
}
