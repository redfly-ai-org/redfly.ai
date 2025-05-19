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
using RedflyDatabaseSyncProxy.Config;
using RedflyDatabaseSyncProxy.Protos.Common;
using RedflyDatabaseSyncProxy.GrpcClients;
using redflyDatabaseAdapters;

namespace RedflyDatabaseSyncProxy.SyncServices;

internal class ChakraSqlServerSyncServiceClient : ChakraDatabaseSyncServiceClientBase
{

    internal ChakraSqlServerSyncServiceClient(IGrpcDatabaseChakraServiceClient grpcClient) : base(grpcClient)
    {
    }

    protected override async Task<StartChakraSyncResponse> StartChakraSyncOnServerAsync()
    {
        return await ((GrpcSqlServerChakraServiceClient)_grpcClient)
                                            .SqlServerChakraServiceClient
                                            .StartChakraSyncAsync(
                                                new StartChakraSyncRequest
                                                {
                                                    ClientSessionId = _clientSessionId,
                                                    // If the key changed AFTER the database was saved locally with a previous key, decryption won't happen!
                                                    EncryptionKey = RedflyEncryptionKeys.AesKey,
                                                    EncryptedClientId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile!.Database.ClientId),
                                                    EncryptedClientName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.ClientName),
                                                    EncryptedDatabaseId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Id),
                                                    EncryptedDatabaseName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Name),
                                                    EncryptedDatabaseServerName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.HostName),
                                                    EncryptedServerOnlyConnectionString = RedflyEncryption.EncryptToString($"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;")
                                                },
                                                _grpcClient.GrpcHeaders);
    }

}
