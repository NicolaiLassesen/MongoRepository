using System;
using System.Configuration;
using System.Security;
using MongoDB.Driver;

namespace MongoRepository
{
    /// <summary>
    /// Internal miscellaneous utility functions.
    /// </summary>
    internal static class Util<TKey>
    {
        /// <summary>
        /// The default key MongoRepository will look for in the App.config or Web.config file.
        /// </summary>
        private const string DEFAULT_CONNECTIONSTRING_NAME = "MongoServerSettings";

        /// <summary>
        /// Retrieves the default connectionstring from the App.config or Web.config file.
        /// </summary>
        /// <returns>Returns the default connectionstring from the App.config or Web.config file.</returns>
        public static string GetDefaultConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[DEFAULT_CONNECTIONSTRING_NAME].ConnectionString;
        }

        /// <summary>
        /// Creates and returns a MongoDatabase from the specified url.
        /// </summary>
        /// <param name="connectionString">The connectionstring to use to get the database from.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        /// <returns>Returns a MongoDatabase from the specified url.</returns>
        public static IMongoDatabase GetDatabaseFromConnectionString(string connectionString,
                                                                     string username,
                                                                     SecureString password)
        {
            return GetDatabaseFromUrl(new MongoUrl(connectionString), username, password);
        }

        /// <summary>
        /// Creates and returns a MongoDatabase from the specified url.
        /// </summary>
        /// <param name="url">The url to use to get the database from.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        /// <returns>Returns a MongoDatabase from the specified url.</returns>
        public static IMongoDatabase GetDatabaseFromUrl(MongoUrl url, string username, SecureString password)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));
            if (username == null) throw new ArgumentNullException(nameof(username));
            if (password == null) throw new ArgumentNullException(nameof(password));
            var settings = MongoClientSettings.FromUrl(url);
            string database = url.DatabaseName;
            if (string.IsNullOrEmpty(database))
                throw new ArgumentException("The mongo url must contain the data base name");
            settings.Credentials = new[]
            {
                MongoCredential.CreateCredential(database, username, password)
            };
            var client = new MongoClient(settings);
            return client.GetDatabase(url.DatabaseName); // WriteConcern defaulted to Acknowledged
        }

        /// <summary>
        /// Creates and returns a MongoCollection from the specified type and connectionstring.
        /// </summary>
        /// <typeparam name="TEntity">The type to get the collection of.</typeparam>
        /// <param name="connectionString">The connectionstring to use to get the collection from.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        /// <returns>Returns a MongoCollection from the specified type and connectionstring.</returns>
        public static IMongoCollection<TEntity> GetCollectionFromConnectionString<TEntity>(string connectionString,
                                                                                           string username,
                                                                                           SecureString password)
            where TEntity : IEntity<TKey>
        {
            return GetCollectionFromConnectionString<TEntity>(connectionString,
                                                              GetCollectionName<TEntity>(),
                                                              username,
                                                              password);
        }

        public static IMongoCollection<TEntity> GetCollectionFromDatabase<TEntity>(IMongoDatabase database)
            where TEntity : IEntity<TKey>
        {
            return database.GetCollection<TEntity>(GetCollectionName<TEntity>());
        }

        /// <summary>
        /// Creates and returns a MongoCollection from the specified type and connectionstring.
        /// </summary>
        /// <typeparam name="TEntity">The type to get the collection of.</typeparam>
        /// <param name="connectionString">The connectionstring to use to get the collection from.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        /// <returns>Returns a MongoCollection from the specified type and connectionstring.</returns>
        public static IMongoCollection<TEntity> GetCollectionFromConnectionString<TEntity>(string connectionString,
                                                                                           string collectionName,
                                                                                           string username,
                                                                                           SecureString password)
            where TEntity : IEntity<TKey>
        {
            return GetDatabaseFromUrl(new MongoUrl(connectionString), username, password)
                .GetCollection<TEntity>(collectionName);
        }

        /// <summary>
        /// Creates and returns a MongoCollection from the specified type and url.
        /// </summary>
        /// <typeparam name="TEntity">The type to get the collection of.</typeparam>
        /// <param name="url">The url to use to get the collection from.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        /// <returns>Returns a MongoCollection from the specified type and url.</returns>
        public static IMongoCollection<TEntity> GetCollectionFromUrl<TEntity>(MongoUrl url,
                                                                              string username,
                                                                              SecureString password)
            where TEntity : IEntity<TKey>
        {
            return GetCollectionFromUrl<TEntity>(url, GetCollectionName<TEntity>(), username, password);
        }

        /// <summary>
        /// Creates and returns a MongoCollection from the specified type and url.
        /// </summary>
        /// <typeparam name="TEntity">The type to get the collection of.</typeparam>
        /// <param name="url">The url to use to get the collection from.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        /// <returns>Returns a MongoCollection from the specified type and url.</returns>
        public static IMongoCollection<TEntity> GetCollectionFromUrl<TEntity>(MongoUrl url,
                                                                              string collectionName,
                                                                              string username,
                                                                              SecureString password)
            where TEntity : IEntity<TKey>
        {
            return GetDatabaseFromUrl(url, username, password)
                .GetCollection<TEntity>(collectionName);
        }

        /// <summary>
        /// Determines the collectionname for T and assures it is not empty
        /// </summary>
        /// <typeparam name="TEntity">The type to determine the collectionname for.</typeparam>
        /// <returns>Returns the collectionname for T.</returns>
        private static string GetCollectionName<TEntity>() where TEntity : IEntity<TKey>
        {
            string collectionName = typeof(TEntity).BaseType == typeof(object)
                ? GetCollectioNameFromInterface<TEntity>()
                : GetCollectionNameFromType(typeof(TEntity));

            if (string.IsNullOrEmpty(collectionName))
            {
                throw new ArgumentException("Collection name cannot be empty for this entity");
            }
            return collectionName;
        }

        /// <summary>
        /// Determines the collectionname from the specified type.
        /// </summary>
        /// <typeparam name="TEntity">The type to get the collectionname from.</typeparam>
        /// <returns>Returns the collectionname from the specified type.</returns>
        private static string GetCollectioNameFromInterface<TEntity>()
        {
            // Check to see if the object (inherited from Entity) has a CollectionName attribute
            var att = Attribute.GetCustomAttribute(typeof(TEntity), typeof(CollectionName));
            string collectionname = att != null ? ((CollectionName)att).Name : typeof(TEntity).Name;

            return collectionname;
        }

        /// <summary>
        /// Determines the collectionname from the specified type.
        /// </summary>
        /// <param name="entitytype">The type of the entity to get the collectionname from.</param>
        /// <returns>Returns the collectionname from the specified type.</returns>
        private static string GetCollectionNameFromType(Type entitytype)
        {
            if (entitytype == null)
                throw new ArgumentNullException(nameof(entitytype));

            // Check to see if the object (inherited from Entity) has a CollectionName attribute
            var att = Attribute.GetCustomAttribute(entitytype, typeof(CollectionName));
            string collectionname = att != null ? ((CollectionName)att).Name : GetBaseEntity(entitytype).Name;

            return collectionname;
        }

        private static Type GetBaseEntity(Type entitytype)
        {
            if (!typeof(Entity).IsAssignableFrom(entitytype))
                return entitytype;
            if (entitytype.BaseType == typeof(Entity))
                return entitytype;
            // No attribute found, get the basetype
            return GetBaseEntity(entitytype.BaseType);
        }
    }
}