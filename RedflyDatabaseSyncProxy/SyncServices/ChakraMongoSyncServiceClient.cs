using Grpc.Core;
using Grpc.Net.Client;
using RedflyCoreFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RedflyDatabaseSyncProxy.Protos.Mongo;
using Azure.Core;
using Microsoft.Extensions.Logging;
using System.Threading;
using RedflyDatabaseSyncProxy.Config;
using RedflyDatabaseSyncProxy.Protos.Common;
using RedflyDatabaseSyncProxy.GrpcClients;

namespace RedflyDatabaseSyncProxy.SyncServices;

internal class ChakraMongoSyncServiceClient : ChakraDatabaseSyncServiceClientBase
{

    #region Fields

    private readonly bool _runInitialSync;

    #endregion Fields

    internal ChakraMongoSyncServiceClient(IGrpcDatabaseChakraServiceClient grpcClient, bool runInitialSync) : base(grpcClient)
    {
        _runInitialSync = runInitialSync;
    }

    protected override async Task<StartChakraSyncResponse> StartChakraSyncOnServerAsync()
    {
        return await ((GrpcMongoChakraServiceClient)_grpcClient)
                                        .MongoChakraServiceClient
                                        .StartChakraSyncAsync(
                                                new StartChakraSyncRequest
                                                {
                                                    ClientSessionId = _clientSessionId,
                                                    EncryptedClientId = RedflyEncryption.EncryptToString(Guid.Empty.ToString()),
                                                    EncryptedClientName = RedflyEncryption.EncryptToString(AppSession.ClientAndUserProfileViewModel!.ClientName),
                                                    EncryptionKey = RedflyEncryptionKeys.AesKey,
                                                    EncryptedMongoServerName = AppSession.MongoDatabase!.EncryptedServerName,
                                                    EncryptedMongoDatabaseName = AppSession.MongoDatabase!.EncryptedDatabaseName,
                                                    EncryptedMongoUserName = AppSession.MongoDatabase!.EncryptedUserName,
                                                    EncryptedMongoPassword = AppSession.MongoDatabase!.EncryptedPassword,
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
                                                },
                                                _grpcClient.GrpcHeaders);
    }

}
