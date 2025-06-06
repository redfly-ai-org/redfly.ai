using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyGeneratedDataAccessApi.Base;

public abstract class BaseTableInsertedData
{

    public bool Success { get; set; }
    public bool CacheUpdated { get; set; }
    public string? Message { get; set; }

}
