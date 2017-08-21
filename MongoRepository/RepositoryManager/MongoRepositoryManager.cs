using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

// ReSharper disable once CheckNamespace

namespace MongoRepository
{
    // TODO: Code coverage here is near-zero. A new RepoManagerTests.cs class needs to be created and we need to
    //      test these methods. Ofcourse we also need to update codeplex documentation on this entirely new object.
    //      This is a work-in-progress.

    // TODO: GetStats(), Validate(), GetIndexes and EnsureIndexes(IMongoIndexKeys, IMongoIndexOptions) "leak"
    //      MongoDb-specific details. These probably need to get wrapped in MongoRepository specific objects to hide
    //      MongoDb.

    /// <summary>
    /// Deals with the collections of entities in MongoDb. This class tries to hide as much MongoDb-specific details
    /// as possible but it's not 100% *yet*. It is a very thin wrapper around most methods on MongoDb's MongoCollection
    /// objects.
    /// </summary>
    /// <typeparam name="TEntity">The type contained in the repository to manage.</typeparam>
    /// <typeparam name="TKey">The type used for the entity's Id.</typeparam>
    public class MongoRepositoryManager<TEntity, TKey> : IRepositoryManager<TEntity, TKey>
        where TEntity : IEntity<TKey>
    {
        /// <summary>
        /// Initializes a new instance of the MongoRepositoryManager class.
        /// Uses the Default App/Web.Config connectionstrings to fetch the connectionString and Database name.
        /// </summary>
        /// <remarks>Default constructor defaults to "MongoServerSettings" key for connectionstring.</remarks>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepositoryManager(string username, SecureString password)
            : this(Util<TKey>.GetDefaultConnectionString(), username, password)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepositoryManager class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepositoryManager(string connectionString, string username, SecureString password)
        {
            Database = Util<TKey>.GetDatabaseFromConnectionString(connectionString, username, password);
            Collection = Util<TKey>.GetCollectionFromDatabase<TEntity>(Database);
            Name = Collection.CollectionNamespace.CollectionName;
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepositoryManager class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepositoryManager(string connectionString,
                                      string collectionName,
                                      string username,
                                      SecureString password)
        {
            Database = Util<TKey>.GetDatabaseFromConnectionString(connectionString, username, password);
            Collection = Database.GetCollection<TEntity>(collectionName);
            Name = Collection.CollectionNamespace.CollectionName;
        }

        /// <summary>
        /// Gets a value indicating whether the collection already exists.
        /// </summary>
        /// <value>Returns true when the collection already exists, false otherwise.</value>
        public virtual bool Exists
            => Database.ListCollections(new ListCollectionsOptions {Filter = new BsonDocument("name", Name)}).Any();

        /// <summary>
        /// Gets the name of the collection as Mongo uses.
        /// </summary>
        /// <value>The name of the collection as Mongo uses.</value>
        public virtual string Name { get; }

        /// <summary>
        /// Drops the collection.
        /// </summary>
        public virtual async Task DropAsync()
        {
            await Database.DropCollectionAsync(Name);
        }

        /// <summary>
        /// Drops specified index on the repository.
        /// </summary>
        /// <param name="keyname">The name of the indexed field.</param>
        public virtual async Task DropIndex(string keyname)
        {
            await Collection.Indexes.DropOneAsync(keyname);
        }

        /// <summary>
        /// Drops specified indexes on the repository.
        /// </summary>
        /// <param name="keynames">The names of the indexed fields.</param>
        public virtual async Task DropIndexes(IEnumerable<string> keynames)
        {
            foreach (var keyname in keynames)
            {
                await DropIndex(keyname);
            }
        }

        /// <summary>
        /// Drops all indexes on this repository.
        /// </summary>
        public virtual async Task DropAllIndexes()
        {
            await Collection.Indexes.DropAllAsync();
        }

        /// <summary>
        /// Ensures that the desired index exist and creates it if it doesn't exist.
        /// </summary>
        /// <param name="keyname">The indexed field.</param>
        /// <remarks>
        /// This is a convenience method for EnsureIndexes(IMongoIndexKeys keys, IMongoIndexOptions options).
        /// Index will be ascending order, non-unique, non-sparse.
        /// </remarks>
        public virtual async Task EnsureIndex(string keyname)
        {
            throw new NotImplementedException();
            //var keys = Builders<TEntity>.IndexKeys.Ascending(keyname);
            //await Collection.Indexes.CreateOneAsync(keys);
            //EnsureIndexes(new[] { keyname });
        }

        /// <summary>
        /// Ensures that the desired index exist and creates it if it doesn't exist.
        /// </summary>
        /// <param name="keyname">The indexed field.</param>
        /// <param name="descending">Set to true to make index descending, false for ascending.</param>
        /// <param name="unique">Set to true to ensure index enforces unique values.</param>
        /// <param name="sparse">Set to true to specify the index is sparse.</param>
        /// <remarks>
        /// This is a convenience method for EnsureIndexes(IMongoIndexKeys keys, IMongoIndexOptions options).
        /// </remarks>
        public virtual async Task EnsureIndex(string keyname, bool descending, bool unique, bool sparse)
        {
            throw new NotImplementedException();
            //EnsureIndexes(new string[] { keyname }, descending, unique, sparse);
        }

        /// <summary>
        /// Ensures that the desired indexes exist and creates them if they don't exist.
        /// </summary>
        /// <param name="keynames">The indexed fields.</param>
        /// <remarks>
        /// This is a convenience method for EnsureIndexes(IMongoIndexKeys keys, IMongoIndexOptions options).
        /// Index will be ascending order, non-unique, non-sparse.
        /// </remarks>
        public virtual async Task EnsureIndexes(IEnumerable<string> keynames)
        {
            throw new NotImplementedException();
            //EnsureIndexes(keynames, false, false, false);
        }

        /// <summary>
        /// Ensures that the desired indexes exist and creates them if they don't exist.
        /// </summary>
        /// <param name="keynames">The indexed fields.</param>
        /// <param name="descending">Set to true to make index descending, false for ascending.</param>
        /// <param name="unique">Set to true to ensure index enforces unique values.</param>
        /// <param name="sparse">Set to true to specify the index is sparse.</param>
        /// <remarks>
        /// This is a convenience method for EnsureIndexes(IMongoIndexKeys keys, IMongoIndexOptions options).
        /// </remarks>
        public virtual async Task EnsureIndexes(IEnumerable<string> keynames, bool descending, bool unique, bool sparse)
        {
            throw new NotImplementedException();
            //var ixk = new IndexKeysBuilder();
            //if (descending)
            //{
            //    ixk.Descending(keynames.ToArray());
            //}
            //else
            //{
            //    ixk.Ascending(keynames.ToArray());
            //}

            //EnsureIndexes(
            //    ixk,
            //    new IndexOptionsBuilder().SetUnique(unique).SetSparse(sparse));
        }

        /// <summary>
        /// Ensures that the desired indexes exist and creates them if they don't exist.
        /// </summary>
        /// <param name="keys">The indexed fields.</param>
        /// <param name="options">The index options.</param>
        /// <remarks>
        /// This method allows ultimate control but does "leak" some MongoDb specific implementation details.
        /// </remarks>
        //public virtual async Task EnsureIndexes(IMongoIndexKeys keys, IMongoIndexOptions options)
        //{
        //    Collection.CreateIndex(keys, options);
        //}

        /// <summary>
        /// Tests whether indexes exist.
        /// </summary>
        /// <param name="keyname">The indexed fields.</param>
        /// <returns>Returns true when the indexes exist, false otherwise.</returns>
        public virtual async Task<bool> IndexExists(string keyname)
        {
            throw new NotImplementedException();
            //var idx = await Collection.Indexes.ListAsync();
            //return idx.ToEnumerable().Select(s => s["name"] == keyname).Any();
        }

        /// <summary>
        /// Tests whether indexes exist.
        /// </summary>
        /// <param name="keynames">The indexed fields.</param>
        /// <returns>Returns true when the indexes exist, false otherwise.</returns>
        public virtual async Task<bool> IndexesExists(IEnumerable<string> keynames)
        {
            throw new NotImplementedException();
            //return Collection.IndexExists(keynames.ToArray());
        }

        /// <summary>
        /// Runs the ReIndex command on this repository.
        /// </summary>
        public virtual async Task ReIndex()
        {
            throw new NotImplementedException();
            //Collection.ReIndex();
        }

        /// <summary>
        /// Gets the total size for the repository (data + indexes).
        /// </summary>
        /// <returns>Returns total size for the repository (data + indexes).</returns>
        [Obsolete("This method will be removed in the next version of the driver")]
        public virtual async Task<long> GetTotalDataSize()
        {
            throw new NotImplementedException();
            //return Collection.GetTotalDataSize();
        }

        /// <summary>
        /// Gets the total storage size for the repository (data + indexes).
        /// </summary>
        /// <returns>Returns total storage size for the repository (data + indexes).</returns>
        [Obsolete("This method will be removed in the next version of the driver")]
        public virtual async Task<long> GetTotalStorageSize()
        {
            throw new NotImplementedException();
            //return Collection.GetTotalStorageSize();
        }

        ///// <summary>
        ///// Validates the integrity of the repository.
        ///// </summary>
        ///// <returns>Returns a ValidateCollectionResult.</returns>
        ///// <remarks>You will need to reference MongoDb.Driver.</remarks>
        //public virtual ValidateCollectionResult Validate()
        //{
        //    throw new NotImplementedException();
        //    return Collection.Validate();
        //}

        ///// <summary>
        ///// Gets stats for this repository.
        ///// </summary>
        ///// <returns>Returns a CollectionStatsResult.</returns>
        ///// <remarks>You will need to reference MongoDb.Driver.</remarks>
        //public virtual CollectionStatsResult GetStats()
        //{
        //    throw new NotImplementedException();
        //    return Collection.GetStats();
        //}

        ///// <summary>
        ///// Gets the indexes for this repository.
        ///// </summary>
        ///// <returns>Returns the indexes for this repository.</returns>
        //public virtual GetIndexesResult GetIndexes()
        //{
        //    throw new NotImplementedException();
        //    return Collection.GetIndexes();
        //}

        /// <summary>
        /// Mongo database
        /// </summary>
        protected IMongoDatabase Database { get; }

        /// <summary>
        /// MongoCollection field.
        /// </summary>
        protected IMongoCollection<TEntity> Collection { get; }
    }

    /// <summary>
    /// Deals with the collections of entities in MongoDb. This class tries to hide as much MongoDb-specific details
    /// as possible but it's not 100% *yet*. It is a very thin wrapper around most methods on MongoDb's MongoCollection
    /// objects.
    /// </summary>
    /// <typeparam name="T">The type contained in the repository to manage.</typeparam>
    /// <remarks>Entities are assumed to use strings for Id's.</remarks>
    public class MongoRepositoryManager<T> : MongoRepositoryManager<T, string>, IRepositoryManager<T>
        where T : IEntity<string>
    {
        /// <summary>
        /// Initializes a new instance of the MongoRepositoryManager class.
        /// Uses the Default App/Web.Config connectionstrings to fetch the connectionString and Database name.
        /// </summary>
        /// <remarks>Default constructor defaults to "MongoServerSettings" key for connectionstring.</remarks>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepositoryManager(string username, SecureString password)
            : base(username, password)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepositoryManager class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepositoryManager(string connectionString, string username, SecureString password)
            : base(connectionString, username, password)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepositoryManager class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepositoryManager(string connectionString,
                                      string collectionName,
                                      string username,
                                      SecureString password)
            : base(connectionString, collectionName, username, password)
        {
        }
    }
}