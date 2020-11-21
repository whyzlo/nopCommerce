﻿using Nop.Core.Domain.Catalog;
using Nop.Services.Caching;

namespace Nop.Services.Catalog.Caching
{
    /// <summary>
    /// Represents a product specification attribute cache event consumer
    /// </summary>
    public partial class ProductSpecificationAttributeCacheEventConsumer : CacheEventConsumer<ProductSpecificationAttribute>
    {
        /// <summary>
        /// Clear cache data
        /// </summary>
        /// <param name="entity">Entity</param>
        protected override void ClearCache(ProductSpecificationAttribute entity)
        {
            RemoveByPrefix(NopCatalogDefaults.ProductSpecificationAttributeByProductPrefix, entity.ProductId);
            Remove(NopCatalogDefaults.SpecificationAttributeGroupByProductCacheKey, entity.ProductId);
        }
    }
}
