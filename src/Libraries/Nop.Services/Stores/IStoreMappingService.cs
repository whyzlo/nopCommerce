using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Nop.Core;
using Nop.Core.Domain.Stores;

namespace Nop.Services.Stores
{
    /// <summary>
    /// Store mapping service interface
    /// </summary>
    public partial interface IStoreMappingService
    {
        /// <summary>
        /// Get an expression predicate to apply a store mapping
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <typeparam name="TEntity">Type of entity with supported store mapping</typeparam>
        /// <returns>Lambda expression</returns>
        Expression<Func<TEntity, bool>> ApplyStoreMapping<TEntity>(int storeId) where TEntity : BaseEntity, IStoreMappingSupported;

        /// <summary>
        /// Deletes a store mapping record
        /// </summary>
        /// <param name="storeMapping">Store mapping record</param>
        void DeleteStoreMapping(StoreMapping storeMapping);

        /// <summary>
        /// Gets a store mapping record
        /// </summary>
        /// <param name="storeMappingId">Store mapping record identifier</param>
        /// <returns>Store mapping record</returns>
        StoreMapping GetStoreMappingById(int storeMappingId);

        /// <summary>
        /// Gets store mapping records
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="entity">Entity</param>
        /// <returns>Store mapping records</returns>
        IList<StoreMapping> GetStoreMappings<T>(T entity) where T : BaseEntity, IStoreMappingSupported;

        /// <summary>
        /// Inserts a store mapping record
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="storeId">Store id</param>
        /// <param name="entity">Entity</param>
        void InsertStoreMapping<T>(T entity, int storeId) where T : BaseEntity, IStoreMappingSupported;

        /// <summary>
        /// Get a value indicating whether a store mapping exists for an entity type
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <typeparam name="T">Entity type</typeparam>
        /// <returns>True if exists; otherwise false</returns>
        bool IsEntityMappingExists<T>(int storeId) where T : BaseEntity, IStoreMappingSupported;

        /// <summary>
        /// Find store identifiers with granted access (mapped to the entity)
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="entity">Entity</param>
        /// <returns>Store identifiers</returns>
        int[] GetStoresIdsWithAccess<T>(T entity) where T : BaseEntity, IStoreMappingSupported;

        /// <summary>
        /// Authorize whether entity could be accessed in the current store (mapped to this store)
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="entity">Entity</param>
        /// <returns>true - authorized; otherwise, false</returns>
        bool Authorize<T>(T entity) where T : BaseEntity, IStoreMappingSupported;

        /// <summary>
        /// Authorize whether entity could be accessed in a store (mapped to this store)
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>true - authorized; otherwise, false</returns>
        bool Authorize<T>(T entity, int storeId) where T : BaseEntity, IStoreMappingSupported;
    }
}