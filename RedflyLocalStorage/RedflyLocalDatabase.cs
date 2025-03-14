using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage
{
    public class RedflyLocalDatabase
    {

        public static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Name);

        public const string Name = "redfly-local.db";

    }
}
