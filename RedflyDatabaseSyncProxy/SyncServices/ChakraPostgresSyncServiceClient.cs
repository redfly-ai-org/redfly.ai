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
    private static int _bidirectionalStreamingRetryCount = 0;

    private static string _clientSessionId = GenerateUniqueClientSessionId();

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
                EnableMultipleHttp2Connections = true
            }
        });

        var chakraClient = new NativeGrpcPostgresChakraService.NativeGrpcPostgresChakraServiceClient(channel);

        var headers = new Metadata
                {
                    { "Authorization", $"Bearer {grpcAuthToken}" },
                    { "client-session-id", _clientSessionId.ToString() }
                };

        // Start Chakra Sync
        if (!await StartChakraSyncAsyncWithRetry(runInitialSync, chakraClient, headers))
        { 
            return; 
        }

        AsyncDuplexStreamingCall<ClientMessage, ServerMessage>? asyncDuplexStreamingCall = null;
        Task bidirectionalTask;

        try
        {
            (asyncDuplexStreamingCall, bidirectionalTask) = await StartBidirStreamingAsync(chakraClient, headers);

            var connMonitorCancelTokenSource = new CancellationTokenSource();
            var connMonitorCancelToken = connMonitorCancelTokenSource.Token;

            var connMonitorTask = Task.Run(async () =>
            {
                while (!connMonitorCancelToken.IsCancellationRequested)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("INIT: Waiting for 1 minute to see IF the bi-directional streaming is still working properly...");
                    Console.ResetColor();
                    await Task.Delay(60 * 1000, connMonitorCancelToken);

                    if (!_bidirectionalStreamingIsWorking)
                    {
                        _bidirectionalStreamingRetryCount += 1;

                        (asyncDuplexStreamingCall, bidirectionalTask) = await StartBidirStreamingAsync(chakraClient, headers);
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
                await StopChakraSyncAsync(chakraClient, headers);

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
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Chakra Sync Service stopped successfully.");
            Console.WriteLine(stopResponse.Message);
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
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Chakra Sync Service started successfully.");
                    Console.WriteLine(startResponse.Message);
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
        Console.WriteLine($"Attempt #{_bidirectionalStreamingRetryCount}: Going to start bi-directional streaming with server");

        // Bi-directional streaming for communication with the server
        var asyncDuplexStreamingCall = chakraClient.CommunicateWithClient(headers);

        var bidirectionalTask = Task.Run(async () =>
        {
            try
            {
                _bidirectionalStreamingIsWorking = true;
                Console.WriteLine($"BI-DIR> Attempt #{_bidirectionalStreamingRetryCount}: Reading from bi-directional stream");

                await foreach (var message in asyncDuplexStreamingCall.ResponseStream.ReadAllAsync())
                {
                    Console.WriteLine(FormatGrpcServerMessage(message.Message));
                    _bidirectionalStreamingIsWorking = true;
                }
            }
            catch (RpcException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"BI-DIR> Attempt #{_bidirectionalStreamingRetryCount}: gRPC error occurred: {ex.Status}");
                Console.ResetColor();
                _bidirectionalStreamingIsWorking = false;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"BI-DIR> Attempt #{_bidirectionalStreamingRetryCount}: An unexpected error occurred: {ex.ToString()}");
                Console.WriteLine($"Attempt #{_bidirectionalStreamingRetryCount}: GIVING UP AFTER EXHAUSTING RETRIES");
                Console.ResetColor();
                _bidirectionalStreamingIsWorking = false;
            }
        });

        Console.WriteLine($"INIT> Attempt #{_bidirectionalStreamingRetryCount}: Sending initial message to establish the stream");

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
                            Message = $"Client Registration Message. Attempt {_bidirectionalStreamingRetryCount}"
                        });

            Console.WriteLine($"INIT> Attempt #{_bidirectionalStreamingRetryCount}: Initial message successfully sent to server");
        }
        catch (Exception ex)
        {
            _bidirectionalStreamingRetryCount += 1;
            Console.WriteLine($"INIT>Attempt #{_bidirectionalStreamingRetryCount}: Error sending initial message: {ex.ToString()}. Waiting 10 seconds before attempt {_bidirectionalStreamingRetryCount}...");

            await Task.Delay(10 * 1000);
            return await StartBidirStreamingAsync(chakraClient, headers);
        }

        Console.WriteLine($"Attempt #{_bidirectionalStreamingRetryCount}: Returning after starting BI-DIR streaming.");
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
                return $"{type}|{operation}";
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
