using RedflyCoreFramework;
using redflyDatabaseAdapters;
using redflyGeneratedDataAccessApi.Base;
using redflyGeneratedDataAccessApi.Common;
using redflyGeneratedDataAccessApi.Protos.DatabaseApi;

namespace redflyGeneratedDataAccessApi.SqlServer.ProxyTestAdventureWorks;

// Strongly-typed classes for [dbo].[BuildVersion] generated by the
// redfly SqlServerGrpcPolyLangCompiler
// This is only meant to be indicative of the features available in the core product.

public class BuildVersion : BaseSqlServerTableSchema
{
    public Guid BuildVersionId { get; set; }
    public byte SystemInformationId { get; set; }
    public string DatabaseVersion { get; set; } = string.Empty;
    public DateTime VersionDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

public class BuildVersionRowsData : BaseTableRowsData
{
    public List<BuildVersion> Rows { get; set; } = new();
}
public class BuildVersionInsertedData : BaseTableInsertedData
{
    public BuildVersion? InsertedRow { get; set; }
}
public class BuildVersionRowData : BaseTableRowData
{
    public BuildVersion? Row { get; set; }
}

public class BuildVersionDataSource : BaseSqlServerTableDataSource<BuildVersion>
{
    public BuildVersionDataSource() : base()
    {
        _encSchema = RedflyEncryption.EncryptToString("dbo");
        _encTable = RedflyEncryption.EncryptToString("BuildVersion");
    }

    public async Task<DeletedData> DeleteAsync(Guid buildVersionId, bool modifyCache = true)
    {
        var req = base.CreateDeleteRequest(modifyCache);
        req.PrimaryKeyValues.Add("BuildVersionId", buildVersionId.ToString());
        return await base.DeleteCoreAsync(req);
    }

    public async Task<BuildVersionRowsData> GetRowsAsync(int pageNo = 1, int pageSize = 50, string orderByColumnName = "", string orderBySort = "", bool useCache = true)
    {
        var req = base.CreateGetRowsRequest(pageNo, pageSize, orderByColumnName, orderBySort);
        var resp = await _client.GetRowsAsync(req, AppGrpcSession.Headers!);
        var rows = new List<BuildVersion>();
        foreach (var row in resp.Rows)
        {
            rows.Add(MapRowToTableEntity(row));
        }
        return new BuildVersionRowsData
        {
            Success = resp.Success,
            Rows = rows,
            FromCache = resp.FromCache,
            Message = resp.Message
        };
    }

    public async Task<BuildVersionInsertedData> InsertAsync(BuildVersion entity, bool modifyCache = true)
    {
        var req = base.CreateInsertRequest(entity, modifyCache);
        var resp = await _client.InsertAsync(req, AppGrpcSession.Headers!);
        return new BuildVersionInsertedData
        {
            Success = resp.Success,
            InsertedRow = resp.InsertedRow != null ? MapRowToTableEntity(resp.InsertedRow) : null,
            CacheUpdated = resp.CacheUpdated,
            Message = resp.Message
        };
    }

    public async Task<BuildVersionRowData> GetAsync(Guid buildVersionId, bool useCache = true)
    {
        var req = base.CreateGetRequest();
        req.PrimaryKeyValues.Add("BuildVersionId", buildVersionId.ToString());
        var resp = await _client.GetAsync(req, AppGrpcSession.Headers!);
        return new BuildVersionRowData
        {
            Success = resp.Success,
            Row = resp.Row != null ? MapRowToTableEntity(resp.Row) : null,
            FromCache = resp.FromCache,
            Message = resp.Message
        };
    }

    public async Task<UpdatedData> UpdateAsync(BuildVersion entity, bool modifyCache = true)
    {
        var req = CreateUpdateRequest(entity, modifyCache);
        return await UpdateCoreAsync(req);
    }

    protected override BuildVersion MapRowToTableEntity(Row row)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var entry in row.Entries)
        {
            dict[entry.Column] = entry.Value?.StringValue;
        }
        return new BuildVersion
        {
            BuildVersionId = dict.TryGetValue("BuildVersionId", out var v1) && Guid.TryParse(v1, out var g1) ? g1 : Guid.Empty,
            SystemInformationId = dict.TryGetValue("SystemInformationID", out var v2) && byte.TryParse(v2, out var b2) ? b2 : (byte)0,
            DatabaseVersion = dict.TryGetValue("Database Version", out var v3) ? v3 ?? string.Empty : string.Empty,
            VersionDate = dict.TryGetValue("VersionDate", out var v4) && DateTime.TryParse(v4, out var d4) ? d4 : DateTime.MinValue,
            ModifiedDate = dict.TryGetValue("ModifiedDate", out var v5) && DateTime.TryParse(v5, out var d5) ? d5 : DateTime.MinValue,
            Version = dict.TryGetValue("Version", out var vVersion) ? Convert.FromBase64String(vVersion ?? "") : Array.Empty<byte>(),
        };
    }

    protected override Row MapTableEntityToRow(BuildVersion entity, DbOperationType dbOperationType)
    {
        var row = new Row();
        if (dbOperationType != DbOperationType.Insert)
        {
            row.Entries.Add(new RowEntry { Column = "BuildVersionId", Value = new Value { StringValue = entity.BuildVersionId.ToString() } });
        }
        row.Entries.Add(new RowEntry { Column = "SystemInformationID", Value = new Value { StringValue = entity.SystemInformationId.ToString() } });
        row.Entries.Add(new RowEntry { Column = "Database Version", Value = new Value { StringValue = entity.DatabaseVersion } });
        if (entity.VersionDate != DateTime.MinValue)
        {
            row.Entries.Add(new RowEntry { Column = "VersionDate", Value = new Value { StringValue = entity.VersionDate.ToString("yyyy-MM-dd HH:mm:ss.fff") } });
        }
        if (entity.ModifiedDate != DateTime.MinValue)
        {
            row.Entries.Add(new RowEntry { Column = "ModifiedDate", Value = new Value { StringValue = entity.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss.fff") } });
        }
        return row;
    }
}
