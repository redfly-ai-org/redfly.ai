using Grpc.Core;
using Grpc.Net.Client;

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

            Console.WriteLine("Press any key to start the performance test...");
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

            await StartChangeManagementService(grpcUrl, grpcAuthToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Console.WriteLine("Contact us at developer@redfly.ai if you need to.");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static async Task StartChangeManagementService(string grpcUrl, string grpcAuthToken)
    {
        var clientId = Guid.NewGuid().ToString(); // Unique client identifier
        var channel = GrpcChannel.ForAddress(grpcUrl);
        var client = new GrpcChangeManagement.GrpcChangeManagementClient(channel);

        var headers = new Metadata
                {
                    { "Authorization", $"Bearer {grpcAuthToken}" }
                };

        // Start Change Management
        var startResponse = await client.StartChangeManagementAsync(new StartChangeManagementRequest { ClientId = clientId }, headers);

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
        await call.RequestStream.WriteAsync(new ClientMessage { ClientId = clientId, Message = "Client connected" });

        // Keep the client running to listen for server messages
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        // Complete the request stream
        await call.RequestStream.CompleteAsync();
        await responseTask;
    }

}
