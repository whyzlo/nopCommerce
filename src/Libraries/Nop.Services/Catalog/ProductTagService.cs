﻿using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Services.Customers;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Product tag service
    /// </summary>
    public partial class ProductTagService : IProductTagService
    {
        #region Fields

        private readonly CatalogSettings _catalogSettings;
        private readonly IAclService _aclService;
        private readonly ICustomerService _customerService;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<ProductProductTagMapping> _productProductTagMappingRepository;
        private readonly IRepository<ProductTag> _productTagRepository;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public ProductTagService(CatalogSettings catalogSettings,
            IAclService aclService,
            ICustomerService customerService,
            IRepository<Product> productRepository,
            IRepository<ProductProductTagMapping> productProductTagMappingRepository,
            IRepository<ProductTag> productTagRepository,
            IStaticCacheManager staticCacheManager,
            IStoreMappingService storeMappingService,
            IUrlRecordService urlRecordService,
            IWorkContext workContext)
        {
            _catalogSettings = catalogSettings;
            _aclService = aclService;
            _customerService = customerService;
            _productRepository = productRepository;
            _productProductTagMappingRepository = productProductTagMappingRepository;
            _productTagRepository = productTagRepository;
            _staticCacheManager = staticCacheManager;
            _storeMappingService = storeMappingService;
            _urlRecordService = urlRecordService;
            _workContext = workContext;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Delete a product-product tag mapping
        /// </summary>
        /// <param name="productId">Product identifier</param>
        /// <param name="productTagId">Product tag identifier</param>
        protected virtual void DeleteProductProductTagMapping(int productId, int productTagId)
        {
            var mappitngRecord = _productProductTagMappingRepository.Table.FirstOrDefault(pptm => pptm.ProductId == productId && pptm.ProductTagId == productTagId);

            if (mappitngRecord is null)
                throw new Exception("Mapping record not found");

            _productProductTagMappingRepository.Delete(mappitngRecord);
        }

        /// <summary>
        /// Filter hidden entries according to constraints if any
        /// </summary>
        /// <param name="query">Query to filter</param>
        /// <param name="storeId">A store identifier</param>
        /// <param name="customerRoleIds">Identifiers of customer's roles</param>
        /// <returns>Filtered query</returns>
        protected virtual IQueryable<TEntity> FilterHiddenEntries<TEntity>(IQueryable<TEntity> query, int storeId, int[] customerRoleIds)
            where TEntity : Product
        {
            //filter unpublished entries
            query = query.Where(entry => entry.Published);

            //apply store mapping constraints
            if (!_catalogSettings.IgnoreStoreLimitations && _storeMappingService.IsEntityMappingExists<TEntity>(storeId))
                query = query.Where(_storeMappingService.ApplyStoreMapping<TEntity>(storeId));

            //apply ACL constraints
            if (!_catalogSettings.IgnoreAcl && _aclService.IsEntityAclMappingExist<TEntity>(customerRoleIds))
                query = query.Where(_aclService.ApplyAcl<TEntity>(customerRoleIds));

            return query;
        }

        /// <summary>
        /// Get product count for each of existing product tag
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Dictionary of "product tag ID : product count"</returns>
        protected virtual Dictionary<int, int> GetProductCount(int storeId, bool showHidden)
        {
            var customerRolesIds = _customerService.GetCustomerRoleIds(_workContext.CurrentCustomer);

            var key = _staticCacheManager.PrepareKeyForDefaultCache(NopCatalogDefaults.ProductTagCountCacheKey, storeId, customerRolesIds, showHidden);

            return _staticCacheManager.Get(key, () =>
            {
                var query = _productProductTagMappingRepository.Table;

                if (!showHidden)
                {
                    var productsQuery = FilterHiddenEntries(_productRepository.Table,
                        storeId, _customerService.GetCustomerRoleIds(_workContext.CurrentCustomer));
                    query = query.Where(pc => productsQuery.Any(p => !p.Deleted && pc.ProductId == p.Id));
                }

                var pTagCount = from pt in _productTagRepository.Table
                    join ptm in query on pt.Id equals ptm.ProductTagId into ptmDefaults
                    from ptm in ptmDefaults.DefaultIfEmpty()
                    group pt by pt.Id into ptGrouped
                    select new
                    {
                        ProductTagId = ptGrouped.Key,
                        ProductCount = ptGrouped.Count()
                    };

                return pTagCount.ToDictionary(item => item.ProductTagId, item => item.ProductCount);
            });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Delete a product tag
        /// </summary>
        /// <param name="productTag">Product tag</param>
        public virtual void DeleteProductTag(ProductTag productTag)
        {
            _productTagRepository.Delete(productTag);
        }

        /// <summary>
        /// Delete product tags
        /// </summary>
        /// <param name="productTags">Product tags</param>
        public virtual void DeleteProductTags(IList<ProductTag> productTags)
        {
            if (productTags == null)
                throw new ArgumentNullException(nameof(productTags));

            foreach (var productTag in productTags)
            {
                DeleteProductTag(productTag);
            }
        }

        /// <summary>
        /// Gets all product tags
        /// </summary>
        /// <param name="tagName">Tag name</param>
        /// <returns>Product tags</returns>
        public virtual IList<ProductTag> GetAllProductTags(string tagName = null)
        {
            var allProductTags = _productTagRepository.GetAll(getCacheKey: cache => default);

            if (!string.IsNullOrEmpty(tagName))
                allProductTags = allProductTags.Where(tag => tag.Name.Contains(tagName)).ToList();

            return allProductTags;
        }

        /// <summary>
        /// Gets all product tags by product identifier
        /// </summary>
        /// <param name="productId">Product identifier</param>
        /// <returns>Product tags</returns>
        public virtual IList<ProductTag> GetAllProductTagsByProductId(int productId)
        {
            var productTags = _productTagRepository.GetAll(query =>
            {
                return from pt in query
                    join ppt in _productProductTagMappingRepository.Table on pt.Id equals ppt.ProductTagId
                    where ppt.ProductId == productId
                    orderby pt.Id
                    select pt;
            }, cache => cache.PrepareKeyForDefaultCache(NopCatalogDefaults.ProductTagsByProductCacheKey, productId));

            return productTags;
        }

        /// <summary>
        /// Gets product tag
        /// </summary>
        /// <param name="productTagId">Product tag identifier</param>
        /// <returns>Product tag</returns>
        public virtual ProductTag GetProductTagById(int productTagId)
        {
            return _productTagRepository.GetById(productTagId, cache => default);
        }

        /// <summary>
        /// Gets product tags
        /// </summary>
        /// <param name="productTagIds">Product tags identifiers</param>
        /// <returns>Product tags</returns>
        public virtual IList<ProductTag> GetProductTagsByIds(int[] productTagIds)
        {
            return _productTagRepository.GetByIds(productTagIds);
        }

        /// <summary>
        /// Gets product tag by name
        /// </summary>
        /// <param name="name">Product tag name</param>
        /// <returns>Product tag</returns>
        public virtual ProductTag GetProductTagByName(string name)
        {
            var query = from pt in _productTagRepository.Table
                        where pt.Name == name
                        select pt;

            var productTag = query.FirstOrDefault();
            return productTag;
        }

        /// <summary>
        /// Inserts a product-product tag mapping
        /// </summary>
        /// <param name="tagMapping">Product-product tag mapping</param>
        public virtual void InsertProductProductTagMapping(ProductProductTagMapping tagMapping)
        {
            _productProductTagMappingRepository.Insert(tagMapping);
        }

        /// <summary>
        /// Inserts a product tag
        /// </summary>
        /// <param name="productTag">Product tag</param>
        public virtual void InsertProductTag(ProductTag productTag)
        {
            _productTagRepository.Insert(productTag);
        }

        /// <summary>
        /// Indicates whether a product tag exists
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="productTagId">Product tag identifier</param>
        /// <returns>Result</returns>
        public virtual bool ProductTagExists(Product product, int productTagId)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            return _productProductTagMappingRepository.Table.Any(pptm => pptm.ProductId == product.Id && pptm.ProductTagId == productTagId);
        }

        /// <summary>
        /// Updates the product tag
        /// </summary>
        /// <param name="productTag">Product tag</param>
        public virtual void UpdateProductTag(ProductTag productTag)
        {
            if (productTag == null)
                throw new ArgumentNullException(nameof(productTag));

            _productTagRepository.Update(productTag);

            var seName = _urlRecordService.ValidateSeName(productTag, string.Empty, productTag.Name, true);
            _urlRecordService.SaveSlug(productTag, seName, 0);
        }

        /// <summary>
        /// Get number of products
        /// </summary>
        /// <param name="productTagId">Product tag identifier</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Number of products</returns>
        public virtual int GetProductCount(int productTagId, int storeId, bool showHidden = false)
        {
            var dictionary = GetProductCount(storeId, showHidden);
            if (dictionary.ContainsKey(productTagId))
                return dictionary[productTagId];

            return 0;
        }

        /// <summary>
        /// Update product tags
        /// </summary>
        /// <param name="product">Product for update</param>
        /// <param name="productTags">Product tags</param>
        public virtual void UpdateProductTags(Product product, string[] productTags)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            //product tags
            var existingProductTags = GetAllProductTagsByProductId(product.Id);
            var productTagsToRemove = new List<ProductTag>();
            foreach (var existingProductTag in existingProductTags)
            {
                var found = false;
                foreach (var newProductTag in productTags)
                {
                    if (!existingProductTag.Name.Equals(newProductTag, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    found = true;
                    break;
                }

                if (!found)
                {
                    productTagsToRemove.Add(existingProductTag);
                }
            }

            foreach (var productTag in productTagsToRemove)
            {
                DeleteProductProductTagMapping(product.Id, productTag.Id);
            }

            foreach (var productTagName in productTags)
            {
                ProductTag productTag;
                var productTag2 = GetProductTagByName(productTagName);
                if (productTag2 == null)
                {
                    //add new product tag
                    productTag = new ProductTag
                    {
                        Name = productTagName
                    };
                    InsertProductTag(productTag);
                }
                else
                {
                    productTag = productTag2;
                }

                if (!ProductTagExists(product, productTag.Id))
                {
                    InsertProductProductTagMapping(new ProductProductTagMapping { ProductTagId = productTag.Id, ProductId = product.Id });
                }

                var seName = _urlRecordService.ValidateSeName(productTag, string.Empty, productTag.Name, true);
                _urlRecordService.SaveSlug(productTag, seName, 0);
            }

            //cache
            _staticCacheManager.RemoveByPrefix(NopEntityCacheDefaults<ProductTag>.Prefix);
        }

        #endregion
    }
}