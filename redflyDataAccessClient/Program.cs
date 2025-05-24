using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RedflyCoreFramework;
using redflyGeneratedDataAccessApi.Protos.SqlServer;
using redflyDatabaseAdapters;
using redflyDatabaseAdapters.Setup;
using RedflyLocalStorage;
using RedflyLocalStorage.Collections;
using System.Diagnostics;
using redflyGeneratedDataAccessApi;
using redflyGeneratedDataAccessApi.Compilers;
using redflyGeneratedDataAccessApi.SqlServer.ProxyTestAdventureWorks;
using redflyGeneratedDataAccessApi.SqlServer; // Add this namespace for GenericRowsData

namespace redflyDataAccessClient;

internal class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Console.Title = "redfly.ai - Data Access Client";

            DisplayWelcomeMessage();

            Console.WriteLine("Press any key to start the process of making a data access call through redfly APIs...");
            Console.ReadKey();
            Console.WriteLine("");

            AppGrpcSession.GrpcUrl = "https://hosted-chakra-grpc-linux.azurewebsites.net/";

            Console.WriteLine("Connect to the LOCAL WIN DEV environment? (y/n)");
            Console.WriteLine("This option is only relevant to redfly employees.");
            var response = Console.ReadLine();

            if (response != null &&
                response.Equals("y", StringComparison.CurrentCultureIgnoreCase))
            {
                AppGrpcSession.GrpcUrl = "https://localhost:7176";
            }
            else
            {
                Console.WriteLine("Connect to the LOCAL LINUX/ WSL DEV environment? (y/n)");
                Console.WriteLine("This option is only relevant to redfly employees.");
                response = Console.ReadLine();

                if (response != null &&
                    response.Equals("y", StringComparison.CurrentCultureIgnoreCase))
                {
                    AppGrpcSession.GrpcUrl = "http://localhost:5053";
                }
                else
                {
                    Console.WriteLine("Connect to a custom URL? (y/n)");
                    Console.WriteLine("This option is only relevant to redfly employees.");
                    response = Console.ReadLine();

                    if (response != null &&
                        response.Equals("y", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Console.WriteLine("Please enter the URL:");
                        response = Console.ReadLine();

                        while (!Uri.TryCreate(response, UriKind.Absolute, out var uriResult) ||
                               (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("The provided input is not a valid URL. Please enter a valid URL here:");
                            Console.ResetColor();

                            response = Console.ReadLine();
                        }

                        AppGrpcSession.GrpcUrl = response;
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Will connect to: {AppGrpcSession.GrpcUrl}");
            Console.ResetColor();

            var grpcAuthToken = await RedflyGrpcAuthServiceClient.AuthGrpcClient.RunAsync(AppGrpcSession.GrpcUrl);

            if (grpcAuthToken == null ||
                grpcAuthToken.Length == 0)
            {
                Console.WriteLine("Failed to authenticate with the gRPC server.");
                Console.WriteLine("Contact us at developer@redfly.ai if you need to.");
                return;
            }

            bool isPostgresSync = false;
            bool isMongoSync = false;
            bool isSqlServerSync = false;

            if (!isPostgresSync &&
                !isMongoSync)
            {
                Console.WriteLine("Are you trying to read data from a Sql Server database? (y/n)");
                response = Console.ReadLine();

                if (response != null &&
                    response.Equals("y", StringComparison.CurrentCultureIgnoreCase))
                {
                    isSqlServerSync = true;

                    if (!SqlServerReady.ForChakraSync(offerToPrepAgain: false))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("redfly SQL Server Data APIs cannot be used without syncing the Sql Server database.");
                        Console.WriteLine("Please sync the Sql Server database using the redflyDatabaseSyncProxy app and try again.");
                        Console.ResetColor();

                        return;
                    }
                }
            }

            var redisServerCollection = new LiteRedisServerCollection();

            if (isSqlServerSync)
            {
                SqlServerSyncRelationship.FindExistingRelationshipWithRedis(redisServerCollection);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("We only support SQL Server APIs at present.");
                Console.ResetColor();

                return;
            }

            //var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var channel = GrpcChannel.ForAddress(AppGrpcSession.GrpcUrl, new GrpcChannelOptions
            {
                //LoggerFactory = loggerFactory,
                HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(30), // Frequency of keepalive pings
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(5) // Timeout before considering the connection dead
                },
                HttpVersion = new Version(2, 0) // Ensure HTTP/2 is used
            });

            AppGrpcSession.Headers = new Metadata
                {
                    { "Authorization", $"Bearer {grpcAuthToken}" }
                };


            if (!await RedflyUserOrOrg.Setup(channel, AppGrpcSession.Headers)) { return; }

            if (isSqlServerSync)
            {
                GetSyncProfilesResponse? getSyncProfilesResponse = null;
                var syncApiClient = new SyncApiService.SyncApiServiceClient(channel);

                var cts = new CancellationTokenSource();
                var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

                try
                {
                    getSyncProfilesResponse = await SqlServerSyncProfile.GetAllAsync(syncApiClient, channel, AppGrpcSession.Headers);
                }
                finally
                {
                    cts.Cancel();
                    await progressTask;
                }

                SyncProfileViewModel? syncProfile = null;

                if (getSyncProfilesResponse != null &&
                    SqlServerSyncProfile.Exists(getSyncProfilesResponse))
                {
                    syncProfile = (from p in getSyncProfilesResponse.Profiles
                                   where p.Database.HostName == AppDbSession.SqlServerDatabase!.DecryptedServerName &&
                                         p.Database.Name == AppDbSession.SqlServerDatabase!.DecryptedDatabaseName &&
                                         p.RedisServer.HostName == AppDbSession.RedisServer!.DecryptedServerName
                                   select p).FirstOrDefault();
                }

                if (syncProfile == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No matching Sync Profiles found.");
                    Console.WriteLine("Please sync the Sql Server database using the redflyDatabaseSyncProxy app and try again.");
                    Console.ResetColor();

                    return;
                }

                AppGrpcSession.SyncProfile = syncProfile;
                Console.WriteLine("The Sync Profile was successfully retrieved from the server.");

                // Make calls to the SQL Server APIs from here!.
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("The API calls can be made now!");
                Console.ResetColor();
                Console.WriteLine();

                Console.WriteLine("Do you want to generate the API classes for your database now? (y/n)");
                response = Console.ReadLine();

                if (response != null &&
                    response.ToLower() == "y")
                {
                    (new SqlServerGrpcPolyLangCompiler())
                        .GenerateForDatabase(
                            $"Server=tcp:{AppGrpcSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppDbSession.SqlServerDatabase!.DecryptedUserName};Password={AppDbSession.SqlServerDatabase.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;Initial Catalog={AppDbSession.SqlServerDatabase!.DecryptedDatabaseName};",
                            "C:\\Code\\redfly-oss\\redflyGeneratedDataAccessApi\\SqlServer\\" + AppGrpcSession.SyncProfile.Database.Name.Replace(" ", "") + "\\");

                    Console.WriteLine("Press ANY key to exit so you can recompile the app and run again.");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("Are you using the AdventureWorks database? (y/n)");
                response = Console.ReadLine();

                if (response != null && 
                    response.ToLower() == "y") 
                {
                    await DemonstrateClientApiUsage();
                }
                else
                {
                    Console.WriteLine("Strongly typed APIs in this repo only work with the AdventureWorks database.");
                }

                await DemonstrateGrpcAPIsDirectly(channel);
            }

            Console.WriteLine("All API calls are completed!");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.ToString());
            Console.WriteLine("Contact us at developer@redfly.ai if you need to.");
            Console.ResetColor();
        }
        finally
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

            RedflyLocalDatabase.Dispose();
        }
    }

    private static async Task DemonstrateClientApiUsage()
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

        await ShowGetSqlRowsApiUsage(addressDataSource);

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
            rowsData = await addressDataSource.GetRowsAsync(pageNo: 1, pageSize: 5);
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

    private static async Task ShowGetSqlRowsApiUsage(SalesLTAddressDataSource addressDataSource)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("// Execute a custom SQL query joining multiple tables");
        Console.WriteLine("// This SQL query joins SalesLT.Address with SalesLT.CustomerAddress and SalesLT.Customer tables");
        Console.WriteLine("string sqlQuery = @\"");
        Console.WriteLine("    SELECT c.CustomerID, c.FirstName, c.LastName, a.AddressID, a.AddressLine1, a.City, a.StateProvince");
        Console.WriteLine("    FROM SalesLT.Address a");
        Console.WriteLine("    JOIN SalesLT.CustomerAddress ca ON a.AddressID = ca.AddressID");
        Console.WriteLine("    JOIN SalesLT.Customer c ON ca.CustomerID = c.CustomerID");
        Console.WriteLine("    WHERE a.City = 'Seattle'");
        Console.WriteLine("    ORDER BY c.LastName, c.FirstName\";");
        Console.ResetColor();

        string sqlQuery = @"
            SELECT c.CustomerID, c.FirstName, c.LastName, a.AddressID, a.AddressLine1, a.City, a.StateProvince
            FROM SalesLT.Address a
            JOIN SalesLT.CustomerAddress ca ON a.AddressID = ca.AddressID
            JOIN SalesLT.Customer c ON ca.CustomerID = c.CustomerID
            WHERE a.City = 'Seattle'
            ORDER BY c.LastName, c.FirstName";

        var watch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

        try
        {
            watch.Restart();
            var sqlRowsData = await addressDataSource.GetSqlRowsAsync(sqlQuery);
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
                Console.WriteLine("First 5 rows of data (or all if less than 5):");
                int displayCount = Math.Min(5, sqlRowsData.Rows.Count);
                
                for (int i = 0; i < displayCount; i++)
                {
                    var row = sqlRowsData.Rows[i];
                    Console.WriteLine($"Row #{i+1}:");
                    
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

    private static async Task DemonstrateGrpcAPIsDirectly(GrpcChannel channel)
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

    private static void DisplayWelcomeMessage()
    {
        Console.WriteLine("This console app is intended to allow anyone to try out our transparent data access APIs for Postgres, MongoDB & SQL Server on-demand.\r\n");

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("(1) Run this app AFTER running the redflyDatabaseSyncProxy and starting database sync.\r\n");

        Console.WriteLine("(2) For the APIs to work, your Database should be accessible over the Internet and its firewall should allow traffic");
        Console.WriteLine("    from these Azure IP addresses (US East Region). We plan to support non-public and local servers in a later");
        Console.WriteLine("    release of this application.\r\n");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("    20.237.7.43, 20.237.7.49, 20.237.7.128, 20.237.7.153, 20.237.7.201, 20.237.7.221, 40.71.11.140");
        Console.WriteLine("    40.121.154.115, 13.82.228.43, 40.121.158.167, 40.117.44.182, 168.61.50.107, 40.121.80.139");
        Console.WriteLine("    40.117.44.94, 23.96.53.166, 40.121.152.91, 20.237.7.43, 20.237.7.49, 20.237.7.128");
        Console.WriteLine("    20.237.7.153, 20.237.7.201, 20.237.7.221, 20.246.144.9, 20.246.144.108, 20.246.144.117");
        Console.WriteLine("    20.246.144.140, 20.246.144.145, 20.246.144.213, 40.71.11.140\r\n");

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("(3) This is a 24x7 setup with multiple servers & elastic servers to handle load.");
        Console.WriteLine("    We typically update our Cloud Services only when necessary usually over the");
        Console.WriteLine("    weekend, on holidays and on alternate Fridays.");
        Console.WriteLine("    The best time to test a long running sync is at the start of the working week.");
        Console.WriteLine("    Please ignore ANY Grpc errors.\r\n");
        Console.ResetColor();

        Console.WriteLine("This is a demo application designed to give you a taste of our capabilities. It is NOT intended for production use.\r\n");
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
}
