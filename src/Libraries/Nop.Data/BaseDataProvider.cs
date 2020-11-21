﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Mapping;
using LinqToDB.Tools;
using Nop.Core;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Data.Mapping;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace Nop.Data
{
    public abstract class BaseDataProvider
    {
        #region Utils

        /// <summary>
        /// Gets an additional mapping schema
        /// </summary>
        private MappingSchema GetMappingSchema()
        {
            if (Singleton<MappingSchema>.Instance is null)
            {
                Singleton<MappingSchema>.Instance = new MappingSchema(ConfigurationName)
                {
                    MetadataReader = new FluentMigratorMetadataReader()
                };
            }

            if (MiniProfillerEnabled)
            {
                var mpMappingSchema = new MappingSchema(new[] { Singleton<MappingSchema>.Instance });

                mpMappingSchema.SetConvertExpression<ProfiledDbConnection, IDbConnection>(db => db.WrappedConnection);
                mpMappingSchema.SetConvertExpression<ProfiledDbDataReader, IDataReader>(db => db.WrappedReader);
                mpMappingSchema.SetConvertExpression<ProfiledDbTransaction, IDbTransaction>(db => db.WrappedTransaction);
                mpMappingSchema.SetConvertExpression<ProfiledDbCommand, IDbCommand>(db => db.InternalCommand);

                return mpMappingSchema;
            }

            return Singleton<MappingSchema>.Instance;

        }

        private void UpdateParameterValue(DataConnection dataConnection, DataParameter parameter)
        {
            if (dataConnection is null)
                throw new ArgumentNullException(nameof(dataConnection));

            if (parameter is null)
                throw new ArgumentNullException(nameof(parameter));

            if (dataConnection.Command is IDbCommand command &&
                command.Parameters.Count > 0 &&
                command.Parameters.Contains(parameter.Name) &&
                command.Parameters[parameter.Name] is IDbDataParameter param)
            {
                parameter.Value = param.Value;
            }
        }

        private void UpdateOutputParameters(DataConnection dataConnection, DataParameter[] dataParameters)
        {
            if (dataParameters is null || dataParameters.Length == 0)
                return;

            foreach (var dataParam in dataParameters.Where(p => p.Direction == ParameterDirection.Output))
            {
                UpdateParameterValue(dataConnection, dataParam);
            }
        }

        /// <summary>
        /// Gets a connection to the database for a current data provider
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <returns>Connection to a database</returns>
        protected abstract IDbConnection GetInternalDbConnection(string connectionString);

        /// <summary>
        /// Creates the database connection
        /// </summary>
        protected virtual DataConnection CreateDataConnection()
        {
            return CreateDataConnection(LinqToDbDataProvider);
        }

        /// <summary>
        /// Creates the database connection
        /// </summary>
        /// <param name="dataProvider">Data provider</param>
        /// <returns>Database connection</returns>
        protected virtual DataConnection CreateDataConnection(IDataProvider dataProvider)
        {
            if (dataProvider is null)
                throw new ArgumentNullException(nameof(dataProvider));

            var dataContext = new DataConnection(dataProvider, CreateDbConnection(), GetMappingSchema())
            {
                CommandTimeout = DataSettingsManager.SQLCommandTimeout
            };

            return dataContext;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a connection to a database
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <returns>Connection to a database</returns>
        public virtual IDbConnection CreateDbConnection(string connectionString = null)
        {
            var dbConnection = GetInternalDbConnection(!string.IsNullOrEmpty(connectionString) ? connectionString : CurrentConnectionString);

            return MiniProfillerEnabled ? new ProfiledDbConnection((DbConnection)dbConnection, MiniProfiler.Current) : dbConnection;
        }

        /// <summary>
        /// Creates a new temporary storage and populate it using data from provided query
        /// </summary>
        /// <param name="storeKey">Name of temporary storage</param>
        /// <param name="query">Query to get records to populate created storage with initial data</param>
        /// <typeparam name="TItem">Storage record mapping class</typeparam>
        /// <returns>IQueryable instance of temporary storage</returns>
        public virtual ITempDataStorage<TItem> CreateTempDataStorage<TItem>(string storeKey, IQueryable<TItem> query)
            where TItem : class
        {
            return new TempSqlDataStorage<TItem>(storeKey, query, CreateDataConnection);
        }

        /// <summary>
        /// Returns mapped entity descriptor.
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <returns>Mapping descriptor</returns>
        public EntityDescriptor GetEntityDescriptor<TEntity>() where TEntity : BaseEntity
        {
            return GetMappingSchema()?.GetEntityDescriptor(typeof(TEntity));
        }

        /// <summary>
        /// Returns queryable source for specified mapping class for current connection,
        /// mapped to database table or view.
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <returns>Queryable source</returns>
        public virtual ITable<TEntity> GetTable<TEntity>() where TEntity : BaseEntity
        {
            return new DataContext(LinqToDbDataProvider, CurrentConnectionString) { MappingSchema = GetMappingSchema() }
                .GetTable<TEntity>();
        }

        /// <summary>
        /// Inserts record into table. Returns inserted entity with identity
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns>Inserted entity</returns>
        public virtual TEntity InsertEntity<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            using var dataContext = CreateDataConnection();
            entity.Id = dataContext.InsertWithInt32Identity(entity);
            return entity;
        }

        /// <summary>
        /// Updates record in table, using values from entity parameter. 
        /// Record to update identified by match on primary key value from obj value.
        /// </summary>
        /// <param name="entity">Entity with data to update</param>
        /// <typeparam name="TEntity">Entity type</typeparam>
        public virtual void UpdateEntity<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            using var dataContext = CreateDataConnection();
            dataContext.Update(entity);
        }

        /// <summary>
        /// Deletes record in table. Record to delete identified
        /// by match on primary key value from obj value.
        /// </summary>
        /// <param name="entity">Entity for delete operation</param>
        /// <typeparam name="TEntity">Entity type</typeparam>
        public virtual void DeleteEntity<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            using var dataContext = CreateDataConnection();
            dataContext.Delete(entity);
        }

        /// <summary>
        /// Performs delete records in a table
        /// </summary>
        /// <param name="entities">Entities for delete operation</param>
        /// <typeparam name="TEntity">Entity type</typeparam>
        public virtual void BulkDeleteEntities<TEntity>(IList<TEntity> entities) where TEntity : BaseEntity
        {
            using var dataContext = CreateDataConnection();
            if (entities.All(entity => entity.Id == 0))
                foreach (var entity in entities)
                    dataContext.Delete(entity);
            else
                dataContext.GetTable<TEntity>()
                    .Where(e => e.Id.In(entities.Select(x => x.Id)))
                    .Delete();
        }

        /// <summary>
        /// Performs delete records in a table by a condition
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <returns>Number of deleted records</returns>
        public virtual int BulkDeleteEntities<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : BaseEntity
        {
            using var dataContext = CreateDataConnection();
            return dataContext.GetTable<TEntity>()
                .Where(predicate)
                .Delete();
        }

        /// <summary>
        /// Performs bulk insert operation for entity colllection.
        /// </summary>
        /// <param name="entities">Entities for insert operation</param>
        /// <typeparam name="TEntity">Entity type</typeparam>
        public virtual void BulkInsertEntities<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity
        {
            using var dataContext = CreateDataConnection(LinqToDbDataProvider);
            dataContext.BulkCopy(new BulkCopyOptions(), entities.RetrieveIdentity(dataContext));
        }

        /// <summary>
        /// Executes command returns number of affected records.
        /// </summary>
        /// <param name="sqlStatement">Command text</param>
        /// <param name="dataParameters">Command parameters</param>
        /// <returns>Number of records, affected by command execution.</returns>
        public virtual int ExecuteNonQuery(string sqlStatement, params DataParameter[] dataParameters)
        {
            using var dataContext = CreateDataConnection();
            var command = new CommandInfo(dataContext, sqlStatement, dataParameters);
            var affectedRecords = command.Execute();

            UpdateOutputParameters(dataContext, dataParameters);

            return affectedRecords;
        }

        /// <summary>
        /// Executes command using LinqToDB.Mapping.StoredProcedure command type and returns
        /// single value
        /// </summary>
        /// <typeparam name="T">Result record type</typeparam>
        /// <param name="procedureName">Procedure name</param>
        /// <param name="parameters">Command parameters</param>
        /// <returns>Resulting value</returns>
        public virtual T ExecuteStoredProcedure<T>(string procedureName, params DataParameter[] parameters)
        {
            using var dataContext = CreateDataConnection();
            var command = new CommandInfo(dataContext, procedureName, parameters);

            var result = command.ExecuteProc<T>();
            UpdateOutputParameters(dataContext, parameters);

            return result;
        }

        /// <summary>
        /// Executes command using LinqToDB.Mapping.StoredProcedure command type and returns
        /// number of affected records.
        /// </summary>
        /// <param name="procedureName">Procedure name</param>
        /// <param name="parameters">Command parameters</param>
        /// <returns>Number of records, affected by command execution.</returns>
        public virtual int ExecuteStoredProcedure(string procedureName, params DataParameter[] parameters)
        {
            using var dataContext = CreateDataConnection();
            var command = new CommandInfo(dataContext, procedureName, parameters);

            var affectedRecords = command.ExecuteProc();
            UpdateOutputParameters(dataContext, parameters);

            return affectedRecords;
        }

        /// <summary>
        /// Executes command using System.Data.CommandType.StoredProcedure command type and
        /// returns results as collection of values of specified type
        /// </summary>
        /// <typeparam name="T">Result record type</typeparam>
        /// <param name="procedureName">Procedure name</param>
        /// <param name="parameters">Command parameters</param>
        /// <returns>Returns collection of query result records</returns>
        public virtual IList<T> QueryProc<T>(string procedureName, params DataParameter[] parameters)
        {
            using var dataContext = CreateDataConnection();
            var command = new CommandInfo(dataContext, procedureName, parameters);
            var rez = command.QueryProc<T>()?.ToList();
            UpdateOutputParameters(dataContext, parameters);
            return rez ?? new List<T>();
        }

        /// <summary>
        /// Executes SQL command and returns results as collection of values of specified type
        /// </summary>
        /// <typeparam name="T">Type of result items</typeparam>
        /// <param name="sql">SQL command text</param>
        /// <param name="parameters">Parameters to execute the SQL command</param>
        /// <returns>Collection of values of specified type</returns>
        public virtual IList<T> Query<T>(string sql, params DataParameter[] parameters)
        {
            using var dataContext = CreateDataConnection();
            return dataContext.Query<T>(sql, parameters)?.ToList() ?? new List<T>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Linq2Db data provider
        /// </summary>
        protected abstract IDataProvider LinqToDbDataProvider { get; }


        /// <summary>
        /// Gets or sets a value that indicates whether should use MiniProfiler for the current connection
        /// </summary>
        protected bool MiniProfillerEnabled => EngineContext.Current.Resolve<AppSettings>().CommonConfig.MiniProfilerEnabled;

        /// <summary>
        /// Database connection string
        /// </summary>
        protected string CurrentConnectionString => DataSettingsManager.LoadSettings().ConnectionString;

        /// <summary>
        /// Name of database provider
        /// </summary>
        public string ConfigurationName => LinqToDbDataProvider.Name;

        #endregion
    }
}