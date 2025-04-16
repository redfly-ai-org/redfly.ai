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

namespace RedflyDatabaseSyncProxy.SyncServices;

internal class ChakraPostgresSyncServiceClient
{

    internal static async Task StartAsync(string grpcUrl, string grpcAuthToken, bool runInitialSync)
    {
        var clientSessionId = Guid.NewGuid().ToString(); // Unique client identifier
        //var channel = GrpcChannel.ForAddress(grpcUrl);

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
                    { "client-session-id", clientSessionId.ToString() }
                };

        // Start Chakra Sync
        if (!await StartChakraSyncAsyncWithRetry(runInitialSync, clientSessionId, chakraClient, headers))
        { 
            return; 
        }

        AsyncDuplexStreamingCall<ClientMessage, ServerMessage>? asyncDuplexStreamingCall = null;
        Task responseTask;

        try
        {
            (asyncDuplexStreamingCall, responseTask) = await StartBidirectionalStreaming(clientSessionId, chakraClient, headers);

            // Keep the client running to listen for server messages
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

            // Complete the request stream
            await asyncDuplexStreamingCall.RequestStream.CompleteAsync();
            await responseTask;
        }
        finally
        {
            asyncDuplexStreamingCall?.Dispose();
        }
    }

    private static async Task<bool> StartChakraSyncAsyncWithRetry(
                                        bool runInitialSync,
                                        string clientSessionId,
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
                            ClientSessionId = clientSessionId,
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

    private static async Task<(AsyncDuplexStreamingCall<ClientMessage, ServerMessage> asyncDuplexStreamingCall, Task responseTask)> StartBidirectionalStreaming(string clientSessionId, NativeGrpcPostgresChakraService.NativeGrpcPostgresChakraServiceClient chakraClient, Metadata headers)
    {
        Console.WriteLine("Going to start bi-directional streaming with server");
        var serverCommunicationReceived = false;

        // Bi-directional streaming for communication with the server
        var asyncDuplexStreamingCall = chakraClient.CommunicateWithClient(headers);

        var responseTask = Task.Run(async () =>
        {
            int maxRetryAttempts = 5; // Maximum number of retry attempts
            int delayMilliseconds = 1000 * 30; // Initial delay in milliseconds

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    Console.WriteLine($"BI-DIR> Attempt #{attempt}: Reading from bi-directional stream");
                    serverCommunicationReceived = true;

                    await foreach (var message in asyncDuplexStreamingCall.ResponseStream.ReadAllAsync())
                    {
                        Console.WriteLine(FormatGrpcServerMessage(message.Message));
                    }

                    // Exit the retry loop if successful
                    break;
                }
                catch (RpcException ex) when (attempt < maxRetryAttempts)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"BI-DIR> Attempt #{attempt}: gRPC error occurred: {ex.Status}. Retrying in {delayMilliseconds/1000} secs... (Attempt {attempt}/{maxRetryAttempts})");
                    Console.ResetColor();

                    Console.WriteLine($"BI-DIR> Attempt #{attempt}: Sending another initial message to establish the stream");

                    try
                    {
                        await asyncDuplexStreamingCall
                                .RequestStream
                                .WriteAsync(
                                    new ClientMessage
                                    {
                                        ClientSessionId = clientSessionId,
                                        Message = $"Client connected (Attempt #{attempt})"
                                    });
                    }
                    catch (Exception writeEx)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"BI-DIR> Attempt #{attempt}: Error sending initial message: {writeEx.ToString()}");
                        Console.ResetColor();
                    }

                    await Task.Delay(delayMilliseconds);

                    // Exponential backoff
                    delayMilliseconds *= 2;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"BI-DIR> Attempt #{attempt}: An unexpected error occurred: {ex.ToString()}");
                    Console.ResetColor();

                    throw; // Re-throw the exception if it's not a gRPC error or max attempts are reached
                }
            }
        });

        Console.WriteLine("INIT: Sending initial message to establish the stream");

        try
        {
            // Send initial message to establish the stream
            await asyncDuplexStreamingCall
                    .RequestStream
                    .WriteAsync(
                        new ClientMessage
                        {
                            ClientSessionId = clientSessionId,
                            Message = "Client connected (initial attempt #1)"
                        });

            Console.WriteLine("INIT: Initial message successfully sent to server");
            Console.WriteLine("INIT: Waiting for 1 minute to see IF the server responded...");
            await Task.Delay(60 * 1000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"INIT: Error sending initial message: {ex.ToString()}");
        }

        if (!serverCommunicationReceived)
        {
            Console.WriteLine("INIT: Sending initial message #2 to establish the stream");

            try
            {
                await asyncDuplexStreamingCall
                    .RequestStream
                    .WriteAsync(
                        new ClientMessage
                        {
                            ClientSessionId = clientSessionId,
                            Message = "Client connected (initial attempt #2)"
                        });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"INIT: Error sending initial message #2: {ex.ToString()}");
            }
        }

        if (!serverCommunicationReceived)
        {
            Console.WriteLine("INIT: Waiting for 2 minutes to see IF the server responded...");
            await Task.Delay(2 * 60 * 1000);

            Console.WriteLine("INIT: Sending initial message #3 to establish the stream");

            try
            {
                await asyncDuplexStreamingCall
                    .RequestStream
                    .WriteAsync(
                        new ClientMessage
                        {
                            ClientSessionId = clientSessionId,
                            Message = "Client connected (initial attempt #3)"
                        });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"INIT: Error sending initial message #3: {ex.ToString()}");
            }
        }

        return (asyncDuplexStreamingCall, responseTask);
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
