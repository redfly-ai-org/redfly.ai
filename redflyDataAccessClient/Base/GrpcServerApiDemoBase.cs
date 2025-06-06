using Grpc.Core;
using Grpc.Net.Client;
using Newtonsoft.Json;
using RedflyCoreFramework;
using System.Diagnostics;

namespace redflyDataAccessClient.Base;

internal abstract class GrpcServerApiDemoBase
{
    protected static void ShowResultsAsJson<T>(Stopwatch watch, T response)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
        Console.ResetColor();
        Console.WriteLine($"Response Time: {watch.ElapsedMilliseconds} ms");
        Console.WriteLine();
    }

    protected static Dictionary<string, string> PromptUserForColumnValuePairs()
    {
        var insertedData = new Dictionary<string, string>();

        while (true)
        {
            Console.WriteLine("Enter column name (leave empty to finish):");
            var columnName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(columnName))
                break;

            Console.WriteLine($"Enter value for column '{columnName}':");
            var columnValue = Console.ReadLine() ?? string.Empty;

            insertedData[columnName] = columnValue;
        }

        Console.WriteLine("Collected columns and values for insertion:");
        foreach (var kvp in insertedData)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
        }

        return insertedData;
    }
}