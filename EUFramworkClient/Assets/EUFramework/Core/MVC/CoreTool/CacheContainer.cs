namespace EUFramework.Core.MVC.CoreTool
{
    /// <summary>
    /// 缓存容器，用于静态存储对象实例，提高访问速度
    /// </summary>
    /// <typeparam name="T">存储对象的类型</typeparam>
    public static class CacheContainer<T>
    {
        private static T _cache;

        /// <summary>
        /// 获取或设置缓存的值
        /// </summary>
        public static T Value
        {
            get => _cache;
            set => _cache = value;
        }
    }
}
