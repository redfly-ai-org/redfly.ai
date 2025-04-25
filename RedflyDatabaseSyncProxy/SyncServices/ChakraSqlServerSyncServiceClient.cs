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

namespace RedflyDatabaseSyncProxy.SyncServices;

internal class ChakraSqlServerSyncServiceClient
{

    private static string _clientSessionId = ClientSessionId.Generate();

    internal static async Task StartAsync(string grpcUrl, string grpcAuthToken)
    {
        var channel = GrpcChannel.ForAddress(grpcUrl);
        var chakraClient = new NativeGrpcSqlServerChakraService.NativeGrpcSqlServerChakraServiceClient(channel);

        var headers = new Metadata
                {
                    { "Authorization", $"Bearer {grpcAuthToken}" },
                    { "client-session-id", _clientSessionId.ToString() }
                };

        // Start Chakra Sync
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
        await call.RequestStream.WriteAsync(new ClientMessage { ClientSessionId = _clientSessionId, Message = "Client connected" });

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
