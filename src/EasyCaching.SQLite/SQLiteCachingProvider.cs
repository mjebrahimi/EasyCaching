﻿namespace EasyCaching.SQLite
{
    using Dapper;
    using EasyCaching.Core;
    using EasyCaching.Core.Internal;
    using Microsoft.Data.Sqlite;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// SQLiteCaching provider.
    /// </summary>
    public class SQLiteCachingProvider : IEasyCachingProvider
    {
        /// <summary>
        /// The cache.
        /// </summary>
        private ISQLiteDatabaseProvider _dbProvider;

        /// <summary>
        /// The cache.
        /// </summary>
        private SqliteConnection _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:EasyCaching.SQLite.SQLiteCachingProvider"/> class.
        /// </summary>
        /// <param name="dbProvider">dbProvider.</param>
        public SQLiteCachingProvider(ISQLiteDatabaseProvider dbProvider)
        {
            this._dbProvider = dbProvider;
            this._cache = _dbProvider.GetConnection();
        }

        /// <summary>
        /// <see cref="T:EasyCaching.SQLite.SQLiteCachingProvider"/> is not distributed cache.
        /// </summary>
        /// <value><c>true</c> if is distributed cache; otherwise, <c>false</c>.</value>
        public bool IsDistributedCache => false;

        /// <summary>
        /// Exists the specified cacheKey.
        /// </summary>
        /// <returns>The exists.</returns>
        /// <param name="cacheKey">Cache key.</param>
        public bool Exists(string cacheKey)
        {
            ArgumentCheck.NotNullOrWhiteSpace(cacheKey, nameof(cacheKey));

            var dbResult = _cache.ExecuteScalar<int>(ConstSQL.EXISTSSQL, new
            {
                cachekey = cacheKey
            });

            return dbResult == 1;
        }

        /// <summary>
        /// Existses the specified cacheKey async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="cacheKey">Cache key.</param>
        public async Task<bool> ExistsAsync(string cacheKey)
        {
            ArgumentCheck.NotNullOrWhiteSpace(cacheKey, nameof(cacheKey));

            var dbResult = await _cache.ExecuteScalarAsync<int>(ConstSQL.EXISTSSQL, new
            {
                cachekey = cacheKey
            });

            return dbResult == 1;
        }

        /// <summary>
        /// Get the specified cacheKey, dataRetriever and expiration.
        /// </summary>
        /// <returns>The get.</returns>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="dataRetriever">Data retriever.</param>
        /// <param name="expiration">Expiration.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public CacheValue<T> Get<T>(string cacheKey, Func<T> dataRetriever, TimeSpan expiration) where T : class
        {
            ArgumentCheck.NotNullOrWhiteSpace(cacheKey, nameof(cacheKey));
            ArgumentCheck.NotNegativeOrZero(expiration, nameof(expiration));

            var dbResult = _cache.Query<string>(ConstSQL.GETSQL, new
            {
                cachekey = cacheKey
            }).FirstOrDefault();

            if(!string.IsNullOrWhiteSpace(dbResult))
            {
                return new CacheValue<T>(Newtonsoft.Json.JsonConvert.DeserializeObject<T>(dbResult),true);
            }

            var item = dataRetriever?.Invoke();

            if (item != null)
            {
                Set(cacheKey, item, expiration);
                return new CacheValue<T>(item, true);
            }
            else
            {
                return CacheValue<T>.NoValue;
            }
        }

        /// <summary>
        /// Gets the specified cacheKey, dataRetriever and expiration async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="dataRetriever">Data retriever.</param>
        /// <param name="expiration">Expiration.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<CacheValue<T>> GetAsync<T>(string cacheKey, Func<Task<T>> dataRetriever, TimeSpan expiration) where T : class
        {
            ArgumentCheck.NotNullOrWhiteSpace(cacheKey, nameof(cacheKey));
            ArgumentCheck.NotNegativeOrZero(expiration, nameof(expiration));

            var list = await _cache.QueryAsync<string>(ConstSQL.GETSQL, new
            {
                cachekey = cacheKey
            });

            var dbResult = list.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(dbResult))
            {
                return new CacheValue<T>(Newtonsoft.Json.JsonConvert.DeserializeObject<T>(dbResult), true);
            }

            var item = await dataRetriever?.Invoke();

            if (item != null)
            {
                await SetAsync(cacheKey, item, expiration);
                return new CacheValue<T>(item, true);
            }
            else
            {
                return CacheValue<T>.NoValue;
            }
        }

        /// <summary>
        /// Get the specified cacheKey.
        /// </summary>
        /// <returns>The get.</returns>
        /// <param name="cacheKey">Cache key.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public CacheValue<T> Get<T>(string cacheKey) where T : class
        {
            ArgumentCheck.NotNullOrWhiteSpace(cacheKey, nameof(cacheKey));

            var dbResult = _cache.Query<string>(ConstSQL.GETSQL, new
            {
                cachekey = cacheKey
            }).FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(dbResult))
            {
                return new CacheValue<T>(Newtonsoft.Json.JsonConvert.DeserializeObject<T>(dbResult), true);
            }
            else
            {
                return CacheValue<T>.NoValue;
            }
        }

        /// <summary>
        /// Gets the specified cacheKey async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="cacheKey">Cache key.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<CacheValue<T>> GetAsync<T>(string cacheKey) where T : class
        {
            ArgumentCheck.NotNullOrWhiteSpace(cacheKey, nameof(cacheKey));
           
            var list = await _cache.QueryAsync<string>(ConstSQL.GETSQL, new
            {
                cachekey = cacheKey
            });

            var dbResult = list.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(dbResult))
            {
                return new CacheValue<T>(Newtonsoft.Json.JsonConvert.DeserializeObject<T>(dbResult), true);
            }
            else
            {
                return CacheValue<T>.NoValue;
            }
        }

        /// <summary>
        /// Remove the specified cacheKey.
        /// </summary>
        /// <returns>The remove.</returns>
        /// <param name="cacheKey">Cache key.</param>
        public void Remove(string cacheKey)
        {
            ArgumentCheck.NotNullOrWhiteSpace(cacheKey, nameof(cacheKey));

            _cache.Execute(ConstSQL.REMOVESQL,new { cachekey = cacheKey });
        }

        /// <summary>
        /// Removes the specified cacheKey async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="cacheKey">Cache key.</param>
        public async Task RemoveAsync(string cacheKey)
        {
            ArgumentCheck.NotNullOrWhiteSpace(cacheKey, nameof(cacheKey));

            await _cache.ExecuteAsync(ConstSQL.REMOVESQL, new { cachekey = cacheKey });
        }

        /// <summary>
        /// Set the specified cacheKey, cacheValue and expiration.
        /// </summary>
        /// <returns>The set.</returns>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="cacheValue">Cache value.</param>
        /// <param name="expiration">Expiration.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public void Set<T>(string cacheKey, T cacheValue, TimeSpan expiration) where T : class
        {
            ArgumentCheck.NotNullOrWhiteSpace(cacheKey, nameof(cacheKey));
            ArgumentCheck.NotNull(cacheValue, nameof(cacheValue));
            ArgumentCheck.NotNegativeOrZero(expiration, nameof(expiration));

            _cache.Execute(ConstSQL.SETSQL,new
            {
                cachekey = cacheKey,
                cachevalue = Newtonsoft.Json.JsonConvert.SerializeObject(cacheValue),
                expiration = expiration.Ticks / 10000000
            });

        }

        /// <summary>
        /// Sets the specified cacheKey, cacheValue and expiration async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="cacheValue">Cache value.</param>
        /// <param name="expiration">Expiration.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task SetAsync<T>(string cacheKey, T cacheValue, TimeSpan expiration) where T : class
        {
            ArgumentCheck.NotNullOrWhiteSpace(cacheKey, nameof(cacheKey));
            ArgumentCheck.NotNull(cacheValue, nameof(cacheValue));
            ArgumentCheck.NotNegativeOrZero(expiration, nameof(expiration));

            await _cache.ExecuteAsync(ConstSQL.SETSQL, new
            {
                cachekey = cacheKey,
                cachevalue = Newtonsoft.Json.JsonConvert.SerializeObject(cacheValue),
                expiration = expiration.Ticks / 10000000
            });
        }

        /// <summary>
        /// Refresh the specified cacheKey, cacheValue and expiration.
        /// </summary>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="cacheValue">Cache value.</param>
        /// <param name="expiration">Expiration.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public void Refresh<T>(string cacheKey, T cacheValue, TimeSpan expiration) where T : class
        {
            ArgumentCheck.NotNullOrWhiteSpace(cacheKey, nameof(cacheKey));
            ArgumentCheck.NotNull(cacheValue, nameof(cacheValue));
            ArgumentCheck.NotNegativeOrZero(expiration, nameof(expiration));

            this.Remove(cacheKey);
            this.Set(cacheKey, cacheValue, expiration);
        }

        /// <summary>
        /// Refreshs the specified cacheKey, cacheValue and expiration.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="cacheKey">Cache key.</param>
        /// <param name="cacheValue">Cache value.</param>
        /// <param name="expiration">Expiration.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task RefreshAsync<T>(string cacheKey, T cacheValue, TimeSpan expiration) where T : class
        {
            ArgumentCheck.NotNullOrWhiteSpace(cacheKey, nameof(cacheKey));
            ArgumentCheck.NotNull(cacheValue, nameof(cacheValue));
            ArgumentCheck.NotNegativeOrZero(expiration, nameof(expiration));

            await this.RemoveAsync(cacheKey);
            await this.SetAsync(cacheKey, cacheValue, expiration);
        }

        /// <summary>
        /// Removes cached item by cachekey's prefix.
        /// </summary>
        /// <param name="prefix">Prefix of CacheKey.</param>
        public void RemoveByPrefix(string prefix)
        {
            ArgumentCheck.NotNullOrWhiteSpace(prefix, nameof(prefix));

            _cache.Execute(ConstSQL.REMOVEBYPREFIXSQL, new { cachekey = string.Concat(prefix, "%") });
        }

        /// <summary>
        /// Removes cached item by cachekey's prefix async.
        /// </summary>
        /// <param name="prefix">Prefix of CacheKey.</param>
        public async Task RemoveByPrefixAsync(string prefix)
        {
            ArgumentCheck.NotNullOrWhiteSpace(prefix, nameof(prefix));

            await _cache.ExecuteAsync(ConstSQL.REMOVEBYPREFIXSQL, new { cachekey = string.Concat(prefix ,"%")  });
        }

        public void SetAll<T>(IDictionary<string, T> value, TimeSpan expiration) where T : class
        {
            throw new NotImplementedException();
        }

        public Task SetAllAsync<T>(IDictionary<string, T> value, TimeSpan expiration) where T : class
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, CacheValue<T>> GetAll<T>(IEnumerable<string> cacheKeys) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<IDictionary<string, CacheValue<T>>> GetAllAsync<T>(IEnumerable<string> cacheKeys) where T : class
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, CacheValue<T>> GetByPrefix<T>(string prefix) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<IDictionary<string, CacheValue<T>>> GetByPrefixAsync<T>(string prefix) where T : class
        {
            throw new NotImplementedException();
        }

        public void RemoveAll(IEnumerable<string> cacheKeys)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAllAsync(IEnumerable<string> cacheKeys)
        {
            throw new NotImplementedException();
        }
    }
}
