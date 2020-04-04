using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using EasyCaching.Core;
using EasyCaching.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LuaScript.Benchmark
{
    [SimpleJob(RunStrategy.Throughput)]
    [MemoryDiagnoser]
    [CategoriesColumn]
    [KeepBenchmarkFiles(false)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class GetCountBenchmark
    {
        public IEasyCachingProvider RedisWithLua;
        public IEasyCachingProvider RedisWithoutLua;
        public string KeyPrefix = "test-prefix-";
        public string TargetKey;
        public string TargetPrefixKey;

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();

            services.AddEasyCaching(opt =>
            {
                opt.UseRedis(config =>
                {
                    config.DBConfig.Endpoints.Add(new ServerEndPoint("127.0.0.1", 6379));
                    config.UseLuaScripts = true;
                }, "WithLua");

                opt.UseRedis(config =>
                {
                    config.DBConfig.Endpoints.Add(new ServerEndPoint("127.0.0.1", 6379));
                    config.UseLuaScripts = false;
                }, "WithoutLua");
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IEasyCachingProviderFactory>();

            RedisWithLua = factory.GetCachingProvider("WithLua");
            RedisWithoutLua = factory.GetCachingProvider("WithoutLua");

            var keyCount = 10_000;

            var dictonary = new Dictionary<string, string>();
            for (int i = 0; i < keyCount; i++)
                dictonary.Add(KeyPrefix + SequentialGuid.NewGuid().ToString(), string.Empty);

            TargetKey = dictonary.Keys.ElementAt(keyCount / 2);
            TargetPrefixKey = TargetKey.Remove(25); //in my test, about of 1000 keys starts with this prefix (because of sequential guid)

            RedisWithLua.SetAll(dictonary, TimeSpan.FromMinutes(5));
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            RedisWithLua.RemoveByPrefix(KeyPrefix);
        }

        [Benchmark(Baseline = true, Description = "WithLua")]
        [BenchmarkCategory("CountOneKey")]
        public void CountOneKey_WithLua()
        {
            RedisWithLua.GetCount(TargetKey);
        }

        [Benchmark(Description = "WithoutLua")]
        [BenchmarkCategory("CountOneKey")]
        public void CountOneKey_WithoutLua()
        {
            RedisWithoutLua.GetCount(TargetKey);
        }

        [Benchmark(Baseline = true, Description = "WithLua")]
        [BenchmarkCategory("CountManyKeys")]
        public void CountManyKeys_WithLua()
        {
            RedisWithLua.GetCount(TargetPrefixKey);
        }

        [Benchmark(Description = "WithoutLua")]
        [BenchmarkCategory("CountManyKeys")]
        public void CountManyKeys_WithoutLua()
        {
            RedisWithoutLua.GetCount(TargetPrefixKey);
        }
    }
}
