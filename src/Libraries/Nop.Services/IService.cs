using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Services.Caching;

namespace Nop.Services
{
    /// <summary>
    /// Represents a service with common CRUD methods
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Get the entity entry
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="id">Entity entry identifier</param>
        /// <param name="cache">Whether to cache entry</param>
        /// <param name="cacheTime">Cache time in minutes; pass null to use default value</param>
        /// <returns>Entity entry</returns>
        TEntity GetById<TEntity>(int id, bool cache = true, int? cacheTime = null)
            where TEntity : BaseEntity;

        /// <summary>
        /// Get entity entries by identifiers
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="ids">Entity entry identifiers</param>
        /// <param name="cache">Whether to cache entry</param>
        /// <param name="cacheTime">Cache time in minutes; pass null to use default value</param>
        /// <returns>Entity entries</returns>
        IList<TEntity> GetByIds<TEntity>(IList<int> ids, bool cache = true, int? cacheTime = null)
            where TEntity : BaseEntity;

        /// <summary>
        /// Get all entity entries
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="func">Function to select entries</param>
        /// <param name="cacheKeyFunc">Function to get cache key; pass null to not cache entries</param>
        /// <returns>Entity entries</returns>
        IList<TEntity> GetAll<TEntity>(Func<IQueryable<TEntity>, IQueryable<TEntity>> func = null, 
            Func<ICacheKeyService, CacheKey> cacheKeyFunc = null)
            where TEntity : BaseEntity;

        /// <summary>
        /// Get paged list of all entity entries
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="func">Function to select entries</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="getOnlyTotalCount">Whether to get only the total number of entries without actually loading data</param>
        /// <returns>Paged list of entity entries</returns>
        IPagedList<TEntity> GetAllPaged<TEntity>(Func<IQueryable<TEntity>, IQueryable<TEntity>> func = null,
            int pageIndex = 0, int pageSize = int.MaxValue, bool getOnlyTotalCount = false)
            where TEntity : BaseEntity;

        /// <summary>
        /// Insert the entity entry
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="entity">Entity entry</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        void Insert<TEntity>(TEntity entity, bool publishEvent = true)
            where TEntity : BaseEntity;

        /// <summary>
        /// Insert entity entries
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="entities">Entity entries</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        void Insert<TEntity>(IList<TEntity> entities, bool publishEvent = true)
            where TEntity : BaseEntity;

        /// <summary>
        /// Update the entity entry
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="entity">Entity entry</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        void Update<TEntity>(TEntity entity, bool publishEvent = true)
            where TEntity : BaseEntity;

        /// <summary>
        /// Update entity entries
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="entities">Entity entries</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        void Update<TEntity>(IEnumerable<TEntity> entities, bool publishEvent = true)
            where TEntity : BaseEntity;

        /// <summary>
        /// Delete the entity entry
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="entity">Entity entry</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        void Delete<TEntity>(TEntity entity, bool publishEvent = true)
            where TEntity : BaseEntity;

        /// <summary>
        /// Delete entity entries
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="entities">Entity entries</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        void Delete<TEntity>(IList<TEntity> entities, bool publishEvent = true)
            where TEntity : BaseEntity;

        /// <summary>
        /// Delete entity entries (soft deletion and event notification are not supported)
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="predicate">A function to test each element for a condition</param>
        void Delete<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : BaseEntity;
    }
}