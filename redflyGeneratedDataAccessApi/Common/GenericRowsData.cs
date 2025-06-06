using redflyGeneratedDataAccessApi.Base;
using redflyGeneratedDataAccessApi.Protos.DatabaseApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyGeneratedDataAccessApi.Common;

public class GenericRowsData : BaseTableRowsData
{
    public List<Row> Rows { get; set; } = new();
}
