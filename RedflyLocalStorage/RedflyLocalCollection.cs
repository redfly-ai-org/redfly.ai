using LiteDB;
using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage
{
    abstract public class RedflyLocalCollection<T> where T : class
    {

        protected LiteDatabase _db;
        protected string _collectionName;
        protected ILiteCollection<T> _collection;

        protected RedflyLocalCollection(LiteDatabase db, string collectionName)
        {
            _db = db;
            _collectionName = collectionName;
            _collection = db.GetCollection<T>(collectionName);
        }

        public virtual IEnumerable<T> All()
        {
            return _collection.FindAll();
        }

        public virtual BsonValue Add(T entity)
        {
            return _collection.Insert(entity);
        }

        public virtual bool Update(T entity)
        {
            return _collection.Update(entity);
        }

        public virtual bool Delete(BsonValue id)
        {
            return _collection.Delete(id);
        }

    }
}
