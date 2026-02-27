using System.Collections.Generic;
using UnityEngine;

namespace EUFramework.Extension.EUUI
{
    /// <summary>
    /// EUUIKit LRU 面板缓存分部类
    /// 面板 Close 时不立即销毁，转入缓存池；缓存满时淘汰最久未使用的面板；
    /// 下次打开同名面板优先从缓存复用，跳过资源加载。
    /// 缓存容量由 EUUIKitConfig.panelCacheCapacity 控制，0 表示不缓存。
    /// </summary>
    public static partial class EUUIKit
    {
        // 缓存池：panelName → IEUUIPanel（面板在 EUUICacheRoot 下，处于隐藏状态）
        private static Dictionary<string, IEUUIPanel> _lruCache
            = new Dictionary<string, IEUUIPanel>();

        // LRU 顺序链表（头 = 最近使用，尾 = 最久未使用）
        private static LinkedList<string> _lruOrder
            = new LinkedList<string>();

        // 快速定位链表节点（O(1) 删除）
        private static Dictionary<string, LinkedListNode<string>> _lruNodes
            = new Dictionary<string, LinkedListNode<string>>();

        // ── 对外查询 ──────────────────────────────────────

        /// <summary>
        /// 查询面板是否在 LRU 缓存中
        /// </summary>
        public static bool IsPanelCached<T>() where T : EUUIPanelBase<T>
            => _lruCache.ContainsKey(typeof(T).Name);

        /// <summary>
        /// 获取当前缓存面板数量
        /// </summary>
        public static int GetCachedPanelCount() => _lruCache.Count;

        // ── 内部核心方法 ──────────────────────────────────

        /// <summary>
        /// 尝试将面板存入 LRU 缓存（移入 EUUICacheRoot，不销毁）
        /// 返回 true = 成功缓存；返回 false = 容量为 0 或面板非 MonoBehaviour，调用方需直接销毁
        /// </summary>
        private static bool TryCachePanel(string panelName, IEUUIPanel panel)
        {
            int capacity = Config.panelCacheCapacity;
            if (capacity <= 0) return false;
            if (!(panel is MonoBehaviour mb)) return false;

            // 已在缓存：刷新到头部（重置 LRU 顺序）
            if (_lruNodes.TryGetValue(panelName, out var existingNode))
            {
                _lruOrder.Remove(existingNode);
                _lruNodes[panelName] = _lruOrder.AddFirst(panelName);
                return true;
            }

            // 缓存已满：淘汰尾部（最久未使用）
            if (_lruCache.Count >= capacity)
                EvictLRUTail();

            // 移入 EUUICacheRoot（_euuiCacheRoot.SetActive(false)，面板自动隐藏）
            mb.transform.SetParent(_euuiCacheRoot.transform, false);

            _lruCache[panelName]  = panel;
            _lruNodes[panelName]  = _lruOrder.AddFirst(panelName);
            return true;
        }

        /// <summary>
        /// 从 LRU 缓存中取出面板（缓存命中时调用）
        /// 成功时面板从缓存移除，由调用方接管生命周期
        /// </summary>
        private static bool TryPopFromCache<T>(string panelName, out T panel)
            where T : EUUIPanelBase<T>
        {
            panel = null;
            if (!_lruCache.TryGetValue(panelName, out var cached)) return false;

            // 从缓存移除
            if (_lruNodes.TryGetValue(panelName, out var node))
            {
                _lruOrder.Remove(node);
                _lruNodes.Remove(panelName);
            }
            _lruCache.Remove(panelName);

            panel = cached as T;
            return panel != null;
        }

        /// <summary>
        /// 将指定面板从缓存中移除并销毁（显式强制关闭时使用）
        /// </summary>
        private static void RemoveFromCache(string panelName)
        {
            if (!_lruCache.TryGetValue(panelName, out var panel)) return;

            if (_lruNodes.TryGetValue(panelName, out var node))
            {
                _lruOrder.Remove(node);
                _lruNodes.Remove(panelName);
            }
            _lruCache.Remove(panelName);

            panel.Close();
            OnPanelClosed(panelName);
        }

        /// <summary>
        /// 清空并销毁全部缓存面板（CloseAll / CloseAllExcept 时调用）
        /// </summary>
        private static void ClearLRUCache()
        {
            foreach (var kvp in _lruCache)
            {
                kvp.Value.Close();
                OnPanelClosed(kvp.Key);
            }
            _lruCache.Clear();
            _lruOrder.Clear();
            _lruNodes.Clear();
        }

        /// <summary>
        /// 淘汰 LRU 尾部（最久未使用的缓存面板）
        /// </summary>
        private static void EvictLRUTail()
        {
            if (_lruOrder.Count == 0) return;

            var tailName = _lruOrder.Last.Value;
            _lruOrder.RemoveLast();
            _lruNodes.Remove(tailName);

            if (_lruCache.TryGetValue(tailName, out var evicted))
            {
                _lruCache.Remove(tailName);
                evicted.Close();
                OnPanelClosed(tailName);
                Debug.Log($"[EUUIKit] LRU 淘汰面板: {tailName}");
            }
        }
    }
}
