﻿using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Juice.MediatR.RequestManager.Redis
{
    public class RequestManager : IRequestManager
    {
        private static RedisOptions configuration;
        /// <summary>  
        /// The lazy connection.  
        /// </summary>  
        private Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect(configuration.ConnectionString);
        });

        /// <summary>  
        /// Gets the connection.  
        /// </summary>  
        /// <value>The connection.</value>  
        public ConnectionMultiplexer Connection => lazyConnection.Value;

        private readonly ILogger _logger;
        public RequestManager(ILogger<RequestManager> logger, IOptions<RedisOptions> options)
        {
            _logger = logger;
            configuration = options.Value;
        }

        private string GetKey<T>(Guid id)
        {
            return typeof(T).Name + ":" + id.ToString();
        }

        public async Task TryCompleteRequestAsync<T>(Guid id, bool success)
            where T : IBaseRequest
        {
            var key = GetKey<T>(id);

            if (success)
            {
                await Connection.GetDatabase().StringSetAsync(key, "", default, When.Exists);
            }
            else
            {
                string lua_script = @"  
                if (redis.call('GET', KEYS[1]) == ARGV[1]) then  
                    redis.call('DEL', KEYS[1])  
                    return true  
                else  
                    return false  
                end  
                ";

                try
                {
                    var res = Connection.GetDatabase().ScriptEvaluate(lua_script,
                                                               new RedisKey[] { key },
                                                               new RedisValue[] { "" });
                    var ok = (bool)res;
                    if (!ok)
                    {
                        _logger.LogError($"Failed to evaluate script");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"ReleaseLock lock fail...{ex.Message}");
                }
            }
        }
        public async Task<bool> TryCreateRequestForCommandAsync<T>(Guid id)
            where T : IBaseRequest
        {
            var key = GetKey<T>(id);
            var flag = await Connection.GetDatabase().StringSetAsync(key, "", TimeSpan.FromMinutes(15), When.NotExists);
            return flag;
        }
    }
}
