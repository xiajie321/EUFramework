using System.Collections.Generic;

namespace EUFramework.Extension.EUObjectPoolKit
{
    public interface IObjectPool<T> where T : class
    {
        /// <summary>
        /// 对象池初始大小
        /// </summary>
        public Stack<T> Pool{ get; }
        /// <summary>
        /// 初始对象数量
        /// </summary>
        public int StartObjectQuantity { get; }
        /// <summary>
        /// 最大池对象数量(该参数为负数时表示不限制对象池对象容量)
        /// </summary>
        public int MaxObjectQuantity{ get; }
        /// <summary>
        /// 在对象池初始化时执行(一般在对象池初始化结束后执行)
        /// </summary>
        public void OnInit();
        /// <summary>
        /// 对象创建时执行
        /// </summary>
        /// <param name="obj"></param>
        public void OnCreate(T obj);
        /// <summary>
        /// 获取对象时执行
        /// </summary>
        /// <param name="obj"></param>
        public void OnGet(T obj);
        /// <summary>
        /// 回收对象时执行
        /// </summary>
        /// <param name="obj"></param>
        public void OnRelease(T obj);
        public void Init();
        public T Create();
        public T Get();
        public void Release(T obj);
        public void Clear();
    }
}