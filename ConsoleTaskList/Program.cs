using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
//using Newtonsoft.Json;

namespace CommandTaskList
{
    class Program
    {
        private static string url = "http://127.0.0.1/edsa-console-task-list/api.php";
        public static string userToken = string.Empty;
        private static bool logedin = false;

        public static string[] userCommands = { "c-help", "c-create task", "c-get list", "c-get task", "c-delete task", "c-logout", "c-clear" };

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
                    Console.WriteLine(">>>The command you entered was not found");
                    Start();
                    break;
            }

        }

        static void LoginSquence()
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
            logedin = true;

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
            WelcomeUserText("register");
            logedin = true;
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
                        Console.WriteLine(">>>l_ calltype was wrong, is was entered: " + l_callType);
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
                    default:
                        Console.WriteLine(">>>The command you entered was not found");
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
                Console.WriteLine("Get list");

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

        #region API Calls
    }

    #endregion
}
