using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage
{
    public class RedflyLocalDatabase
    {

        private static readonly string _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), _name);

        private const string _name = "redfly-local.db";

        private static Lazy<LiteDatabase> _db = new Lazy<LiteDatabase>(() => new LiteDatabase(_filePath));

        internal static Lazy<LiteDatabase> Instance
        {
            get
            {
                return _db;
            }
        }

        public static void Dispose()
        {
            if (_db.IsValueCreated)
            {
                _db.Value.Dispose();
            }
        }

    }
}
