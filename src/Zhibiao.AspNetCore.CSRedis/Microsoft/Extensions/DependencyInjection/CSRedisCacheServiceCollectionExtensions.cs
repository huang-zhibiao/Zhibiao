using CSRedis;
using Microsoft.Extensions.Caching.CSRedis;
using Microsoft.Extensions.Caching.Distributed;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CSRedisCacheServiceCollectionExtensions
    {
        public static IServiceCollection AddCSRedisCache(this IServiceCollection services, Func<CSRedisClient> redisFactory)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (redisFactory == null)
            {
                throw new ArgumentNullException(nameof(redisFactory));
            }

            var redisClient = redisFactory.Invoke();
            services.Add(ServiceDescriptor.Singleton<IDistributedCache>(new CSRedisCache(redisClient)));

            return services;
        }
    }
}
