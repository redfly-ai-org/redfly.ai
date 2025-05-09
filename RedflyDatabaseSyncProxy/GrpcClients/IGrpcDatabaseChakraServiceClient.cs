using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using RedflyDatabaseSyncProxy.Protos.Common;

namespace RedflyDatabaseSyncProxy.GrpcClients;

internal interface IGrpcDatabaseChakraServiceClient
{

    string GrpcUrl { get; }

    Metadata GrpcHeaders { get; set; }

    AsyncDuplexStreamingCall<ClientMessage, ServerMessage> CommunicateWithClient(DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken));

    AsyncUnaryCall<GetChakraSyncStatusResponse> GetChakraSyncStatusAsync(GetChakraSyncStatusRequest request, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken));

    AsyncUnaryCall<StopChakraSyncResponse> StopChakraSyncAsync(StopChakraSyncRequest request, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken));

}
