using Grpc.Core;
using Grpc.Net.Client;
using RedflyDatabaseSyncProxy.Protos.Common;
using RedflyDatabaseSyncProxy.Protos.Mongo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyDatabaseSyncProxy.GrpcClients;

public class GrpcMongoChakraServiceClient : GrpcDatabaseChakraServiceClientBase, IGrpcDatabaseChakraServiceClient
{

    private readonly NativeGrpcMongoChakraService.NativeGrpcMongoChakraServiceClient _mongoChakraServiceClient;

    public GrpcMongoChakraServiceClient(string grpcUrl, string grpcAuthToken, string clientSessionId) : base(grpcUrl, grpcAuthToken, clientSessionId)
    {
        _mongoChakraServiceClient = new NativeGrpcMongoChakraService.NativeGrpcMongoChakraServiceClient(_channel);
    }

    public NativeGrpcMongoChakraService.NativeGrpcMongoChakraServiceClient MongoChakraServiceClient => _mongoChakraServiceClient;

    public AsyncDuplexStreamingCall<ClientMessage, ServerMessage> CommunicateWithClient(DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        return _mongoChakraServiceClient.CommunicateWithClient(_grpcHeaders, deadline, cancellationToken);
    }

    public AsyncUnaryCall<GetChakraSyncStatusResponse> GetChakraSyncStatusAsync(GetChakraSyncStatusRequest request, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        return _mongoChakraServiceClient.GetChakraSyncStatusAsync(request, _grpcHeaders, deadline, cancellationToken);
    }

    public AsyncUnaryCall<StopChakraSyncResponse> StopChakraSyncAsync(StopChakraSyncRequest request, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        return _mongoChakraServiceClient.StopChakraSyncAsync(request, _grpcHeaders, deadline, cancellationToken);
    }
}
