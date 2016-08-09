using System.Configuration;
using MongoDB.Driver;

namespace MongoRepository.Tests
{
    public static class MongoTestUtils
    {
        public static void DropDb()
        {
            var url = new MongoUrl(ConfigurationManager.ConnectionStrings["MongoServerSettings"].ConnectionString);
            var client = new MongoClient(url);
            client.DropDatabase(url.DatabaseName);
        }
    }
}