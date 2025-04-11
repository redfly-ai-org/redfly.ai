using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using LiteDB;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using RedflyCoreFramework;
using RedflyDatabaseSyncProxy.Setup;
using RedflyDatabaseSyncProxy.SyncProfiles;
using RedflyDatabaseSyncProxy.SyncRelationships;
using RedflyDatabaseSyncProxy.SyncServices;
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

            Console.ForegroundColor = ConsoleColor.Green;
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

            bool isPostgresSync = false;

            Console.WriteLine("Are you trying to sync a Postgres database? (y/n)");
            response = Console.ReadLine();

            if (response != null &&
                response.Equals("y", StringComparison.CurrentCultureIgnoreCase))
            {
                isPostgresSync = true;

                if (!PostgresReady.ForChakraSync())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Chakra Sync cannot be started without prepping the Postgres database.");
                    Console.WriteLine("Please prep the Postgres database and try again.");
                    Console.ResetColor();

                    return;
                }
            }

            bool isSqlServerSync = false;

            if (!isPostgresSync)
            {
                Console.WriteLine("Are you trying to sync a Sql Server database? (y/n)");
                response = Console.ReadLine();

                if (response != null &&
                    response.Equals("y", StringComparison.CurrentCultureIgnoreCase))
                {
                    isSqlServerSync = true;

                    if (!SqlServerReady.ForChakraSync())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Chakra Sync cannot be started without prepping the Sql Server database.");
                        Console.WriteLine("Please prep the Sql Server database and try again.");
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
            else if (isPostgresSync)
            {
                PostgresSyncRelationship.FindExistingRelationshipWithRedis(redisServerCollection);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("We only support Sql Server and Postgres at present.");
                Console.ResetColor();

                return;
            }

            var channel = GrpcChannel.ForAddress(grpcUrl);
            var headers = new Metadata
                {
                    { "Authorization", $"Bearer {grpcAuthToken}" }
                };
            
            if (!await RedflyUserOrOrg.Setup(channel, headers)) { return; }

            if (isSqlServerSync)
            {
                var syncApiClient = new SyncApiService.SyncApiServiceClient(channel);
                GetSyncProfilesResponse? getSyncProfilesResponse = null;

                var cts = new CancellationTokenSource();
                var progressTask = RedflyConsole.ShowWaitAnimation(cts.Token);

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

                if (SqlServerSyncProfile.Exists(getSyncProfilesResponse))
                {
                    syncProfile = (from p in getSyncProfilesResponse.Profiles
                                   where p.Database.HostName == AppSession.SqlServerDatabase!.DecryptedServerName &&
                                         p.Database.Name == AppSession.SqlServerDatabase!.DecryptedDatabaseName &&
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
                                EncryptedHostName = AppSession.SqlServerDatabase!.EncryptedServerName,
                                EncryptedName = AppSession.SqlServerDatabase!.EncryptedDatabaseName
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
                                EncryptedClientDatabasePassword = AppSession.SqlServerDatabase!.EncryptedPassword,
                                EncryptedClientDatabaseUserName = AppSession.SqlServerDatabase!.EncryptedUserName,
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
                                   where p.Database.HostName == AppSession.SqlServerDatabase!.DecryptedServerName &&
                                         p.Database.Name == AppSession.SqlServerDatabase!.DecryptedDatabaseName &&
                                         p.RedisServer.HostName == AppSession.RedisServer!.DecryptedServerName
                                   select p).FirstOrDefault();
                }

                AppSession.SyncProfile = syncProfile;
                Console.WriteLine("The Sync Profile was successfully retrieved from the server.");

                // Start Chakra Sync
                await ChakraSqlServerSyncServiceClient.StartAsync(grpcUrl, grpcAuthToken);
            }
            else if (isPostgresSync)
            {
                Console.WriteLine("Do you want to do an initial sync? (y/n)");
                Console.WriteLine("This only makes sense if you are syncing the database for the first time and Redis is empty");
                response = Console.ReadLine();

                var runInitialSync = (response != null &&
                                      response.Equals("y", StringComparison.CurrentCultureIgnoreCase));

                await ChakraPostgresSyncServiceClient.StartAsync(grpcUrl, grpcAuthToken, runInitialSync);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("We only support Sql Server and Postgres at present.");
                Console.ResetColor();

                return;
            }
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

}
