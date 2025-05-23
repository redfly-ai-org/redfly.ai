using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;

namespace redflyGeneratedDataAccessApi.Compilers;

/// <summary>
/// Generates strongly-typed data source and entity classes for every table in a SQL Server database.
/// </summary>
public class SqlServerPolyLangCompiler
{
    public void GenerateForDatabase(string connectionString, string outputFolder)
    {
        var tables = GetTables(connectionString);

        foreach (var table in tables)
        {
            if (IsSystemTable(table.Schema, table.Name))
            {
                Console.WriteLine($"Skipping System table: {table.Schema}.{table.Name}...");
                continue;
            }

            var columns = GetColumns(connectionString, table.Schema, table.Name);

            Console.WriteLine($"Generating code for {table.Schema}.{table.Name}...");
            var classBaseName = table.Schema.Equals("dbo", StringComparison.OrdinalIgnoreCase)
                ? RemoveSpaces(table.Name)
                : RemoveSpaces(table.Schema) + RemoveSpaces(table.Name);
            var code = GenerateCodeForTable(classBaseName, table, columns);
            var fileName = Path.Combine(outputFolder, $"{classBaseName}DataSource.cs");
            File.WriteAllText(fileName, code);
        }
    }

    private List<(string Schema, string Name)> GetTables(string connectionString)
    {
        var tables = new List<(string, string)>();
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            tables.Add((reader.GetString(0), reader.GetString(1)));
        }
        return tables;
    }

    private bool IsSystemTable(string schema, string tableName)
    {
        var systemSchemas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "sys", "db_owner", "db_accessadmin", "db_securityadmin", "db_ddladmin", "db_backupoperator", "db_datareader", "db_datawriter", "db_denydatareader", "db_denydatawriter"
        };
        var systemTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MSchange_tracking_history"
        };
        if (systemSchemas.Contains(schema))
            return true;
        if (systemTables.Contains(tableName))
            return true;
        if (tableName.StartsWith("sys", StringComparison.OrdinalIgnoreCase) ||
            tableName.StartsWith("msdb", StringComparison.OrdinalIgnoreCase) ||
            tableName.StartsWith("dbosys", StringComparison.OrdinalIgnoreCase) ||
            tableName.StartsWith("MSreplication", StringComparison.OrdinalIgnoreCase) ||
            tableName.StartsWith("queue_", StringComparison.OrdinalIgnoreCase) ||
            tableName.StartsWith("filestream_", StringComparison.OrdinalIgnoreCase) ||
            tableName.StartsWith("cdc_", StringComparison.OrdinalIgnoreCase) ||
            tableName.StartsWith("sysdac_", StringComparison.OrdinalIgnoreCase) ||
            tableName.Contains("sysdac", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    private List<(string Name, string Type, bool IsNullable, bool IsPrimaryKey)> GetColumns(string connectionString, string schema, string table)
    {
        var columns = new List<(string, string, bool, bool)>();
        var pkColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            // Get primary key columns
            using (var pkCmd = conn.CreateCommand())
            {
                pkCmd.CommandText = @"
                    SELECT kcu.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                        ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                        AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                        AND tc.TABLE_NAME = kcu.TABLE_NAME
                    WHERE tc.TABLE_SCHEMA = @schema AND tc.TABLE_NAME = @table AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'";
                pkCmd.Parameters.AddWithValue("@schema", schema);
                pkCmd.Parameters.AddWithValue("@table", table);
                using var pkReader = pkCmd.ExecuteReader();
                while (pkReader.Read())
                {
                    pkColumns.Add(pkReader.GetString(0));
                }
            }
            // Get columns
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table";
                cmd.Parameters.AddWithValue("@schema", schema);
                cmd.Parameters.AddWithValue("@table", table);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var colName = reader.GetString(0);
                    var colType = reader.GetString(1);
                    var isNullable = reader.GetString(2) == "YES";
                    var isPk = pkColumns.Contains(colName);
                    columns.Add((colName, colType, isNullable, isPk));
                }
            }
        }
        return columns;
    }

    private string RemoveSpaces(string input)
    {
        return input.Replace(" ", "");
    }

    private string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        input = RemoveSpaces(input);
        if (input.Length == 1) return input.ToLowerInvariant();
        return char.ToLowerInvariant(input[0]) + input.Substring(1);
    }

    private string GenerateCodeForTable(string classBaseName, (string Schema, string Name) table, List<(string Name, string Type, bool IsNullable, bool IsPrimaryKey)> columns)
    {
        var sb = new StringBuilder();
        var entityName = classBaseName;
        var dataSourceName = $"{entityName}DataSource";
        // Usings
        sb.AppendLine("using redflyDatabaseAdapters;");
        sb.AppendLine("using redflyGeneratedDataAccessApi.Protos.SqlServer;");
        sb.AppendLine();
        sb.AppendLine($"namespace redflyGeneratedDataAccessApi.SqlServer;");
        sb.AppendLine();
        sb.AppendLine($"//\n// This is only meant to be indicative of the features available in the core product.\n//\n// Strongly-typed model for [{table.Schema}].[{table.Name}]\n//");
        // Entity class
        sb.AppendLine($"public class {entityName} : TableEntityBase");
        sb.AppendLine("{");
        foreach (var col in columns)
        {
            if (col.Name.Equals("Version", StringComparison.OrdinalIgnoreCase))
                continue;
            var csharpType = MapSqlTypeToCSharp(col.Type, col.IsNullable);
            var propName = ToCamelCase(col.Name);
            if (csharpType == "string" && !col.IsNullable)
                sb.AppendLine($"    public string {propName} {{ get; set; }} = string.Empty;");
            else
                sb.AppendLine($"    public {csharpType} {propName} {{ get; set; }}");
        }
        sb.AppendLine("}");
        sb.AppendLine();
        // RowsData, InsertedData, RowData classes
        sb.AppendLine($"public class {entityName}RowsData : BaseTableRowsData");
        sb.AppendLine("{");
        sb.AppendLine($"    public List<{entityName}> Rows {{ get; set; }} = new();");
        sb.AppendLine("}");
        sb.AppendLine($"public class {entityName}InsertedData : BaseTableInsertedData");
        sb.AppendLine("{");
        sb.AppendLine($"    public {entityName}? InsertedRow {{ get; set; }}");
        sb.AppendLine("}");
        sb.AppendLine($"public class {entityName}RowData : BaseTableRowData");
        sb.AppendLine("{");
        sb.AppendLine($"    public {entityName}? Row {{ get; set; }}");
        sb.AppendLine("}");
        sb.AppendLine();
        // DataSource class
        sb.AppendLine($"public class {dataSourceName} : TableDataSourceBase<{entityName}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public {dataSourceName}() : base()\n    {{\n    }}");
        sb.AppendLine();
        // Primary key columns
        var pkCols = columns.Where(c => c.IsPrimaryKey).ToList();
        // DeleteAsync
        if (pkCols.Count > 0)
        {
            var pkParams = string.Join(", ", pkCols.Select(c => $"{MapSqlTypeToCSharp(c.Type, c.IsNullable)} {ToCamelCase(c.Name)}"));
            sb.AppendLine($"    public async Task<DeletedData> DeleteAsync({pkParams}, bool modifyCache = true)");
            sb.AppendLine("    {");
            sb.AppendLine("        var req = base.CreateDeleteRequest(modifyCache);");
            foreach (var pk in pkCols)
                sb.AppendLine($"        req.PrimaryKeyValues.Add(\"{pk.Name}\", {ToCamelCase(pk.Name)}.ToString());");
            sb.AppendLine("        return await base.DeleteCoreAsync(req);");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        // GetRowsAsync
        sb.AppendLine($"    public async Task<{entityName}RowsData> GetRowsAsync(int pageNo = 1, int pageSize = 50, string orderByColumnName = \"\", string orderBySort = \"\", bool useCache = true)");
        sb.AppendLine("    {");
        sb.AppendLine("        var req = base.CreateGetRowsRequest(pageNo, pageSize, orderByColumnName, orderBySort);");
        sb.AppendLine("        var resp = await _client.GetRowsAsync(req, AppGrpcSession.Headers!);");
        sb.AppendLine($"        var rows = new List<{entityName}>();");
        sb.AppendLine("        foreach (var row in resp.Rows)");
        sb.AppendLine("        {");
        sb.AppendLine("            rows.Add(MapRowToTableEntity(row));");
        sb.AppendLine("        }");
        sb.AppendLine($"        return new {entityName}RowsData");
        sb.AppendLine("        {");
        sb.AppendLine("            Success = resp.Success,");
        sb.AppendLine("            Rows = rows,");
        sb.AppendLine("            FromCache = resp.FromCache,");
        sb.AppendLine("            Message = resp.Message");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();
        // InsertAsync
        sb.AppendLine($"    public async Task<{entityName}InsertedData> InsertAsync({entityName} entity, bool modifyCache = true)");
        sb.AppendLine("    {");
        sb.AppendLine("        var req = base.CreateInsertRequest(entity, modifyCache);");
        sb.AppendLine("        var resp = await _client.InsertAsync(req, AppGrpcSession.Headers!);");
        sb.AppendLine($"        return new {entityName}InsertedData");
        sb.AppendLine("        {");
        sb.AppendLine("            Success = resp.Success,");
        sb.AppendLine("            InsertedRow = resp.InsertedRow != null ? MapRowToTableEntity(resp.InsertedRow) : null,");
        sb.AppendLine("            CacheUpdated = resp.CacheUpdated,");
        sb.AppendLine("            Message = resp.Message");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();
        // GetAsync
        if (pkCols.Count > 0)
        {
            var pkParams = string.Join(", ", pkCols.Select(c => $"{MapSqlTypeToCSharp(c.Type, c.IsNullable)} {ToCamelCase(c.Name)}"));
            sb.AppendLine($"    public async Task<{entityName}RowData> GetAsync({pkParams}, bool useCache = true)");
            sb.AppendLine("    {");
            sb.AppendLine("        var req = base.CreateGetRequest();");
            foreach (var pk in pkCols)
                sb.AppendLine($"        req.PrimaryKeyValues.Add(\"{pk.Name}\", {ToCamelCase(pk.Name)}.ToString());");
            sb.AppendLine("        var resp = await _client.GetAsync(req, AppGrpcSession.Headers!);");
            sb.AppendLine($"        return new {entityName}RowData");
            sb.AppendLine("        {");
            sb.AppendLine("            Success = resp.Success,");
            sb.AppendLine("            Row = resp.Row != null ? MapRowToTableEntity(resp.Row) : null,");
            sb.AppendLine("            FromCache = resp.FromCache,");
            sb.AppendLine("            Message = resp.Message");
            sb.AppendLine("        };");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        // UpdateAsync
        sb.AppendLine($"    public async Task<UpdatedData> UpdateAsync({entityName} entity, bool modifyCache = true)");
        sb.AppendLine("    {");
        sb.AppendLine("        var req = CreateUpdateRequest(entity, modifyCache);");
        sb.AppendLine("        return await UpdateCoreAsync(req);");
        sb.AppendLine("    }");
        sb.AppendLine();
        // MapRowToTableEntity
        sb.AppendLine($"    protected override {entityName} MapRowToTableEntity(Row row)");
        sb.AppendLine("    {");
        sb.AppendLine("        var dict = new Dictionary<string, string?>();");
        sb.AppendLine("        foreach (var entry in row.Entries)");
        sb.AppendLine("        {");
        sb.AppendLine("            dict[entry.Column] = entry.Value?.StringValue;");
        sb.AppendLine("        }");
        sb.AppendLine($"        return new {entityName}");
        sb.AppendLine("        {");
        foreach (var col in columns)
        {
            if (col.Name.Equals("Version", StringComparison.OrdinalIgnoreCase))
                continue;
            var propName = ToCamelCase(col.Name);
            var csharpType = MapSqlTypeToCSharp(col.Type, col.IsNullable);
            if (csharpType == "int")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var v1) && int.TryParse(v1, out var i1) ? i1 : 0,");
            else if (csharpType == "long")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var v2) && long.TryParse(v2, out var l2) ? l2 : 0L,");
            else if (csharpType == "short")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var v3) && short.TryParse(v3, out var s3) ? s3 : (short)0,");
            else if (csharpType == "byte")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var v4) && byte.TryParse(v4, out var b4) ? b4 : (byte)0,");
            else if (csharpType == "bool")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var v5) && bool.TryParse(v5, out var b5) ? b5 : false,");
            else if (csharpType == "decimal")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var v6) && decimal.TryParse(v6, out var d6) ? d6 : 0m,");
            else if (csharpType == "double")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var v7) && double.TryParse(v7, out var d7) ? d7 : 0.0,");
            else if (csharpType == "float")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var v8) && float.TryParse(v8, out var f8) ? f8 : 0f,");
            else if (csharpType == "DateTime")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var v9) && DateTime.TryParse(v9, out var d9) ? d9 : DateTime.MinValue,");
            else if (csharpType == "Guid")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var v10) && Guid.TryParse(v10, out var g10) ? g10 : Guid.Empty,");
            else if (csharpType == "byte[]")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var v11) ? Convert.FromBase64String(v11 ?? \"\") : Array.Empty<byte>(),");
            else if (csharpType == "string")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var v12) ? v12 ?? string.Empty : string.Empty,");
            else if (csharpType == "string?")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var v13) ? v13 : null,");
            else if (csharpType.EndsWith("?"))
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var v14) && !string.IsNullOrEmpty(v14) ? ({csharpType.TrimEnd('?')})Convert.ChangeType(v14, typeof({csharpType.TrimEnd('?')})) : null,");
            else
                sb.AppendLine($"            {propName} = /* parse from dict[\"{col.Name}\"] as {csharpType} */ default,");
        }
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();
        // MapTableEntityToRow
        sb.AppendLine($"    protected override Row MapTableEntityToRow({entityName} entity, DbOperationType dbOperationType)");
        sb.AppendLine("    {");
        sb.AppendLine("        var row = new Row();");
        if (pkCols.Count > 0)
        {
            sb.AppendLine("        if (dbOperationType != DbOperationType.Insert)");
            sb.AppendLine("        {");
            foreach (var pk in pkCols)
            {
                sb.AppendLine($"            row.Entries.Add(new RowEntry {{ Column = \"{pk.Name}\", Value = new Value {{ StringValue = entity.{ToCamelCase(pk.Name)}.ToString() }} }});");
            }
            sb.AppendLine("        }");
        }
        foreach (var col in columns)
        {
            if (col.Name.Equals("Version", StringComparison.OrdinalIgnoreCase) || col.IsPrimaryKey)
                continue;
            var colName = ToCamelCase(col.Name);
            if (col.Type.ToLower().Contains("date"))
            {
                sb.AppendLine($"        if (entity.{colName} != DateTime.MinValue)");
                sb.AppendLine("        {");
                sb.AppendLine($"            row.Entries.Add(new RowEntry {{ Column = \"{col.Name}\", Value = new Value {{ StringValue = entity.{colName}.ToString(\"yyyy-MM-dd HH:mm:ss.fff\") }} }});");
                sb.AppendLine("        }");
            }
            else
            {
                sb.AppendLine($"        row.Entries.Add(new RowEntry {{ Column = \"{col.Name}\", Value = new Value {{ StringValue = entity.{colName}?.ToString() }} }});");
            }
        }
        sb.AppendLine("        return row;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private string MapSqlTypeToCSharp(string sqlType, bool isNullable)
    {
        string type = sqlType.ToLower() switch
        {
            "int" => "int",
            "bigint" => "long",
            "smallint" => "short",
            "tinyint" => "byte",
            "bit" => "bool",
            "decimal" or "numeric" or "money" or "smallmoney" => "decimal",
            "float" => "double",
            "real" => "float",
            "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" => "DateTime",
            "char" or "varchar" or "nchar" or "nvarchar" or "text" or "ntext" => "string",
            "uniqueidentifier" => "Guid",
            "binary" or "varbinary" or "image" => "byte[]",
            _ => "string"
        };
        if (type != "string" && type != "byte[]" && isNullable)
            return type + "?";
        if (type == "string" && isNullable)
            return "string?";
        return type;
    }
}
