using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyGeneratedDataAccessApi.SqlServer;

public class UpdatedData
{
    public bool Success { get; set; }
    public int UpdatedCount { get; set; }
    public bool CacheUpdated { get; set; }
    public string? Message { get; set; }
}
