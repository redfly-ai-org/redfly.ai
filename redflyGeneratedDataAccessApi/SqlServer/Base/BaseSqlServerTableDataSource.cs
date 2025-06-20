﻿using Grpc.Net.Client;
using RedflyCoreFramework;
using redflyGeneratedDataAccessApi.Protos.DatabaseApi;
using redflyDatabaseAdapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using redflyGeneratedDataAccessApi.Protos.SqlServer;
using redflyGeneratedDataAccessApi.Common;
using redflyGeneratedDataAccessApi.Base;

namespace redflyGeneratedDataAccessApi.SqlServer;

public abstract class BaseSqlServerTableDataSource<T> : BaseDatabaseTableDataSource where T : BaseSqlServerTableSchema
{

    protected readonly NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient _client;
    protected readonly string _encDbServer, _encDbName, _encClientId, _encDbId, _encConnStr;
    protected string _encSchema = "";

    protected BaseSqlServerTableDataSource() : base()
    {
        _client = new NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient(_channel);

        //Everything else comes from the environment.        
        _encDbServer = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile!.Database.HostName);
        _encDbName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Name);
        _encClientId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.ClientId);
        _encDbId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Id);
        _encConnStr = RedflyEncryption.EncryptToString($"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;");        
    }

    protected abstract T MapRowToTableEntity(Row row);

    protected abstract Row MapTableEntityToRow(T address, DbOperationType dbOperationType);
    
    public async Task<GenericRowsData> GetSqlRowsAsync(string sqlQuery)
    {
        var encSqlQuery = RedflyEncryption.EncryptToString(sqlQuery);
        var req = new GetSqlRowsRequest
        {
            EncryptedDatabaseServerName = _encDbServer,
            EncryptedDatabaseName = _encDbName,
            EncryptedTableSchemaName = _encSchema,
            EncryptedTableName = _encTable,
            EncryptedClientId = _encClientId,
            EncryptedDatabaseId = _encDbId,
            EncryptedServerOnlyConnectionString = _encConnStr,
            EncryptedSqlQuery = encSqlQuery,
            EncryptionKey = _encryptionKey
        };
        var resp = await _client.GetSqlRowsAsync(req, AppGrpcSession.Headers!);
        return new GenericRowsData
        {
            Success = resp.Success,
            Rows = resp.Rows.ToList(),
            FromCache = resp.FromCache,
            Message = resp.Message
        };
    }

    public async Task<TotalRowCount> GetTotalRowCountAsync()
    {
        var req = new GetTotalRowCountRequest
        {
            EncryptedDatabaseServerName = _encDbServer,
            EncryptedDatabaseName = _encDbName,
            EncryptedTableSchemaName = _encSchema,
            EncryptedTableName = _encTable,
            EncryptedClientId = _encClientId,
            EncryptedDatabaseId = _encDbId,
            EncryptedServerOnlyConnectionString = _encConnStr,
            EncryptionKey = _encryptionKey
        };
        var resp = await _client.GetTotalRowCountAsync(req, AppGrpcSession.Headers!);
        return new TotalRowCount
        {
            Total = resp.Total,
            FromCache = resp.FromCache,
            Message = resp.Message
        };
    }

    protected async Task<UpdatedData> UpdateCoreAsync(UpdateRequest req)
    {
        var resp = await _client.UpdateAsync(req, AppGrpcSession.Headers!);

        return new UpdatedData
        {
            Success = resp.Success,
            UpdatedCount = resp.UpdatedCount,
            CacheUpdated = resp.CacheUpdated,
            Message = resp.Message
        };
    }

    protected async Task<DeletedData> DeleteCoreAsync(DeleteRequest req)
    {
        var resp = await _client.DeleteAsync(req, AppGrpcSession.Headers!);

        return new DeletedData
        {
            Success = resp.Success,
            CacheUpdated = resp.CacheUpdated,
            Message = resp.Message
        };
    }

    protected DeleteRequest CreateDeleteRequest(bool modifyCache)
    {
        return new DeleteRequest
        {
            EncryptedDatabaseServerName = _encDbServer,
            EncryptedDatabaseName = _encDbName,
            EncryptedTableSchemaName = _encSchema,
            EncryptedTableName = _encTable,
            EncryptedClientId = _encClientId,
            EncryptedDatabaseId = _encDbId,
            EncryptedServerOnlyConnectionString = _encConnStr,
            EncryptionKey = _encryptionKey,
            ModifyCache = modifyCache
        };
    }

    protected GetRowsRequest CreateGetRowsRequest(int pageNo, int pageSize, string orderByColumnName, string orderBySort)
    {
        return new GetRowsRequest
        {
            EncryptedDatabaseServerName = _encDbServer,
            EncryptedDatabaseName = _encDbName,
            EncryptedTableSchemaName = _encSchema,
            EncryptedTableName = _encTable,
            EncryptedClientId = _encClientId,
            EncryptedDatabaseId = _encDbId,
            EncryptedServerOnlyConnectionString = _encConnStr,
            EncryptionKey = _encryptionKey,
            PageNo = pageNo,
            PageSize = pageSize,
            OrderbyColumnName = orderByColumnName,
            OrderbyColumnSort = orderBySort
        };
    }

    protected InsertRequest CreateInsertRequest(T address, bool modifyCache)
    {
        return new InsertRequest
        {
            EncryptedDatabaseServerName = _encDbServer,
            EncryptedDatabaseName = _encDbName,
            EncryptedTableSchemaName = _encSchema,
            EncryptedTableName = _encTable,
            EncryptedClientId = _encClientId,
            EncryptedDatabaseId = _encDbId,
            EncryptedServerOnlyConnectionString = _encConnStr,
            EncryptionKey = _encryptionKey,
            ModifyCache = modifyCache,
            Row = MapTableEntityToRow(address, DbOperationType.Insert)
        };
    }

    protected GetRequest CreateGetRequest()
    {
        return new GetRequest
        {
            EncryptedDatabaseServerName = _encDbServer,
            EncryptedDatabaseName = _encDbName,
            EncryptedTableSchemaName = _encSchema,
            EncryptedTableName = _encTable,
            EncryptedClientId = _encClientId,
            EncryptedDatabaseId = _encDbId,
            EncryptedServerOnlyConnectionString = _encConnStr,
            EncryptionKey = _encryptionKey
        };
    }

    protected UpdateRequest CreateUpdateRequest(T address, bool modifyCache)
    {
        return new UpdateRequest
        {
            EncryptedDatabaseServerName = _encDbServer,
            EncryptedDatabaseName = _encDbName,
            EncryptedTableSchemaName = _encSchema,
            EncryptedTableName = _encTable,
            EncryptedClientId = _encClientId,
            EncryptedDatabaseId = _encDbId,
            EncryptedServerOnlyConnectionString = _encConnStr,
            EncryptionKey = _encryptionKey,
            ModifyCache = modifyCache,
            Row = MapTableEntityToRow(address, DbOperationType.Update)
        };
    }

}
