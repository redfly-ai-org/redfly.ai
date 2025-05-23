using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RedflyCoreFramework;
using redflyDataAccessClient.Protos.SqlServer;
using redflyDatabaseAdapters;
using redflyDatabaseAdapters.Setup;
using RedflyLocalStorage;
using RedflyLocalStorage.Collections;
using System.Diagnostics;

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

            var grpcUrl = "https://hosted-chakra-grpc-linux.azurewebsites.net/";

            Console.WriteLine("Connect to the LOCAL WIN DEV environment? (y/n)");
            Console.WriteLine("This option is only relevant to redfly employees.");
            var response = Console.ReadLine();

            if (response != null &&
                response.Equals("y", StringComparison.CurrentCultureIgnoreCase))
            {
                grpcUrl = "https://localhost:7176";
            }
            else
            {
                Console.WriteLine("Connect to the LOCAL LINUX/ WSL DEV environment? (y/n)");
                Console.WriteLine("This option is only relevant to redfly employees.");
                response = Console.ReadLine();

                if (response != null &&
                    response.Equals("y", StringComparison.CurrentCultureIgnoreCase))
                {
                    grpcUrl = "http://localhost:5053";
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

                        grpcUrl = response;
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Will connect to: {grpcUrl}");
            Console.ResetColor();

            var grpcAuthToken = await RedflyGrpcAuthServiceClient.AuthGrpcClient.RunAsync(grpcUrl);

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
            var channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
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

            var headers = new Metadata
                {
                    { "Authorization", $"Bearer {grpcAuthToken}" }
                };


            if (!await RedflyUserOrOrg.Setup(channel, headers)) { return; }

            if (isSqlServerSync)
            {
                GetSyncProfilesResponse? getSyncProfilesResponse = null;
                var syncApiClient = new SyncApiService.SyncApiServiceClient(channel);

                var cts = new CancellationTokenSource();
                var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

                try
                {
                    getSyncProfilesResponse = await SqlServerSyncProfile.GetAllAsync(syncApiClient, channel, headers);
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

                // Create the client
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
                                await PromptUserForTableRowCount(headers, sqlServerApiClient, tableSchemaName, tableName);
                                break;
                            case "2":
                                await PromptUserForGetTableRows(headers, sqlServerApiClient, tableSchemaName, tableName);
                                break;
                            case "3":
                                await PromptUserForGetTableRow(headers, sqlServerApiClient, tableSchemaName, tableName);
                                break;
                            case "4":
                                await PromptUserForInsertRow(headers, sqlServerApiClient, tableSchemaName, tableName);
                                break;
                            case "5":
                                await PromptUserForUpdateRow(headers, sqlServerApiClient, tableSchemaName, tableName);
                                break;
                            case "6":
                                await PromptUserForDeleteRow(headers, sqlServerApiClient, tableSchemaName, tableName);
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

                // TODO: Generate the strongly typed client code for the tables.
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

    private static async Task PromptUserForDeleteRow(
                                Metadata headers,
                                NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient sqlServerApiClient,
                                string tableSchemaName,
                                string tableName)
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

        var watch = new Stopwatch();
        watch.Start();
        var deleteResponse = await sqlServerApiClient.DeleteAsync(deleteRequest, headers);
        watch.Stop();

        ShowResults(watch, deleteResponse);
    }

    private static DeleteRequest CreateDeleteRequest(
        string tableSchemaName,
        string tableName,
        Dictionary<string, string> primaryKeyValues)
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

    private static void ShowResults(Stopwatch watch, DeleteResponse deleteResponse)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(JsonConvert.SerializeObject(deleteResponse, Formatting.Indented));
        Console.ResetColor();
        Console.WriteLine($"Response Time: {watch.ElapsedMilliseconds} ms");
    }

    private static async Task PromptUserForUpdateRow(Metadata headers, NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient sqlServerApiClient, string tableSchemaName, string tableName)
    {
        Console.WriteLine("First enter the details for the row to be updated - this should include the primary keys and updated values.");
        var updatedData = PromptUserForColumnValuePairs();

        var updateRequest = CreateUpdateRequest(tableSchemaName, tableName, updatedData);

        var watch = new Stopwatch();
        watch.Start();
        var updateResponse = await sqlServerApiClient.UpdateAsync(updateRequest, headers);
        watch.Stop();

        ShowResults(watch, updateResponse);
    }

    private static async Task PromptUserForInsertRow(Metadata headers, NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient sqlServerApiClient, string tableSchemaName, string tableName)
    {
        Console.WriteLine("First enter the details for the row to be inserted - only NOT NULL columns have to be mandatorily entered.");
        var insertedData = PromptUserForColumnValuePairs();

        var insertRequest = CreateInsertRequest(tableSchemaName, tableName, insertedData);

        var watch = new Stopwatch();
        watch.Start();
        var insertResponse = await sqlServerApiClient.InsertAsync(insertRequest, headers);
        watch.Stop();

        ShowResults(watch, insertResponse);
    }

    private static async Task PromptUserForGetTableRow(Metadata headers, NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient sqlServerApiClient, string tableSchemaName, string tableName)
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

        var watch = new Stopwatch();
        watch.Start();
        var getResponse = await sqlServerApiClient.GetAsync(getRequest, headers);
        watch.Stop();

        ShowResults(watch, getResponse);
    }

    private static async Task PromptUserForGetTableRows(Metadata headers, NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient sqlServerApiClient, string tableSchemaName, string tableName)
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

        var watch = new Stopwatch();
        watch.Start();
        var getRowsResponse = await sqlServerApiClient.GetRowsAsync(getRowsRequest, headers);
        watch.Stop();

        ShowResults(watch, getRowsResponse);
    }

    private static async Task PromptUserForTableRowCount(Metadata headers, NativeGrpcSqlServerApiService.NativeGrpcSqlServerApiServiceClient sqlServerApiClient, string tableSchemaName, string tableName)
    {
        // Prepare the request
        var getTotalRowCountRequest = CreateGetTotalRowCountRequest(tableSchemaName, tableName);

        var watch = new Stopwatch();
        watch.Start();
        var getTotalRowCountResponse = await sqlServerApiClient.GetTotalRowCountAsync(getTotalRowCountRequest, headers);
        watch.Stop();

        ShowResults(watch, getTotalRowCountResponse);
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

    private static void ShowResults(Stopwatch watch, InsertResponse insertResponse)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(JsonConvert.SerializeObject(insertResponse, Formatting.Indented));
        Console.ResetColor();
        Console.WriteLine($"Response Time: {watch.ElapsedMilliseconds} ms");
    }

    private static void ShowResults(Stopwatch watch, UpdateResponse updateResponse)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(JsonConvert.SerializeObject(updateResponse, Formatting.Indented));
        Console.ResetColor();
        Console.WriteLine($"Response Time: {watch.ElapsedMilliseconds} ms");
    }

    private static void ShowResults(Stopwatch watch, GetResponse getResponse)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(JsonConvert.SerializeObject(getResponse, Formatting.Indented));
        Console.ResetColor();
        Console.WriteLine($"Response Time: {watch.ElapsedMilliseconds} ms");
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

    private static void ShowResults(Stopwatch watch, GetRowsResponse getRowsResponse)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(JsonConvert.SerializeObject(getRowsResponse, Formatting.Indented));
        Console.ResetColor();
        Console.WriteLine($"Response Time: {watch.ElapsedMilliseconds} ms");
    }

    private static void ShowResults(Stopwatch watch, GetTotalRowCountResponse getTotalRowCountResponse)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(JsonConvert.SerializeObject(getTotalRowCountResponse, Formatting.Indented));
        Console.ResetColor();
        Console.WriteLine($"Response Time: {watch.ElapsedMilliseconds} ms");
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

}
