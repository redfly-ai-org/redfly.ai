using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyGeneratedDataAccessApi.SqlServer;

public abstract class BaseSqlServerTableSchema
{

    public byte[] Version { get; set; } = Array.Empty<byte>();

}
