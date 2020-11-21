﻿using Nop.Core.Caching;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Caching;
using Nop.Services.Events;

namespace Nop.Services.Customers.Caching
{
    /// <summary>
    /// Represents a customer cache event consumer
    /// </summary>
    public partial class CustomerCacheEventConsumer : CacheEventConsumer<Customer>, IConsumer<CustomerPasswordChangedEvent>
    {
        #region Methods

        /// <summary>
        /// Handle password changed event
        /// </summary>
        /// <param name="eventMessage">Event message</param>
        public void HandleEvent(CustomerPasswordChangedEvent eventMessage)
        {
            Remove(NopCustomerServicesDefaults.CustomerPasswordLifetimeCacheKey, eventMessage.Password.CustomerId);
        }

        /// <summary>
        /// Clear cache data
        /// </summary>
        /// <param name="entity">Entity</param>
        protected override void ClearCache(Customer entity)
        {
            RemoveByPrefix(NopCustomerServicesDefaults.CustomerCustomerRolesPrefix);
            RemoveByPrefix(NopCustomerServicesDefaults.CustomerAddressesPrefix);
            RemoveByPrefix(NopEntityCacheDefaults<ShoppingCartItem>.AllPrefix);

            if (string.IsNullOrEmpty(entity.SystemName))
                return;

            Remove(NopCustomerServicesDefaults.CustomerBySystemNameCacheKey, entity.SystemName);
        }

        #endregion
    }
}