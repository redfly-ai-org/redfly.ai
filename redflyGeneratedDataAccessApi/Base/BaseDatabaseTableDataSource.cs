using Grpc.Net.Client;
using RedflyCoreFramework;
using redflyDatabaseAdapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyGeneratedDataAccessApi.Base;

public abstract class BaseDatabaseTableDataSource
{

    protected GrpcChannel _channel;
    protected readonly string _encryptionKey;
    protected string _encTable = "";

    protected BaseDatabaseTableDataSource()
    {
        _channel = GrpcChannel.ForAddress(AppGrpcSession.GrpcUrl, new GrpcChannelOptions
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

        _encryptionKey = RedflyEncryptionKeys.AesKey;
    }

}
