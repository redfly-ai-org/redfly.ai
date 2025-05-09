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
using RedflyDatabaseSyncProxy.Config;
using RedflyDatabaseSyncProxy.Protos.Common;
using RedflyDatabaseSyncProxy.GrpcClients;

namespace RedflyDatabaseSyncProxy.SyncServices;

internal class ChakraPostgresSyncServiceClient : ChakraDatabaseSyncServiceClientBase
{

    private readonly bool _runInitialSync;

    internal ChakraPostgresSyncServiceClient(IGrpcDatabaseChakraServiceClient grpcClient, bool runInitialSync) : base(grpcClient)
    {
        _runInitialSync = runInitialSync;
    }

    protected override async Task<bool> StartChakraSyncAsyncWithRetry()
    {
        int maxRetryAttempts = 5; // Maximum number of retry attempts
        int delayMilliseconds = 1000; // Initial delay in milliseconds

        for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
        {
            try
            {
                Console.WriteLine($"Attempt #{attempt}: StartChakraSyncAsync");

                var startResponse = await ((GrpcPostgresChakraServiceClient) _grpcClient)
                                                .PostgresChakraServiceClient
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
                                                            RunInitialSync = _runInitialSync,
                                                            EnableDataReconciliation = true
                                                        });

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

}
