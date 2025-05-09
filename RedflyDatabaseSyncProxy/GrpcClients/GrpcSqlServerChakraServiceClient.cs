using Grpc.Core;
using RedflyDatabaseSyncProxy.Protos.Common;
using RedflyDatabaseSyncProxy.Protos.SqlServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyDatabaseSyncProxy.GrpcClients;

internal class GrpcSqlServerChakraServiceClient : GrpcDatabaseChakraServiceClientBase, IGrpcDatabaseChakraServiceClient
{

    private readonly NativeGrpcSqlServerChakraService.NativeGrpcSqlServerChakraServiceClient _sqlServerChakraServiceClient;

    public GrpcSqlServerChakraServiceClient(string grpcUrl, string grpcAuthToken, string clientSessionId) : base(grpcUrl, grpcAuthToken, clientSessionId)
    {
        _sqlServerChakraServiceClient = new NativeGrpcSqlServerChakraService.NativeGrpcSqlServerChakraServiceClient(_channel);
    }

    public NativeGrpcSqlServerChakraService.NativeGrpcSqlServerChakraServiceClient SqlServerChakraServiceClient => _sqlServerChakraServiceClient;

    public AsyncDuplexStreamingCall<ClientMessage, ServerMessage> CommunicateWithClient(DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        return _sqlServerChakraServiceClient.CommunicateWithClient(_grpcHeaders, deadline, cancellationToken);
    }

    public AsyncUnaryCall<GetChakraSyncStatusResponse> GetChakraSyncStatusAsync(GetChakraSyncStatusRequest request, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        return _sqlServerChakraServiceClient.GetChakraSyncStatusAsync(request, _grpcHeaders, deadline, cancellationToken);
    }

    public AsyncUnaryCall<StopChakraSyncResponse> StopChakraSyncAsync(StopChakraSyncRequest request, DateTime? deadline = null, CancellationToken cancellationToken = default)
    {
        return _sqlServerChakraServiceClient.StopChakraSyncAsync(request, _grpcHeaders, deadline, cancellationToken);
    }
}
