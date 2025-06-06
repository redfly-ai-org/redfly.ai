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
public class SqlServerGrpcPolyLangCompiler
{
    private string _connectionString = "";

    public void GenerateForDatabase(string connectionString, string outputFolder)
    {
        _connectionString = connectionString;
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

    private string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        input = RemoveSpaces(input);
        if (input.Length == 1) return input.ToUpperInvariant();
        return char.ToUpperInvariant(input[0]) + input.Substring(1);
    }

    private string ToParameterCase(string input)
    {
        // PascalCase to camelCase, but also handle ID -> Id, GUID -> Guid, etc.
        if (string.IsNullOrEmpty(input)) return input;
        input = RemoveSpaces(input);
        var pascal = ToPascalCase(input).Replace("ID", "Id"); // Always use Id not ID
        return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
    }

    private string GenerateCodeForTable(string classBaseName, (string Schema, string Name) table, List<(string Name, string Type, bool IsNullable, bool IsPrimaryKey)> columns)
    {
        var sb = new StringBuilder();
        var entityName = classBaseName;
        var dataSourceName = $"{entityName}DataSource";
        // Usings
        sb.AppendLine("using RedflyCoreFramework;");
        sb.AppendLine("using redflyDatabaseAdapters;");
        sb.AppendLine("using redflyGeneratedDataAccessApi.Base;");
        sb.AppendLine("using redflyGeneratedDataAccessApi.Common;");
        sb.AppendLine("using redflyGeneratedDataAccessApi.Protos.DatabaseApi;");
        sb.AppendLine();
        // Add comments as in SalesLTAddressDataSource
        var dbName = GetDatabaseNameFromConnectionString(_connectionString);        
        // Entity class
        var namespaceName = $"redflyGeneratedDataAccessApi.SqlServer.{dbName}";
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine($"// Strongly-typed classes for [{table.Schema}].[{table.Name}] generated by the");
        sb.AppendLine($"// redfly {this.GetType().Name} on {DateTime.Now.ToString("MM/dd/yy hh:mm:ss tt")}");
        sb.AppendLine("// This is only meant to be indicative of the features available in the core product.");
        sb.AppendLine();
        sb.AppendLine($"public class {entityName} : BaseSqlServerTableSchema");
        sb.AppendLine("{");
        foreach (var col in columns)
        {
            if (col.Name.Equals("Version", StringComparison.OrdinalIgnoreCase))
                continue;
            var csharpType = MapSqlTypeToCSharp(col.Type, col.IsNullable);
            var propName = ToPascalCase(col.Name).Replace("ID", "Id");
            // If it's a non-nullable reference type, initialize to string.Empty
            if (csharpType == "string" && !col.IsNullable)
                sb.AppendLine($"    public string {propName} {{ get; set; }} = string.Empty;");
            // If it's a non-nullable byte array, initialize to Array.Empty<byte>()
            else if (csharpType == "byte[]" && !col.IsNullable)
                sb.AppendLine($"    public byte[] {propName} {{ get; set; }} = Array.Empty<byte>();");
            // If it's a nullable byte array
            else if (csharpType == "byte[]" && col.IsNullable)
                sb.AppendLine($"    public byte[]? {propName} {{ get; set; }}");
            // If it's a non-nullable value type, just declare
            else if (!col.IsNullable && (csharpType == "Guid" || csharpType == "int" || csharpType == "decimal" || csharpType == "byte" || csharpType == "short" || csharpType == "long" || csharpType == "bool" || csharpType == "float" || csharpType == "double"))
                sb.AppendLine($"    public {csharpType} {propName} {{ get; set; }}");
            // If it's a nullable value type, use ?
            else if (col.IsNullable && (csharpType == "Guid" || csharpType == "int" || csharpType == "decimal" || csharpType == "byte" || csharpType == "short" || csharpType == "long" || csharpType == "bool" || csharpType == "float" || csharpType == "double"))
                sb.AppendLine($"    public {csharpType}? {propName} {{ get; set; }}");
            // Otherwise, just declare
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
        sb.AppendLine($"public class {dataSourceName} : BaseSqlServerTableDataSource<{entityName}>");
        sb.AppendLine("{");
        // Constructor with _encSchema and _encTable initialization
        sb.AppendLine($"    public {dataSourceName}() : base()");
        sb.AppendLine("    {");
        sb.AppendLine($"        _encSchema = RedflyEncryption.EncryptToString(\"{table.Schema}\");");
        sb.AppendLine($"        _encTable = RedflyEncryption.EncryptToString(\"{table.Name}\");");
        sb.AppendLine("    }");
        sb.AppendLine();
        // Primary key columns
        var pkCols = columns.Where(c => c.IsPrimaryKey).ToList();
        // DeleteAsync
        if (pkCols.Count > 0)
        {
            var pkParams = string.Join(", ", pkCols.Select(c => $"{MapSqlTypeToCSharp(c.Type, c.IsNullable)} {ToParameterCase(c.Name)}"));
            sb.AppendLine($"    public async Task<DeletedData> DeleteAsync({pkParams}, bool modifyCache = true)");
            sb.AppendLine("    {");
            sb.AppendLine("        var req = base.CreateDeleteRequest(modifyCache);");
            foreach (var pk in pkCols)
                sb.AppendLine($"        req.PrimaryKeyValues.Add(\"{pk.Name}\", {ToParameterCase(pk.Name)}.ToString());");
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
            var pkParams = string.Join(", ", pkCols.Select(c => $"{MapSqlTypeToCSharp(c.Type, c.IsNullable)} {ToParameterCase(c.Name)}"));
            sb.AppendLine($"    public async Task<{entityName}RowData> GetAsync({pkParams}, bool useCache = true)");
            sb.AppendLine("    {");
            sb.AppendLine("        var req = base.CreateGetRequest();");
            foreach (var pk in pkCols)
                sb.AppendLine($"        req.PrimaryKeyValues.Add(\"{pk.Name}\", {ToParameterCase(pk.Name)}.ToString());");
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
        int varCounter = 1;
        foreach (var col in columns)
        {
            if (col.Name.Equals("Version", StringComparison.OrdinalIgnoreCase))
                continue;
            var propName = ToPascalCase(col.Name).Replace("ID", "Id");
            var csharpType = MapSqlTypeToCSharp(col.Type, col.IsNullable);
            string varName = $"v{varCounter}";
            // Non-nullable value types
            if (csharpType == "int")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && int.TryParse({varName}, out var i{varCounter}) ? i{varCounter} : 0,");
            else if (csharpType == "long")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && long.TryParse({varName}, out var l{varCounter}) ? l{varCounter} : 0L,");
            else if (csharpType == "short")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && short.TryParse({varName}, out var s{varCounter}) ? s{varCounter} : (short)0,");
            else if (csharpType == "byte")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && byte.TryParse({varName}, out var b{varCounter}) ? b{varCounter} : (byte)0,");
            else if (csharpType == "bool")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && bool.TryParse({varName}, out var b{varCounter}) ? b{varCounter} : false,");
            else if (csharpType == "decimal")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && decimal.TryParse({varName}, out var d{varCounter}) ? d{varCounter} : 0m,");
            else if (csharpType == "double")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && double.TryParse({varName}, out var d{varCounter}) ? d{varCounter} : 0.0,");
            else if (csharpType == "float")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && float.TryParse({varName}, out var f{varCounter}) ? f{varCounter} : 0f,");
            else if (csharpType == "DateTime")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && DateTime.TryParse({varName}, out var d{varCounter}) ? d{varCounter} : DateTime.MinValue,");
            else if (csharpType == "Guid")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && Guid.TryParse({varName}, out var g{varCounter}) ? g{varCounter} : Guid.Empty,");
            // Nullable value types: return null if missing or not parseable (no cast)
            else if (csharpType == "int?")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && !string.IsNullOrEmpty({varName}) && int.TryParse({varName}, out var i{varCounter}) ? i{varCounter} : null,");
            else if (csharpType == "long?")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && !string.IsNullOrEmpty({varName}) && long.TryParse({varName}, out var l{varCounter}) ? l{varCounter} : null,");
            else if (csharpType == "short?")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && !string.IsNullOrEmpty({varName}) && short.TryParse({varName}, out var s{varCounter}) ? s{varCounter} : null,");
            else if (csharpType == "byte?")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && !string.IsNullOrEmpty({varName}) && byte.TryParse({varName}, out var b{varCounter}) ? b{varCounter} : null,");
            else if (csharpType == "bool?")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && !string.IsNullOrEmpty({varName}) && bool.TryParse({varName}, out var b{varCounter}) ? b{varCounter} : null,");
            else if (csharpType == "decimal?")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && !string.IsNullOrEmpty({varName}) && decimal.TryParse({varName}, out var d{varCounter}) ? d{varCounter} : null,");
            else if (csharpType == "double?")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && !string.IsNullOrEmpty({varName}) && double.TryParse({varName}, out var d{varCounter}) ? d{varCounter} : null,");
            else if (csharpType == "float?")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && !string.IsNullOrEmpty({varName}) && float.TryParse({varName}, out var f{varCounter}) ? f{varCounter} : null,");
            else if (csharpType == "DateTime?")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && !string.IsNullOrEmpty({varName}) && DateTime.TryParse({varName}, out var d{varCounter}) ? d{varCounter} : null,");
            else if (csharpType == "Guid?")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && !string.IsNullOrEmpty({varName}) && Guid.TryParse({varName}, out var g{varCounter}) ? g{varCounter} : null,");
            // Byte array
            else if (csharpType == "byte[]")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) ? Convert.FromBase64String({varName} ?? \"\") : Array.Empty<byte>(),");
            else if (csharpType == "byte[]?")
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) && !string.IsNullOrEmpty({varName}) ? Convert.FromBase64String({varName}) : null,");
            // String
            else if (csharpType == "string" && !col.IsNullable)
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) ? {varName} ?? string.Empty : string.Empty,");
            else if (csharpType == "string?" || csharpType == "string" && col.IsNullable)
                sb.AppendLine($"            {propName} = dict.TryGetValue(\"{col.Name}\", out var {varName}) ? {varName} : null,");
            else
                sb.AppendLine($"            {propName} = /* parse from dict[\"{col.Name}\"] as {csharpType} */ default,");
            varCounter++;
        }
        // Version column
        sb.AppendLine("            Version = dict.TryGetValue(\"Version\", out var vVersion) ? Convert.FromBase64String(vVersion ?? \"\") : Array.Empty<byte>(),");
        
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
                var pkProp = ToPascalCase(pk.Name).Replace("ID", "Id");
                sb.AppendLine($"            row.Entries.Add(new RowEntry {{ Column = \"{pk.Name}\", Value = new Value {{ StringValue = entity.{pkProp}.ToString() }} }});");
            }
            sb.AppendLine("        }");
        }
        
        // Non-primary key columns
        foreach (var col in columns)
        {
            if (col.Name.Equals("Version", StringComparison.OrdinalIgnoreCase) || col.IsPrimaryKey)
                continue;
                
            var propName = ToPascalCase(col.Name).Replace("ID", "Id");
            var csharpType = MapSqlTypeToCSharp(col.Type, col.IsNullable);
            
            // DateTime handling
            if (csharpType == "DateTime")
            {
                sb.AppendLine($"        if (entity.{propName} != DateTime.MinValue)");
                sb.AppendLine("        {");
                sb.AppendLine($"            row.Entries.Add(new RowEntry {{ Column = \"{col.Name}\", Value = new Value {{ StringValue = entity.{propName}.ToString(\"yyyy-MM-dd HH:mm:ss.fff\") }} }});");
                sb.AppendLine("        }");
            }
            else if (csharpType == "DateTime?")
            {
                sb.AppendLine($"        if (entity.{propName} != null)");
                sb.AppendLine("        {");
                sb.AppendLine($"            row.Entries.Add(new RowEntry {{ Column = \"{col.Name}\", Value = new Value {{ StringValue = entity.{propName}.Value.ToString(\"yyyy-MM-dd HH:mm:ss.fff\") }} }});");
                sb.AppendLine("        }");
            }
            // Byte array handling
            else if (csharpType == "byte[]")
            {
                sb.AppendLine($"        row.Entries.Add(new RowEntry {{ Column = \"{col.Name}\", Value = new Value {{ StringValue = entity.{propName} != null ? Convert.ToBase64String(entity.{propName}) : null }} }});");
            }
            else if (csharpType == "byte[]?")
            {
                sb.AppendLine($"        row.Entries.Add(new RowEntry {{ Column = \"{col.Name}\", Value = new Value {{ StringValue = entity.{propName} != null ? Convert.ToBase64String(entity.{propName}) : null }} }});");
            }
            // String handling
            else if (csharpType == "string")
            {
                sb.AppendLine($"        row.Entries.Add(new RowEntry {{ Column = \"{col.Name}\", Value = new Value {{ StringValue = entity.{propName} }} }});");
            }
            else if (csharpType == "string?")
            {
                sb.AppendLine($"        row.Entries.Add(new RowEntry {{ Column = \"{col.Name}\", Value = new Value {{ StringValue = entity.{propName} }} }});");
            }
            // Value type handling
            else if (csharpType == "int" || csharpType == "long" || csharpType == "short" || csharpType == "byte" || 
                     csharpType == "bool" || csharpType == "decimal" || csharpType == "double" || csharpType == "float" || 
                     csharpType == "Guid")
            {
                sb.AppendLine($"        row.Entries.Add(new RowEntry {{ Column = \"{col.Name}\", Value = new Value {{ StringValue = entity.{propName}.ToString() }} }});");
            }
            // Nullable value type handling
            else if (csharpType == "int?" || csharpType == "long?" || csharpType == "short?" || csharpType == "byte?" || 
                     csharpType == "bool?" || csharpType == "decimal?" || csharpType == "double?" || csharpType == "float?" || 
                     csharpType == "Guid?")
            {
                sb.AppendLine($"        if (entity.{propName} != null)");
                sb.AppendLine("        {");
                sb.AppendLine($"            row.Entries.Add(new RowEntry {{ Column = \"{col.Name}\", Value = new Value {{ StringValue = entity.{propName}.ToString() }} }});");
                sb.AppendLine("        }");
                sb.AppendLine("        else");
                sb.AppendLine("        {");
                sb.AppendLine($"            row.Entries.Add(new RowEntry {{ Column = \"{col.Name}\", Value = new Value {{ StringValue = null }} }});");
                sb.AppendLine("        }");
            }
            // Default fallback for unknown types
            else
            {
                sb.AppendLine($"        row.Entries.Add(new RowEntry {{ Column = \"{col.Name}\", Value = new Value {{ StringValue = entity.{propName}?.ToString() }} }});");
            }
        }
        
        sb.AppendLine("        return row;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private string GetDatabaseNameFromConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        return builder.InitialCatalog;
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
