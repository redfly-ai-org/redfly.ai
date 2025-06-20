using RedflyCoreFramework;
using redflyDatabaseAdapters;
using redflyGeneratedDataAccessApi.Base;
using redflyGeneratedDataAccessApi.Common;
using redflyGeneratedDataAccessApi.Protos.DatabaseApi;

namespace redflyGeneratedDataAccessApi.SqlServer.ProxyTestAdventureWorks;

// Strongly-typed classes for [SalesLT].[ProductCategory] generated by the
// redfly SqlServerGrpcPolyLangCompiler
// This is only meant to be indicative of the features available in the core product.

public class SalesLTProductCategory : BaseSqlServerTableSchema
{
    public int ProductCategoryId { get; set; }
    public int? ParentProductCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid Rowguid { get; set; }
    public DateTime ModifiedDate { get; set; }
}

public class SalesLTProductCategoryRowsData : BaseTableRowsData
{
    public List<SalesLTProductCategory> Rows { get; set; } = new();
}
public class SalesLTProductCategoryInsertedData : BaseTableInsertedData
{
    public SalesLTProductCategory? InsertedRow { get; set; }
}
public class SalesLTProductCategoryRowData : BaseTableRowData
{
    public SalesLTProductCategory? Row { get; set; }
}

public class SalesLTProductCategoryDataSource : BaseSqlServerTableDataSource<SalesLTProductCategory>
{
    public SalesLTProductCategoryDataSource() : base()
    {
        _encSchema = RedflyEncryption.EncryptToString("SalesLT");
        _encTable = RedflyEncryption.EncryptToString("ProductCategory");
    }

    public async Task<DeletedData> DeleteAsync(int productCategoryId, bool modifyCache = true)
    {
        var req = base.CreateDeleteRequest(modifyCache);
        req.PrimaryKeyValues.Add("ProductCategoryID", productCategoryId.ToString());
        return await base.DeleteCoreAsync(req);
    }

    public async Task<SalesLTProductCategoryRowsData> GetRowsAsync(int pageNo = 1, int pageSize = 50, string orderByColumnName = "", string orderBySort = "", bool useCache = true)
    {
        var req = base.CreateGetRowsRequest(pageNo, pageSize, orderByColumnName, orderBySort);
        var resp = await _client.GetRowsAsync(req, AppGrpcSession.Headers!);
        var rows = new List<SalesLTProductCategory>();
        foreach (var row in resp.Rows)
        {
            rows.Add(MapRowToTableEntity(row));
        }
        return new SalesLTProductCategoryRowsData
        {
            Success = resp.Success,
            Rows = rows,
            FromCache = resp.FromCache,
            Message = resp.Message
        };
    }

    public async Task<SalesLTProductCategoryInsertedData> InsertAsync(SalesLTProductCategory entity, bool modifyCache = true)
    {
        var req = base.CreateInsertRequest(entity, modifyCache);
        var resp = await _client.InsertAsync(req, AppGrpcSession.Headers!);
        return new SalesLTProductCategoryInsertedData
        {
            Success = resp.Success,
            InsertedRow = resp.InsertedRow != null ? MapRowToTableEntity(resp.InsertedRow) : null,
            CacheUpdated = resp.CacheUpdated,
            Message = resp.Message
        };
    }

    public async Task<SalesLTProductCategoryRowData> GetAsync(int productCategoryId, bool useCache = true)
    {
        var req = base.CreateGetRequest();
        req.PrimaryKeyValues.Add("ProductCategoryID", productCategoryId.ToString());
        var resp = await _client.GetAsync(req, AppGrpcSession.Headers!);
        return new SalesLTProductCategoryRowData
        {
            Success = resp.Success,
            Row = resp.Row != null ? MapRowToTableEntity(resp.Row) : null,
            FromCache = resp.FromCache,
            Message = resp.Message
        };
    }

    public async Task<UpdatedData> UpdateAsync(SalesLTProductCategory entity, bool modifyCache = true)
    {
        var req = CreateUpdateRequest(entity, modifyCache);
        return await UpdateCoreAsync(req);
    }

    protected override SalesLTProductCategory MapRowToTableEntity(Row row)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var entry in row.Entries)
        {
            dict[entry.Column] = entry.Value?.StringValue;
        }
        return new SalesLTProductCategory
        {
            ProductCategoryId = dict.TryGetValue("ProductCategoryID", out var v1) && int.TryParse(v1, out var i1) ? i1 : 0,
            ParentProductCategoryId = dict.TryGetValue("ParentProductCategoryID", out var v2) && !string.IsNullOrEmpty(v2) && int.TryParse(v2, out var i2) ? i2 : null,
            Name = dict.TryGetValue("Name", out var v3) ? v3 ?? string.Empty : string.Empty,
            Rowguid = dict.TryGetValue("rowguid", out var v4) && Guid.TryParse(v4, out var g4) ? g4 : Guid.Empty,
            ModifiedDate = dict.TryGetValue("ModifiedDate", out var v5) && DateTime.TryParse(v5, out var d5) ? d5 : DateTime.MinValue,
            Version = dict.TryGetValue("Version", out var vVersion) ? Convert.FromBase64String(vVersion ?? "") : Array.Empty<byte>(),
        };
    }

    protected override Row MapTableEntityToRow(SalesLTProductCategory entity, DbOperationType dbOperationType)
    {
        var row = new Row();
        if (dbOperationType != DbOperationType.Insert)
        {
            row.Entries.Add(new RowEntry { Column = "ProductCategoryID", Value = new Value { StringValue = entity.ProductCategoryId.ToString() } });
        }
        if (entity.ParentProductCategoryId != null)
        {
            row.Entries.Add(new RowEntry { Column = "ParentProductCategoryID", Value = new Value { StringValue = entity.ParentProductCategoryId.ToString() } });
        }
        else
        {
            row.Entries.Add(new RowEntry { Column = "ParentProductCategoryID", Value = new Value { StringValue = null } });
        }
        row.Entries.Add(new RowEntry { Column = "Name", Value = new Value { StringValue = entity.Name } });
        row.Entries.Add(new RowEntry { Column = "rowguid", Value = new Value { StringValue = entity.Rowguid.ToString() } });
        if (entity.ModifiedDate != DateTime.MinValue)
        {
            row.Entries.Add(new RowEntry { Column = "ModifiedDate", Value = new Value { StringValue = entity.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss.fff") } });
        }
        return row;
    }
}
