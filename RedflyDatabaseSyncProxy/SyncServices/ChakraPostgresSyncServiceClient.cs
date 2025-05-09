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

    #region Fields

    private readonly bool _runInitialSync;

    #endregion Fields

    internal ChakraPostgresSyncServiceClient(IGrpcDatabaseChakraServiceClient grpcClient, bool runInitialSync) : base(grpcClient)
    {
        _runInitialSync = runInitialSync;
    }

    protected override async Task<StartChakraSyncResponse> StartChakraSyncOnServerAsync()
    {
        return await ((GrpcPostgresChakraServiceClient)_grpcClient)
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
    }

}
