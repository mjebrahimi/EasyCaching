namespace EasyCaching.Redis
{
    using EasyCaching.Core;
    using EasyCaching.Core.Configurations;

    public class RedisOptions: BaseProviderOptions
    {
        public RedisOptions()
        {

        }

        public RedisDBOptions DBConfig { get; set; } = new RedisDBOptions();
    }

    public enum CountingMethod
    {
        LuaKeys,
        LuaScan,
        ExecuteScan,
        Keys,
        KeysPageSize5000
    }
}
