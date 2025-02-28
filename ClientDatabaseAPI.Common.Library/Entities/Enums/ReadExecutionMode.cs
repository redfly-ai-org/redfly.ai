using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ClientDatabaseAPI.Common.Library
{

    /// <summary>
    /// The execution mode for the data access call
    /// </summary>
    public enum ReadExecutionMode : short
    {

        /// <summary>
        /// The default option which balances safety with performance. Suitable for display scenarios where the result can be slightly stale.
        /// </summary>
        /// <remarks>
        /// The result maybe stale in some scenarios.
        /// </remarks>
        Balanced = 0,
        /// <summary>
        /// The slowest, but safest option. Suitable for data modification scenarios.
        /// </summary>
        /// <remarks>
        /// The result will NEVER be stale.
        /// </remarks>
        SafestButSlow = 1,
        /// <summary>
        /// The fastest, but unsafe option. Use only when performance is paramount above everything else.
        /// </summary>
        /// <remarks>
        /// Prefer the cached version even if it maybe stale.
        /// </remarks>
        UnsafeButFast = 2

    }
}
