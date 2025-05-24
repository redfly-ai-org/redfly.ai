using Microsoft.IdentityModel.Tokens;
using RedflyCoreFramework;
using redflyDatabaseAdapters;
using redflyGeneratedDataAccessApi.Protos.SqlServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyDataAccessClient;
internal class GrpcApiRequests
{

    internal static DeleteRequest CreateDeleteRequest(string tableSchemaName, string tableName, Dictionary<string, string> primaryKeyValues)
    {
        var deleteRequest = new DeleteRequest
        {
            EncryptedDatabaseServerName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.HostName),
            EncryptedDatabaseName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Name),
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile!.Database.ClientId),
            EncryptedDatabaseId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Id),
            EncryptedServerOnlyConnectionString = RedflyEncryption.EncryptToString(
                $"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;"),
            EncryptionKey = RedflyEncryptionKeys.AesKey,
            ModifyCache = true
        };

        foreach (var kvp in primaryKeyValues)
        {
            deleteRequest.PrimaryKeyValues.Add(kvp.Key, kvp.Value);
        }

        return deleteRequest;
    }

    internal static InsertRequest CreateInsertRequest(string tableSchemaName, string tableName, Dictionary<string, string> insertedData)
    {
        var insertRequest = new InsertRequest
        {
            EncryptedDatabaseServerName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.HostName),
            EncryptedDatabaseName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Name),
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile!.Database.ClientId),
            EncryptedDatabaseId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Id),
            EncryptedServerOnlyConnectionString = RedflyEncryption.EncryptToString($"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;"),
            EncryptionKey = RedflyEncryptionKeys.AesKey,
            ModifyCache = true
        };

        insertRequest.Row = new Row();

        foreach (var kvp in insertedData)
        {
            insertRequest.Row.Entries.Add(new RowEntry() { Column = kvp.Key, Value = new Value() { StringValue = kvp.Value.IsNullOrEmpty() ? null : kvp.Value } });
        }

        return insertRequest;
    }

    internal static UpdateRequest CreateUpdateRequest(string tableSchemaName, string tableName, Dictionary<string, string> updatedData)
    {
        var updateRequest = new UpdateRequest
        {
            EncryptedDatabaseServerName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.HostName),
            EncryptedDatabaseName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Name),
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile!.Database.ClientId),
            EncryptedDatabaseId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Id),
            EncryptedServerOnlyConnectionString = RedflyEncryption.EncryptToString($"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;"),
            EncryptionKey = RedflyEncryptionKeys.AesKey,
            ModifyCache = true
        };

        updateRequest.Row = new Row();

        foreach (var kvp in updatedData)
        {
            updateRequest.Row.Entries.Add(new RowEntry() { Column = kvp.Key, Value = new Value() { StringValue = kvp.Value.IsNullOrEmpty() ? null : kvp.Value } });
        }

        return updateRequest;
    }

    internal static GetRequest CreateGetRequest(string tableSchemaName, string tableName, string primaryKeyColumnName, string primaryKeyColumnValue)
    {
        var getRequest = new GetRequest()
        {
            EncryptedDatabaseServerName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.HostName),
            EncryptedDatabaseName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Name),
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile!.Database.ClientId),
            EncryptedDatabaseId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Id),
            EncryptedServerOnlyConnectionString = RedflyEncryption.EncryptToString($"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;"),
            EncryptionKey = RedflyEncryptionKeys.AesKey
        };

        getRequest.PrimaryKeyValues.Add(primaryKeyColumnName, primaryKeyColumnValue);
        return getRequest;
    }

    internal static GetRowsRequest CreateGetRowsCachedRequest(string tableSchemaName, string tableName, string orderByColumnName, string orderByColumnSort)
    {
        return new GetRowsRequest
        {
            EncryptedDatabaseServerName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.HostName),
            EncryptedDatabaseName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Name),
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile!.Database.ClientId),
            EncryptedDatabaseId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Id),
            EncryptedServerOnlyConnectionString = RedflyEncryption.EncryptToString($"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;"),
            EncryptionKey = RedflyEncryptionKeys.AesKey,
            OrderbyColumnName = orderByColumnName,
            OrderbyColumnSort = orderByColumnSort,
            PageNo = 1,
            PageSize = 5
        };
    }

    internal static GetTotalRowCountRequest CreateGetTotalRowCountRequest(string tableSchemaName, string tableName)
    {
        return new GetTotalRowCountRequest
        {
            EncryptedDatabaseServerName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.HostName),
            EncryptedDatabaseName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Name),
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile!.Database.ClientId),
            EncryptedDatabaseId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Id),
            EncryptedServerOnlyConnectionString = RedflyEncryption.EncryptToString($"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;"),
            EncryptionKey = RedflyEncryptionKeys.AesKey
        };
    }


}
