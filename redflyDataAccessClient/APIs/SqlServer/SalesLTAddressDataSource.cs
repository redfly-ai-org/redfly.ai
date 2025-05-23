using Grpc.Core;
using Grpc.Net.Client;
using RedflyCoreFramework;
using redflyDataAccessClient.Protos.SqlServer;
using redflyDatabaseAdapters;
using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace redflyDataAccessClient.APIs.SqlServer
{
    //
    // This is only meant to be indicative of the features available in the core product.
    //
    // Strongly-typed model for [SalesLT].[Address]
    //

    public class SalesLTAddress
    {
        public int AddressID { get; set; }
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string StateProvince { get; set; } = string.Empty;
        public string CountryRegion { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public Guid Rowguid { get; set; }
        public DateTime ModifiedDate { get; set; }
        public byte[] Version { get; set; } = Array.Empty<byte>();
    }

    // Response wrappers
    public class TotalRowCount
    {
        public long Total { get; set; }
        public bool FromCache { get; set; }
        public string? Message { get; set; }
    }
    public class DeletedData
    {
        public bool Success { get; set; }
        public bool CacheUpdated { get; set; }
        public string? Message { get; set; }
    }
    public class RowsData
    {
        public bool Success { get; set; }
        public List<SalesLTAddress> Rows { get; set; } = new();
        public bool FromCache { get; set; }
        public string? Message { get; set; }
    }
    public class InsertedData
    {
        public bool Success { get; set; }
        public SalesLTAddress? InsertedRow { get; set; }
        public bool CacheUpdated { get; set; }
        public string? Message { get; set; }
    }
    public class RowData
    {
        public bool Success { get; set; }
        public SalesLTAddress? Row { get; set; }
        public bool FromCache { get; set; }
        public string? Message { get; set; }
    }
    public class UpdatedData
    {
        public bool Success { get; set; }
        public int UpdatedCount { get; set; }
        public bool CacheUpdated { get; set; }
        public string? Message { get; set; }
    }

    public class SalesLTAddressDataSource
    {
        private readonly NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient _client;
        private readonly string _encDbServer, _encDbName, _encSchema, _encTable, _encClientId, _encDbId, _encConnStr, _encryptionKey;

        public SalesLTAddressDataSource()
        {
            var channel = GrpcChannel.ForAddress(AppGrpcSession.GrpcUrl, new GrpcChannelOptions
            {
                //LoggerFactory = loggerFactory,
                HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(30), // Frequency of keepalive pings
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(5) // Timeout before considering the connection dead
                },
                HttpVersion = new Version(2, 0) // Ensure HTTP/2 is used
            });

            _client = new NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient(channel);
            
            //These are hard-coded as it is specific to this class.
            _encSchema = RedflyEncryption.EncryptToString("SalesLT");
            _encTable = RedflyEncryption.EncryptToString("Address");

            //Everything else comes from the environment.
            _encDbServer = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile!.Database.HostName);
            _encDbName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Name);
            _encClientId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.ClientId);
            _encDbId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Id);
            _encConnStr = RedflyEncryption.EncryptToString($"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;");
            _encryptionKey = RedflyEncryptionKeys.AesKey;
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

        public async Task<DeletedData> DeleteAsync(int addressId, bool modifyCache = true)
        {
            var req = new DeleteRequest
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
            req.PrimaryKeyValues.Add("AddressID", addressId.ToString());
            var resp = await _client.DeleteAsync(req, AppGrpcSession.Headers!);
            return new DeletedData
            {
                Success = resp.Success,
                CacheUpdated = resp.CacheUpdated,
                Message = resp.Message
            };
        }

        public async Task<RowsData> GetRowsAsync(int pageNo = 1, int pageSize = 50, bool useCache = true)
        {
            var req = new GetRowsRequest
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
                OrderbyColumnName = "AddressID",
                OrderbyColumnSort = "asc"
            };
            var resp = await _client.GetRowsAsync(req, AppGrpcSession.Headers!);
            var rows = new List<SalesLTAddress>();
            foreach (var row in resp.Rows)
            {
                rows.Add(MapRowToAddress(row));
            }
            return new RowsData
            {
                Success = resp.Success,
                Rows = rows,
                FromCache = resp.FromCache,
                Message = resp.Message
            };
        }

        public async Task<InsertedData> InsertAsync(SalesLTAddress address, bool modifyCache = true)
        {
            var req = new InsertRequest
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
                Row = MapAddressToRow(address, DbOperationType.Insert)
            };
            var resp = await _client.InsertAsync(req, AppGrpcSession.Headers!);
            return new InsertedData
            {
                Success = resp.Success,
                InsertedRow = resp.InsertedRow != null ? MapRowToAddress(resp.InsertedRow) : null,
                CacheUpdated = resp.CacheUpdated,
                Message = resp.Message
            };
        }

        public async Task<RowData> GetAsync(int addressId, bool useCache = true)
        {
            var req = new GetRequest
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
            req.PrimaryKeyValues.Add("AddressID", addressId.ToString());
            var resp = await _client.GetAsync(req, AppGrpcSession.Headers!);
            return new RowData
            {
                Success = resp.Success,
                Row = resp.Row != null ? MapRowToAddress(resp.Row) : null,
                FromCache = resp.FromCache,
                Message = resp.Message
            };
        }

        public async Task<UpdatedData> UpdateAsync(SalesLTAddress address, bool modifyCache = true)
        {
            var req = new UpdateRequest
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
                Row = MapAddressToRow(address, DbOperationType.Update)
            };
            var resp = await _client.UpdateAsync(req, AppGrpcSession.Headers!);
            return new UpdatedData
            {
                Success = resp.Success,
                UpdatedCount = resp.UpdatedCount,
                CacheUpdated = resp.CacheUpdated,
                Message = resp.Message
            };
        }

        // Helper: Map proto Row to SalesLTAddress
        private static SalesLTAddress MapRowToAddress(Row row)
        {
            var dict = new Dictionary<string, string?>();
            foreach (var entry in row.Entries)
            {
                dict[entry.Column] = entry.Value?.StringValue;
            }
            return new SalesLTAddress
            {
                AddressID = dict.TryGetValue("AddressID", out var v1) && int.TryParse(v1, out var i1) ? i1 : 0,
                AddressLine1 = dict.TryGetValue("AddressLine1", out var v2) ? v2 ?? string.Empty : string.Empty,
                AddressLine2 = dict.TryGetValue("AddressLine2", out var v3) ? v3 : null,
                City = dict.TryGetValue("City", out var v4) ? v4 ?? string.Empty : string.Empty,
                StateProvince = dict.TryGetValue("StateProvince", out var v5) ? v5 ?? string.Empty : string.Empty,
                CountryRegion = dict.TryGetValue("CountryRegion", out var v6) ? v6 ?? string.Empty : string.Empty,
                PostalCode = dict.TryGetValue("PostalCode", out var v7) ? v7 ?? string.Empty : string.Empty,
                Rowguid = dict.TryGetValue("rowguid", out var v8) && Guid.TryParse(v8, out var g8) ? g8 : Guid.Empty,
                ModifiedDate = dict.TryGetValue("ModifiedDate", out var v9) && DateTime.TryParse(v9, out var d9) ? d9 : DateTime.MinValue,
                Version = dict.TryGetValue("Version", out var v10) ? Convert.FromBase64String(v10 ?? "") : Array.Empty<byte>()
            };
        }

        // Helper: Map SalesLTAddress to proto Row
        private static Row MapAddressToRow(SalesLTAddress address, DbOperationType dbOperationType)
        {
            var row = new Row();

            if (dbOperationType != DbOperationType.Insert)
            {
                //Do not add primary key for inserts
                row.Entries.Add(new RowEntry { Column = "AddressID", Value = new Value { StringValue = address.AddressID.ToString() } });
            }

            row.Entries.Add(new RowEntry { Column = "AddressLine1", Value = new Value { StringValue = address.AddressLine1 } });
            row.Entries.Add(new RowEntry { Column = "AddressLine2", Value = new Value { StringValue = address.AddressLine2 } });
            row.Entries.Add(new RowEntry { Column = "City", Value = new Value { StringValue = address.City } });
            row.Entries.Add(new RowEntry { Column = "StateProvince", Value = new Value { StringValue = address.StateProvince } });
            row.Entries.Add(new RowEntry { Column = "CountryRegion", Value = new Value { StringValue = address.CountryRegion } });
            row.Entries.Add(new RowEntry { Column = "PostalCode", Value = new Value { StringValue = address.PostalCode } });
            row.Entries.Add(new RowEntry { Column = "rowguid", Value = new Value { StringValue = address.Rowguid.ToString() } });

            if (address.ModifiedDate != DateTime.MinValue)
            {
                row.Entries.Add(new RowEntry { Column = "ModifiedDate", Value = new Value { StringValue = address.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss.fff") } });
            }

            // The Version column is a special timestamp column in our system and we never set it to a value
            // for inserts or updates.
            //row.Entries.Add(new RowEntry { Column = "Version", Value = new Value { StringValue = Convert.ToBase64String(address.Version ?? Array.Empty<byte>()) } });

            return row;
        }
    }
}
