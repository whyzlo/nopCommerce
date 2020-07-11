using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    /// <typeparam name="TEntity">Entity type</typeparam>
    public partial class Service<TEntity> : IService<TEntity> where TEntity : BaseEntity
    {
        #region Fields

        //TODO: check circular component dependency error
        //private readonly ICacheKeyService _cacheKeyService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IRepository<TEntity> _repository;
        private readonly IStaticCacheManager _staticCacheManager;

        #endregion

        #region Ctor

        public Service(
            //ICacheKeyService cacheKeyService,
            IEventPublisher eventPublisher,
            IRepository<TEntity> repository,
            IStaticCacheManager staticCacheManager)
        {
            //_cacheKeyService = cacheKeyService;
            _eventPublisher = eventPublisher;
            _repository = repository;
            _staticCacheManager = staticCacheManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get the entity entry
        /// </summary>
        /// <param name="id">Entity entry identifier</param>
        /// <param name="cache">Whether to cache entry</param>
        /// <param name="cacheTime">Cache time in minutes; pass null to use default value</param>
        /// <returns>Entity entry</returns>
        public virtual TEntity GetById(int id, bool cache = true, int? cacheTime = null)
        {
            if (id == 0)
                return null;

            if (!cache)
                return _repository.GetById(id);

            //caching
            var cacheKey = new CacheKey(BaseEntity.GetEntityCacheKey(typeof(TEntity), id));
            if (cacheTime.HasValue)
                cacheKey.CacheTime = cacheTime.Value;
            else
                cacheKey = EngineContext.Current.Resolve<ICacheKeyService>().PrepareKeyForDefaultCache(cacheKey);

            return _staticCacheManager.Get(cacheKey, () => _repository.GetById(id));
        }

        /// <summary>
        /// Get entity entries by identifiers
        /// </summary>
        /// <param name="ids">Entity entry identifiers</param>
        /// <param name="cache">Whether to cache entry</param>
        /// <param name="cacheTime">Cache time in minutes; pass null to use default value</param>
        /// <returns>Entity entries</returns>
        public virtual IList<TEntity> GetByIds(IEnumerable<int> ids, bool cache = true, int? cacheTime = null)
        {
            if (!ids?.Any() ?? true)
                return new List<TEntity>();

            IList<TEntity> getbyIds()
            {
                //TODO: need refactoring, details here https://github.com/nopSolutions/nopCommerce/issues/4897
                //get entries 
                var entries = _repository.Table.Where(entry => ids.Contains(entry.Id)).ToList();

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
        /// <param name="func">Function to select entries</param>
        /// <param name="cacheKeyFunc">Function to get cache key; pass null to not cache entries</param>
        /// <returns>Entity entries</returns>
        public virtual IList<TEntity> GetAll(Func<IQueryable<TEntity>, IQueryable<TEntity>> func = null, 
            Func<ICacheKeyService, CacheKey> cacheKeyFunc = null)
        {
            var query = _repository.Table;
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
        /// <param name="func">Function to select entries</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="getOnlyTotalCount">Whether to get only the total number of entries without actually loading data</param>
        /// <returns>Paged list of entity entries</returns>
        public virtual IPagedList<TEntity> GetAllPaged(Func<IQueryable<TEntity>, IQueryable<TEntity>> func = null,
            int pageIndex = 0, int pageSize = int.MaxValue, bool getOnlyTotalCount = false)
        {
            var query = _repository.Table;
            if (func != null)
                query = func.Invoke(query);

            return new PagedList<TEntity>(query, pageIndex, pageSize, getOnlyTotalCount);
        }

        /// <summary>
        /// Insert the entity entry
        /// </summary>
        /// <param name="entity">Entity entry</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        public virtual void Insert(TEntity entity, bool publishEvent = true)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _repository.Insert(entity);

            //event notification
            if (publishEvent)
                _eventPublisher.EntityInserted(entity);
        }

        /// <summary>
        /// Insert entity entries
        /// </summary>
        /// <param name="entities">Entity entries</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        public virtual void Insert(IEnumerable<TEntity> entities, bool publishEvent = true)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            _repository.Insert(entities);

            //event notification
            if (publishEvent)
            {
                foreach (var entity in entities)
                {
                    _eventPublisher.EntityInserted(entity);
                }
            }
        }

        /// <summary>
        /// Update the entity entry
        /// </summary>
        /// <param name="entity">Entity entry</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        public virtual void Update(TEntity entity, bool publishEvent = true)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _repository.Update(entity);

            //event notification
            if (publishEvent)
                _eventPublisher.EntityUpdated(entity);
        }

        /// <summary>
        /// Update entity entries
        /// </summary>
        /// <param name="entities">Entity entries</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        public virtual void Update(IEnumerable<TEntity> entities, bool publishEvent = true)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            foreach (var entity in entities)
            {
                Update(entity, publishEvent);
            }
        }

        /// <summary>
        /// Delete the entity entry
        /// </summary>
        /// <param name="entity">Entity entry</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        public virtual void Delete(TEntity entity, bool publishEvent = true)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity is ISoftDeletedEntity softDeletedEntity)
            {
                softDeletedEntity.Deleted = true;
                _repository.Update(entity);
            }
            else
                _repository.Delete(entity);

            //event notification
            if (publishEvent)
                _eventPublisher.EntityDeleted(entity);
        }

        /// <summary>
        /// Delete entity entries
        /// </summary>
        /// <param name="entities">Entity entries</param>
        /// <param name="publishEvent">Whether to publish event notification</param>
        public virtual void Delete(IEnumerable<TEntity> entities, bool publishEvent = true)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            if (entities.OfType<ISoftDeletedEntity>().Any())
            {
                foreach (var entity in entities)
                {
                    if (entity is ISoftDeletedEntity softDeletedEntity)
                    {
                        softDeletedEntity.Deleted = true;
                        _repository.Update(entity);
                    }
                }
            }
            else
                _repository.Delete(entities);

            //event notification
            if (publishEvent)
            {
                foreach (var entity in entities)
                {
                    _eventPublisher.EntityDeleted(entity);
                }
            }
        }

        /// <summary>
        /// Delete entity entries (soft deletion and event notification are not supported)
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition</param>
        public virtual void Delete(Expression<Func<TEntity, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _repository.Delete(predicate);
        }

        #endregion
    }
}