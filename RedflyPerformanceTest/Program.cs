using RedflyPerformanceTest.Entities;
using RedflyPerformanceTest.GrpcClient;
using System;

namespace RedflyPerformanceTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.Title = "redfly.ai - Performance Test Console";

                Console.WriteLine("Press any key to start the performance test...");
                Console.ReadKey();

                var grpcUrl = "https://advworks-grpc-linux.azurewebsites.net";

                var grpcAuthToken = await AuthGrpcClient.RunAsync(grpcUrl);

                //Increase to run count to see better performance with redfly over SQL.
                int totalRuns = 500;

                var testResults = new PerfTestResults();
                await ProductModelsGrpcClient.RunAsync(grpcUrl, grpcAuthToken, testResults, totalRuns);

                if (testResults.HasAnyResults())
                {
                    Console.WriteLine("==============================================================");
                    Console.WriteLine($"RUNS: {totalRuns}");
                    
                    Console.WriteLine($"   SQL over Grpc (ms): {testResults.SqlOverGrpcTimings.Min()} < {testResults.SqlOverGrpcTimings.Average()} < {testResults.SqlOverGrpcTimings.Max()}", ConsoleColor.Magenta);
                    Console.WriteLine($"redfly over Grpc (ms): {testResults.RedflyOverGrpcTimings.Min()} < {testResults.RedflyOverGrpcTimings.Average()} < {testResults.RedflyOverGrpcTimings.Max()}", ConsoleColor.Cyan);
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
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
