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

// Strongly-typed classes for [production].[productcategory] generated by the
// redfly PostgresGrpcPolyLangCompiler
// This is only meant to be indicative of the features available in the core product.

public class ProductionProductcategory : BasePostgresTableSchema
{
    public int Productcategoryid { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid Rowguid { get; set; }
    public DateTime Modifieddate { get; set; }
}

public class ProductionProductcategoryRowsData : BaseTableRowsData
{
    public List<ProductionProductcategory> Rows { get; set; } = new();
}

public class ProductionProductcategoryInsertedData : BaseTableInsertedData
{
    public ProductionProductcategory? InsertedRow { get; set; }
}

public class ProductionProductcategoryRowData : BaseTableRowData
{
    public ProductionProductcategory? Row { get; set; }
}

public class ProductionProductcategoryDataSource : BasePostgresTableDataSource<ProductionProductcategory>
{
    public ProductionProductcategoryDataSource() : base()
    {
        _encSchema = RedflyEncryption.EncryptToString("production");
        _encTable = RedflyEncryption.EncryptToString("productcategory");
    }

    public async Task<DeletedData> DeleteAsync(int productcategoryid, bool modifyCache = true)
    {
        var req = base.CreateDeleteRequest(modifyCache);
        req.PrimaryKeyValues.Add("productcategoryid", productcategoryid.ToString());
        return await base.DeleteCoreAsync(req);
    }

    public async Task<ProductionProductcategoryRowsData> GetRowsAsync(int pageNo = 1, int pageSize = 50, string orderByColumnName = "", string orderBySort = "", bool useCache = true)
    {
        var req = base.CreateGetRowsRequest(pageNo, pageSize, orderByColumnName, orderBySort);
        var resp = await _client.GetRowsAsync(req, AppGrpcSession.Headers!);
        var rows = new List<ProductionProductcategory>();
        foreach (var row in resp.Rows)
        {
            rows.Add(MapRowToTableEntity(row));
        }
        return new ProductionProductcategoryRowsData
        {
            Success = resp.Success,
            Rows = rows,
            FromCache = resp.FromCache,
            Message = resp.Message
        };
    }

    public async Task<ProductionProductcategoryInsertedData> InsertAsync(ProductionProductcategory entity, bool modifyCache = true)
    {
        var req = base.CreateInsertRequest(entity, modifyCache);
        var resp = await _client.InsertAsync(req, AppGrpcSession.Headers!);
        return new ProductionProductcategoryInsertedData
        {
            Success = resp.Success,
            InsertedRow = resp.InsertedRow != null ? MapRowToTableEntity(resp.InsertedRow) : null,
            CacheUpdated = resp.CacheUpdated,
            Message = resp.Message
        };
    }

    public async Task<ProductionProductcategoryRowData> GetAsync(int productcategoryid, bool useCache = true)
    {
        var req = base.CreateGetRequest();
        req.PrimaryKeyValues.Add("productcategoryid", productcategoryid.ToString());
        var resp = await _client.GetAsync(req, AppGrpcSession.Headers!);
        return new ProductionProductcategoryRowData
        {
            Success = resp.Success,
            Row = resp.Row != null ? MapRowToTableEntity(resp.Row) : null,
            FromCache = resp.FromCache,
            Message = resp.Message
        };
    }

    public async Task<UpdatedData> UpdateAsync(ProductionProductcategory entity, bool modifyCache = true)
    {
        var req = CreateUpdateRequest(entity, modifyCache);
        return await UpdateCoreAsync(req);
    }

    protected override ProductionProductcategory MapRowToTableEntity(Row row)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var entry in row.Entries)
        {
            dict[entry.Column] = entry.Value?.StringValue;
        }
        return new ProductionProductcategory
        {
            Productcategoryid = dict.TryGetValue("productcategoryid", out var v1) && !string.IsNullOrEmpty(v1) ? (int)Convert.ChangeType(v1, typeof(int)) : default(int),
            Name = dict.TryGetValue("name", out var v2) && !string.IsNullOrEmpty(v2) ? (string)Convert.ChangeType(v2, typeof(string)) : default(string),
            Rowguid = dict.TryGetValue("rowguid", out var v3) && !string.IsNullOrEmpty(v3) ? (Guid)Convert.ChangeType(v3, typeof(Guid)) : default(Guid),
            Modifieddate = dict.TryGetValue("modifieddate", out var v4) && !string.IsNullOrEmpty(v4) ? (DateTime)Convert.ChangeType(v4, typeof(DateTime)) : default(DateTime),
        };
    }

    protected override Row MapTableEntityToRow(ProductionProductcategory entity, DbOperationType dbOperationType)
    {
        var row = new Row();

        row.Entries.Add(new RowEntry { Column = "productcategoryid", Value = new Value { StringValue = entity.Productcategoryid.ToString() } });

        // For Postgres, add all non-primary key columns
        row.Entries.Add(new RowEntry { Column = "name", Value = new Value { StringValue = entity.Name } });
        row.Entries.Add(new RowEntry { Column = "rowguid", Value = new Value { StringValue = entity.Rowguid.ToString() } });
        if (entity.Modifieddate != DateTime.MinValue)
        {
            row.Entries.Add(new RowEntry { Column = "modifieddate", Value = new Value { StringValue = entity.Modifieddate.ToString("yyyy-MM-dd HH:mm:ss.fff") } });
        }
        return row;
    }
}
