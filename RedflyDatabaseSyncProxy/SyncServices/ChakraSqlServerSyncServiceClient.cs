using Grpc.Core;
using Grpc.Net.Client;
using RedflyCoreFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RedflyDatabaseSyncProxy.Protos.SqlServer;
using Microsoft.Extensions.Logging;

namespace RedflyDatabaseSyncProxy.SyncServices;

internal class ChakraSqlServerSyncServiceClient
{

    private static bool _bidirectionalStreamingIsWorking = false;
    private static int _bidirStreamingRetryCount = 0;

    private static string _clientSessionId = ClientSessionId.Generate();
    private static readonly FixedSizeList<Exception> _lastBidirErrors = new FixedSizeList<Exception>(5);

    private static Metadata? _grpcHeaders;

    internal static async Task StartAsync(string grpcUrl, string grpcAuthToken)
    {
        //AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        //var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        var channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
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

        var chakraClient = new NativeGrpcSqlServerChakraService.NativeGrpcSqlServerChakraServiceClient(channel);

        _grpcHeaders = new Metadata
                {
                    { "Authorization", $"Bearer {grpcAuthToken}" },
                    { "client-session-id", _clientSessionId.ToString() }
                };

        // Start Chakra Sync
        if (!await StartChakraSyncAsyncWithRetry(chakraClient))
        {
            return;
        }

        AsyncDuplexStreamingCall<ClientMessage, ServerMessage>? asyncDuplexStreamingCall = null;
        Task bidirectionalTask;

        try
        {
            (asyncDuplexStreamingCall, bidirectionalTask) = await StartBidirStreamingAsync(chakraClient);

            var bidirConnMonitorCancelTokenSource = new CancellationTokenSource();
            var bidirConnMonitorCancelToken = bidirConnMonitorCancelTokenSource.Token;

            var bidirConnMonitorTask = Task.Run(async () =>
            {
                int delayTimeMs = 3 * 1000; // Start with 3 seconds
                const int minDelayMs = 3 * 1000; // Minimum delay: 3 seconds
                const int maxDelayMs = 10 * 60 * 1000; // Maximum delay: 10 minutes
                const int delayStepMs = 3 * 1000; // Step to increase or decrease delay: 3 seconds

                while (!bidirConnMonitorCancelToken.IsCancellationRequested)
                {
                    await Task.Delay(delayTimeMs, bidirConnMonitorCancelToken);

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
                            _grpcHeaders = new Metadata
                                            {
                                                { "Authorization", $"Bearer {authToken}" },
                                                { "client-session-id", _clientSessionId.ToString() }
                                            };
                        }

                        //Console.ForegroundColor = ConsoleColor.Red;
                        //Console.WriteLine($"MONITOR: Reconnecting to the server to restart bi-directional streaming. The last delay time was {delayTimeMs / 1000} secs.");
                        //Console.ResetColor();

                        _bidirStreamingRetryCount += 1;

                        if (_bidirStreamingRetryCount < 20)
                        {
                            //Not worth trying to bi-dir stream more than 20 times
                            (asyncDuplexStreamingCall, bidirectionalTask) = await StartBidirStreamingAsync(chakraClient);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Reverting to normal Grpc calls after {_bidirStreamingRetryCount} failed attempts because of network issues in bi-directional Grpc streaming.");
                            Console.ResetColor();
                        }

                        //Get status manually
                        await GetChakraSyncStatusWithRetryAsync(chakraClient);

                        // Decrease delay time when the flag is false
                        delayTimeMs = Math.Max(minDelayMs, delayTimeMs - delayStepMs);
                    }
                    else
                    {
                        // Will give bi-dir streaming more chances to succeed.
                        _bidirStreamingRetryCount -= 1;

                        // Increase delay time when the flag is true
                        delayTimeMs = Math.Min(maxDelayMs, delayTimeMs + delayStepMs);
                    }
                }

            }, bidirConnMonitorCancelToken);

            // Keep the client running to listen for server messages
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Press any key to exit...");
            Console.ResetColor();
            Console.ReadKey();

            try
            {
                await StopChakraSyncAsync(chakraClient);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Stopped Chakra Sync.");
                Console.ResetColor();
            }
            finally
            {
                bidirConnMonitorCancelTokenSource.Cancel();

                try
                {
                    await bidirConnMonitorTask;
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

    private static async Task GetChakraSyncStatusWithRetryAsync(
                                NativeGrpcSqlServerChakraService.NativeGrpcSqlServerChakraServiceClient chakraClient,
                                int retryCount = 0)
    {
        try
        {
            var syncStatusResponse = await chakraClient
                                        .GetChakraSyncStatusAsync(
                                            new GetChakraSyncStatusRequest()
                                            { ClientSessionId = _clientSessionId },
                                            _grpcHeaders);

            if (syncStatusResponse.Success)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(FormatGrpcServerMessage(syncStatusResponse.Message) + " (*)");
                Console.ResetColor();
            }
        }
        catch (RpcException) when (retryCount < 5)
        {
            await Task.Delay(retryCount * 1000 * 5);

            retryCount += 1;
            await GetChakraSyncStatusWithRetryAsync(chakraClient, retryCount);
        }
        catch (Exception)
        {
        }
    }

    private static async Task StopChakraSyncAsync(
                                NativeGrpcSqlServerChakraService.NativeGrpcSqlServerChakraServiceClient chakraClient)
    {
        var stopResponse = await chakraClient
                                    .StopChakraSyncAsync(
                                        new StopChakraSyncRequest()
                                        { ClientSessionId = _clientSessionId },
                                        _grpcHeaders);

        if (stopResponse.Success)
        {
            Console.WriteLine("Chakra Sync Service stopped successfully.");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"SRVR|{stopResponse.Message}");
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

    private static async Task<(AsyncDuplexStreamingCall<ClientMessage, ServerMessage> call, Task responseTask)> StartBidirStreamingAsync(NativeGrpcSqlServerChakraService.NativeGrpcSqlServerChakraServiceClient chakraClient)
    {
        _bidirectionalStreamingIsWorking = false;
        //Console.WriteLine($"Session #{_bidirStreamingRetryCount}: Calling Server to setup bi-directional streaming...");

        // Bi-directional streaming for communication with the server
        var asyncDuplexStreamingCall = chakraClient.CommunicateWithClient(_grpcHeaders);

        var bidirectionalTask = Task.Run(async () =>
        {
            try
            {
                _bidirectionalStreamingIsWorking = true;
                //Console.WriteLine($"BI-DIR> Session #{_bidirStreamingRetryCount}: Reading from bi-directional stream...");

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

                //Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine($"    BI-DIR> Session #{_bidirStreamingRetryCount}: gRPC error occurred: {rpcex.Status}, message: {rpcex.Message}");
                //Console.ResetColor();
                _bidirectionalStreamingIsWorking = false;
            }
            catch (Exception ex)
            {
                _lastBidirErrors.Add(ex);

                //Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine($"    BI-DIR> Session #{_bidirStreamingRetryCount}: An unexpected error occurred: {ex.Message}");
                //Console.ResetColor();
                _bidirectionalStreamingIsWorking = false;
            }
        });

        //Console.WriteLine($"INIT> Session #{_bidirStreamingRetryCount}: Sending initial message to establish the stream");

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
                            Message = $"Client Registration Message. Session #{_bidirStreamingRetryCount}"
                        });

            //Console.WriteLine($"INIT> Session #{_bidirStreamingRetryCount}: Initial message successfully sent to server");
        }
        catch (Exception ex)
        {
            _lastBidirErrors.Add(ex);

            _bidirStreamingRetryCount += 1;
            //Console.WriteLine($"INIT>Session #{_bidirStreamingRetryCount}: Error sending initial message: {ex.Message}. Waiting 10 seconds before attempt {_bidirStreamingRetryCount}...");

            await Task.Delay(10 * 1000);
            return await StartBidirStreamingAsync(chakraClient);
        }

        //Console.WriteLine($"Session #{_bidirStreamingRetryCount}: Started BI-DIR streaming.");
        return (asyncDuplexStreamingCall, bidirectionalTask);
    }

    private static async Task<bool> StartChakraSyncAsyncWithRetry(NativeGrpcSqlServerChakraService.NativeGrpcSqlServerChakraServiceClient chakraClient)
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
                                                    // If the key changed AFTER the database was saved locally with a previous key, decryption won't happen!
                                                    EncryptionKey = RedflyEncryptionKeys.AesKey,
                                                    EncryptedClientId = RedflyEncryption.EncryptToString(AppSession.SyncProfile!.Database.ClientId),
                                                    EncryptedClientName = RedflyEncryption.EncryptToString(AppSession.SyncProfile.ClientName),
                                                    EncryptedDatabaseId = RedflyEncryption.EncryptToString(AppSession.SyncProfile.Database.Id),
                                                    EncryptedDatabaseName = RedflyEncryption.EncryptToString(AppSession.SyncProfile.Database.Name),
                                                    EncryptedDatabaseServerName = RedflyEncryption.EncryptToString(AppSession.SyncProfile.Database.HostName),
                                                    EncryptedServerOnlyConnectionString = RedflyEncryption.EncryptToString($"Server=tcp:{AppSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppSession.SqlServerDatabase!.DecryptedUserName};Password={AppSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;")
                                                },
                                                _grpcHeaders);

                if (startResponse.Success)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("Chakra Sync Service started successfully.");
                    Console.WriteLine("After a few seconds, you can modify your database and see changes sync to Redis immediately.");
                    Console.WriteLine("If you don't see the update log, refresh your Redis to confirm that changes are available immediately.");

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Please ignore ANY Grpc errors.");
                    Console.ResetColor();
                    Console.WriteLine();

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"SRVR|{startResponse.Message}");
                    Console.ResetColor();
                    return true;
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
