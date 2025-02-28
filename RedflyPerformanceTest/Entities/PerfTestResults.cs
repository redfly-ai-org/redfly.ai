using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyPerformanceTest.Entities
{
    internal class PerfTestResults
    {

        internal List<double> RestApiTimings { get; set; } = new List<double>();
        internal List<double> DALTimings { get; set; } = new List<double>();
        internal List<double> RedflyOverGrpcTimings { get; set; } = new  List<double>();
        internal List<double> SqlOverGrpcTimings { get; set; } = new List<double>();

        internal bool Populated()
        {
            return (RestApiTimings.Count > 0 || 
                    DALTimings.Count > 0 || 
                    RedflyOverGrpcTimings.Count > 0 || 
                    SqlOverGrpcTimings.Count > 0);
        }

    }
}
