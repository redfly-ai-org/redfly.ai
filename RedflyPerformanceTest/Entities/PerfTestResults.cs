using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyPerformanceTest.Entities
{
    internal class PerfTestResults
    {

        internal ConcurrentBag<double> RedflyOverGrpcTimingsInMs { get; set; } = new ConcurrentBag<double>();

        internal ConcurrentBag<double> SqlOverGrpcTimingsInMs { get; set; } = new ConcurrentBag<double>();

        internal ConcurrentBag<Exception> RedflyOverGrpcErrors { get; set; } = new ConcurrentBag<Exception>();

        internal ConcurrentBag<Exception> SqlOverGrpcErrors { get; set; } = new ConcurrentBag<Exception>();

        internal ConcurrentBag<Exception> OtherErrors { get; set; } = new ConcurrentBag<Exception>();

        internal int ErrorCount()
        {
            return RedflyOverGrpcErrors.Count + SqlOverGrpcErrors.Count + OtherErrors.Count;
        }

        internal bool Populated()
        {
            return (RedflyOverGrpcTimingsInMs.Count > 0 || 
                    SqlOverGrpcTimingsInMs.Count > 0);
        }

    }
}
