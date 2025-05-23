using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyDataAccessClient.APIs.SqlServer;

public abstract class TableEntityBase
{

    public byte[] Version { get; set; } = Array.Empty<byte>();

}
