using System;
using System.Runtime.CompilerServices;

namespace EUFarmworker.Core.MVC.CoreTool
{
    /// <summary>
    /// 高性能事件系统 - 运行时发送零GC
    /// 仅供MVC内部使用，不对外暴露
    /// 
    /// 性能设计:
    /// - 静态泛型缓存：O(1)类型查找，JIT编译时确定内存地址，无字典哈希开销
    /// - 数组存储：避免委托 += 合并产生的GC分配
    /// - Send操作：完全零GC，直接索引遍历
    /// - 快速移除：使用交换删除算法，O(1)复杂度
    /// - 单播优化：单个监听者时避免循环开销
    /// </summary>
    internal static class TypeEventSystem
    {
        #region 事件缓存 - 利用静态泛型的JIT编译优势
        
        private static class EventCache<T> where T : struct
        {
            // 预分配数组，避免动态扩容的频繁GC
            public static Action<T>[] Handlers = new Action<T>[4];
            public static int Count;
            public static bool IsTracked;
        }
        
        #endregion

        #region 清理追踪系统
        
        private interface IClearable 
        { 
            void DoClear(); 
        }
        
        private sealed class Clearable<T> : IClearable where T : struct
        {
            // 单例模式，避免重复分配
            public static readonly Clearable<T> Instance = new();
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DoClear()
            {
                Array.Clear(EventCache<T>.Handlers, 0, EventCache<T>.Count);
                EventCache<T>.Count = 0;
                EventCache<T>.IsTracked = false;
            }
        }
        
        // 预分配追踪数组
        private static IClearable[] _clearables = new IClearable[32];
        private static int _clearableCount;
        
        #endregion

        #region 公共API

        /// <summary>
        /// 注册事件监听器
        /// 首次注册某类型时会有少量分配用于追踪，后续注册零GC
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register<T>(Action<T> handler) where T : struct
        {
            // 追踪类型以便Clear时清理（仅首次注册时执行）
            if (!EventCache<T>.IsTracked)
            {
                EventCache<T>.IsTracked = true;
                if (_clearableCount >= _clearables.Length)
                {
                    Array.Resize(ref _clearables, _clearables.Length * 2);
                }
                _clearables[_clearableCount++] = Clearable<T>.Instance;
            }

            // 确保容量
            ref var handlers = ref EventCache<T>.Handlers;
            ref var count = ref EventCache<T>.Count;
            
            if (count >= handlers.Length)
            {
                // 扩容策略：翻倍
                Array.Resize(ref handlers, handlers.Length * 2);
            }
            
            handlers[count++] = handler;
        }

        /// <summary>
        /// 注销事件监听器
        /// 使用交换删除算法，O(1)复杂度，零GC
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnRegister<T>(Action<T> handler) where T : struct
        {
            var handlers = EventCache<T>.Handlers;
            ref var count = ref EventCache<T>.Count;
            
            for (int i = 0; i < count; i++)
            {
                // 使用引用比较，避免委托Equals的开销
                if (ReferenceEquals(handlers[i], handler))
                {
                    // 快速移除：用最后一个元素填充当前位置
                    count--;
                    if (i < count)
                    {
                        handlers[i] = handlers[count];
                    }
                    handlers[count] = null; // 帮助GC
                    return;
                }
            }
        }

        /// <summary>
        /// 发送事件 - 完全零GC
        /// 使用in参数避免结构体复制
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Send<T>(in T evt) where T : struct
        {
            var handlers = EventCache<T>.Handlers;
            int count = EventCache<T>.Count;
            
            // 单播快速路径
            if (count == 1)
            {
                handlers[0](evt);
                return;
            }
            
            // 多播路径：直接索引遍历，无迭代器分配
            for (int i = 0; i < count; i++)
            {
                handlers[i](evt);
            }
        }

        /// <summary>
        /// 清理所有已注册的事件
        /// 用于架构销毁时重置状态
        /// </summary>
        public static void Clear()
        {
            for (int i = 0; i < _clearableCount; i++)
            {
                _clearables[i].DoClear();
            }
            _clearableCount = 0;
        }
        
        #endregion
    }
}
