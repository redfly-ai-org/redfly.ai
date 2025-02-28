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

                Console.WriteLine($"Starting the Test from {grpcUrl}...");

                await TestGetRowCount(productModelsClient, token);
                
                Console.WriteLine("");

                int pageNo = 1;
                int pageSize = 10;
                int runCount = 0;
                GetManyResponse? response = null;
                var validResponses = new List<GetManyResponse?>();

                do
                {

                    response = await TestGetMany(productModelsClient, token, testResults, runCount, totalRuns, pageNo, pageSize);
                    runCount++;

                    if (response != null &&
                       response.Results != null &&
                       response.Results.Count > 0)
                    {
                        validResponses.Add(response);
                    }
                }
                while (runCount <= totalRuns &&
                       response != null &&
                       response.Results != null &&
                       response.Results.Count > 0);

                var remainingRunCount = (totalRuns - runCount);

                if (remainingRunCount > 0)
                { 
                    foreach (var validResponse in validResponses)
                    {
                        foreach (var result in validResponse!.Results)
                        {
                            await TestGetSingle(productModelsClient, token, testResults, runCount, totalRuns, result.ProductModelId);

                            runCount++;

                            if (runCount >= totalRuns)
                            {
                                //Satisfied!
                                break;
                            }
                        }
                    }
                }

                remainingRunCount = (totalRuns - runCount);

                if (remainingRunCount > 0)
                {
                    while (runCount < remainingRunCount)
                    {
                        var inserted = await TestInsertRow(productModelsClient, token);

                        if (inserted != null)
                        {
                            await TestUpdateRow(productModelsClient, token, inserted);

                            await TestGetSingle(productModelsClient, token, testResults, runCount, totalRuns, inserted.ProductModelId);

                            // Server will only let you delete rows that did not exist in the database originally 
                            // (i.e., someone else like you created them).
                            await TestDelete(productModelsClient, token, inserted!.ProductModelId);

                            runCount++;
                        }
                    }
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

        private static async Task<ApiProductModel?> TestInsertRow(ProductModelsService.ProductModelsServiceClient client, string token)
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

                //Console.WriteLine("    Executing Insert Request with JWT Token...");
                var restWatch = new Stopwatch();
                restWatch.Start();
                var response = await client.InsertAsync(request, headers);
                restWatch.Stop();
                //Console.WriteLine($"    Inserted ProductModelId: {response.Result.ProductModelId}, Message: {response.Message}");

                //Console.WriteLine($"Grpc RunTime: {restWatch.Elapsed.TotalMilliseconds}ms, Result: {response?.Result.ProductModelId}");

                return response?.Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
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

        private static bool getSingleCallSqlFirst = true;

        private static async Task TestGetSingle(
            ProductModelsService.ProductModelsServiceClient client, 
            string token, 
            PerfTestResults testResults,                              
            int index, 
            int total, 
            int productModelId)
        {
            try
            {
                DisplayProgress(index, total);

                var headers = new Metadata
                {
                    { "Authorization", $"Bearer {token}" }
                };

                if (getSingleCallSqlFirst)
                {
                    await GetSingleWithSql(client, testResults, headers, productModelId);
                    await GetSingleWithRedfly(client, testResults, headers, productModelId);
                    getSingleCallSqlFirst = false;
                }
                else
                {
                    await GetSingleWithRedfly(client, testResults, headers, productModelId);
                    await GetSingleWithSql(client, testResults, headers, productModelId);
                    getSingleCallSqlFirst = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task GetSingleWithRedfly(ProductModelsService.ProductModelsServiceClient client, PerfTestResults testResults, Metadata headers, int productModelId)
        {
            var request = new GetSingleRequest
            {
                ProductModelId = productModelId, // Replace with a valid ProductModelId
                UseCache = true,
                ExecutionMode = (int)ReadExecutionMode.Balanced
            };

            //Console.Write($"\r Executing GetSingle Request with JWT Token...");
            var restWatch = new Stopwatch();
            restWatch.Start();
            var response = await client.GetSingleAsync(request, headers);
            restWatch.Stop();
            testResults.RedflyOverGrpcTimings.Add(restWatch.Elapsed.TotalMilliseconds);
            //Console.Write($"\r {index}/{total}|redfly|  ProductModel: {response.Result?.Name ?? ""}, Message: {response.Message}");
        }

        private static async Task GetSingleWithSql(ProductModelsService.ProductModelsServiceClient client, PerfTestResults testResults, Metadata headers, int productModelId)
        {
            var request = new GetSingleRequest
            {
                ProductModelId = productModelId, // Replace with a valid ProductModelId
                UseCache = false,
                ExecutionMode = (int)ReadExecutionMode.Balanced
            };

            //Console.Write($"\r Executing GetSingle Request with JWT Token...");
            var restWatch = new Stopwatch();
            restWatch.Start();
            var response = await client.GetSingleAsync(request, headers);
            restWatch.Stop();
            testResults.SqlOverGrpcTimings.Add(restWatch.Elapsed.TotalMilliseconds);
            //Console.Write($"\r {index}/{total}|SQL| ProductModel: {response.Result?.Name ?? ""}, Message: {response.Message}");
        }

        private static async Task TestUpdateRow(ProductModelsService.ProductModelsServiceClient client, string token, ApiProductModel row)
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
                        ProductModelId = row.ProductModelId,
                        Name = $"Name:{Dns.GetHostName()} {Guid.NewGuid()}".Substring(0, 50),
                        ModifiedDate = DateTime.UtcNow.ToString("o")
                    }
                };

                //Console.WriteLine("    Executing Update Request with JWT Token...");
                var restWatch = new Stopwatch();
                restWatch.Start();
                var response = await client.UpdateAsync(request, headers);
                restWatch.Stop();
                //Console.WriteLine($"    Updated Rows: {response.Result}, Message: {response.Message}");

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

                //Console.WriteLine("    Executing Delete Request with JWT Token...");
                var restWatch = new Stopwatch();
                restWatch.Start();
                var response = await client.DeleteAsync(request, headers);
                restWatch.Stop();
                //Console.WriteLine($"    Delete Success: {response.Success}, Message: {response.Message}");

                //Console.WriteLine($"Grpc RunTime: {restWatch.Elapsed.TotalMilliseconds} ms, Result: {response.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static bool getManyCallSqlFirst = true;

        private static async Task<GetManyResponse?> TestGetMany(
            ProductModelsService.ProductModelsServiceClient client, 
            string token, 
            PerfTestResults testResults,                              
            int index, 
            int total,
            int pageNo, 
            int pageSize)
        {
            try
            {
                DisplayProgress(index, total);

                var headers = new Metadata
                {
                    { "Authorization", $"Bearer {token}" }
                };

                GetManyResponse? response = null;

                if (getManyCallSqlFirst)
                {
                    await GetManyWithSql(client, testResults, headers, pageNo, pageSize);
                    response = await GetManyWithRedfly(client, testResults, headers, pageNo, pageSize);
                    getManyCallSqlFirst = false;
                }
                else
                {
                    await GetManyWithRedfly(client, testResults, headers, pageNo, pageSize);
                    response = await GetManyWithSql(client, testResults, headers, pageNo, pageSize);
                    getManyCallSqlFirst = true;
                }

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        private static async Task<GetManyResponse?> GetManyWithRedfly(ProductModelsService.ProductModelsServiceClient client, PerfTestResults testResults, Metadata headers, int pageNo, int pageSize)
        {
            var request = new GetManyRequest
            {
                PageNo = pageNo,
                PageSize = pageSize,
                UseCache = true,
                ExecutionMode = (int)ReadExecutionMode.Balanced
            };

            //Console.Write($"\r Executing GetMany Request with JWT Token...");
            var restWatch = new Stopwatch();
            restWatch.Start();
            var response = await client.GetManyAsync(request, headers);
            restWatch.Stop();
            testResults.RedflyOverGrpcTimings.Add(restWatch.Elapsed.TotalMilliseconds);
            //Console.Write($"\r {index}/{total}|redfly| Total Products: {response.Results.Count}, Message: {response.Message}");

            return response;
        }

        private static async Task<GetManyResponse?> GetManyWithSql(ProductModelsService.ProductModelsServiceClient client, PerfTestResults testResults, Metadata headers, int pageNo, int pageSize)
        {
            var request = new GetManyRequest
            {
                PageNo = pageNo,
                PageSize = pageSize,
                UseCache = false,
                ExecutionMode = (int)ReadExecutionMode.Balanced
            };

            //Console.Write($"\r Executing GetMany Request with JWT Token...");
            var restWatch = new Stopwatch();
            restWatch.Start();
            var response = await client.GetManyAsync(request, headers);
            restWatch.Stop();
            testResults.SqlOverGrpcTimings.Add(restWatch.Elapsed.TotalMilliseconds);
            //Console.Write($"\r {index}/{total}|SQL| Total Products: {response.Results.Count}, Message: {response.Message}");

            return response;
        }
    }
}
