
using Newtonsoft.Json;

namespace redflyDataAccessClient;

internal class Program
{
    static void Main(string[] args)
    {
        Console.Title = "redfly.ai - Data Access Client";

        var query = new QueryBuilder()
                        .From("enc_hostname", "enc_dbname", "enc_uid", "enc_pwd", "enc_key")
                        .Table("Orders")
                        .Select("Id", "Total", "CustomerId")
                        .Where("Status", "=", "Pending")
                        .And("CreatedAt", ">", DateTime.UtcNow)
                        .OrderByDesc("Total")
                        .Join("INNER", "Customers", "Orders.CustomerId", "Customers.Id");

        string json = JsonConvert.SerializeObject(query, Formatting.Indented);
        Console.WriteLine("Generated Query JSON:");
        Console.WriteLine(json);

        Console.ReadLine();
    }
}
