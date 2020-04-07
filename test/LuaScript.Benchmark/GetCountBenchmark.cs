using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;
using EasyCaching.Core;
using EasyCaching.Core.Configurations;
using EasyCaching.Redis;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LuaScript.Benchmark
{
    [SimpleJob(RunStrategy.Throughput)]
    [MemoryDiagnoser]
    [KeepBenchmarkFiles(false)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
    [Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
    public class GetCountBenchmark
    {
        public DefaultRedisCachingProvider Redis;
        public string KeyPrefix = "test-prefix-";
        public string TargetKey;
        public string TargetPrefixKey;

        public IEnumerable<CountingMethod> GetCountingMethods
        {
            get
            {
                yield return CountingMethod.LuaKeys;
                yield return CountingMethod.LuaScan;
                yield return CountingMethod.ExecuteScan;
                yield return CountingMethod.Keys;
                yield return CountingMethod.KeysPageSize5000;
            }
        }
        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();

            services.AddEasyCaching(opt =>
            {
                opt.UseRedis(config =>
                {
                    config.DBConfig.Endpoints.Add(new ServerEndPoint("127.0.0.1", 6379));
                });
            });

            var serviceProvider = services.BuildServiceProvider();

            Redis = (DefaultRedisCachingProvider)serviceProvider.GetRequiredService<IEasyCachingProvider>();

            var keyCount = 10_000;

            var dictonary = new Dictionary<string, string>();
            for (int i = 0; i < keyCount; i++)
                dictonary.Add(KeyPrefix + SequentialGuid.NewGuid().ToString(), string.Empty);

            TargetKey = dictonary.Keys.ElementAt(keyCount / 2) + "*";
            TargetPrefixKey = TargetKey.Remove(25) + "*"; //in my test, about of 1000 keys starts with this prefix (because of sequential guid)

            Redis.SetAll(dictonary, TimeSpan.FromMinutes(5));
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            Redis.RemoveByPrefix(KeyPrefix);
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetCountingMethods))]
        public void CountOneKey(CountingMethod CountingMethod)
        {
            Redis.SearchRedisKeys(TargetKey, CountingMethod);
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetCountingMethods))]
        public void CountManyKeys(CountingMethod CountingMethod)
        {
            Redis.SearchRedisKeys(TargetPrefixKey, CountingMethod);
        }
    }
}
