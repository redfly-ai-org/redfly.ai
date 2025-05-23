using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyDataAccessClient.APIs;

public enum DbOperationType
{
    Unspecified = 0,
    Select = 1,
    Insert = 2,
    Update = 3,
    Delete = 4
}
