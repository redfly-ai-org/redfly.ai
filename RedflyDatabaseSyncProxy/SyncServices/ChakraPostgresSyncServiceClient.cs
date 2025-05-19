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
using redflyDatabaseSyncProxy;
using redflyDatabaseAdapters;

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
                                                    EncryptedClientName = RedflyEncryption.EncryptToString(AppGrpcSession.ClientAndUserProfileViewModel!.ClientName),
                                                    EncryptionKey = RedflyEncryptionKeys.AesKey,
                                                    EncryptedPostgresServerName = AppDbSession.PostgresDatabase!.EncryptedServerName,
                                                    EncryptedPostgresDatabaseName = AppDbSession.PostgresDatabase!.EncryptedDatabaseName,
                                                    EncryptedPostgresUserName = AppDbSession.PostgresDatabase!.EncryptedUserName,
                                                    EncryptedPostgresPassword = AppDbSession.PostgresDatabase!.EncryptedPassword,
                                                    EncryptedPostgresTestDecodingSlotName = AppDbSession.PostgresDatabase!.EncryptedTestDecodingSlotName,
                                                    EncryptedPostgresPgOutputSlotName = AppDbSession.PostgresDatabase!.EncryptedPgOutputSlotName,
                                                    EncryptedPostgresPublicationName = AppDbSession.PostgresDatabase!.EncryptedPublicationName,
                                                    EncryptedRedisServerName = AppDbSession.RedisServer!.EncryptedServerName,
                                                    RedisPortNo = AppDbSession.RedisServer!.Port,
                                                    EncryptedRedisPassword = AppDbSession.RedisServer!.EncryptedPassword,
                                                    RedisUsesSsl = AppDbSession.RedisServer!.UsesSsl,
                                                    RedisSslProtocol = AppDbSession.RedisServer!.SslProtocol,
                                                    RedisAbortConnect = AppDbSession.RedisServer!.AbortConnect,
                                                    RedisConnectTimeout = AppDbSession.RedisServer!.ConnectTimeout,
                                                    RedisSyncTimeout = AppDbSession.RedisServer!.SyncTimeout,
                                                    RedisAsyncTimeout = AppDbSession.RedisServer!.AsyncTimeout,
                                                    RunInitialSync = _runInitialSync,
                                                    EnableDataReconciliation = true
                                                }, 
                                                _grpcClient.GrpcHeaders);
    }

}
