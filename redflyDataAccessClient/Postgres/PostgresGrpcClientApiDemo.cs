using RedflyCoreFramework;
using redflyGeneratedDataAccessApi.Postgres.AdventureWorks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyDataAccessClient.Postgres;
internal class PostgresGrpcClientApiDemo
{
    internal static async Task Run()
    {
        Console.WriteLine("Let us now explore the power of redfly.ai APIs accessed through Grpc with the AdventureWorks Postgres database:");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// Create the data source object.");
        Console.WriteLine("var departmentDataSource = new HumanresourcesDepartmentDataSource();");
        Console.ResetColor();
        var departmentDataSource = new HumanresourcesDepartmentDataSource();

        await ShowTotalRowCountApiUsage(departmentDataSource);

        Console.WriteLine("Press ANY key to continue...");
        Console.ReadKey();
        Console.WriteLine();

        var rowsData = await ShowGetRowsApiUsage(departmentDataSource);

        Console.WriteLine("Press ANY key to continue...");
        Console.ReadKey();
        Console.WriteLine();

        if (rowsData != null &&
            rowsData.Rows.Count > 0)
        {
            var rowData = await ShowGetApiUsage(departmentDataSource, rowsData.Rows[0]);

            Console.WriteLine("Press ANY key to continue...");
            Console.ReadKey();
            Console.WriteLine();
        }

        await ShowSqlRowsApiUsage(departmentDataSource);

        Console.WriteLine("Press ANY key to continue...");
        Console.ReadKey();
        Console.WriteLine();

        var inserted = await ShowInsertApiUsage(departmentDataSource);

        Console.WriteLine("Press ANY key to continue...");
        Console.ReadKey();
        Console.WriteLine();

        if (inserted != null &&
            inserted.InsertedRow != null)
        {
            await ShowUpdateApiUsage(departmentDataSource, inserted);

            Console.WriteLine("Press ANY key to continue...");
            Console.ReadKey();
            Console.WriteLine();

            await ShowDeleteApiUsage(departmentDataSource, inserted);

            Console.WriteLine("Press ANY key to continue...");
            Console.ReadKey();
            Console.WriteLine();
        }

        Console.WriteLine("Completed demonstrating the Postgres Grpc Client API.");
    }

    private static async Task ShowDeleteApiUsage(HumanresourcesDepartmentDataSource departmentDataSource, HumanresourcesDepartmentInsertedData inserted)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"// Delete the record");
        Console.WriteLine("// Call the delete method with the primary key value.");
        Console.WriteLine($"var deleted = await departmentDataSource.DeleteAsync({inserted.InsertedRow.Departmentid});");
        Console.ResetColor();

        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            watch.Restart();
            var deleted = await departmentDataSource.DeleteAsync(inserted.InsertedRow.Departmentid);
            watch.Stop();

            cts.Cancel();
            await progressTask;
            Console.WriteLine();
            ShowObjectResult(watch, deleted);
        }
        catch (Exception ex)
        {
            cts.Cancel();
            await progressTask;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static async Task ShowUpdateApiUsage(HumanresourcesDepartmentDataSource departmentDataSource, HumanresourcesDepartmentInsertedData inserted)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"// Update the name from '{inserted.InsertedRow.Name}' to 'Updated Department'");
        Console.WriteLine($"inserted.InsertedRow.Name = \"Updated Department\";");
        Console.WriteLine();
        Console.WriteLine("// Call the update method with the object.");
        Console.WriteLine("var updated = await departmentDataSource.UpdateAsync(inserted.InsertedRow);");
        Console.ResetColor();

        inserted.InsertedRow.Name = "Updated Department";

        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            watch.Restart();
            var updated = await departmentDataSource.UpdateAsync(inserted.InsertedRow);
            watch.Stop();

            cts.Cancel();
            await progressTask;
            Console.WriteLine();
            ShowObjectResult(watch, updated);
        }
        catch (Exception ex)
        {
            cts.Cancel();
            await progressTask;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static async Task<HumanresourcesDepartmentInsertedData?> ShowInsertApiUsage(HumanresourcesDepartmentDataSource departmentDataSource)
    {
        var newDepartment = new HumanresourcesDepartment
        {
            Departmentid = 99,
            Name = "Test Department",
            Groupname = "Test Group",
            Modifieddate = DateTime.Now
        };

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// Create a new object to insert it into the database.");
        Console.WriteLine("var newDepartment = new HumanresourcesDepartment");
        Console.WriteLine("{");
        Console.WriteLine("    Departmentid = 99,");
        Console.WriteLine("    Name = \"Test Department\",");
        Console.WriteLine("    Groupname = \"Test Group\",");
        Console.WriteLine("    Modifieddate = DateTime.Now");
        Console.WriteLine("};");

        Console.WriteLine();
        Console.WriteLine("// Call the insert method with the object.");
        Console.WriteLine("var inserted = await departmentDataSource.InsertAsync(newDepartment);");
        Console.ResetColor();

        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);
        HumanresourcesDepartmentInsertedData? inserted = null;

        try
        {
            watch.Restart();
            inserted = await departmentDataSource.InsertAsync(newDepartment);
            watch.Stop();

            cts.Cancel();
            await progressTask;
            Console.WriteLine();
            ShowObjectResult(watch, inserted);
        }
        catch (Exception ex)
        {
            cts.Cancel();
            await progressTask;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.ResetColor();
        }

        return inserted;
    }

    private static async Task<HumanresourcesDepartmentRowData?> ShowGetApiUsage(HumanresourcesDepartmentDataSource departmentDataSource, HumanresourcesDepartment department)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// Get a row by its primary key");
        Console.WriteLine("var rowData = await departmentDataSource.GetAsync(department.Departmentid);");
        Console.ResetColor();

        HumanresourcesDepartmentRowData? rowData = null;
        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            watch.Restart();
            rowData = await departmentDataSource.GetAsync(department.Departmentid);
            watch.Stop();

            cts.Cancel();
            await progressTask;
            Console.WriteLine();
            ShowObjectResult(watch, rowData);
        }
        catch (Exception ex)
        {
            cts.Cancel();
            await progressTask;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.ResetColor();
        }

        return rowData;
    }

    private static async Task<HumanresourcesDepartmentRowsData?> ShowGetRowsApiUsage(HumanresourcesDepartmentDataSource departmentDataSource)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// Get rows with support for pagination");
        Console.WriteLine("var rowsData = await departmentDataSource.GetRowsAsync(pageNo: 1, pageSize: 5);");
        Console.ResetColor();

        HumanresourcesDepartmentRowsData? rowsData = null;
        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            watch.Restart();
            rowsData = await departmentDataSource.GetRowsAsync(
                                pageNo: 1, 
                                pageSize: 5, 
                                orderByColumnName: "name", 
                                orderBySort: "asc");
            watch.Stop();

            cts.Cancel();
            await progressTask;
            Console.WriteLine();
            ShowObjectResult(watch, rowsData);
        }
        catch (Exception ex)
        {
            cts.Cancel();
            await progressTask;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.ResetColor();
        }

        return rowsData;
    }

    private static async Task ShowSqlRowsApiUsage(HumanresourcesDepartmentDataSource departmentDataSource)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// Execute a custom SQL query");
        Console.WriteLine("string sqlQuery = @\"");
        Console.WriteLine("    SELECT * FROM humanresources.department");
        Console.WriteLine("    ORDER BY name ASC");
        Console.WriteLine("    LIMIT 5\";");
        Console.ResetColor();

        string sqlQuery = @"
            SELECT * FROM humanresources.department
            ORDER BY name ASC
            LIMIT 5";

        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            watch.Restart();
            var sqlRowsData = await departmentDataSource.GetSqlRowsAsync(sqlQuery);
            watch.Stop();

            cts.Cancel();
            await progressTask;
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("// Get the result as a list of rows.");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Result Type: GenericRowsData");
            Console.WriteLine($"  Success: {sqlRowsData.Success}");
            Console.WriteLine($"  FromCache: {sqlRowsData.FromCache}");
            Console.WriteLine($"  Message: {sqlRowsData.Message}");
            Console.WriteLine($"  Rows Count: {sqlRowsData.Rows.Count}");
            Console.WriteLine();

            if (sqlRowsData.Rows.Count > 0)
            {
                Console.WriteLine("First row of data:");
                int displayCount = Math.Min(5, sqlRowsData.Rows.Count);

                for (int i = 0; i < displayCount; i++)
                {
                    var row = sqlRowsData.Rows[i];
                    Console.WriteLine($"Row #{i + 1}:");

                    foreach (var entry in row.Entries)
                    {
                        string value = entry.Value.StringValue ?? "null";
                        Console.WriteLine($"  {entry.Column}: {value}");
                    }
                    Console.WriteLine();
                }
            }

            Console.WriteLine($"Response Time: {watch.ElapsedMilliseconds} ms");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            cts.Cancel();
            await progressTask;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine();
    }

    private static async Task ShowTotalRowCountApiUsage(HumanresourcesDepartmentDataSource departmentDataSource)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// Make the method call");
        Console.WriteLine("var rowCount = await departmentDataSource.GetTotalRowCountAsync();");
        Console.ResetColor();

        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            watch.Restart();
            var rowCount = await departmentDataSource.GetTotalRowCountAsync();
            watch.Stop();

            cts.Cancel();
            await progressTask;
            Console.WriteLine();
            ShowObjectResult(watch, rowCount);
        }
        catch (Exception ex)
        {
            cts.Cancel();
            await progressTask;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static void ShowObjectResult<T>(Stopwatch watch, T result)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// Get the result as an object.");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Result Type: {typeof(T).Name}");

        foreach (var prop in typeof(T).GetProperties())
        {
            var value = prop.GetValue(result, null);
            Console.WriteLine($"  {prop.Name}: {value}");
        }

        Console.WriteLine();
        Console.WriteLine($"Response Time: {watch.ElapsedMilliseconds} ms");
        Console.ResetColor();

        Console.WriteLine();
    }
}