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

// Strongly-typed classes for [sales].[specialoffer] generated by the
// redfly PostgresGrpcPolyLangCompiler
// This is only meant to be indicative of the features available in the core product.

public class SalesSpecialoffer : BasePostgresTableSchema
{
    public int Specialofferid { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Discountpct { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime Startdate { get; set; }
    public DateTime Enddate { get; set; }
    public int Minqty { get; set; }
    public int? Maxqty { get; set; }
    public Guid Rowguid { get; set; }
    public DateTime Modifieddate { get; set; }
}

public class SalesSpecialofferRowsData : BaseTableRowsData
{
    public List<SalesSpecialoffer> Rows { get; set; } = new();
}

public class SalesSpecialofferInsertedData : BaseTableInsertedData
{
    public SalesSpecialoffer? InsertedRow { get; set; }
}

public class SalesSpecialofferRowData : BaseTableRowData
{
    public SalesSpecialoffer? Row { get; set; }
}

public class SalesSpecialofferDataSource : BasePostgresTableDataSource<SalesSpecialoffer>
{
    public SalesSpecialofferDataSource() : base()
    {
        _encSchema = RedflyEncryption.EncryptToString("sales");
        _encTable = RedflyEncryption.EncryptToString("specialoffer");
    }

    public async Task<DeletedData> DeleteAsync(int specialofferid, bool modifyCache = true)
    {
        var req = base.CreateDeleteRequest(modifyCache);
        req.PrimaryKeyValues.Add("specialofferid", specialofferid.ToString());
        return await base.DeleteCoreAsync(req);
    }

    public async Task<SalesSpecialofferRowsData> GetRowsAsync(int pageNo = 1, int pageSize = 50, string orderByColumnName = "", string orderBySort = "", bool useCache = true)
    {
        var req = base.CreateGetRowsRequest(pageNo, pageSize, orderByColumnName, orderBySort);
        var resp = await _client.GetRowsAsync(req, AppGrpcSession.Headers!);
        var rows = new List<SalesSpecialoffer>();
        foreach (var row in resp.Rows)
        {
            rows.Add(MapRowToTableEntity(row));
        }
        return new SalesSpecialofferRowsData
        {
            Success = resp.Success,
            Rows = rows,
            FromCache = resp.FromCache,
            Message = resp.Message
        };
    }

    public async Task<SalesSpecialofferInsertedData> InsertAsync(SalesSpecialoffer entity, bool modifyCache = true)
    {
        var req = base.CreateInsertRequest(entity, modifyCache);
        var resp = await _client.InsertAsync(req, AppGrpcSession.Headers!);
        return new SalesSpecialofferInsertedData
        {
            Success = resp.Success,
            InsertedRow = resp.InsertedRow != null ? MapRowToTableEntity(resp.InsertedRow) : null,
            CacheUpdated = resp.CacheUpdated,
            Message = resp.Message
        };
    }

    public async Task<SalesSpecialofferRowData> GetAsync(int specialofferid, bool useCache = true)
    {
        var req = base.CreateGetRequest();
        req.PrimaryKeyValues.Add("specialofferid", specialofferid.ToString());
        var resp = await _client.GetAsync(req, AppGrpcSession.Headers!);
        return new SalesSpecialofferRowData
        {
            Success = resp.Success,
            Row = resp.Row != null ? MapRowToTableEntity(resp.Row) : null,
            FromCache = resp.FromCache,
            Message = resp.Message
        };
    }

    public async Task<UpdatedData> UpdateAsync(SalesSpecialoffer entity, bool modifyCache = true)
    {
        var req = CreateUpdateRequest(entity, modifyCache);
        return await UpdateCoreAsync(req);
    }

    protected override SalesSpecialoffer MapRowToTableEntity(Row row)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var entry in row.Entries)
        {
            dict[entry.Column] = entry.Value?.StringValue;
        }
        return new SalesSpecialoffer
        {
            Specialofferid = dict.TryGetValue("specialofferid", out var v1) && !string.IsNullOrEmpty(v1) ? (int)Convert.ChangeType(v1, typeof(int)) : default(int),
            Description = dict.TryGetValue("description", out var v2) && !string.IsNullOrEmpty(v2) ? (string)Convert.ChangeType(v2, typeof(string)) : default(string),
            Discountpct = dict.TryGetValue("discountpct", out var v3) && !string.IsNullOrEmpty(v3) ? (decimal)Convert.ChangeType(v3, typeof(decimal)) : default(decimal),
            Type = dict.TryGetValue("type", out var v4) && !string.IsNullOrEmpty(v4) ? (string)Convert.ChangeType(v4, typeof(string)) : default(string),
            Category = dict.TryGetValue("category", out var v5) && !string.IsNullOrEmpty(v5) ? (string)Convert.ChangeType(v5, typeof(string)) : default(string),
            Startdate = dict.TryGetValue("startdate", out var v6) && !string.IsNullOrEmpty(v6) ? (DateTime)Convert.ChangeType(v6, typeof(DateTime)) : default(DateTime),
            Enddate = dict.TryGetValue("enddate", out var v7) && !string.IsNullOrEmpty(v7) ? (DateTime)Convert.ChangeType(v7, typeof(DateTime)) : default(DateTime),
            Minqty = dict.TryGetValue("minqty", out var v8) && !string.IsNullOrEmpty(v8) ? (int)Convert.ChangeType(v8, typeof(int)) : default(int),
            Maxqty = dict.TryGetValue("maxqty", out var v9) && !string.IsNullOrEmpty(v9) ? (int)Convert.ChangeType(v9, typeof(int)) : null,
            Rowguid = dict.TryGetValue("rowguid", out var v10) && !string.IsNullOrEmpty(v10) ? (Guid)Convert.ChangeType(v10, typeof(Guid)) : default(Guid),
            Modifieddate = dict.TryGetValue("modifieddate", out var v11) && !string.IsNullOrEmpty(v11) ? (DateTime)Convert.ChangeType(v11, typeof(DateTime)) : default(DateTime),
        };
    }

    protected override Row MapTableEntityToRow(SalesSpecialoffer entity, DbOperationType dbOperationType)
    {
        var row = new Row();

        row.Entries.Add(new RowEntry { Column = "specialofferid", Value = new Value { StringValue = entity.Specialofferid.ToString() } });

        // For Postgres, add all non-primary key columns
        row.Entries.Add(new RowEntry { Column = "description", Value = new Value { StringValue = entity.Description } });
        row.Entries.Add(new RowEntry { Column = "discountpct", Value = new Value { StringValue = entity.Discountpct.ToString() } });
        row.Entries.Add(new RowEntry { Column = "type", Value = new Value { StringValue = entity.Type } });
        row.Entries.Add(new RowEntry { Column = "category", Value = new Value { StringValue = entity.Category } });
        row.Entries.Add(new RowEntry { Column = "startdate", Value = new Value { StringValue = entity.Startdate.ToString() } });
        row.Entries.Add(new RowEntry { Column = "enddate", Value = new Value { StringValue = entity.Enddate.ToString() } });
        row.Entries.Add(new RowEntry { Column = "minqty", Value = new Value { StringValue = entity.Minqty.ToString() } });
        if (entity.Maxqty != null)
        {
            row.Entries.Add(new RowEntry { Column = "maxqty", Value = new Value { StringValue = entity.Maxqty.ToString() } });
        }
        else
        {
            row.Entries.Add(new RowEntry { Column = "maxqty", Value = new Value { StringValue = null } });
        }
        row.Entries.Add(new RowEntry { Column = "rowguid", Value = new Value { StringValue = entity.Rowguid.ToString() } });
        if (entity.Modifieddate != DateTime.MinValue)
        {
            row.Entries.Add(new RowEntry { Column = "modifieddate", Value = new Value { StringValue = entity.Modifieddate.ToString("yyyy-MM-dd HH:mm:ss.fff") } });
        }
        return row;
    }
}
