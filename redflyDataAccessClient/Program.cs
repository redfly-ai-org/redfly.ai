using Newtonsoft.Json;

namespace redflyDataAccessClient;

internal class Program
{
    static void Main(string[] args)
    {
        Console.Title = "redfly.ai - Data Access Client";

        var query = new QueryBuilder()
                        .From("enc_hostname", "enc_dbname", "enc_uid", "enc_pwd", "enc_key")
                        .Cached("enc_redis_host", 6380, "enc_redis_pwd",
                            redisUsesSsl: true,
                            redisSslProtocol: "TLS12",
                            redisAbortConnect: false,
                            redisConnectTimeout: 5000,
                            redisSyncTimeout: 10000,
                            redisAsyncTimeout: 15000
                        )
                        .Table("Orders")
                        .Select("Id", "Total", "CustomerId")
                        .Where("Status", "=", "Pending")
                        .And("CreatedAt", ">", DateTime.UtcNow)
                        .OrderByDesc("Total")
                        .Join("INNER", "Customers", "Orders.CustomerId", "Customers.Id");

        string json = JsonConvert.SerializeObject(query, Formatting.Indented);
        Console.WriteLine("Generated Query JSON:");
        Console.WriteLine(json);

        // Next steps - TBD

        Console.ReadLine();
    }
}
