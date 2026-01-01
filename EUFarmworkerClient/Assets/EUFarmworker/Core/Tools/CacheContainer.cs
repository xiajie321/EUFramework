namespace EUFarmworker.Core.Tools
{
    public static class CacheContainer<T>
    {
        private static T _cache;

        public static T Value
        {
            get => _cache;
            set => _cache = value;
        }
    }
}