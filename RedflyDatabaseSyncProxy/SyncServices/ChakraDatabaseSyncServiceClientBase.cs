using Grpc.Core;
using RedflyCoreFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedflyDatabaseSyncProxy.GrpcClients;
using System.Text.RegularExpressions;

namespace RedflyDatabaseSyncProxy.SyncServices;

internal abstract class ChakraDatabaseSyncServiceClientBase
{
    #region Fields

    protected bool _bidirectionalStreamingIsWorking = false;
    protected int _bidirStreamingRetryCount = 0;

    protected string _clientSessionId = ClientSessionId.Generate();
    protected readonly FixedSizeList<Exception> _lastBidirErrors = new FixedSizeList<Exception>(5);

    protected Metadata? _grpcHeaders;

    protected IGrpcDatabaseChakraServiceClient _grpcClient;

    #endregion Fields

    protected ChakraDatabaseSyncServiceClientBase(IGrpcDatabaseChakraServiceClient grpcClient)
    {
        _grpcClient = grpcClient;
    }

    protected string FormatGrpcServerMessage(string logMessage)
    {
        try
        {
            var regex = new Regex(@"Type: (?<Type>[^,]+), Data: \{ Operation = (?<Operation>[^}]+) \}");
            var match = regex.Match(logMessage);

            if (match.Success)
            {
                var type = match.Groups["Type"].Value;
                var operation = match.Groups["Operation"].Value;
                return $"SRVR|{type}|{operation}";
            }

            return logMessage; // Return the original message if it doesn't match the expected format
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error formatting message: {ex}");

            return logMessage; // Return the original message if an exception occurs
        }
    }

}
