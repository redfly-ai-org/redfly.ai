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

        protected string _collectionName;
        protected Lazy<ILiteCollection<T>> _lazyCollection;

        protected RedflyLocalCollection(string collectionName)
        {
            _collectionName = collectionName;
            _lazyCollection = new Lazy<ILiteCollection<T>>(
                                    () => RedflyLocalDatabase.Instance.Value.GetCollection<T>(collectionName));
        }

        public virtual T FindById(BsonValue id)
        {
            return _lazyCollection.Value.FindById(id);
        }

        public virtual IEnumerable<T> All()
        {
            return _lazyCollection.Value.FindAll();
        }

        public virtual BsonValue Add(T entity)
        {
            return _lazyCollection.Value.Insert(entity);
        }

        public virtual bool Update(T entity)
        {
            return _lazyCollection.Value.Update(entity);
        }

        public virtual bool Delete(BsonValue id)
        {
            return _lazyCollection.Value.Delete(id);
        }

    }
}
