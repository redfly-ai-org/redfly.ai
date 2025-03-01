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
using System.Reflection;

namespace RedflyPerformanceTest.GrpcClient
{
    internal static class ProductModelsGrpcClient
    {

        internal static PerfTestResults TestResults { get; private set; } = new PerfTestResults();

        public static async Task RunAsync(string grpcUrl, string token, int totalRuns)
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

                var rowCountResponse = await TestGetRowCount(productModelsClient, token);
                var actualDbRowCount = rowCountResponse?.Result ?? 0;

                Console.WriteLine("");

                int pageSize = 50;
                int runCount = 0;

                var noOfPagedCalls = (int)Math.Ceiling((double)actualDbRowCount / pageSize);

                var getManyTasks = new List<Task<GetManyResponse?>>();

                Console.WriteLine($"{totalRuns} runs remaining. Running {noOfPagedCalls} GetMany tests asynchronously.");

                for (int pageNo=1;pageNo<noOfPagedCalls;pageNo++)
                {
                    getManyTasks.Add(TestGetMany(productModelsClient, token, runCount, totalRuns, pageNo, pageSize));

                    runCount++;
                    
                    if (runCount >= totalRuns)
                    {
                        //Satisfied!
                        break;
                    }
                }

                var responses = await Task.WhenAll(getManyTasks);
                var validResponses = new List<GetManyResponse?>();

                foreach (var response in responses)
                {
                    if (response != null &&
                        response.Results != null &&
                        response.Results.Count > 0)
                    {
                        validResponses.Add(response);
                    }
                }

                getManyTasks.Clear();
                var tasks = new List<Task>();
                var remainingRunCount = (totalRuns - runCount);

                if (remainingRunCount > 0)
                {
                    DisplayMessageDuringProgress($"{remainingRunCount} runs remaining. Running ~{validResponses.Count* pageSize} GetSingle tests asynchronously.");

                    foreach (var validResponse in validResponses)
                    {
                        foreach (var result in validResponse!.Results)
                        {
                            tasks.Add(TestGetSingle(productModelsClient, token, runCount, totalRuns, result.ProductModelId));

                            runCount++;

                            if (runCount >= totalRuns)
                            {
                                //Satisfied!
                                break;
                            }
                        }
                    }

                    await Task.WhenAll(tasks);
                }

                tasks.Clear();
                remainingRunCount = (totalRuns - runCount);

                if (remainingRunCount > 0)
                {
                    DisplayMessageDuringProgress($"{remainingRunCount} runs remaining. Running the Insert > Update > GetSingle > Delete tests asynchronously");

                    var currentRunCount = runCount;
                    var insertTasks = new List<Task<ApiProductModel?>>();

                    var cts = new CancellationTokenSource();
                    var animationTask = ShowProgressAnimation(cts.Token, "Inserting rows asynchronously");

                    while (currentRunCount < totalRuns)
                    {
                        insertTasks.Add(TestInsertRow(productModelsClient, token));
                        currentRunCount++;
                    }

                    var insertedRows = await Task.WhenAll(insertTasks);
                    insertedRows = insertedRows.Where(x => x != null).ToArray();

                    cts.Cancel();
                    await animationTask;

                    var updateTasks = new List<Task>();

                    cts = new CancellationTokenSource();
                    animationTask = ShowProgressAnimation(cts.Token, "Updating rows asynchronously");

                    foreach (var insertedRow in insertedRows)
                    {
                        updateTasks.Add(TestUpdateRow(productModelsClient, token, insertedRow!));
                    }

                    await Task.WhenAll(updateTasks);

                    cts.Cancel();
                    await animationTask;

                    var getSingleTasks = new List<Task>();

                    DisplayMessageDuringProgress($"Reading rows asynchronously...");

                    foreach (var insertedRow in insertedRows)
                    {
                        getSingleTasks.Add(TestGetSingle(productModelsClient, token, runCount, totalRuns, insertedRow!.ProductModelId));

                        runCount++;
                    }

                    await Task.WhenAll(getSingleTasks);

                    cts = new CancellationTokenSource();
                    animationTask = ShowProgressAnimation(cts.Token, "Deleting rows asynchronously");

                    var deleteTasks = new List<Task>();

                    int deletedRowCount = 0;
                    foreach (var insertedRow in insertedRows)
                    {
                        //Let the row count increase till 100k.
                        if ((actualDbRowCount + insertedRows.Count() - deletedRowCount) > 100000)
                        {
                            // Server will only let you delete rows that did not exist in the database originally 
                            // (i.e., someone else like you created them).
                            deleteTasks.Add(TestDelete(productModelsClient, token, insertedRow!.ProductModelId));
                            deletedRowCount++;
                        }
                    }

                    if (deleteTasks.Count > 0)
                    {
                        await Task.WhenAll(deleteTasks);
                    }

                    cts.Cancel();
                    await animationTask;
                }

                DisplayProgress(runCount, totalRuns, force: true);

                Console.WriteLine("\r\n\r\nTest Completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task ShowProgressAnimation(CancellationToken token, string message)
        {
            var animation = new[] { '/', '-', '\\', '|' };
            int counter = 0;

            while (!token.IsCancellationRequested)
            {
                Console.Write($"\r{message} {animation[counter % animation.Length]}");
                counter++;
                await Task.Delay(100);
            }

            // Clear the animation line
            Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
        }

        private static async Task<GetRowCountResponse?> TestGetRowCount(ProductModelsService.ProductModelsServiceClient client, string token, int retryCount = 0)
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

                return response;
            }
            catch (Exception ex)
            {
                TestResults.OtherErrors.Add(ex);

                if (retryCount < 3)
                {
                    Console.WriteLine($"    Retry {retryCount + 1}...");
                    return await TestGetRowCount(client, token, retryCount + 1);
                }
                else
                {
                    return null;
                }
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
                //var restWatch = new Stopwatch();
                //restWatch.Start();
                var response = await client.InsertAsync(request, headers);
                //restWatch.Stop();
                //Console.WriteLine($"    Inserted ProductModelId: {response.Result.ProductModelId}, Message: {response.Message}");

                //Console.WriteLine($"Grpc RunTime: {restWatch.Elapsed.TotalMilliseconds}ms, Result: {response?.Result.ProductModelId}");

                return response?.Result;
            }
            catch (Exception ex)
            {
                TestResults.OtherErrors.Add(ex);
                return null;
            }
        }

        private static void DisplayProgress(int index, int total, int updateInterval = 2, bool force = false)
        {
            if (index % updateInterval == 0 || index == total - 1 || force)
            {
                int progressWidth = 50; // Width of the progress bar
                double percentage = (double)index / total;
                int progress = (int)(percentage * progressWidth);

                Console.Write("\r[");
                Console.Write(new string('#', progress));
                Console.Write(new string(' ', progressWidth - progress));
                Console.Write($"] {index}/{total} ({percentage:P0})");
                Console.Out.Flush(); // Ensure the output is flushed immediately
            }
        }

        private static void DisplayMessageDuringProgress(string message)
        {
            // Clear the progress bar line
            Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
            Console.WriteLine(message);
        }

        private static bool getSingleCallSqlFirst = true;

        private static async Task TestGetSingle(
            ProductModelsService.ProductModelsServiceClient client, 
            string token,                         
            int index, 
            int total, 
            int productModelId)
        {
            try
            {
                var headers = new Metadata
                {
                    { "Authorization", $"Bearer {token}" }
                };

                var tasks = new List<Task>();

                if (getSingleCallSqlFirst)
                {
                    tasks.Add(GetSingleWithSqlAsync(client, headers, productModelId));
                    tasks.Add(GetSingleWithRedflyAsync(client, headers, productModelId));
                    getSingleCallSqlFirst = false;
                }
                else
                {
                    tasks.Add(GetSingleWithRedflyAsync(client, headers, productModelId));
                    tasks.Add(GetSingleWithSqlAsync(client, headers, productModelId));
                    getSingleCallSqlFirst = true;
                }

                DisplayProgress(index, total);
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task GetSingleWithRedflyAsync(ProductModelsService.ProductModelsServiceClient client, Metadata headers, int productModelId)
        {
            var request = new GetSingleRequest
            {
                ProductModelId = productModelId, // Replace with a valid ProductModelId
                UseCache = true,
                ExecutionMode = (int)ReadExecutionMode.UnsafeButFast
            };

            //Console.Write($"\r Executing GetSingle Request with JWT Token...");
            var restWatch = new Stopwatch();

            try
            {   
                restWatch.Start();
                var response = await client.GetSingleAsync(request, headers);
            }
            catch (Exception ex)
            {
                TestResults.RedflyOverGrpcErrors.Add(ex);
            }
            finally
            {
                restWatch.Stop();
                TestResults.RedflyOverGrpcTimings.Add(restWatch.Elapsed.TotalMilliseconds);
                //Console.Write($"\r {index}/{total}|redfly|  ProductModel: {response.Result?.Name ?? ""}, Message: {response.Message}");
            }
        }

        private static async Task GetSingleWithSqlAsync(ProductModelsService.ProductModelsServiceClient client, Metadata headers, int productModelId)
        {
            var request = new GetSingleRequest
            {
                ProductModelId = productModelId, // Replace with a valid ProductModelId
                UseCache = false,
                ExecutionMode = (int)ReadExecutionMode.Balanced
            };

            //Console.Write($"\r Executing GetSingle Request with JWT Token...");
            var restWatch = new Stopwatch();

            try
            {
                restWatch.Start();
                var response = await client.GetSingleAsync(request, headers);
            }
            catch (Exception ex)
            {
                TestResults.SqlOverGrpcErrors.Add(ex);
            }
            finally
            {
                restWatch.Stop();
                TestResults.SqlOverGrpcTimings.Add(restWatch.Elapsed.TotalMilliseconds);
                //Console.Write($"\r {index}/{total}|SQL| ProductModel: {response.Result?.Name ?? ""}, Message: {response.Message}");
            }
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
                        Rowguid = Guid.NewGuid().ToString(),
                        ModifiedDate = DateTime.UtcNow.ToString("o")
                    }
                };

                //Console.WriteLine("    Executing Update Request with JWT Token...");
                //var restWatch = new Stopwatch();
                //restWatch.Start();
                var response = await client.UpdateAsync(request, headers);
                //restWatch.Stop();
                //Console.WriteLine($"    Updated Rows: {response.Result}, Message: {response.Message}");

                //Console.WriteLine($"Grpc RunTime: {restWatch.Elapsed.TotalMilliseconds}ms, Result: {response.Result}");
            }
            catch (Exception ex)
            {
                TestResults.OtherErrors.Add(ex);
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
                //var restWatch = new Stopwatch();
                //restWatch.Start();
                var response = await client.DeleteAsync(request, headers);
                //restWatch.Stop();
                //Console.WriteLine($"    Delete Success: {response.Success}, Message: {response.Message}");

                //Console.WriteLine($"Grpc RunTime: {restWatch.Elapsed.TotalMilliseconds} ms, Result: {response.Message}");
            }
            catch (Exception ex)
            {
                TestResults.OtherErrors.Add(ex);
            }
        }

        private static bool getManyCallSqlFirst = true;

        private static async Task<GetManyResponse?> TestGetMany(
            ProductModelsService.ProductModelsServiceClient client, 
            string token,                             
            int index, 
            int total,
            int pageNo, 
            int pageSize)
        {
            try
            {
                var headers = new Metadata
                {
                    { "Authorization", $"Bearer {token}" }
                };

                var getManyTasks = new List<Task<GetManyResponse?>>();

                if (getManyCallSqlFirst)
                {
                    getManyTasks.Add(GetManyWithSqlAsync(client, headers, pageNo, pageSize));
                    getManyTasks.Add(GetManyWithRedflyAsync(client, headers, pageNo, pageSize));
                    getManyCallSqlFirst = false;
                }
                else
                {
                    getManyTasks.Add(GetManyWithRedflyAsync(client, headers, pageNo, pageSize));
                    getManyTasks.Add(GetManyWithSqlAsync(client, headers, pageNo, pageSize));
                    getManyCallSqlFirst = true;
                }

                DisplayProgress(index, total);
                var responses = await Task.WhenAll(getManyTasks);
                
                return responses.Where(x => x != null).FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        private static async Task<GetManyResponse?> GetManyWithRedflyAsync(ProductModelsService.ProductModelsServiceClient client, Metadata headers, int pageNo, int pageSize)
        {
            var request = new GetManyRequest
            {
                PageNo = pageNo,
                PageSize = pageSize,
                UseCache = true,
                ExecutionMode = (int)ReadExecutionMode.UnsafeButFast
            };

            //Console.Write($"\r Executing GetMany Request with JWT Token...");
            var restWatch = new Stopwatch();
            GetManyResponse? response = null;

            try
            {
                restWatch.Start();
                response = await client.GetManyAsync(request, headers);
            }
            catch (Exception ex)
            {
                TestResults.RedflyOverGrpcErrors.Add(ex);
            }
            finally
            {
                restWatch.Stop();
                TestResults.RedflyOverGrpcTimings.Add(restWatch.Elapsed.TotalMilliseconds);
                //Console.Write($"\r {index}/{total}|redfly| Total Products: {response.Results.Count}, Message: {response.Message}");
            }

            return response;
        }

        private static async Task<GetManyResponse?> GetManyWithSqlAsync(ProductModelsService.ProductModelsServiceClient client, Metadata headers, int pageNo, int pageSize)
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
            GetManyResponse? response = null;

            try
            {
                restWatch.Start();
                response = await client.GetManyAsync(request, headers);
            }
            catch (Exception ex)
            {
                TestResults.SqlOverGrpcErrors.Add(ex);
            }
            finally
            {
                restWatch.Stop();
                TestResults.SqlOverGrpcTimings.Add(restWatch.Elapsed.TotalMilliseconds);
                //Console.Write($"\r {index}/{total}|SQL| Total Products: {response.Results.Count}, Message: {response.Message}");
            }

            return response;
        }
    }
}
