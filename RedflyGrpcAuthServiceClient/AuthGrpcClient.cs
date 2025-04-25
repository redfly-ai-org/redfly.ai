using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using RedflyCoreFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace RedflyGrpcAuthServiceClient
{
    public static class AuthGrpcClient
    {

        public static async Task<string?> RunAsync(string grpcUrl, bool autoLogin = false)
        {
            try
            {
                Console.WriteLine("Starting the gRPC client test");

                //var loggerFactory = LoggerFactory.Create(builder =>
                //{
                //    builder.AddConsole();
                //    //builder.SetMinimumLevel(LogLevel.Warning);
                //});

                using var channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
                {
                    //LoggerFactory = loggerFactory,
                    HttpHandler = new SocketsHttpHandler
                    {
                        EnableMultipleHttp2Connections = true,
                        KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                        KeepAlivePingDelay = TimeSpan.FromSeconds(30), // Frequency of keepalive pings
                        KeepAlivePingTimeout = TimeSpan.FromSeconds(5) // Timeout before considering the connection dead
                    },
                    HttpVersion = new Version(2, 0) // Ensure HTTP/2 is used
                });

                var authServiceClient = new AuthService.AuthServiceClient(channel);

                string? userName;
                StringBuilder passwordBuilder;
                bool credentialsLoadedFromDisk = false;

                if (autoLogin)
                {
                    (userName, passwordBuilder) = SecureCredentials.Get();
                    credentialsLoadedFromDisk = true;
                    Console.WriteLine("Using saved login credentials...");
                }
                else
                {
                    if (SecureCredentials.Exist())
                    {
                        Console.WriteLine("Do you want to login using the saved credentials? (y/n)");
                        var response = Console.ReadLine();

                        if (response?.ToLower() == "y")
                        {
                            (userName, passwordBuilder) = SecureCredentials.Get();
                            credentialsLoadedFromDisk = true;
                            Console.WriteLine("Using saved login credentials...");
                        }
                        else
                        {
                            PromptUserForLogin(out userName, out passwordBuilder, out credentialsLoadedFromDisk);
                        }
                    }
                    else
                    {
                        PromptUserForLogin(out userName, out passwordBuilder, out credentialsLoadedFromDisk);
                    }
                }

                var loginRequest = new LoginRequest
                {
                    Username = userName,
                    Password = passwordBuilder.ToString()
                };

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\r\nLogging in to {grpcUrl} as {userName}");
                Console.ResetColor();
                Console.WriteLine("Please be patient - these are small servers.");
                Console.WriteLine("Contact us at developer@redfly.ai if you need to.\r\n");

                string token = await LoginWithRetryAsync(authServiceClient, loginRequest);

                if (await TestSecureGrpcCallAsync(authServiceClient, token))
                {
                    Console.WriteLine("You can now make secure calls to the gRPC server.");

                    //Only save if auth is a success and credentials were not loaded from disk.
                    if (!credentialsLoadedFromDisk)
                    {
                        SecureCredentials.Save(userName, passwordBuilder);
                        Console.WriteLine("Your credentials have been saved successfully.\r\n");
                    }
                }
                else
                {
                    Console.WriteLine("Authentication failed!");
                }

                return token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"If you got an error, please try again later.");
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        private static void PromptUserForLogin(out string? userName, out StringBuilder passwordBuilder, out bool credentialsLoadedFromDisk)
        {
            Console.WriteLine("\r\nInstructions: ");
            Console.WriteLine("You can register your account here: https://transparent.azurewebsites.net/Identity/Account/Register");
            Console.WriteLine("Be sure to check your Junk folder for the verification email after you register.");
            Console.WriteLine("Make sure you setup your User Account and Organization after you login.");
            Console.WriteLine("https://transparent.azurewebsites.net/user-setup");
            Console.WriteLine("Registration & Organization setup is necessary to fully access our secure cloud services.\r\n");

            do
            {
                Console.WriteLine("Enter your user name:");
                userName = Console.ReadLine();
            }
            while (string.IsNullOrWhiteSpace(userName));

            Console.WriteLine("Enter your password:");
            passwordBuilder = RedflyConsole.GetPasswordFromUser();

            credentialsLoadedFromDisk = false;
        }

        private static async Task<string> LoginWithRetryAsync(AuthService.AuthServiceClient authServiceClient, LoginRequest loginRequest, int retryCount = 0)
        {
            var cts = new CancellationTokenSource();
            var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

            try
            {
                var loginResponse = await authServiceClient.LoginAsync(loginRequest);

                cts.Cancel();
                await progressTask;

                var token = loginResponse.Token;
                Console.WriteLine($"Token is Valid: {!string.IsNullOrEmpty(token)} ({token.Length} characters)");
                Console.WriteLine("Login is successful!");

                return token;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                Console.WriteLine();

                cts.Cancel();
                await progressTask;

                if (retryCount < 3)
                {
                    Console.WriteLine($"Retrying Login {retryCount + 1}...");

                    await Task.Delay(1000);
                    return await LoginWithRetryAsync(authServiceClient, loginRequest, retryCount + 1);
                }
                else
                {
                    Console.WriteLine($"Failed to login after {retryCount + 1} attempts.");
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
        }

        private static async Task<bool> TestSecureGrpcCallAsync(AuthService.AuthServiceClient client, string token, int retryCount = 0)
        {
            try
            {
                var headers = new Grpc.Core.Metadata
                {
                    { "Authorization", $"Bearer {token}" }
                };

                // Now you can make requests to secure endpoints
                var request = new TestDataRequest();
                Console.WriteLine($"Testing Secure Connectivity with Server ({retryCount + 1})...");

                var cts = new CancellationTokenSource();
                var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

                try
                {
                    var response = await client.TestDataAsync(request, headers);
                    Console.WriteLine($"Response: {response.Message}");

                    return true;
                }
                finally
                {
                    cts.Cancel();
                    await progressTask;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                Console.WriteLine();

                if (retryCount < 3)
                {
                    Console.WriteLine($"Retrying Secure Call {retryCount + 1}...");

                    await Task.Delay(1000);

                    return await TestSecureGrpcCallAsync(client, token, retryCount + 1);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed to make secure request after {retryCount + 1} attempts.");
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();

                    return false;
                }
            }
        }
    }
}
