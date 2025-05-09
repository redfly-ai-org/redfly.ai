using Grpc.Core;
using RedflyCoreFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyDatabaseSyncProxy.SyncServices;
internal abstract class ChakraDatabaseSyncServiceClientBase
{
    #region Fields

    protected bool _bidirectionalStreamingIsWorking = false;
    protected int _bidirStreamingRetryCount = 0;

    protected string _clientSessionId = ClientSessionId.Generate();
    protected readonly FixedSizeList<Exception> _lastBidirErrors = new FixedSizeList<Exception>(5);

    protected Metadata? _grpcHeaders;

    #endregion Fields

}
