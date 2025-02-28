using RedflyPerformanceTest.Entities;
using RedflyPerformanceTest.GrpcClient;
using System;

namespace RedflyPerformanceTest
{
    internal class Program
    {
        internal static async Task Main(string[] args)
        {
            try
            {
                Console.Title = "redfly.ai - Performance Test Console";

                Console.WriteLine("This console app is intended to demonstrate the benefits of using backend services built with redfly.ai tech over conventional data access techniques.\r\n");

                Console.WriteLine("1. We natively sync ANY database schema with Redis in the background.");
                Console.WriteLine("2. We can generate the backend code for any database with Redis caching built-in.");
                Console.WriteLine("3. This is hosted over Grpc.");

                Console.WriteLine("\r\nNow imagine you being able to do that with your database, without any manual effort! That's what our product does.");
                Console.WriteLine("Contact us at developer@redfly.ai to directly work with us so you can do the same thing with your database (cloud/ on-premises).");
                Console.WriteLine("No matter how large or complex your DB is, redfly.ai can do it! \r\n");

                Console.WriteLine("Press any key to start the performance test...");
                Console.ReadKey();

                var grpcUrl = "https://advworks-grpc-linux.azurewebsites.net";

                var grpcAuthToken = await AuthGrpcClient.RunAsync(grpcUrl);

                if (grpcAuthToken == null ||
                    grpcAuthToken.Length == 0)
                {
                    Console.WriteLine("Failed to authenticate with the gRPC server.");
                    Console.WriteLine("Contact us at developer@redfly.ai if you need to.");
                    return;
                }

                //Increase to run count to see better performance with redfly over SQL.
                int totalRuns = 6000;

                var testResults = new PerfTestResults();
                await ProductModelsGrpcClient.RunAsync(grpcUrl, grpcAuthToken, testResults, totalRuns);

                if (testResults.Populated())
                {
                    Console.WriteLine("==============================================================");
                    Console.WriteLine($"RUNS: {totalRuns}\r\n");
                    
                    Console.WriteLine($"   SQL over Grpc (ms): {testResults.SqlOverGrpcTimings.Min():F2} (MIN) < {testResults.SqlOverGrpcTimings.Average():F2} (AVG) < {testResults.SqlOverGrpcTimings.Max():F2} (MAX)", ConsoleColor.Magenta);
                    Console.WriteLine($"redfly over Grpc (ms): {testResults.RedflyOverGrpcTimings.Min():F2} (MIN) < {testResults.RedflyOverGrpcTimings.Average():F2} (AVG) < {testResults.RedflyOverGrpcTimings.Max():F2} (MAX)", ConsoleColor.Cyan);

                    Console.WriteLine("");

                    if (testResults.SqlOverGrpcTimings.Min() > testResults.RedflyOverGrpcTimings.Min())
                    {
                        Console.WriteLine($"At the minimum, redfly.ai is {testResults.SqlOverGrpcTimings.Min() / testResults.RedflyOverGrpcTimings.Min():F2}x faster");
                    }

                    if (testResults.SqlOverGrpcTimings.Average() > testResults.RedflyOverGrpcTimings.Average())
                    {
                        Console.WriteLine($"On average, redfly.ai is {testResults.SqlOverGrpcTimings.Average() / testResults.RedflyOverGrpcTimings.Average():F2}x faster");
                    }

                    if (testResults.SqlOverGrpcTimings.Max() > testResults.RedflyOverGrpcTimings.Max())
                    {
                        Console.WriteLine($"In the worst case, redfly.ai is {testResults.SqlOverGrpcTimings.Max() / testResults.RedflyOverGrpcTimings.Max():F2}x faster");
                    }
                    
                    Console.WriteLine("--------------------------------------------------------------");
                }
                else
                {
                    Console.WriteLine("Test results are incomplete.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Contact us at developer@redfly.ai if you need to.");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
