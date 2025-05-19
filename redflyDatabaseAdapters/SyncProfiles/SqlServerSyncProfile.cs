using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyDatabaseAdapters;

public class SqlServerSyncProfile
{

    public static bool Exists(GetSyncProfilesResponse getSyncProfilesResponse)
    {
        return (getSyncProfilesResponse.Success &&
                getSyncProfilesResponse.Profiles != null &&
                getSyncProfilesResponse.Profiles.Count > 0);
    }

    public static async Task<GetSyncProfilesResponse?> GetAllAsync(
        SyncApiService.SyncApiServiceClient syncApiClient,
        GrpcChannel channel,
        Metadata headers,
        int retryCount = 0)
    {
        try
        {
            return await syncApiClient.GetSyncProfilesAsync(new GetSyncProfilesRequest() { PageNo = 1, PageSize = 10 }, headers);
        }
        catch (Exception ex)
        {
            //Console.ForegroundColor = ConsoleColor.Red;
            //Console.WriteLine(ex.Message);
            //Console.ResetColor();
            //Console.WriteLine();

            if (retryCount < 5)
            {
                Console.WriteLine($"Retrying to get sync profiles {retryCount + 1}...");

                await Task.Delay(1000 * retryCount);

                return await GetAllAsync(syncApiClient, channel, headers, retryCount + 1);
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error reading sync profiles from the server.");
            Console.ResetColor();

            throw;
        }
    }

}
