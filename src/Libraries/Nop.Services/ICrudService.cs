using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;

namespace Nop.Services
{
    /// <summary>
    /// Represents a CRUD service
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public partial interface ICrudService<TEntity> where TEntity : BaseEntity
    {
        /// <summary>
        /// Delete the database item
        /// </summary>
        /// <param name="entity">Database item</param>
        void Delete(TEntity entity);

        /// <summary>
        /// Deletes database items
        /// </summary>
        /// <param name="entities">Database items</param>
        void Delete(IList<TEntity> entities);

        /// <summary>
        /// Get all database item
        /// </summary>
        /// <returns>Database items</returns>
        IList<TEntity> GetAll(Func<IQueryable<TEntity>, IList<TEntity>> predicate);

        /// <summary>
        /// Get the database item 
        /// </summary>
        /// <param name="id">Database item identifier</param>
        /// <param name="cached">Use the cache system</param>
        /// <returns>Database item</returns>
        TEntity GetById(int id, bool cached = true);

        /// <summary>
        /// Get the database items by identifiers
        /// </summary>
        /// <param name="ids">Database item identifiers</param>
        /// <returns>Database items</returns>
        IList<TEntity> GetByIds(int[] ids);

        /// <summary>
        /// Insert the database item 
        /// </summary>
        /// <param name="entity">Database item</param>
        void Insert(TEntity entity);

        /// <summary>
        /// Update the review type
        /// </summary>
        /// <param name="entity">Database item</param>
        void Update(TEntity entity);
    }
}
