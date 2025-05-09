using Grpc.Core;
using Grpc.Net.Client;
using RedflyDatabaseSyncProxy.Protos.Common;
using RedflyDatabaseSyncProxy.Protos.Postgres;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyDatabaseSyncProxy.GrpcClients;

public class GrpcPostgresChakraServiceClient : GrpcDatabaseChakraServiceClientBase, IGrpcDatabaseChakraServiceClient
{

    private readonly NativeGrpcPostgresChakraService.NativeGrpcPostgresChakraServiceClient _postgresChakraServiceClient;

    public GrpcPostgresChakraServiceClient(string grpcUrl, string grpcAuthToken, string clientSessionId) : base(grpcUrl, grpcAuthToken, clientSessionId)
    {
        _postgresChakraServiceClient = new NativeGrpcPostgresChakraService.NativeGrpcPostgresChakraServiceClient(_channel);
    }

    public NativeGrpcPostgresChakraService.NativeGrpcPostgresChakraServiceClient PostgresChakraServiceClient => _postgresChakraServiceClient;

    public AsyncDuplexStreamingCall<ClientMessage, ServerMessage> CommunicateWithClient(DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        return _postgresChakraServiceClient.CommunicateWithClient(_grpcHeaders, deadline, cancellationToken);
    }

    public AsyncUnaryCall<GetChakraSyncStatusResponse> GetChakraSyncStatusAsync(GetChakraSyncStatusRequest request, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        return _postgresChakraServiceClient.GetChakraSyncStatusAsync(request, _grpcHeaders, deadline, cancellationToken);
    }

    public AsyncUnaryCall<StopChakraSyncResponse> StopChakraSyncAsync(StopChakraSyncRequest request, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        return _postgresChakraServiceClient.StopChakraSyncAsync(request, _grpcHeaders, deadline, cancellationToken);
    }
}
