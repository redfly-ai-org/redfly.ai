using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyDataAccessClient;

public class QueryResult
{
    public List<Dictionary<string, object>> Rows { get; set; } = new();
}
