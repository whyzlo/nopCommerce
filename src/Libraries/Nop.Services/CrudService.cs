using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Caching.Extensions;
using Nop.Services.Events;

namespace Nop.Services
{
    /// <summary>
    /// Represents the default CRUD service implementation
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract class CrudService<TEntity> : ICrudService<TEntity>
        where TEntity : BaseEntity
    {
        protected readonly IEventPublisher _eventPublisher;
        protected readonly IRepository<TEntity> _repository;

        protected CrudService()
        {
            _eventPublisher = EngineContext.Current.Resolve<IEventPublisher>();
            _repository = EngineContext.Current.Resolve<IRepository<TEntity>>();
        }

        /// <summary>
        /// Get all database item
        /// </summary>
        /// <returns>Database items</returns>
        public virtual IList<TEntity> GetAll(Func<IQueryable<TEntity>, IList<TEntity>> predicate = null)
        {
            return predicate ==null ? _repository.Table.ToList() : predicate(_repository.Table);
        }

        /// <summary>
        /// Get the database item 
        /// </summary>
        /// <param name="id">Database item identifier</param>
        /// <param name="cached">Use the cache system</param>
        /// <returns>Database item</returns>
        public virtual TEntity GetById(int id, bool cached = true)
        {
            if (id == 0)
                return null;

            return cached ? _repository.ToCachedGetById(id) : _repository.GetById(id);
        }

        /// <summary>
        /// Get the database items by identifiers
        /// </summary>
        /// <param name="ids">Database item identifiers</param>
        /// <returns>Database items</returns>
        public virtual IList<TEntity> GetByIds(int[] ids)
        {
            if (ids == null || ids.Length == 0)
                return new List<TEntity>();

            var query = from bc in _repository.Table
                where ids.Contains(bc.Id)
                select bc;
            var comments = query.ToList();

            //sort by passed identifiers
            var sortedComments = new List<TEntity>();
            foreach (var id in ids)
            {
                var comment = comments.Find(x => x.Id == id);
                if (comment != null)
                    sortedComments.Add(comment);
            }

            return sortedComments;
        }

        /// <summary>
        /// Delete the database item
        /// </summary>
        /// <param name="entity">Database item</param>
        public virtual void Delete(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _repository.Delete(entity);

            //event notification
            _eventPublisher.EntityDeleted(entity);
        }

        /// <summary>
        /// Deletes database items
        /// </summary>
        /// <param name="entities">Database items</param>
        public virtual void Delete(IList<TEntity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            _repository.Delete(entities);

            //event notification
            foreach (var entity in entities) 
                _eventPublisher.EntityDeleted(entity);
        }

        /// <summary>
        /// Insert the database item 
        /// </summary>
        /// <param name="entity">Database item</param>
        public virtual void Insert(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _repository.Insert(entity);

            //event notification
            _eventPublisher.EntityInserted(entity);
        }

        /// <summary>
        /// Update the review type
        /// </summary>
        /// <param name="entity">Database item</param>
        public virtual void Update(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _repository.Update(entity);

            //event notification
            _eventPublisher.EntityUpdated(entity);
        }
    }
}