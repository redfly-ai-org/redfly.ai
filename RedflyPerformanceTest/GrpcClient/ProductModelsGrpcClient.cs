using Grpc.Net.Client;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using RedflyPerformanceTest.Entities;
using System.Security.Authentication;
using Microsoft.Extensions.Logging;
using ClientDatabaseAPI.Common.Library;
using System.Net;

namespace RedflyPerformanceTest.GrpcClient
{
    internal static class ProductModelsGrpcClient
    {
        
        public static async Task RunAsync(string grpcUrl, string token, PerfTestResults testResults, int totalRuns)
        {
            try
            {
                Console.WriteLine("Starting the gRPC client test for ProductModels");

                var httpHandler = new HttpClientHandler();
                httpHandler.SslProtocols = SslProtocols.Tls12;

                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    _ = builder.AddConsole();
                    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);
                });

                using var channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    LoggerFactory = loggerFactory,
                    HttpVersion = new Version(2, 0) // Ensure HTTP/2 is used
                });

                var productModelsClient = new ProductModelsService.ProductModelsServiceClient(channel);

                Console.WriteLine($"Connecting to gRPC server at {grpcUrl}...");

                await TestGetRowCount(productModelsClient, token);
                var apiProductModel = await TestInsert(productModelsClient, token);
                await TestUpdate(productModelsClient, token, apiProductModel);

                if (apiProductModel != null)
                {
                    // Server will only let you delete rows that did not exist in the database originally 
                    // (i.e., someone else like you created them).
                    await TestDelete(productModelsClient, token, apiProductModel!.ProductModelId);
                }

                Console.WriteLine("");

                for (int i = 0; i < totalRuns; i++)
                {
                    await TestGetSingle(productModelsClient, token, testResults, i, totalRuns);
                    await TestGetMany(productModelsClient, token, testResults, i, totalRuns);
                }

                Console.WriteLine("\r\n\r\nTest Completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task TestGetRowCount(ProductModelsService.ProductModelsServiceClient client, string token)
        {
            try
            {
                var headers = new Metadata
                {
                    { "Authorization", $"Bearer {token}" }
                };

                var request = new GetRowCountRequest { ExecutionMode = (int)ReadExecutionMode.Balanced };

                Console.WriteLine("    Executing GetRowCount Request with JWT Token...");
                var response = await client.GetRowCountAsync(request, headers);
                Console.WriteLine($"    RowCount: {response.Result}, Message: {response.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task<ApiProductModel?> TestInsert(ProductModelsService.ProductModelsServiceClient client, string token)
        {
            try
            {
                var headers = new Metadata
                {
                    { "Authorization", $"Bearer {token}" }
                };

                var request = new InsertRequest
                {
                    Inserted = new ApiProductModel
                    {
                        Name = $"Name:{Dns.GetHostName()} {Guid.NewGuid()}".Substring(0, 50),
                        CatalogDescription = "",
                        Rowguid = Guid.NewGuid().ToString(),
                        ModifiedDate = DateTime.UtcNow.ToString("o")
                    }
                };

                Console.WriteLine("    Executing Insert Request with JWT Token...");
                var restWatch = new Stopwatch();
                restWatch.Start();
                var response = await client.InsertAsync(request, headers);
                restWatch.Stop();
                Console.WriteLine($"    Inserted ProductModelId: {response.Result.ProductModelId}, Message: {response.Message}");

                //Console.WriteLine($"Grpc RunTime: {restWatch.Elapsed.TotalMilliseconds}ms, Result: {response?.Result.ProductModelId}");

                return response?.Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        private static void DisplayProgress(int index, int total, int updateInterval = 2)
        {
            if (index % updateInterval == 0 || index == total - 1)
            {
                int progressWidth = 50; // Width of the progress bar
                double percentage = (double)index / total;
                int progress = (int)(percentage * progressWidth);

                Console.Write("\r[");
                Console.Write(new string('#', progress));
                Console.Write(new string(' ', progressWidth - progress));
                Console.Write($"] {index}/{total} ({percentage:P0})");
            }
        }

        private static async Task TestGetSingle(
            ProductModelsService.ProductModelsServiceClient client, 
            string token, 
            PerfTestResults testResults,                              
            int index, 
            int total)
        {
            try
            {
                DisplayProgress(index, total);

                var headers = new Metadata
                {
                    { "Authorization", $"Bearer {token}" }
                };

                var request = new GetSingleRequest
                {
                    ProductModelId = 1, // Replace with a valid ProductModelId
                    UseCache = false,
                    ExecutionMode = (int)ReadExecutionMode.Balanced
                };

                await GetSingleWithSql(client, testResults, headers, request);

                request = new GetSingleRequest
                {
                    ProductModelId = 1, // Replace with a valid ProductModelId
                    UseCache = true,
                    ExecutionMode = (int)ReadExecutionMode.Balanced
                };
                await GetSingleWithRedfly(client, testResults, headers, request);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task GetSingleWithRedfly(ProductModelsService.ProductModelsServiceClient client, PerfTestResults testResults, Metadata headers, GetSingleRequest request)
        {
            //Console.Write($"\r Executing GetSingle Request with JWT Token...");
            var restWatch = new Stopwatch();
            restWatch.Start();
            var response = await client.GetSingleAsync(request, headers);
            restWatch.Stop();
            testResults.RedflyOverGrpcTimings.Add(restWatch.Elapsed.TotalMilliseconds);
            //Console.Write($"\r {index}/{total}|redfly|  ProductModel: {response.Result?.Name ?? ""}, Message: {response.Message}");
        }

        private static async Task GetSingleWithSql(ProductModelsService.ProductModelsServiceClient client, PerfTestResults testResults, Metadata headers, GetSingleRequest request)
        {
            //Console.Write($"\r Executing GetSingle Request with JWT Token...");
            var restWatch = new Stopwatch();
            restWatch.Start();
            var response = await client.GetSingleAsync(request, headers);
            restWatch.Stop();
            testResults.SqlOverGrpcTimings.Add(restWatch.Elapsed.TotalMilliseconds);
            //Console.Write($"\r {index}/{total}|SQL| ProductModel: {response.Result?.Name ?? ""}, Message: {response.Message}");
        }

        private static async Task TestUpdate(ProductModelsService.ProductModelsServiceClient client, string token, ApiProductModel? apiProductModel)
        {
            try
            {
                var headers = new Metadata
                {
                    { "Authorization", $"Bearer {token}" }
                };

                var request = new UpdateRequest
                {
                    Updated = new ApiProductModel
                    {
                        ProductModelId = 1,
                        Name = $"Name:{Dns.GetHostName()} {Guid.NewGuid()}".Substring(0, 50),
                        CatalogDescription = "",
                        Rowguid = Guid.NewGuid().ToString(),
                        ModifiedDate = DateTime.UtcNow.ToString("o")
                    }
                };

                Console.WriteLine("    Executing Update Request with JWT Token...");
                var restWatch = new Stopwatch();
                restWatch.Start();
                var response = await client.UpdateAsync(request, headers);
                restWatch.Stop();
                Console.WriteLine($"    Updated Rows: {response.Result}, Message: {response.Message}");

                //Console.WriteLine($"Grpc RunTime: {restWatch.Elapsed.TotalMilliseconds}ms, Result: {response.Result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task TestDelete(ProductModelsService.ProductModelsServiceClient client, string token, System.Int32 productModelId)
        {
            try
            {
                var headers = new Metadata
                {
                    { "Authorization", $"Bearer {token}" }
                };

                var request = new DeleteRequest
                {
                    ProductModelId = productModelId
                };

                Console.WriteLine("    Executing Delete Request with JWT Token...");
                var restWatch = new Stopwatch();
                restWatch.Start();
                var response = await client.DeleteAsync(request, headers);
                restWatch.Stop();
                Console.WriteLine($"    Delete Success: {response.Success}, Message: {response.Message}");

                //Console.WriteLine($"Grpc RunTime: {restWatch.Elapsed.TotalMilliseconds} ms, Result: {response.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task TestGetMany(
            ProductModelsService.ProductModelsServiceClient client, 
            string token, 
            PerfTestResults testResults,                              
            int index, 
            int total)
        {
            try
            {
                DisplayProgress(index, total);

                var headers = new Metadata
                {
                    { "Authorization", $"Bearer {token}" }
                };

                var request = new GetManyRequest
                {
                    PageNo = 1,
                    PageSize = 10,
                    UseCache = false,
                    ExecutionMode = (int)ReadExecutionMode.Balanced
                };

                await GetManyWithSql(client, testResults, headers, request);

                request = new GetManyRequest
                {
                    PageNo = 1,
                    PageSize = 10,
                    UseCache = true,
                    ExecutionMode = (int)ReadExecutionMode.Balanced
                };

                await GetManyWithRedfly(client, testResults, headers, request);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task GetManyWithRedfly(ProductModelsService.ProductModelsServiceClient client, PerfTestResults testResults, Metadata headers, GetManyRequest request)
        {
            //Console.Write($"\r Executing GetMany Request with JWT Token...");
            var restWatch = new Stopwatch();
            restWatch.Start();
            var response = await client.GetManyAsync(request, headers);
            restWatch.Stop();
            testResults.RedflyOverGrpcTimings.Add(restWatch.Elapsed.TotalMilliseconds);
            //Console.Write($"\r {index}/{total}|redfly| Total Products: {response.Results.Count}, Message: {response.Message}");
        }

        private static async Task GetManyWithSql(ProductModelsService.ProductModelsServiceClient client, PerfTestResults testResults, Metadata headers, GetManyRequest request)
        {
            //Console.Write($"\r Executing GetMany Request with JWT Token...");
            var restWatch = new Stopwatch();
            restWatch.Start();
            var response = await client.GetManyAsync(request, headers);
            restWatch.Stop();
            testResults.SqlOverGrpcTimings.Add(restWatch.Elapsed.TotalMilliseconds);
            //Console.Write($"\r {index}/{total}|SQL| Total Products: {response.Results.Count}, Message: {response.Message}");
        }
    }
}
