using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using LiteDB;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using RedflyCoreFramework;
using RedflyLocalStorage;
using RedflyLocalStorage.Collections;
using RedflyLocalStorage.Entities;
using System.Data.SqlTypes;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace RedflyDatabaseSyncProxy;

internal class Program
{
    internal static async Task Main(string[] _)
    {
        try
        {
            Console.Title = "redfly.ai - Database Sync Proxy";

            Console.WriteLine("This console app is intended to allow anyone to try out our synchronization services on-demand.\r\n");

            Console.WriteLine("1. We natively sync ANY database schema with Redis in the background.");
            Console.WriteLine("2. We can generate the backend code for any database with Redis caching built-in.");
            Console.WriteLine("3. This is hosted over Grpc.");

            Console.WriteLine("\r\nNow imagine you being able to do that with your database, without any manual effort! That's what our product does.");
            Console.WriteLine("Contact us at developer@redfly.ai to directly work with us so you can do the same thing with your database (cloud/ on-premises).");
            Console.WriteLine("No matter how large or complex your DB is, redfly.ai can do it! \r\n");

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("We suggest starting with a test database as we make non-invasive changes to your database which should NOT affect well designed, modern applications.\r\n");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("For sync to work, your Database should be accessible over the Internet and its firewall should allow traffic from these Azure IP addresses (US East Region). We plan to support non-public and local servers in a later release of this application.\r\n");
            Console.WriteLine("20.237.7.43, 20.237.7.49, 20.237.7.128, 20.237.7.153, 20.237.7.201, 20.237.7.221, 40.71.11.140");
            Console.WriteLine("40.121.154.115, 13.82.228.43, 40.121.158.167, 40.117.44.182, 168.61.50.107, 40.121.80.139, 40.117.44.94, 23.96.53.166, 40.121.152.91, 20.237.7.43, 20.237.7.49, 20.237.7.128, 20.237.7.153, 20.237.7.201, 20.237.7.221, 20.246.144.9, 20.246.144.108, 20.246.144.117, 20.246.144.140, 20.246.144.145, 20.246.144.213, 40.71.11.140\r\n");
            Console.ResetColor();

            Console.WriteLine("This is a demo application designed to give you a taste of our capabilities. It is NOT intended for production use.");
            Console.WriteLine("Press any key to start the process of synchronizing your database with Redis transparently...");
            Console.ReadKey();

            var grpcUrl = "https://hosted-chakra-grpc-linux.azurewebsites.net/";

            Console.WriteLine("Connect to the local DEV environment? (y/n)");
            var response = Console.ReadLine();
            
            if (response != null &&
                response.Equals("y", StringComparison.CurrentCultureIgnoreCase))
            {
                grpcUrl = "https://localhost:7176";                
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

            if (!SqlServerDatabasePrep.ForChakraSync())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Change Management cannot be started without prepping the database.");
                Console.WriteLine("Please prep the database and try again.");
                Console.ResetColor();

                return;
            }

            var redisServerCollection = new LiteRedisServerCollection();
            var syncRelationshipCollection = new LiteSyncRelationshipCollection();

            var syncRelationship = syncRelationshipCollection
                                        .FindByDatabase(AppSession.Database!.Id.ToString()).FirstOrDefault();

            if (syncRelationship == null)
            {
                if (!RedisServerPicker.SelectFromLocalStorage())
                {
                    if (!RedisServerPicker.GetFromUser())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Change Management cannot be started without selecting a target Redis Server.");
                        Console.WriteLine("Please select a Redis Server and try again.");
                        Console.ResetColor();
                        return;
                    }
                }

                syncRelationship = CreateSyncRelationship(syncRelationshipCollection);
            }

            AppSession.RedisServer = redisServerCollection
                                                            .FindById(new BsonValue(new ObjectId(syncRelationship.RedisServerId)));

            Console.WriteLine($"This database has a sync relationship with {AppSession.RedisServer.DecryptedServerName}:{AppSession.RedisServer.Port}");

            var channel = GrpcChannel.ForAddress(grpcUrl);
            var headers = new Metadata
                {
                    { "Authorization", $"Bearer {grpcAuthToken}" }
                };

            var userSetupApiClient = new UserSetupApi.UserSetupApiClient(channel);
            ServiceResponse? getUserSetupDataResponse = null;

            var cts = new CancellationTokenSource();
            var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

            try
            {
                getUserSetupDataResponse = await GetUserSetupData(userSetupApiClient, headers);
            }
            finally
            {
                cts.Cancel();
                await progressTask;
            }

            if (UserAccountOrOrgSetupRequired(getUserSetupDataResponse))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(getUserSetupDataResponse.Message);
                Console.ResetColor();

                ServiceValueResponse? addOrUpdateClientAndUserProfileResponse = null;

                cts = new CancellationTokenSource();
                progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

                try
                {
                    addOrUpdateClientAndUserProfileResponse = await PromptUserToSetupUserAccountAndOrg(userSetupApiClient, headers);
                }
                finally
                {
                    cts.Cancel();
                    await progressTask;
                }

                if (!addOrUpdateClientAndUserProfileResponse.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(addOrUpdateClientAndUserProfileResponse.Message);
                    Console.WriteLine("User Account and Organization setup could NOT be completed successfully. Please try again later");
                    Console.ResetColor();
                    return;
                }

                if (addOrUpdateClientAndUserProfileResponse.Success)
                {
                    Console.WriteLine(addOrUpdateClientAndUserProfileResponse.Message);
                    Console.WriteLine("User Account and Organization setup completed successfully.");

                    //Reload data, so it can be used.
                    getUserSetupDataResponse = await GetUserSetupData(userSetupApiClient, headers);
                }
            }

            var syncApiClient = new SyncApiService.SyncApiServiceClient(channel);
            GetSyncProfilesResponse? getSyncProfilesResponse = null;

            cts = new CancellationTokenSource();
            progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

            try
            {
                getSyncProfilesResponse = await syncApiClient.GetSyncProfilesAsync(new GetSyncProfilesRequest() { PageNo = 1, PageSize = 10 }, headers);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red; 
                Console.WriteLine($"Error reading sync profiles from the server: {ex}"); 
                Console.ResetColor();

                throw;
            }
            finally
            {
                cts.Cancel();
                await progressTask;
            }

            SyncProfileViewModel? syncProfile = null;

            if (SyncProfilesExist(getSyncProfilesResponse))
            {
                syncProfile = (from p in getSyncProfilesResponse.Profiles
                               where p.Database.HostName == AppSession.Database!.DecryptedServerName &&
                                     p.Database.Name == AppSession.Database!.DecryptedDatabaseName &&
                                     p.RedisServer.HostName == AppSession.RedisServer!.DecryptedServerName
                               select p).FirstOrDefault();
            }

            if (syncProfile == null)
            {
                Console.WriteLine("No matching Sync Profiles found. We will create one now.");

                var request = new AddOrUpdateSyncProfileRequest
                {
                    Profile = new AddOrUpdateSyncProfileViewModel()
                    {
                        IsNewSyncProfile = true,
                        EncryptionKey = RedflyEncryptionKeys.AesKey,
                        Database = new AddOrUpdateSyncedDatabaseViewModel()
                        {
                            EncryptedHostName = AppSession.Database!.EncryptedServerName,
                            EncryptedName = AppSession.Database!.EncryptedDatabaseName
                        },
                        RedisServer = new AddOrUpdateSyncedRedisServerViewModel()
                        {
                            EncryptedHostName = AppSession.RedisServer!.EncryptedServerName,
                            MaxAllowedConcurrentOperations = 256                            
                        },
                        SetupConfig = new AddOrUpdateSyncSetupConfigViewModel()
                        {
                            CtAndSnapshotIsolationEnabled = true,
                            CtAndSnapshotIsolationEnabledValidated = true,
                            CtEnabledOnTables = true,
                            RedisPort = AppSession.RedisServer!.Port,
                            TimestampColumnAdded = true,
                            TimestampColumnAddedValidated = true,
                            EncryptedClientDatabasePassword = AppSession.Database!.EncryptedPassword,
                            EncryptedClientDatabaseUserName = AppSession.Database!.EncryptedUserName,
                            EncryptedRedisPassword = AppSession.RedisServer!.EncryptedPassword,
                        }
                    }
                };

                AddOrUpdateSyncProfileResponse? addOrUpdateSyncProfileResponse = null;

                cts = new CancellationTokenSource();
                progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

                try
                {
                    addOrUpdateSyncProfileResponse = await syncApiClient.AddOrUpdateSyncProfileAsync(request, headers);
                }
                finally
                {
                    cts.Cancel();
                    await progressTask;
                }

                if (!addOrUpdateSyncProfileResponse.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(addOrUpdateSyncProfileResponse.Message);
                    Console.WriteLine("The Sync Profile could NOT be created. Please try again later");
                    Console.ResetColor();
                    return;
                }

                Console.WriteLine(addOrUpdateSyncProfileResponse.Message);
                Console.WriteLine("The Sync Profile was created successfully.");

                cts = new CancellationTokenSource();
                progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

                try
                {
                    getSyncProfilesResponse = await syncApiClient.GetSyncProfilesAsync(new GetSyncProfilesRequest() { PageNo = 1, PageSize = 10 }, headers);
                }
                finally
                {
                    cts.Cancel();
                    await progressTask;
                }

                syncProfile = (from p in getSyncProfilesResponse.Profiles
                               where p.Database.HostName == AppSession.Database!.DecryptedServerName &&
                                     p.Database.Name == AppSession.Database!.DecryptedDatabaseName &&
                                     p.RedisServer.HostName == AppSession.RedisServer!.DecryptedServerName
                               select p).FirstOrDefault();
            }

            AppSession.SyncProfile = syncProfile;
            Console.WriteLine("The Sync Profile was successfully retrieved from the server.");

            // Start Change Management
            await StartChakraSyncService(grpcUrl, grpcAuthToken);
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

    private static LiteSyncRelationshipDocument CreateSyncRelationship(LiteSyncRelationshipCollection syncRelationshipCollection)
    {
        LiteSyncRelationshipDocument syncRelationship = new()
        {
            SqlServerDatabaseId = AppSession.Database!.Id.ToString(),
            RedisServerId = AppSession.RedisServer!.Id.ToString()
        };
        syncRelationshipCollection.Add(syncRelationship);

        Console.WriteLine($"The local sync relationship with this Redis Server has been saved successfully");
        return syncRelationship;
    }

    private static bool SyncProfilesExist(GetSyncProfilesResponse getSyncProfilesResponse)
    {
        return (getSyncProfilesResponse.Success &&
                getSyncProfilesResponse.Profiles != null &&
                getSyncProfilesResponse.Profiles.Count > 0);
    }

    private static async Task<ServiceResponse> GetUserSetupData(UserSetupApi.UserSetupApiClient userSetupApiClient, Metadata headers)
    {
        var getUserSetupDataResponse = await userSetupApiClient
                                            .GetUserSetupDataAsync(new UserIdRequest
                                            {
                                                UserId = Guid.NewGuid().ToString()
                                            }, headers);
        return getUserSetupDataResponse;
    }

    private static bool UserAccountOrOrgSetupRequired(ServiceResponse getUserSetupDataResponse)
    {
        return (!getUserSetupDataResponse.Success ||
                getUserSetupDataResponse.Result == null ||
                getUserSetupDataResponse.Result.IsFreshNewUser ||
                string.IsNullOrEmpty(getUserSetupDataResponse.Result.UserFirstName) ||
                string.IsNullOrEmpty(getUserSetupDataResponse.Result.UserLastName) ||
                string.IsNullOrEmpty(getUserSetupDataResponse.Result.ClientName));
    }

    private static async Task<ServiceValueResponse> PromptUserToSetupUserAccountAndOrg(UserSetupApi.UserSetupApiClient userSetupApiClient, Metadata headers)
    {
        var viewModel = new AddClientAndUserProfileViewModel();

        //The user account and organization have to be setup first.
        Console.WriteLine("Please setup your User Account and Organization to proceed further.");

        do
        {
            Console.WriteLine("Please enter your First Name:");
            viewModel.UserFirstName = Console.ReadLine();
        }
        while (string.IsNullOrWhiteSpace(viewModel.UserFirstName));

        do
        {
            Console.WriteLine("Please enter your Last Name:");
            viewModel.UserLastName = Console.ReadLine();
        }
        while (string.IsNullOrWhiteSpace(viewModel.UserLastName));

        do
        {
            Console.WriteLine("Please enter your Organization Name:");
            viewModel.ClientName = Console.ReadLine();
        }
        while (string.IsNullOrWhiteSpace(viewModel.ClientName));

        var addOrUpdateClientAndUserProfileResponse = await userSetupApiClient.AddOrUpdateClientAndUserProfileAsync(new AddOrUpdateClientAndUserProfileRequest
        {
            Model = viewModel
        }, headers);
        return addOrUpdateClientAndUserProfileResponse;
    }

    private static async Task StartChakraSyncService(string grpcUrl, string grpcAuthToken)
    {
        var clientSessionId = Guid.NewGuid().ToString(); // Unique client identifier
        var channel = GrpcChannel.ForAddress(grpcUrl);
        var cmsClient = new NativeGrpcSqlServerChakraService.NativeGrpcSqlServerChakraServiceClient(channel);

        var headers = new Metadata
                {
                    { "Authorization", $"Bearer {grpcAuthToken}" },
                    { "client-session-id", clientSessionId.ToString() }
                };

        // Start Change Management
        var startResponse = await cmsClient
                                    .StartChakraSyncAsync(
                                        new StartChakraSyncRequest 
                                        { 
                                            ClientSessionId = clientSessionId,
                                            EncryptionKey = RedflyEncryptionKeys.AesKey,
                                            EncryptedClientId = RedflyEncryption.EncryptToString(AppSession.SyncProfile!.Database.ClientId),
                                            EncryptedClientName = RedflyEncryption.EncryptToString(AppSession.SyncProfile.ClientName),
                                            EncryptedDatabaseId = RedflyEncryption.EncryptToString(AppSession.SyncProfile.Database.Id),
                                            EncryptedDatabaseName = RedflyEncryption.EncryptToString(AppSession.SyncProfile.Database.Name),
                                            EncryptedDatabaseServerName = RedflyEncryption.EncryptToString(AppSession.SyncProfile.Database.HostName),
                                            EncryptedServerOnlyConnectionString = RedflyEncryption.EncryptToString($"Server=tcp:{AppSession.SyncProfile.Database.HostName},1433;Persist Security Info=False;User ID={AppSession.Database!.DecryptedUserName};Password={AppSession.Database.GetPassword()};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;application name=ArcApp;")
                                        }, 
                                        headers);

        if (startResponse.Success)
        {
            Console.WriteLine("Chakra Sync Service started successfully.");
        }
        else
        {
            Console.WriteLine("Failed to start the Chakra Sync Service.");
            return;
        }

        // Bi-directional streaming for communication with the server
        using var call = cmsClient.CommunicateWithClient(headers);

        var responseTask = Task.Run(async () =>
        {
            await foreach (var message in call.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine(FormatGrpcServerMessage(message.Message));
            }
        });

        // Send initial message to establish the stream
        await call.RequestStream.WriteAsync(new ClientMessage { ClientSessionId = clientSessionId, Message = "Client connected" });

        // Keep the client running to listen for server messages
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        // Complete the request stream
        await call.RequestStream.CompleteAsync();
        await responseTask;
    }

    private static string FormatGrpcServerMessage(string logMessage)
    {
        try
        {
            var regex = new Regex(@"Type: (?<Type>[^,]+), Data: \{ Operation = (?<Operation>[^}]+) \}");
            var match = regex.Match(logMessage);

            if (match.Success)
            {
                var type = match.Groups["Type"].Value;
                var operation = match.Groups["Operation"].Value;
                return $"{type}|{operation}";
            }

            return logMessage; // Return the original message if it doesn't match the expected format
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error formatting message: {ex}");

            return logMessage; // Return the original message if an exception occurs
        }
    }

}
