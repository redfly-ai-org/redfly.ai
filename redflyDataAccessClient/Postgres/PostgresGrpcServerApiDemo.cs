using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RedflyCoreFramework;
using redflyDatabaseAdapters;
using PostgresProtos = redflyGeneratedDataAccessApi.Protos.Postgres;
using System.Diagnostics;
using redflyDataAccessClient.Base;

namespace redflyDataAccessClient.Postgres;

internal class PostgresGrpcServerApiDemo : GrpcServerApiDemoBase
{
    internal static async Task Run(GrpcChannel channel)
    {
        var postgresApiClient = new PostgresProtos.NativeGrpcPostgresApiService.NativeGrpcPostgresApiServiceClient(channel);

        Console.WriteLine("Next, we will go through many operations for one table in the database...");

        string? tableSchemaName = null;
        var tableName = "";

        while (tableSchemaName == null)
        {
            Console.WriteLine("Please enter the table schema name");
            Console.WriteLine("This could be an empty string.");
            tableSchemaName = Console.ReadLine();
        }

        while (string.IsNullOrEmpty(tableName))
        {
            Console.WriteLine("Please enter the table name");
            tableName = Console.ReadLine();
        }

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nChoose an operation to perform on the table:");
            Console.WriteLine("  1. Get table row count");
            Console.WriteLine("  2. Get table rows (paged)");
            Console.WriteLine("  3. Get a row by primary key");
            Console.WriteLine("  4. Insert a row");
            Console.WriteLine("  5. Update a row");
            Console.WriteLine("  6. Delete a row");
            Console.WriteLine("  0. Exit");
            Console.ResetColor();

            Console.Write("Enter your choice: ");
            var choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await PromptUserForTableRowCount(postgresApiClient, tableSchemaName, tableName);
                        break;
                    case "2":
                        await PromptUserForGetTableRows(postgresApiClient, tableSchemaName, tableName);
                        break;
                    case "3":
                        await PromptUserForGetTableRow(postgresApiClient, tableSchemaName, tableName);
                        break;
                    case "4":
                        await PromptUserForInsertRow(postgresApiClient, tableSchemaName, tableName);
                        break;
                    case "5":
                        await PromptUserForUpdateRow(postgresApiClient, tableSchemaName, tableName);
                        break;
                    case "6":
                        await PromptUserForDeleteRow(postgresApiClient, tableSchemaName, tableName);
                        break;
                    case "0":
                        Console.WriteLine("Exiting table operations menu.");
                        return;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid choice. Please enter a number from the list.");
                        Console.ResetColor();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR on command execution: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    private static async Task PromptUserForDeleteRow(PostgresProtos.NativeGrpcPostgresApiService.NativeGrpcPostgresApiServiceClient postgresApiClient, string tableSchemaName, string tableName)
    {
        // Collect primary key column(s) and value(s)
        var primaryKeyValues = new Dictionary<string, string>();
        Console.WriteLine("Enter the primary key column(s) and value(s) for the row to delete.");
        while (true)
        {
            Console.WriteLine("Enter primary key column name (leave empty to finish):");
            var columnName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(columnName))
                break;

            Console.WriteLine($"Enter value for column '{columnName}':");
            var columnValue = Console.ReadLine() ?? string.Empty;

            primaryKeyValues[columnName] = columnValue;
        }

        if (primaryKeyValues.Count == 0)
        {
            Console.WriteLine("No primary key values entered. Aborting delete operation.");
            return;
        }

        var deleteRequest = PostgresGrpcServerApiRequests.CreateDeleteRequest(tableSchemaName, tableName, primaryKeyValues);

        Console.WriteLine("Getting results from the server...");

        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            var watch = new Stopwatch();
            watch.Start();
            var deleteResponse = await postgresApiClient.DeleteAsync(deleteRequest, AppGrpcSession.Headers!);
            watch.Stop();

            cts.Cancel();
            await progressTask;

            ShowResultsAsJson(watch, deleteResponse);
        }
        catch
        {
            cts.Cancel();
            await progressTask;

            throw;
        }
    }

    private static async Task PromptUserForUpdateRow(PostgresProtos.NativeGrpcPostgresApiService.NativeGrpcPostgresApiServiceClient postgresApiClient, string tableSchemaName, string tableName)
    {
        Console.WriteLine("First enter the details for the row to be updated - this should include the primary keys and updated values.");
        var updatedData = PromptUserForColumnValuePairs();

        var updateRequest = PostgresGrpcServerApiRequests.CreateUpdateRequest(tableSchemaName, tableName, updatedData);

        Console.WriteLine("Getting results from the server...");

        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            var watch = new Stopwatch();
            watch.Start();
            var updateResponse = await postgresApiClient.UpdateAsync(updateRequest, AppGrpcSession.Headers!);
            watch.Stop();

            cts.Cancel();
            await progressTask;

            ShowResultsAsJson(watch, updateResponse);
        }
        catch
        {
            cts.Cancel();
            await progressTask;

            throw;
        }
    }

    private static async Task PromptUserForInsertRow(PostgresProtos.NativeGrpcPostgresApiService.NativeGrpcPostgresApiServiceClient postgresApiClient, string tableSchemaName, string tableName)
    {
        Console.WriteLine("First enter the details for the row to be inserted - only NOT NULL columns have to be mandatorily entered.");
        var insertedData = PromptUserForColumnValuePairs();

        var insertRequest = PostgresGrpcServerApiRequests.CreateInsertRequest(tableSchemaName, tableName, insertedData);

        Console.WriteLine("Getting results from the server...");

        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            var watch = new Stopwatch();
            watch.Start();
            var insertResponse = await postgresApiClient.InsertAsync(insertRequest, AppGrpcSession.Headers!);
            watch.Stop();

            cts.Cancel();
            await progressTask;

            ShowResultsAsJson(watch, insertResponse);
        }
        catch
        {
            cts.Cancel();
            await progressTask;

            throw;
        }
    }

    private static async Task PromptUserForGetTableRow(PostgresProtos.NativeGrpcPostgresApiService.NativeGrpcPostgresApiServiceClient postgresApiClient, string tableSchemaName, string tableName)
    {
        var primaryKeyColumnName = "";
        var primaryKeyColumnValue = "";

        while (string.IsNullOrEmpty(primaryKeyColumnName))
        {
            Console.WriteLine("Please enter the primary key column name:");
            primaryKeyColumnName = Console.ReadLine();
        }

        while (string.IsNullOrEmpty(primaryKeyColumnValue))
        {
            Console.WriteLine("Please enter the primary key column value:");
            primaryKeyColumnValue = Console.ReadLine();
        }

        var getRequest = PostgresGrpcServerApiRequests.CreateGetRequest(tableSchemaName, tableName, primaryKeyColumnName, primaryKeyColumnValue);

        Console.WriteLine("Getting results from the server...");

        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            var watch = new Stopwatch();
            watch.Start();
            var getResponse = await postgresApiClient.GetAsync(getRequest, AppGrpcSession.Headers!);
            watch.Stop();

            cts.Cancel();
            await progressTask;

            ShowResultsAsJson(watch, getResponse);
        }
        catch
        {
            cts.Cancel();
            await progressTask;

            throw;
        }
    }

    private static async Task PromptUserForGetTableRows(PostgresProtos.NativeGrpcPostgresApiService.NativeGrpcPostgresApiServiceClient postgresApiClient, string tableSchemaName, string tableName)
    {
        var orderByColumnName = "";
        var orderByColumnSort = "asc";

        while (string.IsNullOrEmpty(orderByColumnName))
        {
            Console.WriteLine("Please enter the column by which you want the records ordered:");
            orderByColumnName = Console.ReadLine();
        }

        Console.WriteLine();

        var getRowsRequest = PostgresGrpcServerApiRequests.CreateGetRowsRequest(tableSchemaName, tableName, orderByColumnName, orderByColumnSort);

        Console.WriteLine("Getting results from the server...");

        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            var watch = new Stopwatch();
            watch.Start();
            var getRowsResponse = await postgresApiClient.GetRowsAsync(getRowsRequest, AppGrpcSession.Headers!);
            watch.Stop();

            cts.Cancel();
            await progressTask;

            ShowResultsAsJson(watch, getRowsResponse);
        }
        catch
        {
            cts.Cancel();
            await progressTask;

            throw;
        }
    }

    private static async Task PromptUserForTableRowCount(PostgresProtos.NativeGrpcPostgresApiService.NativeGrpcPostgresApiServiceClient postgresApiClient, string tableSchemaName, string tableName)
    {
        // Prepare the request
        var getTotalRowCountRequest = PostgresGrpcServerApiRequests.CreateGetTotalRowCountRequest(tableSchemaName, tableName);

        Console.WriteLine("Getting results from the server...");

        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            var watch = new Stopwatch();
            watch.Start();
            var getTotalRowCountResponse = await postgresApiClient.GetTotalRowCountAsync(getTotalRowCountRequest, AppGrpcSession.Headers!);
            watch.Stop();

            cts.Cancel();
            await progressTask;

            ShowResultsAsJson(watch, getTotalRowCountResponse);
        }
        catch
        {
            cts.Cancel();
            await progressTask;

            throw;
        }
    }
}