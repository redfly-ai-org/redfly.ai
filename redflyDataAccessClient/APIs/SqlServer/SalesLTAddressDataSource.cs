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

    public class SalesLTAddress : TableEntityBase
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
    }

    public class SalesLTAddressRowsData : BaseTableRowsData
    {
        public List<SalesLTAddress> Rows { get; set; } = new();
    }

    public class SalesLTAddressInsertedData : BaseTableInsertedData
    {
        public SalesLTAddress? InsertedRow { get; set; }
    }

    public class SalesLTAddressRowData : BaseTableRowData
    {
        public SalesLTAddress? Row { get; set; }
    }
    
    public class SalesLTAddressDataSource : TableDataSourceBase<SalesLTAddress>
    {
        
        public SalesLTAddressDataSource() : base()
        {            
        }

        public async Task<DeletedData> DeleteAsync(int addressId, bool modifyCache = true)
        {
            var req = base.CreateDeleteRequest(modifyCache);

            req.PrimaryKeyValues.Add("AddressID", addressId.ToString());

            return await base.DeleteCoreAsync(req);
        }

        public async Task<SalesLTAddressRowsData> GetRowsAsync(
            int pageNo = 1, 
            int pageSize = 50, 
            string orderByColumnName = "",
            string orderBySort = "",
            bool useCache = true)
        {
            var req = base.CreateGetRowsRequest(pageNo, pageSize, orderByColumnName, orderBySort);

            var resp = await _client.GetRowsAsync(req, AppGrpcSession.Headers!);
            var rows = new List<SalesLTAddress>();

            foreach (var row in resp.Rows)
            {
                rows.Add(MapRowToTableEntity(row));
            }

            return new SalesLTAddressRowsData
            {
                Success = resp.Success,
                Rows = rows,
                FromCache = resp.FromCache,
                Message = resp.Message
            };
        }

        public async Task<SalesLTAddressInsertedData> InsertAsync(SalesLTAddress address, bool modifyCache = true)
        {
            var req = base.CreateInsertRequest(address, modifyCache);

            var resp = await _client.InsertAsync(req, AppGrpcSession.Headers!);

            return new SalesLTAddressInsertedData
            {
                Success = resp.Success,
                InsertedRow = resp.InsertedRow != null ? MapRowToTableEntity(resp.InsertedRow) : null,
                CacheUpdated = resp.CacheUpdated,
                Message = resp.Message
            };
        }

        public async Task<SalesLTAddressRowData> GetAsync(int addressId, bool useCache = true)
        {
            var req = base.CreateGetRequest();

            req.PrimaryKeyValues.Add("AddressID", addressId.ToString());

            var resp = await _client.GetAsync(req, AppGrpcSession.Headers!);

            return new SalesLTAddressRowData
            {
                Success = resp.Success,
                Row = resp.Row != null ? MapRowToTableEntity(resp.Row) : null,
                FromCache = resp.FromCache,
                Message = resp.Message
            };
        }

        public async Task<UpdatedData> UpdateAsync(SalesLTAddress address, bool modifyCache = true)
        {
            var req = CreateUpdateRequest(address, modifyCache);

            return await UpdateCoreAsync(req);
        }

        protected override SalesLTAddress MapRowToTableEntity(Row row)
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

        protected override Row MapTableEntityToRow(SalesLTAddress address, DbOperationType dbOperationType)
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
