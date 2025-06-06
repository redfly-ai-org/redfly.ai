using Grpc.Net.Client;
using RedflyCoreFramework;
using redflyGeneratedDataAccessApi.Protos.DatabaseApi;
using redflyDatabaseAdapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using redflyGeneratedDataAccessApi.Protos.Postgres;
using redflyGeneratedDataAccessApi.Common;
using redflyGeneratedDataAccessApi.Base;

namespace redflyGeneratedDataAccessApi.Postgres;

public abstract class BasePostgresTableDataSource<T> : BaseDatabaseTableDataSource where T : BasePostgresTableSchema
{

    protected readonly NativeGrpcPostgresApiService.NativeGrpcPostgresApiServiceClient _client;
    protected readonly string _encDbServer, _encDbName, _encClientId, _encClientName, _encPostgresUserName, _encPostgresPassword, _encRedisServerName, _encRedisPassword, _redisSslProtocol;
    protected readonly int _redisPortNo, _redisConnectTimeout, _redisSyncTimeout, _redisAsyncTimeout;
    protected readonly bool _redisUsesSsl, _redisAbortConnect;
    protected string _encSchema = "";

    protected BasePostgresTableDataSource() : base()
    {
        _client = new NativeGrpcPostgresApiService.NativeGrpcPostgresApiServiceClient(_channel);

        //Everything else comes from the environment.
        _encDbServer = AppDbSession.PostgresDatabase!.EncryptedServerName;
        _encDbName = AppDbSession.PostgresDatabase!.EncryptedDatabaseName;
        _encClientId = RedflyEncryption.EncryptToString(Guid.Empty.ToString());
        _encClientName = RedflyEncryption.EncryptToString(AppGrpcSession.ClientAndUserProfileViewModel!.ClientName);
        _encPostgresUserName = AppDbSession.PostgresDatabase!.EncryptedUserName;
        _encPostgresPassword = AppDbSession.PostgresDatabase!.EncryptedPassword;
        _encRedisServerName = AppDbSession.RedisServer!.EncryptedServerName;
        _encRedisPassword = AppDbSession.RedisServer!.EncryptedPassword;
        _redisSslProtocol = AppDbSession.RedisServer!.SslProtocol;
        _redisPortNo = AppDbSession.RedisServer!.Port;
        _redisConnectTimeout = AppDbSession.RedisServer!.ConnectTimeout;
        _redisSyncTimeout = AppDbSession.RedisServer!.SyncTimeout;
        _redisAsyncTimeout = AppDbSession.RedisServer!.AsyncTimeout;
        _redisUsesSsl = AppDbSession.RedisServer!.UsesSsl;
        _redisAbortConnect = AppDbSession.RedisServer!.AbortConnect;
    }

    protected abstract T MapRowToTableEntity(Row row);

    protected abstract Row MapTableEntityToRow(T address, DbOperationType dbOperationType);

    public async Task<GenericRowsData> GetSqlRowsAsync(string sqlQuery)
    {
        var encSqlQuery = RedflyEncryption.EncryptToString(sqlQuery);
        var req = new GetSqlRowsRequest
        {
            EncryptedClientId = _encClientId,
            EncryptedClientName = _encClientName,
            EncryptionKey = _encryptionKey,
            EncryptedPostgresServerName = _encDbServer,
            EncryptedPostgresDatabaseName = _encDbName,
            EncryptedPostgresUserName = _encPostgresUserName,
            EncryptedPostgresPassword = _encPostgresPassword,
            EncryptedTableSchemaName = _encSchema,
            EncryptedTableName = _encTable,
            EncryptedSqlQuery = encSqlQuery
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
            EncryptedClientId = _encClientId,
            EncryptedClientName = _encClientName,
            EncryptionKey = _encryptionKey,
            EncryptedPostgresServerName = _encDbServer,
            EncryptedPostgresDatabaseName = _encDbName,
            EncryptedPostgresUserName = _encPostgresUserName,
            EncryptedPostgresPassword = _encPostgresPassword,
            EncryptedRedisServerName = _encRedisServerName,
            RedisPortNo = _redisPortNo,
            EncryptedRedisPassword = _encRedisPassword,
            RedisUsesSsl = _redisUsesSsl,
            RedisSslProtocol = _redisSslProtocol,
            RedisAbortConnect = _redisAbortConnect,
            RedisConnectTimeout = _redisConnectTimeout,
            RedisSyncTimeout = _redisSyncTimeout,
            RedisAsyncTimeout = _redisAsyncTimeout,
            EncryptedTableSchemaName = _encSchema,
            EncryptedTableName = _encTable
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
            EncryptedClientId = _encClientId,
            EncryptedClientName = _encClientName,
            EncryptionKey = _encryptionKey,
            EncryptedPostgresServerName = _encDbServer,
            EncryptedPostgresDatabaseName = _encDbName,
            EncryptedPostgresUserName = _encPostgresUserName,
            EncryptedPostgresPassword = _encPostgresPassword,
            EncryptedRedisServerName = _encRedisServerName,
            RedisPortNo = _redisPortNo,
            EncryptedRedisPassword = _encRedisPassword,
            RedisUsesSsl = _redisUsesSsl,
            RedisSslProtocol = _redisSslProtocol,
            RedisAbortConnect = _redisAbortConnect,
            RedisConnectTimeout = _redisConnectTimeout,
            RedisSyncTimeout = _redisSyncTimeout,
            RedisAsyncTimeout = _redisAsyncTimeout,
            EncryptedTableSchemaName = _encSchema,
            EncryptedTableName = _encTable,
            ModifyCache = modifyCache
        };
    }

    protected GetRowsRequest CreateGetRowsRequest(int pageNo, int pageSize, string orderByColumnName, string orderBySort)
    {
        return new GetRowsRequest
        {
            EncryptedClientId = _encClientId,
            EncryptedClientName = _encClientName,
            EncryptionKey = _encryptionKey,
            EncryptedPostgresServerName = _encDbServer,
            EncryptedPostgresDatabaseName = _encDbName,
            EncryptedPostgresUserName = _encPostgresUserName,
            EncryptedPostgresPassword = _encPostgresPassword,
            EncryptedRedisServerName = _encRedisServerName,
            RedisPortNo = _redisPortNo,
            EncryptedRedisPassword = _encRedisPassword,
            RedisUsesSsl = _redisUsesSsl,
            RedisSslProtocol = _redisSslProtocol,
            RedisAbortConnect = _redisAbortConnect,
            RedisConnectTimeout = _redisConnectTimeout,
            RedisSyncTimeout = _redisSyncTimeout,
            RedisAsyncTimeout = _redisAsyncTimeout,
            EncryptedTableSchemaName = _encSchema,
            EncryptedTableName = _encTable,
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
            EncryptedClientId = _encClientId,
            EncryptedClientName = _encClientName,
            EncryptionKey = _encryptionKey,
            EncryptedPostgresServerName = _encDbServer,
            EncryptedPostgresDatabaseName = _encDbName,
            EncryptedPostgresUserName = _encPostgresUserName,
            EncryptedPostgresPassword = _encPostgresPassword,
            EncryptedRedisServerName = _encRedisServerName,
            RedisPortNo = _redisPortNo,
            EncryptedRedisPassword = _encRedisPassword,
            RedisUsesSsl = _redisUsesSsl,
            RedisSslProtocol = _redisSslProtocol,
            RedisAbortConnect = _redisAbortConnect,
            RedisConnectTimeout = _redisConnectTimeout,
            RedisSyncTimeout = _redisSyncTimeout,
            RedisAsyncTimeout = _redisAsyncTimeout,
            EncryptedTableSchemaName = _encSchema,
            EncryptedTableName = _encTable,
            ModifyCache = modifyCache,
            Row = MapTableEntityToRow(address, DbOperationType.Insert)
        };
    }

    protected GetRequest CreateGetRequest()
    {
        return new GetRequest
        {
            EncryptedClientId = _encClientId,
            EncryptedClientName = _encClientName,
            EncryptionKey = _encryptionKey,
            EncryptedPostgresServerName = _encDbServer,
            EncryptedPostgresDatabaseName = _encDbName,
            EncryptedPostgresUserName = _encPostgresUserName,
            EncryptedPostgresPassword = _encPostgresPassword,
            EncryptedRedisServerName = _encRedisServerName,
            RedisPortNo = _redisPortNo,
            EncryptedRedisPassword = _encRedisPassword,
            RedisUsesSsl = _redisUsesSsl,
            RedisSslProtocol = _redisSslProtocol,
            RedisAbortConnect = _redisAbortConnect,
            RedisConnectTimeout = _redisConnectTimeout,
            RedisSyncTimeout = _redisSyncTimeout,
            RedisAsyncTimeout = _redisAsyncTimeout,
            EncryptedTableSchemaName = _encSchema,
            EncryptedTableName = _encTable
        };
    }

    protected UpdateRequest CreateUpdateRequest(T address, bool modifyCache)
    {
        return new UpdateRequest
        {
            EncryptedClientId = _encClientId,
            EncryptedClientName = _encClientName,
            EncryptionKey = _encryptionKey,
            EncryptedPostgresServerName = _encDbServer,
            EncryptedPostgresDatabaseName = _encDbName,
            EncryptedPostgresUserName = _encPostgresUserName,
            EncryptedPostgresPassword = _encPostgresPassword,
            EncryptedRedisServerName = _encRedisServerName,
            RedisPortNo = _redisPortNo,
            EncryptedRedisPassword = _encRedisPassword,
            RedisUsesSsl = _redisUsesSsl,
            RedisSslProtocol = _redisSslProtocol,
            RedisAbortConnect = _redisAbortConnect,
            RedisConnectTimeout = _redisConnectTimeout,
            RedisSyncTimeout = _redisSyncTimeout,
            RedisAsyncTimeout = _redisAsyncTimeout,
            EncryptedTableSchemaName = _encSchema,
            EncryptedTableName = _encTable,
            ModifyCache = modifyCache,
            Row = MapTableEntityToRow(address, DbOperationType.Update)
        };
    }

}
