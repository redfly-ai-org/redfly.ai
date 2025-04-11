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

namespace RedflyDatabaseSyncProxy.SyncServices;

internal class ChakraPostgresSyncServiceClient
{

    internal static async Task StartAsync(string grpcUrl, string grpcAuthToken, bool runInitialSync)
    {
        var clientSessionId = Guid.NewGuid().ToString(); // Unique client identifier
        var channel = GrpcChannel.ForAddress(grpcUrl);
        var chakraClient = new NativeGrpcPostgresChakraService.NativeGrpcPostgresChakraServiceClient(channel);

        var headers = new Metadata
                {
                    { "Authorization", $"Bearer {grpcAuthToken}" },
                    { "client-session-id", clientSessionId.ToString() }
                };

        // Start Chakra Sync
        var startResponse = await chakraClient
                                    .StartChakraSyncAsync(
                                        new StartChakraSyncRequest
                                        {
                                            ClientSessionId = clientSessionId,
                                            // If the key changed AFTER the database was saved locally with a previous key, decryption won't happen!
                                            EncryptionKey = RedflyEncryptionKeys.AesKey,
                                            // TODO: Return client id within ClientAndUserProfileViewModel from cloud so we can use it here.
                                            EncryptedClientId = RedflyEncryption.EncryptToString(Guid.Empty.ToString()),
                                            EncryptedClientName = RedflyEncryption.EncryptToString(AppSession.ClientAndUserProfileViewModel!.ClientName),
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
            Console.WriteLine("Chakra Sync Service started successfully.");
        }
        else
        {
            Console.WriteLine("Failed to start the Chakra Sync Service.");
            return;
        }

        // Bi-directional streaming for communication with the server
        using var call = chakraClient.CommunicateWithClient(headers);

        var responseTask = Task.Run(async () =>
        {
            await foreach (var message in call.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine(FormatGrpcServerMessage(message.Message));
            }
        });

        // Send initial message to establish the stream
        await call.RequestStream.WriteAsync(new ClientMessage { ClientSessionId = clientSessionId, Message = "Client connected" });

        // Keep the client running to listen for server messages
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        // Complete the request stream
        await call.RequestStream.CompleteAsync();
        await responseTask;
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
