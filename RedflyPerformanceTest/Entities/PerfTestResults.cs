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

        internal ConcurrentBag<double> RedflyOverGrpcTimings { get; set; } = new ConcurrentBag<double>();

        internal ConcurrentBag<double> SqlOverGrpcTimings { get; set; } = new ConcurrentBag<double>();

        internal ConcurrentBag<Exception> RedflyOverGrpcErrors { get; set; } = new ConcurrentBag<Exception>();

        internal ConcurrentBag<Exception> SqlOverGrpcErrors { get; set; } = new ConcurrentBag<Exception>();

        internal ConcurrentBag<Exception> OtherErrors { get; set; } = new ConcurrentBag<Exception>();

        internal bool Populated()
        {
            return (RedflyOverGrpcTimings.Count > 0 || 
                    SqlOverGrpcTimings.Count > 0);
        }

    }
}
