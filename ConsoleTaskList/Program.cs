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
                        CommandExit();
                        break;
                    case "c-logout":
                        CommandLogout();
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
                Console.WriteLine(">>>Please enter the task name");
                string taskNameInput = Console.ReadLine();
                Console.WriteLine(">>>Please enter a description for the task");
                string taskDescriptionInput = Console.ReadLine();

                // Check if a task with the same name already exists
                bool taskExists = false;

                for (int i = 1; i <= questionAmount; i++)
                {
                    string checkTaskQuery = $@"SELECT task{i} 
                                        FROM users 
                                        WHERE user_id = @user_id AND task{i} ILIKE @taskName";

                    try
                    {
                        using (var connection = new NpgsqlConnection(connString))
                        {
                            connection.Open();
                            using (var command = new NpgsqlCommand(checkTaskQuery, connection))
                            {
                                command.Parameters.AddWithValue("user_id", userID);
                                command.Parameters.AddWithValue("taskName", taskNameInput);
                                var result = command.ExecuteScalar();

                                if (result != null && result.ToString().Equals(taskNameInput, StringComparison.OrdinalIgnoreCase))
                                {
                                    taskExists = true;
                                    break; // Exit loop if a matching task is found
                                }
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
                }

                // If a task with the same name exists, inform the user
                if (taskExists)
                {
                    Console.WriteLine(">>>A task with the same name already exists. Please choose a different name.");
                    SetBlankLine();
                    GetUserCommandInput();
                    return; // Exit the function early
                }

                // If no task exists, proceed with creating the new task
                questionAmount++;
                string taskNumber = "task" + questionAmount;
                string createTaskQuery = $"ALTER TABLE users ADD {taskNumber} TEXT, ADD {taskNumber}_description TEXT";
                string updateTaskQuery = $"UPDATE users SET {taskNumber} = @taskName, " +
                                          $"{taskNumber}_description = @taskDescription, " +
                                          $"questionamount = {questionAmount} WHERE user_id = @user_id";

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
                                command.CommandText = createTaskQuery;
                                command.ExecuteNonQuery();

                                // Step 2: Update the new column
                                command.CommandText = updateTaskQuery;
                                command.Parameters.AddWithValue("taskName", taskNameInput);
                                command.Parameters.AddWithValue("taskDescription", taskDescriptionInput);
                                command.Parameters.AddWithValue("user_id", userID);
                                command.ExecuteNonQuery();

                                // Commit the transaction
                                transaction.Commit();

                                Console.WriteLine(">>>Task created successfully.");
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
                    Console.WriteLine($"PostgreSQL error: {ex.Message}");
                }
                catch (Exception ex)
                {
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
                Console.WriteLine(">>>Get list");


                // Final SQL query with user_id condition
                for (int i = 1; i <= questionAmount; i++) // Changed < to <= to include last question
                {
                    string listQuesry = $@"SELECT task{i} FROM users WHERE user_id = @user_id"; // ILIKE for case-insensitive search

                    try
                    {
                        using (var connection = new NpgsqlConnection(connString))
                        {
                            connection.Open();
                            using (var command = new NpgsqlCommand(listQuesry, connection))
                            {
                                command.Parameters.AddWithValue("user_id", userID);

                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        Console.WriteLine($">>>Task {i}: {reader[$"task{i}"]}");

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
                }
                SetBlankLine();
                GetUserCommandInput();
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
                Console.WriteLine(">>>Enter like \"{task name}\"");
                string userTaskInput = Console.ReadLine();

                bool taskFound = false; // To track if the task was found

                // Final SQL query with user_id condition
                for (int i = 1; i <= questionAmount; i++) // Changed < to <= to include last question
                {
                    string query = $@"SELECT user_id, task{i}_description 
                      FROM users
                      WHERE task{i} ILIKE @userinput AND user_id = @user_id"; // ILIKE for case-insensitive search

                    try
                    {
                        using (var connection = new NpgsqlConnection(connString))
                        {
                            connection.Open();
                            using (var command = new NpgsqlCommand(query, connection))
                            {
                                // Add parameters to prevent SQL injection
                                command.Parameters.AddWithValue("userinput", userTaskInput);
                                command.Parameters.AddWithValue("user_id", userID);

                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        taskFound = true; // Task found
                                        Console.WriteLine($">>>Task Description: {reader[$"task{i}_description"]}");

                                        SetBlankLine();
                                        GetUserCommandInput();
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
                }

                // After the loop, check if any task was found
                if (!taskFound)
                {
                    Console.WriteLine("No matching tasks found.");
                    SetBlankLine();
                    GetUserCommandInput();
                }

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
                Console.WriteLine(">>>Enter like \"{task name}\"");
                string userTaskInput = Console.ReadLine();

                bool taskFound = false; // To track if the task was found
                int taskIndexToDelete = -1;


                // First, check if the task exists and get its index
                for (int i = 1; i <= questionAmount; i++)
                {
                    string query = $@"SELECT task{i} 
                      FROM users 
                      WHERE task{i} ILIKE @userinput AND user_id = @user_id";

                    try
                    {
                        using (var connection = new NpgsqlConnection(connString))
                        {
                            connection.Open();
                            using (var command = new NpgsqlCommand(query, connection))
                            {
                                // Add parameters to prevent SQL injection
                                command.Parameters.AddWithValue("userinput", userTaskInput);
                                command.Parameters.AddWithValue("user_id", userID);

                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        taskFound = true; // Task found
                                        taskIndexToDelete = i; // Store the index of the task to delete
                                        Console.WriteLine(">>>Task found. Deleting...");

                                        // You can break here since you only need the first match
                                        break;
                                    }
                                }
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
                }

                // If the task was found, delete it and update subsequent tasks
                if (taskFound && taskIndexToDelete != -1)
                {
                    // Step 1: Delete the task
                    string deleteQuery = $@"UPDATE users 
                            SET task{taskIndexToDelete} = NULL, 
                                task{taskIndexToDelete}_description = NULL 
                            WHERE user_id = @user_id AND task{taskIndexToDelete} ILIKE @userinput";

                    try
                    {
                        using (var connection = new NpgsqlConnection(connString))
                        {
                            connection.Open();
                            using (var command = new NpgsqlCommand(deleteQuery, connection))
                            {
                                command.Parameters.AddWithValue("userinput", userTaskInput);
                                command.Parameters.AddWithValue("user_id", userID);
                                command.ExecuteNonQuery();
                                Console.WriteLine(">>>Task Deleted");
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

                    // Step 2: Update subsequent tasks
                    for (int i = taskIndexToDelete + 1; i <= questionAmount; i++)
                    {
                        string updateQuery = $@"UPDATE users 
                                SET task{i - 1} = task{i}, 
                                    task{i - 1}_description = task{i}_description 
                                WHERE user_id = @user_id";

                        try
                        {
                            using (var connection = new NpgsqlConnection(connString))
                            {
                                connection.Open();
                                using (var command = new NpgsqlCommand(updateQuery, connection))
                                {
                                    command.Parameters.AddWithValue("user_id", userID);
                                    command.ExecuteNonQuery();
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
                    }

                    // Step 3: Update the total question amount in the users table
                    string updateQuestionAmountQuery = @"UPDATE users 
                                          SET questionAmount = questionAmount - 1 
                                          WHERE user_id = @user_id";

                    try
                    {
                        using (var connection = new NpgsqlConnection(connString))
                        {
                            connection.Open();
                            using (var command = new NpgsqlCommand(updateQuestionAmountQuery, connection))
                            {
                                command.Parameters.AddWithValue("user_id", userID);
                                command.ExecuteNonQuery();
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
                    questionAmount--;

                    // Step 4: Drops old columns that will break system
                    string updateColumnsFromOld = @$"ALTER TABLE users  DROP COLUMN task{questionAmount + 1}, DROP COLUMN task{questionAmount + 1}_description";

                    try
                    {
                        using (var connection = new NpgsqlConnection(connString))
                        {
                            connection.Open();
                            using (var command = new NpgsqlCommand(updateColumnsFromOld, connection))
                            {
                                command.Parameters.AddWithValue("user_id", userID);
                                command.ExecuteNonQuery();
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
                }
                else if (!taskFound)
                {
                    Console.WriteLine("No matching tasks found.");
                }
                SetBlankLine();
                GetUserCommandInput();
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