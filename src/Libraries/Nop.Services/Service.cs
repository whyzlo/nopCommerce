using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Common;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Caching;
using Nop.Services.Caching.Extensions;
using Nop.Services.Events;

namespace Nop.Services
{
    /// <summary>
    /// Represents default service with common CRUD methods implementation
    /// </summary>
    public partial class Service : IService
    {
        #region Fields

        //TODO: check circular component dependency error
        //private readonly ICacheKeyService _cacheKeyService;
        private readonly IEventPublisher _eventPublisher;
        private readonly INopDataProvider _dataProvider;
        private readonly IStaticCacheManager _staticCacheManager;

        #endregion

        #region Ctor

        public Service(
            //ICacheKeyService cacheKeyService,
            IEventPublisher eventPublisher,
            INopDataProvider dataProvider,
            IStaticCacheManager staticCacheManager)
        {
            //_cacheKeyService = cacheKeyService;
            _eventPublisher = eventPublisher;
            _dataProvider = dataProvider;
            _staticCacheManager = staticCacheManager;
        }

        /// <summary>
        /// Get the entity entry
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="id">Entity entry identifier</param>
        /// <param name="cache">Whether to cache entry</param>
        /// <param name="cacheTime">Cache time in minutes; pass null to use default value</param>
        /// <returns>Entity entry</returns>
        public virtual TEntity GetById<TEntity>(int id, bool cache = true, int? cacheTime = null)
            where TEntity : BaseEntity
        {
            if (id == 0)
                return null;

            TEntity getEntity()
            {
                var table = _dataProvider.GetTable<TEntity>();

                return table.FirstOrDefault(e => e.Id == Convert.ToInt32(id));
            }

            if (!cache)
                return getEntity();

            //caching
            var cacheKey = new CacheKey(BaseEntity.GetEntityCacheKey(typeof(TEntity), id));
            if (cacheTime.HasValue)
                cacheKey.CacheTime = cacheTime.Value;
            else
                cacheKey = EngineContext.Current.Resolve<ICacheKeyService>().PrepareKeyForDefaultCache(cacheKey);

            return _staticCacheManager.Get(cacheKey, () => getEntity());
        }

        /// <summary>
        /// Get entity entries by identifiers
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="ids">Entity entry identifiers</param>
        /// <param name="cache">Whether to cache entry</param>
        /// <param name="cacheTime">Cache time in minutes; pass null to use default value</param>
        /// <returns>Entity entries</returns>
        public virtual IList<TEntity> GetByIds<TEntity>(IList<int> ids, bool cache = true, int? cacheTime = null)
            where TEntity : BaseEntity
        {
            if (!ids?.Any() ?? true)
                return new List<TEntity>();

            IList<TEntity> getbyIds()
            {
                var table = _dataProvider.GetTable<TEntity>();

                //TODO: need refactoring, details here https://github.com/nopSolutions/nopCommerce/issues/4897
                //get entries 
                var entries = table.Where(entry => ids.Contains(entry.Id)).ToList();

                //sort by passed identifiers
                var sortedEntries = new List<TEntity>();
                foreach (var id in ids)
                {
                    var sortedEntry = entries.FirstOrDefault(entry => entry.Id == id);
                    if (sortedEntry != null)
                        sortedEntries.Add(sortedEntry);
                }

                return sortedEntries;
            }

            if (!cache)
                return getbyIds();

            //caching
            var cacheKey = new CacheKey(BaseEntity.GetEntityCacheKey(typeof(TEntity), ids));
            if (cacheTime.HasValue)
                cacheKey.CacheTime = cacheTime.Value;
            else
                cacheKey = EngineContext.Current.Resolve<ICacheKeyService>().PrepareKeyForDefaultCache(cacheKey);

            return _staticCacheManager.Get(cacheKey, () => getbyIds());
        }

        /// <summary>
        /// Get all entity entries
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="func">Function to select entries</param>
        /// <param name="cacheKeyFunc">Function to get cache key; pass null to not cache entries</param>
        /// <returns>Entity entries</returns>
        public virtual IList<TEntity> GetAll<TEntity>(Func<IQueryable<TEntity>, IQueryable<TEntity>> func = null, 
            Func<ICacheKeyService, CacheKey> cacheKeyFunc = null)
            where TEntity : BaseEntity
        {
            var query = _dataProvider.GetTable<TEntity>() as IQueryable<TEntity>;
            if (func != null)
                query = func.Invoke(query);

            if (cacheKeyFunc == null)
                return query.ToList();

            //TODO: make getting a cache key more simple
            //caching
            var cacheKey = cacheKeyFunc(EngineContext.Current.Resolve<ICacheKeyService>());
            return query.ToCachedList(cacheKey);
        }

        /// <summary>
        /// Get paged list of all entity entries
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="func">Function to select entries</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="getOnlyTotalCount">Whether to get only the total number of entries without actually loading data</param>
        /// <returns>Paged list of entity entries</returns>
        public virtual IPagedList<TEntity> GetAllPaged<TEntity>(Func<IQueryable<TEntity>, IQueryable<TEntity>> func = null,
            int pageIndex = 0, int pageSize = int.MaxValue, bool getOnlyTotalCount = false)
            where TEntity : BaseEntity
        {
            var query = _dataProvider.GetTable<TEntity>() as IQueryable<TEntity>;
            if (func != null)
                query = func.Invoke(query);

            return new PagedList<TEntity>(query, pageIndex, pageSize, getOnlyTotalCount);
        }

        /// <summary>
        /// Insert the entity entry
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="entity">Entity entry</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        public virtual void Insert<TEntity>(TEntity entity, bool publishEvent = true)
            where TEntity : BaseEntity
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dataProvider.InsertEntity(entity);

            //event notification
            if (publishEvent)
                _eventPublisher.EntityInserted(entity);
        }

        /// <summary>
        /// Insert entity entries
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="entities">Entity entries</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        public virtual void Insert<TEntity>(IList<TEntity> entities, bool publishEvent = true)
            where TEntity : BaseEntity
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            using (var transaction = new TransactionScope())
            {
                _dataProvider.BulkInsertEntities(entities);
                transaction.Complete();
            }

            //event notification
            if (publishEvent)
                foreach (var entity in entities)
                    _eventPublisher.EntityInserted(entity);
        }

        /// <summary>
        /// Update the entity entry
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="entity">Entity entry</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        public virtual void Update<TEntity>(TEntity entity, bool publishEvent = true)
            where TEntity : BaseEntity
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dataProvider.UpdateEntity(entity);

            //event notification
            if (publishEvent)
                _eventPublisher.EntityUpdated(entity);
        }

        /// <summary>
        /// Update entity entries
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="entities">Entity entries</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        public virtual void Update<TEntity>(IEnumerable<TEntity> entities, bool publishEvent = true)
            where TEntity : BaseEntity
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            foreach (var entity in entities) 
                Update(entity, publishEvent);
        }

        /// <summary>
        /// Delete the entity entry
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="entity">Entity entry</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        public virtual void Delete<TEntity>(TEntity entity, bool publishEvent = true)
            where TEntity : BaseEntity
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity is ISoftDeletedEntity softDeletedEntity)
            {
                softDeletedEntity.Deleted = true;
                _dataProvider.UpdateEntity(entity);
            }
            else
                _dataProvider.DeleteEntity(entity);

            //event notification
            if (publishEvent)
                _eventPublisher.EntityDeleted(entity);
        }

        /// <summary>
        /// Delete entity entries
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="entities">Entity entries</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        public virtual void Delete<TEntity>(IList<TEntity> entities, bool publishEvent = true)
            where TEntity : BaseEntity
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            if (entities.OfType<ISoftDeletedEntity>().Any())
            {
                foreach (var entity in entities)
                    if (entity is ISoftDeletedEntity softDeletedEntity)
                    {
                        softDeletedEntity.Deleted = true;
                        _dataProvider.UpdateEntity(entity);
                    }
            }
            else
                _dataProvider.BulkDeleteEntities(entities);

            //event notification
            if (!publishEvent)
                return;

            foreach (var entity in entities)
                _eventPublisher.EntityDeleted(entity);
        }

        /// <summary>
        /// Delete entity entries (soft deletion and event notification are not supported)
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="predicate">A function to test each element for a condition</param>
        public virtual void Delete<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : BaseEntity
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _dataProvider.BulkDeleteEntities(predicate);
        }

        #endregion
    }
}