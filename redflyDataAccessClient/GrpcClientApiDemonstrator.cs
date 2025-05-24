using RedflyCoreFramework;
using redflyGeneratedDataAccessApi.SqlServer.ProxyTestAdventureWorks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyDataAccessClient;
internal class GrpcClientApiDemonstrator
{

    internal static async Task Demonstrate()
    {
        Console.WriteLine("Let us now explore the power of redfly.ai APIs accessed through Grpc with the AdventureWorks sample database:");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// Create the data source object.");
        Console.WriteLine("var addressDataSource = new SalesLTAddressDataSource();");
        Console.ResetColor();
        var addressDataSource = new SalesLTAddressDataSource();

        await ShowTotalRowCountApiUsage(addressDataSource);

        Console.WriteLine("Press ANY key to continue...");
        Console.ReadKey();
        Console.WriteLine();

        var rowsData = await ShowGetRowsApiUsage(addressDataSource);

        Console.WriteLine("Press ANY key to continue...");
        Console.ReadKey();
        Console.WriteLine();

        if (rowsData != null &&
            rowsData.Rows.Count > 0)
        {
            var rowData = await ShowGetApiUsage(addressDataSource, rowsData.Rows[0]);

            Console.WriteLine("Press ANY key to continue...");
            Console.ReadKey();
            Console.WriteLine();
        }

        var productDataSource = new SalesLTProductDataSource();
        var productRowsData = await productDataSource.GetRowsAsync(1, 5, "ProductID", "ASC");

        await ShowJoinUsingGetSqlRowsApi(productDataSource, productRowsData.Rows[0]);

        Console.WriteLine("Press ANY key to continue...");
        Console.ReadKey();
        Console.WriteLine();

        await ShowJoinUsingGrpcApi(productDataSource, productRowsData.Rows[0]);

        Console.WriteLine("Press ANY key to continue...");
        Console.ReadKey();
        Console.WriteLine();

        var inserted = await ShowInsertApiUsage(addressDataSource);

        Console.WriteLine("Press ANY key to continue...");
        Console.ReadKey();
        Console.WriteLine();

        if (inserted != null &&
            inserted.InsertedRow != null)
        {
            await ShowUpdateApiUsage(addressDataSource, inserted);

            Console.WriteLine("Press ANY key to continue...");
            Console.ReadKey();
            Console.WriteLine();

            await ShowDeleteApiUsage(addressDataSource, inserted);

            Console.WriteLine("Press ANY key to continue...");
            Console.ReadKey();
            Console.WriteLine();
        }

        Console.WriteLine("Nothing else to demo.");
    }

    private static async Task ShowDeleteApiUsage(SalesLTAddressDataSource addressDataSource, SalesLTAddressInsertedData inserted)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"// Delete the record");
        Console.WriteLine("// Call the delete method with the primary key value.");
        Console.WriteLine($"var deleted = await addressDataSource.DeleteAsync({inserted.InsertedRow.AddressId});");
        Console.ResetColor();

        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            watch.Restart();
            var deleted = await addressDataSource.DeleteAsync(inserted.InsertedRow.AddressId);
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

    private static async Task ShowUpdateApiUsage(SalesLTAddressDataSource addressDataSource, SalesLTAddressInsertedData inserted)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"// Update the city from '{inserted.InsertedRow.City}' to 'Redmond'");
        Console.WriteLine($"inserted.InsertedRow.City = \"Redmond\";");
        Console.WriteLine();
        Console.WriteLine("// Call the update method with the object.");
        Console.WriteLine("var updated = await addressDataSource.UpdateAsync(inserted.InsertedRow);");
        Console.ResetColor();

        inserted.InsertedRow.City = "Redmond";

        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            watch.Restart();
            var updated = await addressDataSource.UpdateAsync(inserted.InsertedRow);
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

    private static async Task<SalesLTAddressInsertedData?> ShowInsertApiUsage(SalesLTAddressDataSource addressDataSource)
    {
        var newAddress = new SalesLTAddress
        {
            AddressLine1 = "123 Main St",
            AddressLine2 = "Apt 4B",
            City = "Seattle",
            StateProvince = "WA",
            CountryRegion = "USA",
            PostalCode = "98101",
            ModifiedDate = DateTime.Now
        };

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// Create a new object to insert it into the database.");
        Console.WriteLine("var newAddress = new SalesLTAddress");
        Console.WriteLine("{");
        Console.WriteLine("    AddressLine1 = \"123 Main St\",");
        Console.WriteLine("    AddressLine2 = \"Apt 4B\",");
        Console.WriteLine("    City = \"Seattle\",");
        Console.WriteLine("    StateProvince = \"WA\",");
        Console.WriteLine("    CountryRegion = \"USA\",");
        Console.WriteLine("    PostalCode = \"98101\"");
        Console.WriteLine("    ModifiedDate = DateTime.Now");
        Console.WriteLine("};");

        Console.WriteLine();
        Console.WriteLine("// Call the insert method with the object.");
        Console.WriteLine("var inserted = await addressDataSource.InsertAsync(newAddress);");
        Console.ResetColor();

        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);
        SalesLTAddressInsertedData? inserted = null;

        try
        {
            watch.Restart();
            inserted = await addressDataSource.InsertAsync(newAddress);
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

    private static async Task<SalesLTAddressRowData?> ShowGetApiUsage(SalesLTAddressDataSource addressDataSource, SalesLTAddress salesLTAddress)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// Get a row by its primary key");
        Console.WriteLine("var rowData = await addressDataSource.GetAsync(salesLTAddress.AddressId);");
        Console.ResetColor();

        SalesLTAddressRowData? rowData = null;
        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            watch.Restart();
            rowData = await addressDataSource.GetAsync(salesLTAddress.AddressId);
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

    private static async Task<SalesLTAddressRowsData?> ShowGetRowsApiUsage(SalesLTAddressDataSource addressDataSource)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// Get rows with support for pagination");
        Console.WriteLine("var rowsData = await addressDataSource.GetRowsAsync(pageNo: 1, pageSize: 5);");
        Console.ResetColor();

        SalesLTAddressRowsData? rowsData = null;
        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            watch.Restart();
            rowsData = await addressDataSource.GetRowsAsync(
                                pageNo: 1, 
                                pageSize: 5, 
                                orderByColumnName: "City", 
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

    private static async Task ShowJoinUsingGetSqlRowsApi(
        SalesLTProductDataSource productDataSource, 
        SalesLTProduct product)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// Execute a custom SQL query joining several tables");
        Console.WriteLine("string sqlQuery = @\"");
        Console.WriteLine("    select PR.*, PC.*, PM.* from SalesLT.Product PR");
        Console.WriteLine("    INNER JOIN SalesLT.ProductCategory PC ON PC.ProductCategoryID = PR.ProductCategoryID");
        Console.WriteLine("    INNER JOIN SalesLT.ProductModel PM ON PM.ProductModelID = PR.ProductModelID");
        Console.WriteLine($"    where PR.ProductID = {product.ProductId};");
        Console.ResetColor();

        string sqlQuery = @"
            select PR.*, PC.*, PM.* from SalesLT.Product PR
            INNER JOIN SalesLT.ProductCategory PC ON PC.ProductCategoryID = PR.ProductCategoryID 
            INNER JOIN SalesLT.ProductModel PM ON PM.ProductModelID = PR.ProductModelID
            where PR.ProductID = " + product.ProductId.ToString() + ";";

        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            watch.Restart();
            var sqlRowsData = await productDataSource.GetSqlRowsAsync(sqlQuery);
            watch.Stop();

            cts.Cancel();
            await progressTask;
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("// Get the result as a list of rows.");
            Console.WriteLine("// This demonstrates getting a specific Address by its primary key (AddressID = 1)");
            Console.WriteLine("// and then getting the linked CustomerAddress data in one query.");
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

    private static async Task ShowJoinUsingGrpcApi(
        SalesLTProductDataSource productDataSource, 
        SalesLTProduct product)
    {
        var productCategoryDataSource = new SalesLTProductCategoryDataSource();
        var productModelDataSource = new SalesLTProductModelDataSource();

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// The same custom SQL query joining two tables can be executed");
        Console.WriteLine("// using redfly Grpc API calls in a much more intuitive manner.");
        Console.WriteLine("// This is also more scalable than using SQL queries which");
        Console.WriteLine("// will slow down over time with data and usage growth.");
        Console.WriteLine("var productRow = await productDataSource.GetAsync(product.ProductId);");
        Console.WriteLine("productCategoryRow = await productCategoryDataSource.GetAsync(product.ProductCategoryId.Value);");
        Console.WriteLine("productModelRow = await productModelDataSource.GetAsync(product.ProductModelId.Value);");
        Console.ResetColor();

        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        SalesLTProductCategoryRowData? productCategoryRow = null;
        SalesLTProductModelRowData? productModelRow = null;

        try
        {
            watch.Restart();

            var productRow = await productDataSource.GetAsync(product.ProductId);

            if (product.ProductCategoryId != null)
            {
                productCategoryRow = await productCategoryDataSource.GetAsync(product.ProductCategoryId.Value);
            }

            if (product.ProductModelId != null)
            {
                productModelRow = await productModelDataSource.GetAsync(product.ProductModelId.Value);
            }

            watch.Stop();

            cts.Cancel();
            await progressTask;
            Console.WriteLine();

            ShowObjectResult(watch, productRow, productCategoryRow, productModelRow);
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

    private static async Task ShowTotalRowCountApiUsage(SalesLTAddressDataSource addressDataSource)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// Make the method call");
        Console.WriteLine("var rowCount = await addressDataSource.GetTotalRowCountAsync();");
        Console.ResetColor();

        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            watch.Restart();
            var rowCount = await addressDataSource.GetTotalRowCountAsync();
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

    private static void ShowObjectResult<A, B, C>(Stopwatch watch, A result1, B result2, C result3)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// Get the result as an object.");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Result Types: {typeof(A).Name}, {typeof(B).Name}, {typeof(C).Name}");

        foreach (var prop in typeof(A).GetProperties())
        {
            var value = prop.GetValue(result1, null);
            Console.WriteLine($"  {prop.Name}: {value}");
        }

        foreach (var prop in typeof(B).GetProperties())
        {
            var value = prop.GetValue(result2, null);
            Console.WriteLine($"  {prop.Name}: {value}");
        }

        foreach (var prop in typeof(C).GetProperties())
        {
            var value = prop.GetValue(result3, null);
            Console.WriteLine($"  {prop.Name}: {value}");
        }

        Console.WriteLine();
        Console.WriteLine($"Response Time: {watch.ElapsedMilliseconds} ms");
        Console.ResetColor();

        Console.WriteLine();
    }

}
