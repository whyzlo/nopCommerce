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
    /// <typeparam name="TEntity">Entity type</typeparam>
    public partial interface IService<TEntity> where TEntity : BaseEntity
    {
        /// <summary>
        /// Get the entity entry
        /// </summary>
        /// <param name="id">Entity entry identifier</param>
        /// <param name="cache">Whether to cache entry</param>
        /// <param name="cacheTime">Cache time in minutes; pass null to use default value</param>
        /// <returns>Entity entry</returns>
        TEntity GetById(int id, bool cache = true, int? cacheTime = null);

        /// <summary>
        /// Get entity entries by identifiers
        /// </summary>
        /// <param name="ids">Entity entry identifiers</param>
        /// <param name="cache">Whether to cache entry</param>
        /// <param name="cacheTime">Cache time in minutes; pass null to use default value</param>
        /// <returns>Entity entries</returns>
        IList<TEntity> GetByIds(IEnumerable<int> ids, bool cache = true, int? cacheTime = null);

        /// <summary>
        /// Get all entity entries
        /// </summary>
        /// <param name="func">Function to select entries</param>
        /// <param name="cacheKeyFunc">Function to get cache key; pass null to not cache entries</param>
        /// <returns>Entity entries</returns>
        IList<TEntity> GetAll(Func<IQueryable<TEntity>, IQueryable<TEntity>> func = null,
            Func<ICacheKeyService, CacheKey> cacheKeyFunc = null);

        /// <summary>
        /// Get paged list of all entity entries (caching is not supported)
        /// </summary>
        /// <param name="func">Function to select entries</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="getOnlyTotalCount">Whether to get only the total number of entries without actually loading data</param>
        /// <returns>Paged list of entity entries</returns>
        IPagedList<TEntity> GetAllPaged(Func<IQueryable<TEntity>, IQueryable<TEntity>> func = null,
            int pageIndex = 0, int pageSize = int.MaxValue, bool getOnlyTotalCount = false);

        /// <summary>
        /// Insert the entity entry
        /// </summary>
        /// <param name="entity">Entity entry</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        void Insert(TEntity entity, bool publishEvent = true);

        /// <summary>
        /// Insert entity entries
        /// </summary>
        /// <param name="entities">Entity entries</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        void Insert(IEnumerable<TEntity> entities, bool publishEvent = true);

        /// <summary>
        /// Update the entity entry
        /// </summary>
        /// <param name="entity">Entity entry</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        void Update(TEntity entity, bool publishEvent = true);

        /// <summary>
        /// Update entity entries
        /// </summary>
        /// <param name="entities">Entity entries</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        void Update(IEnumerable<TEntity> entities, bool publishEvent = true);

        /// <summary>
        /// Delete the entity entry
        /// </summary>
        /// <param name="entity">Entity entry</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        void Delete(TEntity entity, bool publishEvent = true);

        /// <summary>
        /// Delete entity entries
        /// </summary>
        /// <param name="entities">Entity entries</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        void Delete(IEnumerable<TEntity> entities, bool publishEvent = true);

        /// <summary>
        /// Delete entity entries (soft deletion and event notification are not supported)
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition</param>
        void Delete(Expression<Func<TEntity, bool>> predicate);
    }
}