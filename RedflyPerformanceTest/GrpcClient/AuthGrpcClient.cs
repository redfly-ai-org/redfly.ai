using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace RedflyPerformanceTest.GrpcClient
{
    internal static class AuthGrpcClient
    {

        public static async Task<string?> RunAsync(string grpcUrl)
        {
            try
            {
                Console.WriteLine("Starting the gRPC client test");

                var httpHandler = new HttpClientHandler
                {
                    SslProtocols = SslProtocols.Tls12
                };

                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Warning);
                });

                using var channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    LoggerFactory = loggerFactory,
                    HttpVersion = new Version(2, 0) // Ensure HTTP/2 is used
                });

                var authServiceClient = new AuthService.AuthServiceClient(channel);

                Console.WriteLine("\r\nInstructions: ");
                Console.WriteLine("You can register your account here: https://transparent.azurewebsites.net/Identity/Account/Register");
                Console.WriteLine("Be sure to check your Junk folder for the verification email after you register.");
                Console.WriteLine("Registration is necessary to be able to access our secure cloud services.\r\n");

                Console.WriteLine("Enter your user name:");
                var userName = Console.ReadLine();

                Console.WriteLine("Enter your password:");
                var passwordBuilder = GetPasswordFromConsole();

                var loginRequest = new LoginRequest
                {
                    Username = userName,
                    Password = passwordBuilder.ToString()
                };

                Console.WriteLine($"Logging in to {grpcUrl}");
                Console.WriteLine("Please be patient - these are small servers.");
                Console.WriteLine("Contact us at developer@redfly.ai if you need to.\r\n");

                var cts = new CancellationTokenSource();
                var progressTask = ShowProgressAnimation(cts.Token);

                var loginResponse = await authServiceClient.LoginAsync(loginRequest);

                cts.Cancel();
                await progressTask;

                var token = loginResponse.Token;
                Console.WriteLine($"Token is Valid: {!string.IsNullOrEmpty(token)} ({token.Length} characters)");

                await TestSecureGrpcCall(authServiceClient, token);

                Console.WriteLine("Authentication is complete!");

                return token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"If you got an error, please try again later.");
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        private static StringBuilder GetPasswordFromConsole()
        {
            var password = new StringBuilder();
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(intercept: true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password;
        }

        private static async Task ShowProgressAnimation(CancellationToken token)
        {
            var animation = new[] { '/', '-', '\\', '|' };
            int counter = 0;

            while (!token.IsCancellationRequested)
            {
                Console.Write(animation[counter % animation.Length]);
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                counter++;
                await Task.Delay(100);
            }
        }

        private static async Task TestSecureGrpcCall(AuthService.AuthServiceClient client, string token)
        {
            try
            {
                var headers = new Grpc.Core.Metadata
                {
                    { "Authorization", $"Bearer {token}" }
                };

                // Now you can make requests to secure endpoints
                var request = new TestDataRequest();

                Console.WriteLine("Executing Secure Request with JWT Token...");
                var response = await client.TestDataAsync(request, headers);
                Console.WriteLine($"Response: {response.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
