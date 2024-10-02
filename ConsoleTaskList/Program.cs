using System;
using System.Collections.Generic;
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


        public static string[] userCommands = { "c-help", "c-create task", "c-get list", "c-get task", "c-delete task", "c-logout" };

        static void Main(string[] args)
        {
            //make a login Cycle
            Console.WriteLine("Welcome to the CommandTaskList!");
            //Would you like to login or register?
            Console.WriteLine("Would you like to login or register?");
            Console.WriteLine("To login type \"c-login\", and to register type \"c-register\"");
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
                    Console.WriteLine("The command you entered was not found");
                    Start();
                    break;
            }

        }

        static void LoginSquence()
        {
            Console.WriteLine("Please enter your email");
            string email = Console.ReadLine();
            Console.WriteLine("Please enter your password");

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
            

            //Send data to php server to check
            //Do not check here to protect a bit more against hackers

            //Response if correct
            //userToken = "Token from server"
            //GetUserCommandInput();


            //Response if not correct


        }

        static async Task RegisterAccountSequence()
        {
            Console.WriteLine("Please enter your email");
            string email = Console.ReadLine();
            Console.WriteLine("Please enter password");

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





        static public void GetUserCommandInput()
        {
            Console.WriteLine("Enter command! If you need help type c-help");
            Console.WriteLine("To write a command use the prefix \"c-\"");
            Console.WriteLine("_____________________________________________");
            Console.WriteLine("");
            string userInput = Console.ReadLine();

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
                default:
                    Console.WriteLine("The command you entered was not found");
                    break;
            }

            Console.WriteLine("");
            Console.WriteLine("");

            GetUserCommandInput();
        }

        static void CommandHelp()
        {
            Console.WriteLine("");
            Console.WriteLine("All the commands that you can execute are:");
            foreach (string com in userCommands)
            {
                Console.WriteLine(com);
            }
        }

        static void CommandCreateTask()
        {

        }

        static void CommandGetTaskList()
        {
            Console.WriteLine("Get list");
        }

        static void CommandGetTask()
        {
            Console.WriteLine("What task do you want to see?");
            Console.WriteLine("Enter like \"Task {task name}\"");
            string userTaskInput = Console.ReadLine();
        }

        static void CommandDeleteTask()
        {
            Console.WriteLine("What task do you want to delete?");
            Console.WriteLine("Enter like \"Delete {task name}\"");
            string userTaskInput = Console.ReadLine();
        }

        static void CommandLogout()
        {
            //TODO: Logout

            Console.Clear();
            Console.WriteLine("Logout successful");
            Console.WriteLine("");
        }

        #region API Calls









        //Console.WriteLine("1");
        //var requestData = new CreateAccountRequest
        //{
        //    email = l_email,
        //    password = l_password
        //};
        //// Convert the C# object to JSON
        ////string jsonRequest = JsonConvert.SerializeObject(new { json = JsonConvert.SerializeObject(requestData) });
        //string jsonRequest = JsonSerializer.Serialize(requestData);
        //// Create an HttpClient instance
        //Console.WriteLine("2");
        //using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
        //{
        //    Console.WriteLine("3");
        //    // Set the content type to application/x-www-form-urlencoded and wrap the JSON data
        //    StringContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/x-www-form-urlencoded");
        //    try
        //    {
        //        Console.WriteLine("4");
        //        // Send a POST request to the PHP server
        //        HttpResponseMessage response = await client.PostAsync(url, content);

        //        // Check if the request was successful


        //        if (response.IsSuccessStatusCode)
        //        {
        //            Console.WriteLine("5");
        //            // Get the response content as a string
        //            string jsonResponse = await response.Content.ReadAsStringAsync();
        //            // Log the raw response for debugging
        //            Console.WriteLine("Raw JSON Response: " + jsonResponse);

        //            // Try deserializing the response if it is valid
        //            try
        //            {
        //                //var responseData = JsonConvert.DeserializeObject<CreateAccountRequest>(jsonResponse);
        //                var responseData = JsonSerializer.Deserialize<CreateAccountRequest>(jsonResponse);
        //                // Check for specific messages and print them out
        //                Console.WriteLine("Server Message: " + responseData.serverMessage);
        //                Console.WriteLine("Registration Successful!");
        //                GetUserCommandInput();
        //            }
        //            catch (Exception deserializationEx)
        //            {
        //                Console.WriteLine("Deserialization failed: " + deserializationEx.Message);
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine("Error: " + response.StatusCode);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Exception occurred: " + ex.Message);
        //    }
        //}
        //Console.WriteLine("end api");
    }

    #endregion
}



public class CreateAccountAPICall
{
    private static readonly HttpClient client = new HttpClient();
    public async Task CreateAccount(string l_email, string l_password)
    {
        string url = "http://127.0.0.1/edsa-console-task-list/api.php";


        try
        {
            var sendData = new CreateAccountRequest
            {
                email = l_email,
                password = l_password
            };

            var jsonData = JsonSerializer.Serialize(sendData);

            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");


            // Prepare the request (e.g., GET request)
            var response = await client.PostAsync(url, content);

            // Check if the response is successful
            if (response.IsSuccessStatusCode)
            {
                // Read the response content as a string
                var responseBody = await response.Content.ReadAsStringAsync();

                // Deserialize the JSON response to a C# object (assuming it returns a User object)
                var apiResponse = JsonSerializer.Deserialize<CreateAccountRequest>(responseBody);

                Console.WriteLine($"Server Message:{apiResponse.serverMessage}");
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }
    }
}










//public class Data
//{
//    public int UserId { get; set; }
//    public string Email { get; set; }
//}

public class CreateAccountRequest
{
    public string action = "create_account";

    public string email { get; set; }
    public string password { get; set; }
    public string serverMessage { get; set; }
    public string errorMessage { get; set; }
}
