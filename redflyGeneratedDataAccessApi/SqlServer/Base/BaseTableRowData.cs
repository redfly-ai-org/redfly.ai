using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyGeneratedDataAccessApi.SqlServer;

public abstract class BaseTableRowData
{
    public bool Success { get; set; }
    public bool FromCache { get; set; }
    public string? Message { get; set; }
}
