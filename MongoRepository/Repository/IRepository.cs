using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace

namespace MongoRepository
{
    /// <summary>
    /// IRepository definition.
    /// </summary>
    /// <typeparam name="TEntity">The type contained in the repository.</typeparam>
    /// <typeparam name="TKey">The type used for the entity's Id.</typeparam>
    public interface IRepository<TEntity, in TKey> : IQueryable<TEntity>
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Gets the name of the repository
        /// </summary>
        string RepositoryName { get; }

        /// <summary>
        /// Returns the entity by its given id.
        /// </summary>
        /// <param name="id">The value representing the ObjectId of the entity to retrieve.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The Entity</returns>
        Task<TEntity> GetByIdAsync(TKey id, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Adds the new entity in the repository.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>The added entity including its new ObjectId.</returns>
        TEntity Add(TEntity entity);

        /// <summary>
        /// Adds the new entities in the repository.
        /// </summary>
        /// <param name="entities">The entities of type T.</param>
        void Add(IEnumerable<TEntity> entities);

        /// <summary>
        /// Adds the new entity in the repository.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The added entity including its new ObjectId.</returns>
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Adds the new entities in the repository.
        /// </summary>
        /// <param name="entities">The entities of type T.</param>
        /// <param name="cancellationToken"></param>
        Task AddAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Upserts an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The updated entity.</returns>
        Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Upserts the entities.
        /// </summary>
        /// <param name="entities">The entities to update.</param>
        /// <param name="cancellationToken"></param>
        Task UpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes an entity from the repository by its id.
        /// </summary>
        /// <param name="id">The entity's id.</param>
        /// <param name="cancellationToken"></param>
        Task DeleteAsync(TKey id, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes the given entity.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="cancellationToken"></param>
        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes the entities matching the predicate.
        /// </summary>
        /// <param name="predicate">The expression.</param>
        /// <param name="cancellationToken"></param>
        Task DeleteAsync(Expression<Func<TEntity, bool>> predicate,
                         CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes all entities in the repository.
        /// </summary>
        /// <param name="cancellationToken"></param>
        Task DeleteAllAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Counts the total entities in the repository.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Count of entities in the repository.</returns>
        Task<long> CountAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Counts the entities matching the predicate.
        /// </summary>
        /// <param name="predicate">The expression.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Count of entities in the repository.</returns>
        Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate,
                              CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Checks if the entity exists for given predicate.
        /// </summary>
        /// <param name="predicate">The expression.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>True when an entity matching the predicate exists, false otherwise.</returns>
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate,
                               CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// IRepository definition.
    /// </summary>
    /// <typeparam name="T">The type contained in the repository.</typeparam>
    /// <remarks>Entities are assumed to use strings for Id's.</remarks>
    public interface IRepository<T> : IRepository<T, string>
        where T : IEntity<string>
    {
    }
}