﻿using System;
using System.Linq;
using Google.Authenticator;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Data;
using Nop.Plugin.MultiFactorAuth.GoogleAuthenticator.Domains;

namespace Nop.Plugin.MultiFactorAuth.GoogleAuthenticator.Services
{
    /// <summary>
    /// Represents Google Authenticator service
    /// </summary>
    public class GoogleAuthenticatorService
    {
        #region Fields

        private readonly IRepository<GoogleAuthenticatorRecord> _repository;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IWorkContext _workContext;
        private readonly GoogleAuthenticatorSettings _googleAuthenticatorSettings;
        private TwoFactorAuthenticator _twoFactorAuthenticator;
        

        #endregion

        #region Ctr

        public GoogleAuthenticatorService(
            IRepository<GoogleAuthenticatorRecord> repository,
            IStaticCacheManager staticCacheManager,
            IWorkContext workContext,
            GoogleAuthenticatorSettings googleAuthenticatorSettings)
        {
            _repository = repository;
            _staticCacheManager = staticCacheManager;
            _workContext = workContext;
            _googleAuthenticatorSettings = googleAuthenticatorSettings;
        }
        #endregion

        #region Properties

        private TwoFactorAuthenticator TwoFactorAuthenticator
        {
            get
            {
                _twoFactorAuthenticator = new TwoFactorAuthenticator();
                return _twoFactorAuthenticator;
            }
        }

        #endregion

        #region Utilites

        /// <summary>
        /// Insert the configuration
        /// </summary>
        /// <param name="configuration">Configuration</param>
        protected void InsertConfiguration(GoogleAuthenticatorRecord configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _repository.Insert(configuration);
            _staticCacheManager.RemoveByPrefix(GoogleAuthenticatorDefaults.PrefixCacheKey);
        }

        /// <summary>
        /// Update the configuration
        /// </summary>
        /// <param name="configuration">Configuration</param>
        protected void UpdateConfiguration(GoogleAuthenticatorRecord configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _repository.Update(configuration);
            _staticCacheManager.RemoveByPrefix(GoogleAuthenticatorDefaults.PrefixCacheKey);
        }

        /// <summary>
        /// Delete the configuration
        /// </summary>
        /// <param name="configuration">Configuration</param>
        internal void DeleteConfiguration(GoogleAuthenticatorRecord configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _repository.Delete(configuration);
            _staticCacheManager.RemoveByPrefix(GoogleAuthenticatorDefaults.PrefixCacheKey);
        }

        /// <summary>
        /// Get a configuration by the identifier
        /// </summary>
        /// <param name="configurationId">Configuration identifier</param>
        /// <returns>Configuration</returns>
        internal GoogleAuthenticatorRecord GetConfigurationById(int configurationId)
        {
            if (configurationId == 0)
                return null;

            return _staticCacheManager.Get(_staticCacheManager.PrepareKeyForDefaultCache(GoogleAuthenticatorDefaults.ConfigurationCacheKey, configurationId), () =>
                _repository.GetById(configurationId));
        }

        internal GoogleAuthenticatorRecord GetConfigurationByCustomerEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            var query = _repository.Table;
            return query.Where(record => record.Customer == email).FirstOrDefault();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get configurations
        /// </summary>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paged list of configurations</returns>
        public IPagedList<GoogleAuthenticatorRecord> GetPagedConfigurations(string email = null, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _repository.Table;
            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(c => c.Customer.Contains(email));
            query = query.OrderBy(configuration => configuration.Id);

            return new PagedList<GoogleAuthenticatorRecord>(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Check if the customer is registered  
        /// </summary>
        /// <param name="customerEmail"></param>
        /// <returns></returns>
        public bool IsRegisteredCustomer(string customerEmail)
        {
            return GetConfigurationByCustomerEmail(customerEmail) != null;
        }

        /// <summary>
        /// Add configuration of GoogleAuthenticator
        /// </summary>
        /// <param name="customerEmail">Customer email</param>
        /// <param name="key">Secret key</param>
        public void AddGoogleAuthenticatorAccount(string customerEmail, string key)
        {
            var account = new GoogleAuthenticatorRecord
            {
                Customer = customerEmail,
                SecretKey = key,
            };
            InsertConfiguration(account);

        }

        /// <summary>
        /// Update configuration of GoogleAuthenticator
        /// </summary>
        /// <param name="customerEmail">Customer email</param>
        /// <param name="key">Secret key</param>
        public void UpdateGoogleAuthenticatorAccount(string customerEmail, string key)
        {
            var account = GetConfigurationByCustomerEmail(customerEmail);
            if (account != null)
            {
                account.SecretKey = key;
                UpdateConfiguration(account);
            }
        }

        /// <summary>
        /// Generate a setup code for a Google Authenticator user to scan
        /// </summary>
        /// <param name="secretkey">Secret key</param>
        /// <returns></returns>
        public SetupCode GenerateSetupCode(string secretkey)
        {
            return TwoFactorAuthenticator.GenerateSetupCode(
                _googleAuthenticatorSettings.BusinessPrefix, 
                _workContext.CurrentCustomer.Email, 
                secretkey, false, _googleAuthenticatorSettings.QRPixelsPerModule);
        }

        /// <summary>
        /// Validate token auth
        /// </summary>
        /// <param name="secretkey">Secret key</param>
        /// <param name="token">Token</param>
        /// <returns></returns>
        public bool ValidateTwoFactorToken(string secretkey, string token)
        {
            return TwoFactorAuthenticator.ValidateTwoFactorPIN(secretkey, token);
        }

        #endregion
    }
}
