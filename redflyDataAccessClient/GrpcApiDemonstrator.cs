using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RedflyCoreFramework;
using redflyDatabaseAdapters;
using redflyGeneratedDataAccessApi.Protos.SqlServer;
using redflyGeneratedDataAccessApi.SqlServer.ProxyTestAdventureWorks;
using System.Diagnostics;

namespace redflyDataAccessClient;

internal class GrpcApiDemonstrator
{

    internal static async Task DemonstrateGrpcAPIsDirectly(GrpcChannel channel)
    {
        var sqlServerApiClient = new NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient(channel);

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
                        await PromptUserForTableRowCount(sqlServerApiClient, tableSchemaName, tableName);
                        break;
                    case "2":
                        await PromptUserForGetTableRows(sqlServerApiClient, tableSchemaName, tableName);
                        break;
                    case "3":
                        await PromptUserForGetTableRow(sqlServerApiClient, tableSchemaName, tableName);
                        break;
                    case "4":
                        await PromptUserForInsertRow(sqlServerApiClient, tableSchemaName, tableName);
                        break;
                    case "5":
                        await PromptUserForUpdateRow(sqlServerApiClient, tableSchemaName, tableName);
                        break;
                    case "6":
                        await PromptUserForDeleteRow(sqlServerApiClient, tableSchemaName, tableName);
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

    private static async Task PromptUserForDeleteRow(NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient sqlServerApiClient, string tableSchemaName, string tableName)
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

        var deleteRequest = CreateDeleteRequest(tableSchemaName, tableName, primaryKeyValues);

        Console.WriteLine("Getting results from the server...");

        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            var watch = new Stopwatch();
            watch.Start();
            var deleteResponse = await sqlServerApiClient.DeleteAsync(deleteRequest, AppGrpcSession.Headers!);
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

    private static DeleteRequest CreateDeleteRequest(string tableSchemaName, string tableName, Dictionary<string, string> primaryKeyValues)
    {
        var deleteRequest = new DeleteRequest
        {
            EncryptedDatabaseServerName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.HostName),
            EncryptedDatabaseName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Name),
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile!.Database.ClientId),
            EncryptedDatabaseId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Id),
            EncryptedServerOnlyConnectionString = RedflyEncryption.EncryptToString(
                $"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;"),
            EncryptionKey = RedflyEncryptionKeys.AesKey,
            ModifyCache = true
        };

        foreach (var kvp in primaryKeyValues)
        {
            deleteRequest.PrimaryKeyValues.Add(kvp.Key, kvp.Value);
        }

        return deleteRequest;
    }

    private static void ShowResultsAsJson<T>(Stopwatch watch, T response)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
        Console.ResetColor();
        Console.WriteLine($"Response Time: {watch.ElapsedMilliseconds} ms");
        Console.WriteLine();
    }

    private static async Task PromptUserForUpdateRow(NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient sqlServerApiClient, string tableSchemaName, string tableName)
    {
        Console.WriteLine("First enter the details for the row to be updated - this should include the primary keys and updated values.");
        var updatedData = PromptUserForColumnValuePairs();

        var updateRequest = CreateUpdateRequest(tableSchemaName, tableName, updatedData);

        Console.WriteLine("Getting results from the server...");

        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            var watch = new Stopwatch();
            watch.Start();
            var updateResponse = await sqlServerApiClient.UpdateAsync(updateRequest, AppGrpcSession.Headers!);
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

    private static async Task PromptUserForInsertRow(NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient sqlServerApiClient, string tableSchemaName, string tableName)
    {
        Console.WriteLine("First enter the details for the row to be inserted - only NOT NULL columns have to be mandatorily entered.");
        var insertedData = PromptUserForColumnValuePairs();

        var insertRequest = CreateInsertRequest(tableSchemaName, tableName, insertedData);

        Console.WriteLine("Getting results from the server...");

        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            var watch = new Stopwatch();
            watch.Start();
            var insertResponse = await sqlServerApiClient.InsertAsync(insertRequest, AppGrpcSession.Headers!);
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

    private static async Task PromptUserForGetTableRow(NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient sqlServerApiClient, string tableSchemaName, string tableName)
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

        GetRequest getRequest = CreateGetRequest(tableSchemaName, tableName, primaryKeyColumnName, primaryKeyColumnValue);

        Console.WriteLine("Getting results from the server...");

        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            var watch = new Stopwatch();
            watch.Start();
            var getResponse = await sqlServerApiClient.GetAsync(getRequest, AppGrpcSession.Headers!);
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

    private static async Task PromptUserForGetTableRows(NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient sqlServerApiClient, string tableSchemaName, string tableName)
    {
        var orderByColumnName = "";
        var orderByColumnSort = "asc";

        while (string.IsNullOrEmpty(orderByColumnName))
        {
            Console.WriteLine("Please enter the column by which you want the records ordered:");
            orderByColumnName = Console.ReadLine();
        }

        Console.WriteLine();

        var getRowsRequest = CreateGetRowsCachedRequest(tableSchemaName, tableName, orderByColumnName, orderByColumnSort);

        Console.WriteLine("Getting results from the server...");

        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            var watch = new Stopwatch();
            watch.Start();
            var getRowsResponse = await sqlServerApiClient.GetRowsAsync(getRowsRequest, AppGrpcSession.Headers!);
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

    private static async Task PromptUserForTableRowCount(NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient sqlServerApiClient, string tableSchemaName, string tableName)
    {
        // Prepare the request
        var getTotalRowCountRequest = CreateGetTotalRowCountRequest(tableSchemaName, tableName);

        Console.WriteLine("Getting results from the server...");

        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            var watch = new Stopwatch();
            watch.Start();
            var getTotalRowCountResponse = await sqlServerApiClient.GetTotalRowCountAsync(getTotalRowCountRequest, AppGrpcSession.Headers!);
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

    private static Dictionary<string, string> PromptUserForColumnValuePairs()
    {
        var insertedData = new Dictionary<string, string>();

        while (true)
        {
            Console.WriteLine("Enter column name (leave empty to finish):");
            var columnName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(columnName))
                break;

            Console.WriteLine($"Enter value for column '{columnName}':");
            var columnValue = Console.ReadLine() ?? string.Empty;

            insertedData[columnName] = columnValue;
        }

        Console.WriteLine("Collected columns and values for insertion:");
        foreach (var kvp in insertedData)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
        }

        return insertedData;
    }

    private static InsertRequest CreateInsertRequest(string tableSchemaName, string tableName, Dictionary<string, string> insertedData)
    {
        var insertRequest = new InsertRequest
        {
            EncryptedDatabaseServerName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.HostName),
            EncryptedDatabaseName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Name),
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile!.Database.ClientId),
            EncryptedDatabaseId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Id),
            EncryptedServerOnlyConnectionString = RedflyEncryption.EncryptToString($"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;"),
            EncryptionKey = RedflyEncryptionKeys.AesKey,
            ModifyCache = true
        };

        insertRequest.Row = new Row();

        foreach (var kvp in insertedData)
        {
            insertRequest.Row.Entries.Add(new RowEntry() { Column = kvp.Key, Value = new Value() { StringValue = kvp.Value.IsNullOrEmpty() ? null : kvp.Value } });
        }

        return insertRequest;
    }

    private static UpdateRequest CreateUpdateRequest(string tableSchemaName, string tableName, Dictionary<string, string> updatedData)
    {
        var updateRequest = new UpdateRequest
        {
            EncryptedDatabaseServerName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.HostName),
            EncryptedDatabaseName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Name),
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile!.Database.ClientId),
            EncryptedDatabaseId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Id),
            EncryptedServerOnlyConnectionString = RedflyEncryption.EncryptToString($"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;"),
            EncryptionKey = RedflyEncryptionKeys.AesKey,
            ModifyCache = true
        };

        updateRequest.Row = new Row();

        foreach (var kvp in updatedData)
        {
            updateRequest.Row.Entries.Add(new RowEntry() { Column = kvp.Key, Value = new Value() { StringValue = kvp.Value.IsNullOrEmpty() ? null : kvp.Value } });
        }

        return updateRequest;
    }

    private static GetRequest CreateGetRequest(string tableSchemaName, string tableName, string primaryKeyColumnName, string primaryKeyColumnValue)
    {
        var getRequest = new GetRequest()
        {
            EncryptedDatabaseServerName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.HostName),
            EncryptedDatabaseName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Name),
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile!.Database.ClientId),
            EncryptedDatabaseId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Id),
            EncryptedServerOnlyConnectionString = RedflyEncryption.EncryptToString($"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;"),
            EncryptionKey = RedflyEncryptionKeys.AesKey
        };

        getRequest.PrimaryKeyValues.Add(primaryKeyColumnName, primaryKeyColumnValue);
        return getRequest;
    }

    private static GetRowsRequest CreateGetRowsCachedRequest(string tableSchemaName, string tableName, string orderByColumnName, string orderByColumnSort)
    {
        return new GetRowsRequest
        {
            EncryptedDatabaseServerName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.HostName),
            EncryptedDatabaseName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Name),
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile!.Database.ClientId),
            EncryptedDatabaseId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Id),
            EncryptedServerOnlyConnectionString = RedflyEncryption.EncryptToString($"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;"),
            EncryptionKey = RedflyEncryptionKeys.AesKey,
            OrderbyColumnName = orderByColumnName,
            OrderbyColumnSort = orderByColumnSort,
            PageNo = 1,
            PageSize = 5
        };
    }

    private static GetTotalRowCountRequest CreateGetTotalRowCountRequest(string tableSchemaName, string tableName)
    {
        return new GetTotalRowCountRequest
        {
            EncryptedDatabaseServerName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.HostName),
            EncryptedDatabaseName = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Name),
            EncryptedTableSchemaName = RedflyEncryption.EncryptToString(tableSchemaName),
            EncryptedTableName = RedflyEncryption.EncryptToString(tableName),
            EncryptedClientId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile!.Database.ClientId),
            EncryptedDatabaseId = RedflyEncryption.EncryptToString(AppGrpcSession.SyncProfile.Database.Id),
            EncryptedServerOnlyConnectionString = RedflyEncryption.EncryptToString($"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;"),
            EncryptionKey = RedflyEncryptionKeys.AesKey
        };
    }

}