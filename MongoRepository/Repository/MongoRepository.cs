using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;

// ReSharper disable once CheckNamespace

namespace MongoRepository
{
    /// <summary>
    /// Deals with entities in MongoDb.
    /// </summary>
    /// <typeparam name="TEntity">The type contained in the repository.</typeparam>
    /// <typeparam name="TKey">The type used for the entity's Id.</typeparam>
    public class MongoRepository<TEntity, TKey> : IRepository<TEntity, TKey>
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// Uses the Default App/Web.Config connectionstrings to fetch the connectionString and Database name.
        /// </summary>
        /// <remarks>Default constructor defaults to "MongoServerSettings" key for connectionstring.</remarks>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepository(string username, SecureString password)
            : this(Util<TKey>.GetDefaultConnectionString(), username, password)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepository(string connectionString, string username, SecureString password)
        {
            Collection = Util<TKey>.GetCollectionFromConnectionString<TEntity>(connectionString, username, password);
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepository(string connectionString, string collectionName, string username, SecureString password)
        {
            Collection = Util<TKey>.GetCollectionFromConnectionString<TEntity>(connectionString,
                                                                               collectionName,
                                                                               username,
                                                                               password);
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepository(MongoUrl url, string username, SecureString password)
        {
            Collection = Util<TKey>.GetCollectionFromUrl<TEntity>(url, username, password);
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepository(MongoUrl url, string collectionName, string username, SecureString password)
        {
            Collection = Util<TKey>.GetCollectionFromUrl<TEntity>(url, collectionName, username, password);
        }

        /// <summary>
        /// Gets the name of the collection
        /// </summary>
        public string RepositoryName => Collection.CollectionNamespace.CollectionName;

        /// <summary>
        /// Returns the entity by its given id.
        /// </summary>
        /// <param name="id">The Id of the entity to retrieve.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The entity.</returns>
        public virtual async Task<TEntity> GetByIdAsync(TKey id,
                                                        CancellationToken cancellationToken = default(CancellationToken))
        {
            var filter = Builders<TEntity>.Filter.Eq(f => f.Id, id);
            return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Adds the new entity in the repository.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>The added entity including its new ObjectId.</returns>
        public TEntity Add(TEntity entity)
        {
            Collection.InsertOne(entity);
            return entity;
        }

        /// <summary>
        /// Adds the new entities in the repository.
        /// </summary>
        /// <param name="entities">The entities of type T.</param>
        public void Add(IEnumerable<TEntity> entities)
        {
            Collection.InsertMany(entities);
        }

        /// <summary>
        /// Adds the new entity in the repository.
        /// </summary>
        /// <param name="entity">The entity T.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The added entity including its new ObjectId.</returns>
        public virtual async Task<TEntity> AddAsync(TEntity entity,
                                                    CancellationToken cancellationToken = default(CancellationToken))
        {
            await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
            return entity;
        }

        /// <summary>
        /// Adds the new entities in the repository.
        /// </summary>
        /// <param name="entities">The entities of type T.</param>
        /// <param name="cancellationToken"></param>
        public virtual async Task AddAsync(IEnumerable<TEntity> entities,
                                           CancellationToken cancellationToken = default(CancellationToken))
        {
            await Collection.InsertManyAsync(entities, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Upserts an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The updated entity.</returns>
        public virtual async Task<TEntity> UpdateAsync(TEntity entity,
                                                       CancellationToken cancellationToken = default(CancellationToken))
        {
            var filter = Builders<TEntity>.Filter.Eq(f => f.Id, entity.Id);
            await Collection.ReplaceOneAsync(filter, entity, new UpdateOptions {IsUpsert = true}, cancellationToken);
            return entity;
        }

        /// <summary>
        /// Upserts the entities.
        /// </summary>
        /// <param name="entities">The entities to update.</param>
        /// <param name="cancellationToken"></param>
        public virtual async Task UpdateAsync(IEnumerable<TEntity> entities,
                                              CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (TEntity entity in entities)
            {
                var filter = Builders<TEntity>.Filter.Eq(f => f.Id, entity.Id);
                await Collection.ReplaceOneAsync(filter, entity, new UpdateOptions {IsUpsert = true}, cancellationToken);
            }
        }

        /// <summary>
        /// Deletes an entity from the repository by its id.
        /// </summary>
        /// <param name="id">The entity's id.</param>
        /// <param name="cancellationToken"></param>
        public virtual async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default(CancellationToken))
        {
            var filter = Builders<TEntity>.Filter.Eq(f => f.Id, id);
            await Collection.DeleteOneAsync(filter, cancellationToken);
        }

        /// <summary>
        /// Deletes the given entity.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="cancellationToken"></param>
        public virtual async Task DeleteAsync(TEntity entity,
                                              CancellationToken cancellationToken = default(CancellationToken))
        {
            var filter = Builders<TEntity>.Filter.Eq(f => f.Id, entity.Id);
            await Collection.DeleteOneAsync(filter, cancellationToken);
        }

        /// <summary>
        /// Deletes the entities matching the predicate.
        /// </summary>
        /// <param name="predicate">The expression.</param>
        /// <param name="cancellationToken"></param>
        public virtual async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate,
                                              CancellationToken cancellationToken = default(CancellationToken))
        {
            var filter = Builders<TEntity>.Filter.Where(predicate);
            await Collection.DeleteManyAsync(filter, cancellationToken);
        }

        /// <summary>
        /// Deletes all entities in the repository.
        /// </summary>
        /// <param name="cancellationToken"></param>
        public virtual async Task DeleteAllAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await Collection.DeleteManyAsync(new BsonDocument(), cancellationToken);
        }

        /// <summary>
        /// Counts the total entities in the repository.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Count of entities in the collection.</returns>
        public virtual async Task<long> CountAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Collection.CountAsync(new BsonDocument(), cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Counts the entities matching the predicate.
        /// </summary>
        /// <param name="predicate">The expression.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Count of entities in the repository.</returns>
        public virtual async Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate,
                                                   CancellationToken cancellationToken = default(CancellationToken))
        {
            var filter = Builders<TEntity>.Filter.Where(predicate);
            return await Collection.CountAsync(filter, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Checks if the entity exists for given predicate.
        /// </summary>
        /// <param name="predicate">The expression.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>True when an entity matching the predicate exists, false otherwise.</returns>
        public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate,
                                                    CancellationToken cancellationToken = default(CancellationToken))
        {
            //var filter = Builders<TEntity>.Filter.Where(predicate);
            var test = await Collection.Find(predicate).Limit(1).ToListAsync();
            return test.Any();
        }

        #region IQueryable<T>

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An IEnumerator&lt;T&gt; object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<TEntity> GetEnumerator()
        {
            return Collection.AsQueryable().GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An IEnumerator object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Collection.AsQueryable().GetEnumerator();
        }

        /// <summary>
        /// Gets the type of the element(s) that are returned when the expression tree associated with this instance of IQueryable is executed.
        /// </summary>
        public virtual Type ElementType => Collection.AsQueryable().ElementType;

        /// <summary>
        /// Gets the expression tree that is associated with the instance of IQueryable.
        /// </summary>
        public virtual Expression Expression => Collection.AsQueryable().Expression;

        /// <summary>
        /// Gets the query provider that is associated with this data source.
        /// </summary>
        public virtual IQueryProvider Provider => Collection.AsQueryable().Provider;

        #endregion

        /// <summary>
        /// Gets the Mongo collection (to perform advanced operations).
        /// </summary>
        /// <value>The Mongo collection (to perform advanced operations).</value>
        protected IMongoCollection<TEntity> Collection { get; }
    }

    /// <summary>
    /// Deals with entities in MongoDb.
    /// </summary>
    /// <typeparam name="T">The type contained in the repository.</typeparam>
    /// <remarks>Entities are assumed to use strings for Id's.</remarks>
    public class MongoRepository<T> : MongoRepository<T, string>, IRepository<T>
        where T : IEntity<string>
    {
        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// Uses the Default App/Web.Config connectionstrings to fetch the connectionString and Database name.
        /// </summary>
        /// <remarks>Default constructor defaults to "MongoServerSettings" key for connectionstring.</remarks>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepository(string username, SecureString password)
            : base(username, password)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepository(MongoUrl url, string username, SecureString password)
            : base(url, username, password)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepository(MongoUrl url, string collectionName, string username, SecureString password)
            : base(url, collectionName, username, password)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepository(string connectionString, string username, SecureString password)
            : base(connectionString, username, password)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        /// <param name="username">Username with access to the database</param>
        /// <param name="password">Password for authenticating user</param>
        public MongoRepository(string connectionString, string collectionName, string username, SecureString password)
            : base(connectionString, collectionName, username, password)
        {
        }
    }

    public static class MongoRepository
    {
        public static BsonMemberMap SetDictionarySerializer(this BsonMemberMap memberMap,
                                                            DictionaryRepresentation representation)
        {
            var serializer = ConfigureSerializer(memberMap.GetSerializer(), representation);
            return memberMap.SetSerializer(serializer);
        }

        private static IBsonSerializer ConfigureSerializer(IBsonSerializer serializer,
                                                           DictionaryRepresentation representation)
        {
            var dictionaryRepresentationConfigurable = serializer as IDictionaryRepresentationConfigurable;
            if (dictionaryRepresentationConfigurable != null)
            {
                serializer = dictionaryRepresentationConfigurable.WithDictionaryRepresentation(representation);
            }

            var childSerializerConfigurable = serializer as IChildSerializerConfigurable;
            return childSerializerConfigurable == null
                ? serializer
                : childSerializerConfigurable.WithChildSerializer(
                    ConfigureSerializer(childSerializerConfigurable.ChildSerializer, representation));
        }
    }
}