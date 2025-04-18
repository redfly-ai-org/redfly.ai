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
            (asyncDuplexStreamingCall, responseTask) = await StartBidirectionalStreamingAsync(clientSessionId, chakraClient, headers);

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

    private static async Task<(AsyncDuplexStreamingCall<ClientMessage, ServerMessage> asyncDuplexStreamingCall, Task responseTask)> StartBidirectionalStreamingAsync(
                                                    string clientSessionId, 
                                                    NativeGrpcPostgresChakraService.NativeGrpcPostgresChakraServiceClient chakraClient, 
                                                    Metadata headers,
                                                    int retryCount = 0)
    {
        var streamingSucceeded = false;
        Console.WriteLine($"Attempt #{retryCount}: Going to start bi-directional streaming with server");

        // Bi-directional streaming for communication with the server
        var asyncDuplexStreamingCall = chakraClient.CommunicateWithClient(headers);

        var responseTask = Task.Run(async () =>
        {
            try
            {
                Console.WriteLine($"BI-DIR> Attempt #{retryCount}: Reading from bi-directional stream");

                await foreach (var message in asyncDuplexStreamingCall.ResponseStream.ReadAllAsync())
                {
                    Console.WriteLine(FormatGrpcServerMessage(message.Message));
                    streamingSucceeded = true;
                }
            }
            catch (RpcException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"BI-DIR> Attempt #{retryCount}: gRPC error occurred: {ex.Status}");
                Console.ResetColor();
                streamingSucceeded = false;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"BI-DIR> Attempt #{retryCount}: An unexpected error occurred: {ex.ToString()}");
                Console.WriteLine($"Attempt #{retryCount}: GIVING UP AFTER EXHAUSTING RETRIES");
                Console.ResetColor();
                streamingSucceeded = false;
            }
        });

        Console.WriteLine($"INIT> Attempt #{retryCount}: Sending initial message to establish the stream");

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
                            ClientSessionId = clientSessionId,
                            Message = $"Client Registration Message. Attempt {retryCount}"
                        });

            Console.WriteLine($"INIT> Attempt #{retryCount}: Initial message successfully sent to server");
        }
        catch (Exception ex)
        {
            retryCount += 1;
            Console.WriteLine($"INIT>Attempt #{retryCount}: Error sending initial message: {ex.ToString()}. Waiting 10 seconds before attempt {retryCount}...");

            await Task.Delay(10 * 1000);
            return await StartBidirectionalStreamingAsync(clientSessionId, chakraClient, headers, retryCount);
        }

        Console.WriteLine("INIT: Waiting for 1 minute to see IF the bi-directional streaming is still working properly...");
        await Task.Delay(60 * 1000);

        if (!streamingSucceeded)
        {
            retryCount += 1;
            Console.WriteLine($"INIT>Attempt #{retryCount}: Bi-directional streaming failed after a minute. Waiting 10 seconds before attempt {retryCount}...");

            await Task.Delay(10 * 1000);
            return await StartBidirectionalStreamingAsync(clientSessionId, chakraClient, headers, retryCount);
        }

        Console.WriteLine("INIT: Waiting for 5 minutes to see IF the bi-directional streaming is still working properly...");
        await Task.Delay(5 * 60 * 1000);

        if (!streamingSucceeded)
        {
            retryCount += 1;
            Console.WriteLine($"INIT>Attempt #{retryCount}: Bi-directional streaming failed after 5 minutes. Waiting 10 seconds before attempt {retryCount}...");

            await Task.Delay(10 * 1000);
            return await StartBidirectionalStreamingAsync(clientSessionId, chakraClient, headers, retryCount);
        }

        Console.WriteLine($"Attempt #{retryCount}: Returning after starting BI-DIR streaming.");
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
