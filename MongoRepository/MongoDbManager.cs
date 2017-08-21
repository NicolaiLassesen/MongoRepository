using System.Collections.Generic;
using System.Linq;
using System.Security;
using MongoDB.Driver;

namespace MongoRepository
{
    public class MongoDbManager
    {
        public MongoDbManager(string username, SecureString password)
            : this(Util<int>.GetDefaultConnectionString(), username, password)
        {
        }

        public MongoDbManager(string connectionString, string username, SecureString password)
        {
            Database = Util<int>.GetDatabaseFromConnectionString(connectionString, username, password);
        }

        public IReadOnlyList<string> GetCollectionsList()
        {
            var list = Database.ListCollections().ToList();
            return list.Select(d => d["name"].ToString()).ToList();
        }

        protected IMongoDatabase Database { get; }
    }
}