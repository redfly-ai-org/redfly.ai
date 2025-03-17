using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using LiteDB;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using RedflyCoreFramework;
using RedflyLocalStorage;
using RedflyLocalStorage.Collections;
using RedflyLocalStorage.Entities;
using System.Threading.Channels;

namespace RedflyDatabaseSyncProxy;

internal class Program
{
    internal static async Task Main(string[] args)
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

            Console.WriteLine("Press any key to start the process of synchronizing your database with Redis transparently...");
            Console.ReadKey();

            //TODO: This will change.
            var grpcUrl = "https://localhost:7176";

            var grpcAuthToken = await RedflyGrpcAuthServiceClient.AuthGrpcClient.RunAsync(grpcUrl);

            if (grpcAuthToken == null ||
                grpcAuthToken.Length == 0)
            {
                Console.WriteLine("Failed to authenticate with the gRPC server.");
                Console.WriteLine("Contact us at developer@redfly.ai if you need to.");
                return;
            }

            if (SqlServerDatabasePrep.ForChangeManagement())
            {
                var redisServerCollection = new LiteRedisServerCollection();
                var syncRelationshipCollection = new LiteSyncRelationshipCollection();

                var syncRelationship = syncRelationshipCollection
                                            .FindByDatabase(SqlServerDatabasePicker.SelectedDatabase.Id.ToString()).FirstOrDefault();

                if (syncRelationship != null)
                {
                    RedisServerPicker.SelectedRedisServer = redisServerCollection
                                                                .FindById(new BsonValue(new ObjectId(syncRelationship.RedisServerId)));

                    Console.WriteLine($"This database has an existing sync relationship with {RedisServerPicker.SelectedRedisServer.DecryptedServerName}:{RedisServerPicker.SelectedRedisServer.Port}");
                }
                else
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

                    syncRelationship = new LiteSyncRelationshipDocument
                    {
                        SqlServerDatabaseId = SqlServerDatabasePicker.SelectedDatabase.Id.ToString(),
                        RedisServerId = RedisServerPicker.SelectedRedisServer!.Id.ToString()
                    };

                    syncRelationshipCollection.Add(syncRelationship);

                    Console.WriteLine($"The local sync relationship with this Redis Server has been saved successfully");
                }

                var channel = GrpcChannel.ForAddress(grpcUrl);
                var headers = new Metadata
                {
                    { "Authorization", $"Bearer {grpcAuthToken}" }
                };

                var userSetupApiClient = new UserSetupApi.UserSetupApiClient(channel);

                var getUserSetupDataResponse = await GetUserSetupData(userSetupApiClient, headers);

                if (UserAccountOrOrgSetupRequired(getUserSetupDataResponse))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(getUserSetupDataResponse.Message);
                    Console.ResetColor();

                    var addOrUpdateClientAndUserProfileResponse = await PromptUserToSetupUserAccountAndOrg(userSetupApiClient, headers);

                    if (addOrUpdateClientAndUserProfileResponse.Success)
                    {
                        Console.WriteLine(addOrUpdateClientAndUserProfileResponse.Message);
                        Console.WriteLine("User Account and Organization setup completed successfully.");

                        // TODO: Setup sync relationship on the server IF not exists already

                        // Start Change Management
                        await StartChangeManagementService(grpcUrl, grpcAuthToken);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(addOrUpdateClientAndUserProfileResponse.Message);
                        Console.WriteLine("User Account and Organization setup could NOT be completed successfully. Please try again later");
                        Console.ResetColor();
                    }
                }
                else
                {
                    // TODO: Setup sync relationship on the server IF not exists already

                    // Start Change Management
                    await StartChangeManagementService(grpcUrl, grpcAuthToken);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Change Management cannot be started without prepping the database.");
                Console.WriteLine("Please prep the database and try again.");
                Console.ResetColor();
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
        return !getUserSetupDataResponse.Success ||
                            getUserSetupDataResponse.Result == null ||
                            getUserSetupDataResponse.Result.IsFreshNewUser ||
                            string.IsNullOrEmpty(getUserSetupDataResponse.Result.UserFirstName) ||
                            string.IsNullOrEmpty(getUserSetupDataResponse.Result.UserLastName) ||
                            string.IsNullOrEmpty(getUserSetupDataResponse.Result.ClientName);
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

    private static async Task StartChangeManagementService(string grpcUrl, string grpcAuthToken)
    {
        var clientSessionId = Guid.NewGuid().ToString(); // Unique client identifier
        var channel = GrpcChannel.ForAddress(grpcUrl);
        var cmsClient = new GrpcChangeManagement.GrpcChangeManagementClient(channel);

        var headers = new Metadata
                {
                    { "Authorization", $"Bearer {grpcAuthToken}" },
                    { "client-session-id", clientSessionId.ToString() }
                };

        // Start Change Management
        var startResponse = await cmsClient.StartChangeManagementAsync(new StartChangeManagementRequest { ClientSessionId = clientSessionId }, headers);

        if (startResponse.Success)
        {
            Console.WriteLine("Change management service started successfully.");
        }
        else
        {
            Console.WriteLine("Failed to start change management service.");
            return;
        }

        // Bi-directional streaming for communication with the server
        using var call = cmsClient.CommunicateWithClient(headers);

        var responseTask = Task.Run(async () =>
        {
            await foreach (var message in call.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine($"Server: {message.Message}");
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

}
