using Microsoft.IdentityModel.Tokens;
using RedflyCoreFramework;
using redflyDatabaseAdapters;
using PostgresProtos = redflyGeneratedDataAccessApi.Protos.Postgres;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using redflyGeneratedDataAccessApi.Protos.DatabaseApi;

namespace redflyDataAccessClient.Postgres;

internal class PostgresGrpcServerApiRequests
{
    internal static PostgresProtos.DeleteRequest CreateDeleteRequest(string tableSchemaName, string tableName, Dictionary<string, string> primaryKeyValues)
    {
        var deleteRequest = new PostgresProtos.DeleteRequest
        {
            EncryptedPostgresServerName = AppDbSession.PostgresDatabase!.EncryptedServerName,
            EncryptedPostgresDatabaseName = AppDbSession.PostgresDatabase!.EncryptedDatabaseName,
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(Guid.Empty.ToString()),
            EncryptedClientName = RedflyEncryption.EncryptToString(AppGrpcSession.ClientAndUserProfileViewModel!.ClientName),
            EncryptedPostgresUserName = AppDbSession.PostgresDatabase!.EncryptedUserName,
            EncryptedPostgresPassword = AppDbSession.PostgresDatabase!.EncryptedPassword,
            EncryptedRedisServerName = AppDbSession.RedisServer!.EncryptedServerName,
            RedisPortNo = AppDbSession.RedisServer!.Port,
            EncryptedRedisPassword = AppDbSession.RedisServer!.EncryptedPassword,
            RedisUsesSsl = AppDbSession.RedisServer!.UsesSsl,
            RedisSslProtocol = AppDbSession.RedisServer!.SslProtocol,
            RedisAbortConnect = AppDbSession.RedisServer!.AbortConnect,
            RedisConnectTimeout = AppDbSession.RedisServer!.ConnectTimeout,
            RedisSyncTimeout = AppDbSession.RedisServer!.SyncTimeout,
            RedisAsyncTimeout = AppDbSession.RedisServer!.AsyncTimeout,
            EncryptionKey = RedflyEncryptionKeys.AesKey,
            ModifyCache = true
        };

        foreach (var kvp in primaryKeyValues)
        {
            deleteRequest.PrimaryKeyValues.Add(kvp.Key, kvp.Value);
        }

        return deleteRequest;
    }

    internal static PostgresProtos.InsertRequest CreateInsertRequest(string tableSchemaName, string tableName, Dictionary<string, string> insertedData)
    {
        var insertRequest = new PostgresProtos.InsertRequest
        {
            EncryptedPostgresServerName = AppDbSession.PostgresDatabase!.EncryptedServerName,
            EncryptedPostgresDatabaseName = AppDbSession.PostgresDatabase!.EncryptedDatabaseName,
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(Guid.Empty.ToString()),
            EncryptedClientName = RedflyEncryption.EncryptToString(AppGrpcSession.ClientAndUserProfileViewModel!.ClientName),
            EncryptedPostgresUserName = AppDbSession.PostgresDatabase!.EncryptedUserName,
            EncryptedPostgresPassword = AppDbSession.PostgresDatabase!.EncryptedPassword,
            EncryptedRedisServerName = AppDbSession.RedisServer!.EncryptedServerName,
            RedisPortNo = AppDbSession.RedisServer!.Port,
            EncryptedRedisPassword = AppDbSession.RedisServer!.EncryptedPassword,
            RedisUsesSsl = AppDbSession.RedisServer!.UsesSsl,
            RedisSslProtocol = AppDbSession.RedisServer!.SslProtocol,
            RedisAbortConnect = AppDbSession.RedisServer!.AbortConnect,
            RedisConnectTimeout = AppDbSession.RedisServer!.ConnectTimeout,
            RedisSyncTimeout = AppDbSession.RedisServer!.SyncTimeout,
            RedisAsyncTimeout = AppDbSession.RedisServer!.AsyncTimeout,
            EncryptionKey = RedflyEncryptionKeys.AesKey,
            ModifyCache = true
        };

        insertRequest.Row = new Row();

        foreach (var kvp in insertedData)
        {
            insertRequest.Row.Entries.Add(new RowEntry() 
            { 
                Column = kvp.Key, 
                Value = new Value() 
                { 
                    StringValue = kvp.Value.IsNullOrEmpty() ? null : kvp.Value 
                } 
            });
        }

        return insertRequest;
    }

    internal static PostgresProtos.UpdateRequest CreateUpdateRequest(string tableSchemaName, string tableName, Dictionary<string, string> updatedData)
    {
        var updateRequest = new PostgresProtos.UpdateRequest
        {
            EncryptedPostgresServerName = AppDbSession.PostgresDatabase!.EncryptedServerName,
            EncryptedPostgresDatabaseName = AppDbSession.PostgresDatabase!.EncryptedDatabaseName,
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(Guid.Empty.ToString()),
            EncryptedClientName = RedflyEncryption.EncryptToString(AppGrpcSession.ClientAndUserProfileViewModel!.ClientName),
            EncryptedPostgresUserName = AppDbSession.PostgresDatabase!.EncryptedUserName,
            EncryptedPostgresPassword = AppDbSession.PostgresDatabase!.EncryptedPassword,
            EncryptedRedisServerName = AppDbSession.RedisServer!.EncryptedServerName,
            RedisPortNo = AppDbSession.RedisServer!.Port,
            EncryptedRedisPassword = AppDbSession.RedisServer!.EncryptedPassword,
            RedisUsesSsl = AppDbSession.RedisServer!.UsesSsl,
            RedisSslProtocol = AppDbSession.RedisServer!.SslProtocol,
            RedisAbortConnect = AppDbSession.RedisServer!.AbortConnect,
            RedisConnectTimeout = AppDbSession.RedisServer!.ConnectTimeout,
            RedisSyncTimeout = AppDbSession.RedisServer!.SyncTimeout,
            RedisAsyncTimeout = AppDbSession.RedisServer!.AsyncTimeout,
            EncryptionKey = RedflyEncryptionKeys.AesKey,
            ModifyCache = true
        };

        updateRequest.Row = new Row();

        foreach (var kvp in updatedData)
        {
            updateRequest.Row.Entries.Add(new RowEntry() 
            { 
                Column = kvp.Key, 
                Value = new Value() 
                { 
                    StringValue = kvp.Value.IsNullOrEmpty() ? null : kvp.Value 
                } 
            });
        }

        return updateRequest;
    }

    internal static PostgresProtos.GetRequest CreateGetRequest(string tableSchemaName, string tableName, string primaryKeyColumnName, string primaryKeyColumnValue)
    {
        var getRequest = new PostgresProtos.GetRequest()
        {
            EncryptedPostgresServerName = AppDbSession.PostgresDatabase!.EncryptedServerName,
            EncryptedPostgresDatabaseName = AppDbSession.PostgresDatabase!.EncryptedDatabaseName,
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(Guid.Empty.ToString()),
            EncryptedClientName = RedflyEncryption.EncryptToString(AppGrpcSession.ClientAndUserProfileViewModel!.ClientName),
            EncryptedPostgresUserName = AppDbSession.PostgresDatabase!.EncryptedUserName,
            EncryptedPostgresPassword = AppDbSession.PostgresDatabase!.EncryptedPassword,
            EncryptedRedisServerName = AppDbSession.RedisServer!.EncryptedServerName,
            RedisPortNo = AppDbSession.RedisServer!.Port,
            EncryptedRedisPassword = AppDbSession.RedisServer!.EncryptedPassword,
            RedisUsesSsl = AppDbSession.RedisServer!.UsesSsl,
            RedisSslProtocol = AppDbSession.RedisServer!.SslProtocol,
            RedisAbortConnect = AppDbSession.RedisServer!.AbortConnect,
            RedisConnectTimeout = AppDbSession.RedisServer!.ConnectTimeout,
            RedisSyncTimeout = AppDbSession.RedisServer!.SyncTimeout,
            RedisAsyncTimeout = AppDbSession.RedisServer!.AsyncTimeout,
            EncryptionKey = RedflyEncryptionKeys.AesKey
        };

        getRequest.PrimaryKeyValues.Add(primaryKeyColumnName, primaryKeyColumnValue);
        return getRequest;
    }

    internal static PostgresProtos.GetRowsRequest CreateGetRowsRequest(string tableSchemaName, string tableName, string orderByColumnName, string orderByColumnSort)
    {
        return new PostgresProtos.GetRowsRequest
        {
            EncryptedPostgresServerName = AppDbSession.PostgresDatabase!.EncryptedServerName,
            EncryptedPostgresDatabaseName = AppDbSession.PostgresDatabase!.EncryptedDatabaseName,
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(Guid.Empty.ToString()),
            EncryptedClientName = RedflyEncryption.EncryptToString(AppGrpcSession.ClientAndUserProfileViewModel!.ClientName),
            EncryptedPostgresUserName = AppDbSession.PostgresDatabase!.EncryptedUserName,
            EncryptedPostgresPassword = AppDbSession.PostgresDatabase!.EncryptedPassword,
            EncryptedRedisServerName = AppDbSession.RedisServer!.EncryptedServerName,
            RedisPortNo = AppDbSession.RedisServer!.Port,
            EncryptedRedisPassword = AppDbSession.RedisServer!.EncryptedPassword,
            RedisUsesSsl = AppDbSession.RedisServer!.UsesSsl,
            RedisSslProtocol = AppDbSession.RedisServer!.SslProtocol,
            RedisAbortConnect = AppDbSession.RedisServer!.AbortConnect,
            RedisConnectTimeout = AppDbSession.RedisServer!.ConnectTimeout,
            RedisSyncTimeout = AppDbSession.RedisServer!.SyncTimeout,
            RedisAsyncTimeout = AppDbSession.RedisServer!.AsyncTimeout,
            EncryptionKey = RedflyEncryptionKeys.AesKey,
            OrderbyColumnName = orderByColumnName,
            OrderbyColumnSort = orderByColumnSort,
            PageNo = 1,
            PageSize = 5
        };
    }

    internal static PostgresProtos.GetTotalRowCountRequest CreateGetTotalRowCountRequest(string tableSchemaName, string tableName)
    {
        return new PostgresProtos.GetTotalRowCountRequest
        {
            EncryptedPostgresServerName = AppDbSession.PostgresDatabase!.EncryptedServerName,
            EncryptedPostgresDatabaseName = AppDbSession.PostgresDatabase!.EncryptedDatabaseName,
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(Guid.Empty.ToString()),
            EncryptedClientName = RedflyEncryption.EncryptToString(AppGrpcSession.ClientAndUserProfileViewModel!.ClientName),
            EncryptedPostgresUserName = AppDbSession.PostgresDatabase!.EncryptedUserName,
            EncryptedPostgresPassword = AppDbSession.PostgresDatabase!.EncryptedPassword,
            EncryptedRedisServerName = AppDbSession.RedisServer!.EncryptedServerName,
            RedisPortNo = AppDbSession.RedisServer!.Port,
            EncryptedRedisPassword = AppDbSession.RedisServer!.EncryptedPassword,
            RedisUsesSsl = AppDbSession.RedisServer!.UsesSsl,
            RedisSslProtocol = AppDbSession.RedisServer!.SslProtocol,
            RedisAbortConnect = AppDbSession.RedisServer!.AbortConnect,
            RedisConnectTimeout = AppDbSession.RedisServer!.ConnectTimeout,
            RedisSyncTimeout = AppDbSession.RedisServer!.SyncTimeout,
            RedisAsyncTimeout = AppDbSession.RedisServer!.AsyncTimeout,
            EncryptionKey = RedflyEncryptionKeys.AesKey
        };
    }
}