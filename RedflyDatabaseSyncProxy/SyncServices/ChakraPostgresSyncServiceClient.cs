using Grpc.Core;
using Grpc.Net.Client;
using RedflyCoreFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RedflyDatabaseSyncProxy.Protos.Postgres;
using Azure.Core;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace RedflyDatabaseSyncProxy.SyncServices;

internal class ChakraPostgresSyncServiceClient
{

    private static bool _bidirectionalStreamingIsWorking = false;
    private static int _bidirStreamingRetryCount = 0;

    private static string _clientSessionId = GenerateUniqueClientSessionId();
    private static readonly FixedSizeList<Exception> _lastBidirErrors = new FixedSizeList<Exception>(5);

    private static string GenerateUniqueClientSessionId()
    {
        // Combine the machine name and a GUID to ensure uniqueness
        string machineName = Environment.MachineName;
        string guid = "9052b6a0-03bf-4f36-b811-e7038ef1b692";

        // Hash the combination for a consistent length (optional)
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{machineName}-{guid}"));
            return Convert.ToBase64String(hashBytes).Substring(0, 32); // Truncate for readability
        }
    }

    internal static async Task StartAsync(string grpcUrl, string grpcAuthToken, bool runInitialSync)
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
        {
            LoggerFactory = loggerFactory,
            HttpHandler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true,
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                KeepAlivePingDelay = TimeSpan.FromSeconds(30), // Frequency of keepalive pings
                KeepAlivePingTimeout = TimeSpan.FromSeconds(5) // Timeout before considering the connection dead
            }
        });

        var chakraClient = new NativeGrpcPostgresChakraService.NativeGrpcPostgresChakraServiceClient(channel);

        var grpcHeaders = new Metadata
                {
                    { "Authorization", $"Bearer {grpcAuthToken}" },
                    { "client-session-id", _clientSessionId.ToString() }
                };

        // Start Chakra Sync
        if (!await StartChakraSyncAsyncWithRetry(runInitialSync, chakraClient, grpcHeaders))
        { 
            return; 
        }

        AsyncDuplexStreamingCall<ClientMessage, ServerMessage>? asyncDuplexStreamingCall = null;
        Task bidirectionalTask;

        try
        {
            (asyncDuplexStreamingCall, bidirectionalTask) = await StartBidirStreamingAsync(chakraClient, grpcHeaders);

            var connMonitorCancelTokenSource = new CancellationTokenSource();
            var connMonitorCancelToken = connMonitorCancelTokenSource.Token;

            var connMonitorTask = Task.Run(async () =>
            {
                int delayTimeMs = 15 * 1000; // Start with 15 seconds
                const int minDelayMs = 15 * 1000; // Minimum delay: 15 seconds
                const int maxDelayMs = 5 * 60 * 1000; // Maximum delay: 5 minutes
                const int delayStepMs = 15 * 1000; // Step to increase or decrease delay: 15 seconds

                while (!connMonitorCancelToken.IsCancellationRequested)
                {
                    await Task.Delay(delayTimeMs, connMonitorCancelToken);

                    if (!_bidirectionalStreamingIsWorking)
                    {
                        //If last 5 errors had a gRPC error, with status code Unauthenticated
                        if (_lastBidirErrors.Count > 0 && 
                            _lastBidirErrors.All(e => e is RpcException rpcEx && rpcEx.StatusCode == StatusCode.Unauthenticated))
                        {
                            // Authenticate again.
                            var authToken = await RedflyGrpcAuthServiceClient.AuthGrpcClient.RunAsync(grpcUrl, autoLogin: true);

                            if (authToken == null ||
                                authToken.Length == 0)
                            {
                                Console.WriteLine("Failed to authenticate with the gRPC server.");
                                Console.WriteLine("Contact us at developer@redfly.ai if you need to.");
                                return;
                            }

                            //Recreate
                            grpcHeaders = new Metadata
                                            {
                                                { "Authorization", $"Bearer {authToken}" },
                                                { "client-session-id", _clientSessionId.ToString() }
                                            };
                        }

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"MONITOR: Reconnecting to the server to restart bi-directional streaming. The last delay time was {delayTimeMs / 1000} secs.");
                        Console.ResetColor();

                        _bidirStreamingRetryCount += 1;
                        (asyncDuplexStreamingCall, bidirectionalTask) = await StartBidirStreamingAsync(chakraClient, grpcHeaders);

                        // Decrease delay time when the flag is false
                        delayTimeMs = Math.Max(minDelayMs, delayTimeMs - delayStepMs);
                    }
                    else
                    {
                        // Increase delay time when the flag is true
                        delayTimeMs = Math.Min(maxDelayMs, delayTimeMs + delayStepMs);
                    }
                }

            }, connMonitorCancelToken);

            // Keep the client running to listen for server messages
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Press any key to exit...");
            Console.ResetColor();
            Console.ReadKey();

            try
            {
                await StopChakraSyncAsync(chakraClient, grpcHeaders);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Stopped Chakra Sync.");
                Console.ResetColor();
            }
            finally
            {
                connMonitorCancelTokenSource.Cancel();

                try
                {
                    await connMonitorTask;
                }
                catch (TaskCanceledException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Monitoring Task was canceled.");
                    Console.ResetColor();
                }

                // Complete the request stream
                await asyncDuplexStreamingCall.RequestStream.CompleteAsync();
                await bidirectionalTask;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Stopped bi-directional streaming.");
                Console.ResetColor();
            }
        }
        finally
        {
            asyncDuplexStreamingCall?.Dispose();
        }
    }

    private static async Task StopChakraSyncAsync(
                                NativeGrpcPostgresChakraService.NativeGrpcPostgresChakraServiceClient chakraClient, 
                                Metadata headers)
    {
        var stopResponse = await chakraClient
                                    .StopChakraSyncAsync(
                                        new StopChakraSyncRequest() 
                                                { ClientSessionId = _clientSessionId }, 
                                        headers);

        if (stopResponse.Success)
        {
            Console.WriteLine("Chakra Sync Service stopped successfully.");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"SERVER|{stopResponse.Message}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed to stop the Chakra Sync Service.");
            Console.WriteLine(stopResponse.Message);
            Console.ResetColor();
        }
    }

    private static async Task<bool> StartChakraSyncAsyncWithRetry(
                                        bool runInitialSync,
                                        NativeGrpcPostgresChakraService.NativeGrpcPostgresChakraServiceClient chakraClient,
                                        Metadata headers)
    {
        int maxRetryAttempts = 5; // Maximum number of retry attempts
        int delayMilliseconds = 1000; // Initial delay in milliseconds

        for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
        {
            try
            {
                Console.WriteLine($"Attempt #{attempt}: StartChakraSyncAsync");

                var startResponse = await chakraClient
                    .StartChakraSyncAsync(
                        new StartChakraSyncRequest
                        {
                            ClientSessionId = _clientSessionId,
                            EncryptedClientId = RedflyEncryption.EncryptToString(Guid.Empty.ToString()),
                            EncryptedClientName = RedflyEncryption.EncryptToString(AppSession.ClientAndUserProfileViewModel!.ClientName),
                            EncryptionKey = RedflyEncryptionKeys.AesKey,
                            EncryptedPostgresServerName = AppSession.PostgresDatabase!.EncryptedServerName,
                            EncryptedPostgresDatabaseName = AppSession.PostgresDatabase!.EncryptedDatabaseName,
                            EncryptedPostgresUserName = AppSession.PostgresDatabase!.EncryptedUserName,
                            EncryptedPostgresPassword = AppSession.PostgresDatabase!.EncryptedPassword,
                            EncryptedPostgresTestDecodingSlotName = AppSession.PostgresDatabase!.EncryptedTestDecodingSlotName,
                            EncryptedPostgresPgOutputSlotName = AppSession.PostgresDatabase!.EncryptedPgOutputSlotName,
                            EncryptedPostgresPublicationName = AppSession.PostgresDatabase!.EncryptedPublicationName,
                            EncryptedRedisServerName = AppSession.RedisServer!.EncryptedServerName,
                            RedisPortNo = AppSession.RedisServer!.Port,
                            EncryptedRedisPassword = AppSession.RedisServer!.EncryptedPassword,
                            RedisUsesSsl = AppSession.RedisServer!.UsesSsl,
                            RedisSslProtocol = AppSession.RedisServer!.SslProtocol,
                            RedisAbortConnect = AppSession.RedisServer!.AbortConnect,
                            RedisConnectTimeout = AppSession.RedisServer!.ConnectTimeout,
                            RedisSyncTimeout = AppSession.RedisServer!.SyncTimeout,
                            RedisAsyncTimeout = AppSession.RedisServer!.AsyncTimeout,
                            RunInitialSync = runInitialSync,
                            EnableDataReconciliation = true
                        },
                        headers);

                if (startResponse.Success)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("Chakra Sync Service started successfully.");
                    Console.WriteLine("After a few seconds, you can modify your database and see changes sync to Redis immediately.");
                    Console.WriteLine("If you don't see the update log, refresh your Redis to confirm that changes are available immediately.");

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Please ignore the Grpc errors - these are normal.");
                    Console.ResetColor();
                    Console.WriteLine();

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"SERVER|{startResponse.Message}");
                    Console.ResetColor();
                    return true;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to start the Chakra Sync Service.");
                    Console.WriteLine(startResponse.Message);
                    Console.ResetColor();
                    return false;
                }
            }
            catch (RpcException ex) when (attempt < maxRetryAttempts)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"gRPC error occurred: {ex.Status}. Retrying in {delayMilliseconds / 1000} seconds... (Attempt {attempt}/{maxRetryAttempts})");
                Console.ResetColor();

                await Task.Delay(delayMilliseconds);

                // Exponential backoff
                delayMilliseconds *= 2;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                Console.ResetColor();

                throw; // Re-throw the exception if it's not a gRPC error
            }
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Max retry attempts reached. Failed to start the Chakra Sync Service.");
        Console.ResetColor();
        return false;
    }

    private static async Task<(AsyncDuplexStreamingCall<ClientMessage, ServerMessage> asyncDuplexStreamingCall, Task bidirectionalTask)> StartBidirStreamingAsync(
                                NativeGrpcPostgresChakraService.NativeGrpcPostgresChakraServiceClient chakraClient, 
                                Metadata headers)
    {
        _bidirectionalStreamingIsWorking = false;
        Console.WriteLine($"Session #{_bidirStreamingRetryCount}: Calling Server to setup bi-directional streaming...");

        // Bi-directional streaming for communication with the server
        var asyncDuplexStreamingCall = chakraClient.CommunicateWithClient(headers);

        var bidirectionalTask = Task.Run(async () =>
        {
            try
            {
                _bidirectionalStreamingIsWorking = true;
                Console.WriteLine($"BI-DIR> Session #{_bidirStreamingRetryCount}: Reading from bi-directional stream...");

                await foreach (var message in asyncDuplexStreamingCall.ResponseStream.ReadAllAsync())
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(FormatGrpcServerMessage(message.Message));
                    Console.ResetColor();
                    _bidirectionalStreamingIsWorking = true;
                }
            }
            catch (RpcException rpcex)
            {
                _lastBidirErrors.Add(rpcex);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"    BI-DIR> Session #{_bidirStreamingRetryCount}: gRPC error occurred: {rpcex.Status}, message: {rpcex.Message}");
                Console.ResetColor();
                _bidirectionalStreamingIsWorking = false;
            }
            catch (Exception ex)
            {
                _lastBidirErrors.Add(ex);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"    BI-DIR> Session #{_bidirStreamingRetryCount}: An unexpected error occurred: {ex.Message}");
                Console.ResetColor();
                _bidirectionalStreamingIsWorking = false;
            }
        });

        Console.WriteLine($"INIT> Session #{_bidirStreamingRetryCount}: Sending initial message to establish the stream");

        try
        {
            // Send initial message to establish the stream - retrying this never works
            // The CommunicateWithClient() method is what needs to be called again to
            // re-establish the stream. It could re-establish a new stream when the old
            // one dies.
            await asyncDuplexStreamingCall
                    .RequestStream
                    .WriteAsync(
                        new ClientMessage
                        {
                            ClientSessionId = _clientSessionId,
                            Message = $"Client Registration Message. Session {_bidirStreamingRetryCount}"
                        });

            Console.WriteLine($"INIT> Session #{_bidirStreamingRetryCount}: Initial message successfully sent to server");
        }
        catch (Exception ex)
        {
            _lastBidirErrors.Add(ex);

            _bidirStreamingRetryCount += 1;
            Console.WriteLine($"INIT>Session #{_bidirStreamingRetryCount}: Error sending initial message: {ex.Message}. Waiting 10 seconds before attempt {_bidirStreamingRetryCount}...");

            await Task.Delay(10 * 1000);
            return await StartBidirStreamingAsync(chakraClient, headers);
        }

        Console.WriteLine($"Session #{_bidirStreamingRetryCount}: Started BI-DIR streaming.");
        return (asyncDuplexStreamingCall, bidirectionalTask);
    }

    private static string FormatGrpcServerMessage(string logMessage)
    {
        try
        {
            var regex = new Regex(@"Type: (?<Type>[^,]+), Data: \{ Operation = (?<Operation>[^}]+) \}");
            var match = regex.Match(logMessage);

            if (match.Success)
            {
                var type = match.Groups["Type"].Value;
                var operation = match.Groups["Operation"].Value;
                return $"SERVER|{type}|{operation}";
            }

            return logMessage; // Return the original message if it doesn't match the expected format
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error formatting message: {ex}");

            return logMessage; // Return the original message if an exception occurs
        }
    }


}
