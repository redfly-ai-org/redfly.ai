using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Data.SqlClient;
using RedflyCoreFramework;
using RedflyLocalStorage;

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
                Console.WriteLine("Have you setup your User Account and Organization from our website? (y/n)");
                Console.WriteLine("https://transparent.azurewebsites.net/user-setup");
                var response = Console.ReadLine();

                if (response != null && 
                    response.Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    await StartChangeManagementService(grpcUrl, grpcAuthToken);
                }
                else
                {
                    Console.WriteLine("Change Management cannot be started without setting up your User Account and Organization here:");
                    Console.WriteLine("https://transparent.azurewebsites.net/user-setup");
                    Console.WriteLine("Please setup your User Account and Organization and try again.");
                }
            }
            else
            {
                Console.WriteLine("Change Management cannot be started without prepping the database.");
                Console.WriteLine("Please prep the database and try again.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Console.WriteLine("Contact us at developer@redfly.ai if you need to.");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        RedflyLocalDatabase.Dispose();
    }

    private static async Task StartChangeManagementService(string grpcUrl, string grpcAuthToken)
    {
        var clientSessionId = Guid.NewGuid().ToString(); // Unique client identifier
        var channel = GrpcChannel.ForAddress(grpcUrl);
        var client = new GrpcChangeManagement.GrpcChangeManagementClient(channel);

        var headers = new Metadata
                {
                    { "Authorization", $"Bearer {grpcAuthToken}" },
                    { "client-session-id", clientSessionId.ToString() }
                };

        // Start Change Management
        var startResponse = await client.StartChangeManagementAsync(new StartChangeManagementRequest { ClientSessionId = clientSessionId }, headers);

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
        using var call = client.CommunicateWithClient(headers);

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
