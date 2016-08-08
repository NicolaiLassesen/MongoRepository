using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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
        public MongoRepository()
            : this(Util<TKey>.GetDefaultConnectionString())
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        public MongoRepository(string connectionString)
        {
            Collection = Util<TKey>.GetCollectionFromConnectionString<TEntity>(connectionString);
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        public MongoRepository(string connectionString, string collectionName)
        {
            Collection = Util<TKey>.GetCollectionFromConnectionString<TEntity>(connectionString, collectionName);
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        public MongoRepository(MongoUrl url)
        {
            Collection = Util<TKey>.GetCollectionFromUrl<TEntity>(url);
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        public MongoRepository(MongoUrl url, string collectionName)
        {
            Collection = Util<TKey>.GetCollectionFromUrl<TEntity>(url, collectionName);
        }

        /// <summary>
        /// Gets the Mongo collection (to perform advanced operations).
        /// </summary>
        /// <remarks>
        /// One can argue that exposing this property (and with that, access to it's Database property for instance
        /// (which is a "parent")) is not the responsibility of this class. Use of this property is highly discouraged;
        /// for most purposes you can use the MongoRepositoryManager&lt;T&gt;
        /// </remarks>
        /// <value>The Mongo collection (to perform advanced operations).</value>
        public IMongoCollection<TEntity> Collection { get; }

        /// <summary>
        /// Gets the name of the collection
        /// </summary>
        public string CollectionName => Collection.CollectionNamespace.CollectionName;

        /// <summary>
        /// Returns the entity by its given id.
        /// </summary>
        /// <param name="id">The Id of the entity to retrieve.</param>
        /// <returns>The entity.</returns>
        public virtual async Task<TEntity> GetById(TKey id)
        {
            //if (typeof(TEntity).IsSubclassOf(typeof(Entity)))
            //{
            //    return await GetById(new ObjectId(id as string));
            //}
            var filter = Builders<TEntity>.Filter.Eq(f => f.Id, id);
            return await Collection.Find(filter).FirstOrDefaultAsync();
        }

        ///// <summary>
        ///// Returns the entity by its object id.
        ///// </summary>
        ///// <param name="id">The Id of the entity to retrieve.</param>
        ///// <returns>The Entity T.</returns>
        //public virtual async Task<TEntity> GetById(ObjectId id)
        //{
        //    var filter = Builders<Entity>.Filter.Eq(f => f.Id, id);
        //    var entity = await collection.FindOneByIdAs<TEntity>(id);
        //}

        /// <summary>
        /// Adds the new entity in the repository.
        /// </summary>
        /// <param name="entity">The entity T.</param>
        /// <returns>The added entity including its new ObjectId.</returns>
        public virtual async Task<TEntity> Add(TEntity entity)
        {
            await Collection.InsertOneAsync(entity);
            // TODO: Did the entity get updated with an ID?
            return entity;
        }

        /// <summary>
        /// Adds the new entities in the repository.
        /// </summary>
        /// <param name="entities">The entities of type T.</param>
        public virtual async Task Add(IEnumerable<TEntity> entities)
        {
            await Collection.InsertManyAsync(entities);
        }

        /// <summary>
        /// Upserts an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The updated entity.</returns>
        public virtual async Task<TEntity> Update(TEntity entity)
        {
            var filter = Builders<TEntity>.Filter.Eq(f => f.Id, entity.Id);
            await Collection.ReplaceOneAsync(filter, entity);
            return entity;
        }

        /// <summary>
        /// Upserts the entities.
        /// </summary>
        /// <param name="entities">The entities to update.</param>
        public virtual async Task Update(IEnumerable<TEntity> entities)
        {
            foreach (TEntity entity in entities)
            {
                var filter = Builders<TEntity>.Filter.Eq(f => f.Id, entity.Id);
                await Collection.ReplaceOneAsync(filter, entity);
            }
        }

        /// <summary>
        /// Deletes an entity from the repository by its id.
        /// </summary>
        /// <param name="id">The entity's id.</param>
        public virtual async Task Delete(TKey id)
        {
            var filter = Builders<TEntity>.Filter.Eq(f => f.Id, id);
            await Collection.DeleteOneAsync(filter);
            //if (typeof(TEntity).IsSubclassOf(typeof(Entity)))
            //{
            //    collection.Remove(Query.EQ("_id", new ObjectId(id as string)));
            //}
            //else
            //{
            //    collection.Remove(Query.EQ("_id", BsonValue.Create(id)));
            //}
        }

        ///// <summary>
        ///// Deletes an entity from the repository by its ObjectId.
        ///// </summary>
        ///// <param name="id">The ObjectId of the entity.</param>
        //public virtual void Delete(ObjectId id)
        //{
        //    collection.Remove(Query.EQ("_id", id));
        //}

        /// <summary>
        /// Deletes the given entity.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        public virtual async Task Delete(TEntity entity)
        {
            var filter = Builders<TEntity>.Filter.Eq(f => f.Id, entity.Id);
            await Collection.DeleteOneAsync(filter);
        }

        /// <summary>
        /// Deletes the entities matching the predicate.
        /// </summary>
        /// <param name="predicate">The expression.</param>
        public virtual async Task Delete(Expression<Func<TEntity, bool>> predicate)
        {
            var filter = Builders<TEntity>.Filter.Where(predicate);
            await Collection.DeleteManyAsync(filter);
        }

        /// <summary>
        /// Deletes all entities in the repository.
        /// </summary>
        public virtual async Task DeleteAll()
        {
            await Collection.DeleteManyAsync(new BsonDocument());
        }

        /// <summary>
        /// Counts the total entities in the repository.
        /// </summary>
        /// <returns>Count of entities in the collection.</returns>
        public virtual async Task<long> Count()
        {
            return await Collection.CountAsync(new BsonDocument());
        }

        /// <summary>
        /// Counts the entities matching the predicate.
        /// </summary>
        /// <param name="predicate">The expression.</param>
        /// <returns>Count of entities in the repository.</returns>
        public virtual async Task<long> Count(Expression<Func<TEntity, bool>> predicate)
        {
            var filter = Builders<TEntity>.Filter.Where(predicate);
            return await Collection.CountAsync(filter);
        }

        /// <summary>
        /// Checks if the entity exists for given predicate.
        /// </summary>
        /// <param name="predicate">The expression.</param>
        /// <returns>True when an entity matching the predicate exists, false otherwise.</returns>
        public virtual async Task<bool> Exists(Expression<Func<TEntity, bool>> predicate)
        {
            var filter = Builders<TEntity>.Filter.Where(predicate);
            return await Collection.Find(filter).Limit(1).AnyAsync();
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
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
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
        public MongoRepository()
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        public MongoRepository(MongoUrl url)
            : base(url)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="url">Url to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        public MongoRepository(MongoUrl url, string collectionName)
            : base(url, collectionName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        public MongoRepository(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoRepository class.
        /// </summary>
        /// <param name="connectionString">Connectionstring to use for connecting to MongoDB.</param>
        /// <param name="collectionName">The name of the collection to use.</param>
        public MongoRepository(string connectionString, string collectionName)
            : base(connectionString, collectionName)
        {
        }
    }
}