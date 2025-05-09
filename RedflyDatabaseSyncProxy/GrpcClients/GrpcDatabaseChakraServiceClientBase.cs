using Grpc.Core;
using Grpc.Net.Client;
using RedflyCoreFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RedflyDatabaseSyncProxy.GrpcClients;

public abstract class GrpcDatabaseChakraServiceClientBase
{

    #region Fields

    private string _grpcUrl;
    protected string _grpcAuthToken;
    protected string _clientSessionId;

    protected GrpcChannel _channel;
    protected Metadata _grpcHeaders;

    #endregion Fields

    protected GrpcDatabaseChakraServiceClientBase(string grpcUrl, string grpcAuthToken, string clientSessionId)
    {
        _grpcUrl = grpcUrl;
        _grpcAuthToken = grpcAuthToken;
        _clientSessionId = clientSessionId;

        _channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
        {
            //LoggerFactory = loggerFactory,
            HttpHandler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true,
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                KeepAlivePingDelay = TimeSpan.FromSeconds(30), // Frequency of keepalive pings
                KeepAlivePingTimeout = TimeSpan.FromSeconds(5) // Timeout before considering the connection dead
            },
            HttpVersion = new Version(2, 0) // Ensure HTTP/2 is used
        });

        _grpcHeaders = new Metadata
                {
                    { "Authorization", $"Bearer {grpcAuthToken}" },
                    { "client-session-id", clientSessionId.ToString() }
                };
    }

    public Metadata GrpcHeaders { get => _grpcHeaders; set => _grpcHeaders = value; }

    public string GrpcUrl { get => _grpcUrl; }

}
