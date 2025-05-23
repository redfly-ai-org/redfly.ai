using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyDataAccessClient.APIs.SqlServer;
public class DeletedData
{
    public bool Success { get; set; }
    public bool CacheUpdated { get; set; }
    public string? Message { get; set; }
}
