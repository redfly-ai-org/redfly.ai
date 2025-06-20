using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RedflyCoreFramework;
using redflyDatabaseAdapters;
using redflyGeneratedDataAccessApi.Base;
using redflyGeneratedDataAccessApi.Common;
using redflyGeneratedDataAccessApi.Protos.DatabaseApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace redflyGeneratedDataAccessApi.Postgres.AdventureWorks;

// Strongly-typed classes for [purchasing].[shipmethod] generated by the
// redfly PostgresGrpcPolyLangCompiler
// This is only meant to be indicative of the features available in the core product.

public class PurchasingShipmethod : BasePostgresTableSchema
{
    public int Shipmethodid { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Shipbase { get; set; }
    public decimal Shiprate { get; set; }
    public Guid Rowguid { get; set; }
    public DateTime Modifieddate { get; set; }
}

public class PurchasingShipmethodRowsData : BaseTableRowsData
{
    public List<PurchasingShipmethod> Rows { get; set; } = new();
}

public class PurchasingShipmethodInsertedData : BaseTableInsertedData
{
    public PurchasingShipmethod? InsertedRow { get; set; }
}

public class PurchasingShipmethodRowData : BaseTableRowData
{
    public PurchasingShipmethod? Row { get; set; }
}

public class PurchasingShipmethodDataSource : BasePostgresTableDataSource<PurchasingShipmethod>
{
    public PurchasingShipmethodDataSource() : base()
    {
        _encSchema = RedflyEncryption.EncryptToString("purchasing");
        _encTable = RedflyEncryption.EncryptToString("shipmethod");
    }

    public async Task<DeletedData> DeleteAsync(int shipmethodid, bool modifyCache = true)
    {
        var req = base.CreateDeleteRequest(modifyCache);
        req.PrimaryKeyValues.Add("shipmethodid", shipmethodid.ToString());
        return await base.DeleteCoreAsync(req);
    }

    public async Task<PurchasingShipmethodRowsData> GetRowsAsync(int pageNo = 1, int pageSize = 50, string orderByColumnName = "", string orderBySort = "", bool useCache = true)
    {
        var req = base.CreateGetRowsRequest(pageNo, pageSize, orderByColumnName, orderBySort);
        var resp = await _client.GetRowsAsync(req, AppGrpcSession.Headers!);
        var rows = new List<PurchasingShipmethod>();
        foreach (var row in resp.Rows)
        {
            rows.Add(MapRowToTableEntity(row));
        }
        return new PurchasingShipmethodRowsData
        {
            Success = resp.Success,
            Rows = rows,
            FromCache = resp.FromCache,
            Message = resp.Message
        };
    }

    public async Task<PurchasingShipmethodInsertedData> InsertAsync(PurchasingShipmethod entity, bool modifyCache = true)
    {
        var req = base.CreateInsertRequest(entity, modifyCache);
        var resp = await _client.InsertAsync(req, AppGrpcSession.Headers!);
        return new PurchasingShipmethodInsertedData
        {
            Success = resp.Success,
            InsertedRow = resp.InsertedRow != null ? MapRowToTableEntity(resp.InsertedRow) : null,
            CacheUpdated = resp.CacheUpdated,
            Message = resp.Message
        };
    }

    public async Task<PurchasingShipmethodRowData> GetAsync(int shipmethodid, bool useCache = true)
    {
        var req = base.CreateGetRequest();
        req.PrimaryKeyValues.Add("shipmethodid", shipmethodid.ToString());
        var resp = await _client.GetAsync(req, AppGrpcSession.Headers!);
        return new PurchasingShipmethodRowData
        {
            Success = resp.Success,
            Row = resp.Row != null ? MapRowToTableEntity(resp.Row) : null,
            FromCache = resp.FromCache,
            Message = resp.Message
        };
    }

    public async Task<UpdatedData> UpdateAsync(PurchasingShipmethod entity, bool modifyCache = true)
    {
        var req = CreateUpdateRequest(entity, modifyCache);
        return await UpdateCoreAsync(req);
    }

    protected override PurchasingShipmethod MapRowToTableEntity(Row row)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var entry in row.Entries)
        {
            dict[entry.Column] = entry.Value?.StringValue;
        }
        return new PurchasingShipmethod
        {
            Shipmethodid = dict.TryGetValue("shipmethodid", out var v1) && !string.IsNullOrEmpty(v1) ? (int)Convert.ChangeType(v1, typeof(int)) : default(int),
            Name = dict.TryGetValue("name", out var v2) && !string.IsNullOrEmpty(v2) ? (string)Convert.ChangeType(v2, typeof(string)) : default(string),
            Shipbase = dict.TryGetValue("shipbase", out var v3) && !string.IsNullOrEmpty(v3) ? (decimal)Convert.ChangeType(v3, typeof(decimal)) : default(decimal),
            Shiprate = dict.TryGetValue("shiprate", out var v4) && !string.IsNullOrEmpty(v4) ? (decimal)Convert.ChangeType(v4, typeof(decimal)) : default(decimal),
            Rowguid = dict.TryGetValue("rowguid", out var v5) && !string.IsNullOrEmpty(v5) ? (Guid)Convert.ChangeType(v5, typeof(Guid)) : default(Guid),
            Modifieddate = dict.TryGetValue("modifieddate", out var v6) && !string.IsNullOrEmpty(v6) ? (DateTime)Convert.ChangeType(v6, typeof(DateTime)) : default(DateTime),
        };
    }

    protected override Row MapTableEntityToRow(PurchasingShipmethod entity, DbOperationType dbOperationType)
    {
        var row = new Row();

        row.Entries.Add(new RowEntry { Column = "shipmethodid", Value = new Value { StringValue = entity.Shipmethodid.ToString() } });

        // For Postgres, add all non-primary key columns
        row.Entries.Add(new RowEntry { Column = "name", Value = new Value { StringValue = entity.Name } });
        row.Entries.Add(new RowEntry { Column = "shipbase", Value = new Value { StringValue = entity.Shipbase.ToString() } });
        row.Entries.Add(new RowEntry { Column = "shiprate", Value = new Value { StringValue = entity.Shiprate.ToString() } });
        row.Entries.Add(new RowEntry { Column = "rowguid", Value = new Value { StringValue = entity.Rowguid.ToString() } });
        if (entity.Modifieddate != DateTime.MinValue)
        {
            row.Entries.Add(new RowEntry { Column = "modifieddate", Value = new Value { StringValue = entity.Modifieddate.ToString("yyyy-MM-dd HH:mm:ss.fff") } });
        }
        return row;
    }
}
