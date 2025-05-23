using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

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
            var columns = GetColumns(connectionString, table.Schema, table.Name);
            var code = GenerateCodeForTable(table, columns);
            var fileName = Path.Combine(outputFolder, $"{table.Schema}{table.Name}DataSource.cs");
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

    private List<(string Name, string Type, bool IsNullable)> GetColumns(string connectionString, string schema, string table)
    {
        var columns = new List<(string, string, bool)>();
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table";
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", table);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            columns.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2) == "YES"));
        }
        return columns;
    }

    private string GenerateCodeForTable((string Schema, string Name) table, List<(string Name, string Type, bool IsNullable)> columns)
    {
        var sb = new StringBuilder();
        var entityName = $"{table.Schema}{table.Name}";
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
            sb.AppendLine($"    public {MapSqlTypeToCSharp(col.Type, col.IsNullable)} {col.Name} {{ get; set; }}");
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
        sb.AppendLine("    // TODO: Implement CRUD methods similar to the template, using base methods");
        sb.AppendLine();
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
            sb.AppendLine($"            {col.Name} = /* parse from dict[\"{col.Name}\"] as {MapSqlTypeToCSharp(col.Type, col.IsNullable)} */ default,");
        }
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    protected override Row MapTableEntityToRow({entityName} entity, DbOperationType dbOperationType)");
        sb.AppendLine("    {");
        sb.AppendLine("        var row = new Row();");
        sb.AppendLine("        // TODO: Add logic to handle primary key and special columns as in the template");
        foreach (var col in columns)
        {
            sb.AppendLine($"        row.Entries.Add(new RowEntry {{ Column = \"{col.Name}\", Value = new Value {{ StringValue = entity.{col.Name}.ToString() }} }});");
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
        return type;
    }
}
