using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;

namespace MongoRepository
{
    public class MongoDbManager
    {
        public MongoDbManager()
            : this(Util<int>.GetDefaultConnectionString())
        {
        }

        public MongoDbManager(string connectionString)
        {
            Database = Util<int>.GetDatabaseFromConnectionString(connectionString);
        }

        public IReadOnlyList<string> GetCollectionsList()
        {
            var list = Database.ListCollections().ToList();
            return list.Select(d => d["name"].ToString()).ToList();
        }

        protected IMongoDatabase Database { get; }
    }
}