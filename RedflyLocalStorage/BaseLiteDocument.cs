using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage
{
    abstract public class BaseLiteDocument
    {

        [BsonId]
        public ObjectId Id { get; set; }

    }
}
