using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EUFramework.Extension.EUFSMKit
{
    /// <summary>
/// EUFSM 状态机
/// </summary>
/// <typeparam name="TKey">枚举类型的状态ID</typeparam>
public class EUFSM<TKey> where TKey : struct, Enum
{
    // 存储状态是否存在的列表
    private readonly List<bool> _keyList = new(10);
    // 存储状态对象的列表
    private readonly List<IState> _stateList = new(10);
    
    // 当前状态
    private IState _currentState;
    // 当前状态ID
    private TKey _currentId;
    // 上一个状态
    private IState _previousState;
    // 上一个状态ID
    private TKey _previousId;
    private Action<TKey, TKey> _onStateChanged;

    /// <summary>
    /// 获取当前状态对象
    /// </summary>
    public IState CurrentState => _currentState;
    /// <summary>
    /// 获取当前状态ID
    /// </summary>
    public TKey CurrentId => _currentId;
    /// <summary>
    /// 获取上一个状态对象
    /// </summary>
    public IState PreviousState => _previousState;
    /// <summary>
    /// 获取上一个状态ID
    /// </summary>
    public TKey PreviousId => _previousId;
    public Action<TKey, TKey> OnStateChanged => _onStateChanged;
    /// <summary>
    /// 将枚举转换为int索引
    /// </summary>
    private int GetEnumIndex(TKey id)
    {
        return Unsafe.As<TKey, int>(ref id);
    }

    /// <summary>
    /// 确保列表容量足够容纳该ID
    /// </summary>
    private int EnsureCapacity(TKey id)
    {
        int index = GetEnumIndex(id);
        // 如果索引超出当前数量，则进行自动扩容
        // 这里使用 Count 而不是 Capacity，是为了防止 Clear 后 Capacity 未变但 Count 为 0 导致的越界问题
        if (index >= _stateList.Count)
        {
            int expandCount = index - _stateList.Count + 1;
            for (int i = 0; i < expandCount; i++)
            {
                _stateList.Add(null);
                _keyList.Add(false);
            }
        }

        return index;
    }

    // 添加状态，如果已存在则覆盖
    private void Add(TKey key, IState state)
    {
        int index = EnsureCapacity(key);
        if (state == null)
        {
            Debug.LogError($"[EUFSM] Add: 状态 {key} 不允许为空！");
            return;
        }
        if (_keyList.Count == 0)
        {
            _keyList[index] = true;
            _stateList[index] = state;
            StartState(key);
            return;
        }
        _keyList[index] = true;
        _stateList[index] = state;
    }

    // 移除状态
    private void Remove(TKey key)
    {
        int index = GetEnumIndex(key);
        if (!_keyList[index])
        {
            Debug.LogError($"[EUFSM] Remove: 状态 {key} 不存在！");
            return;
        }
        if (index < _keyList.Count)
        {
            _keyList[index] = false;
            _stateList[index] = null;
        }
    }
    
    /// <summary>
    /// 启动状态机（进入初始状态）
    /// </summary>
    /// <param name="id">初始状态ID</param>
    public void StartState(TKey id)
    {
        int index = GetEnumIndex(id);
        if (index >= _keyList.Count || !_keyList[index])
        {
            Debug.LogError($"[EUFSM] StartState: 状态 {id} 未找到！请先添加该状态。");
            return;
        }

        _previousId = default;
        _previousState = null;
        _currentId = id;
        _currentState = _stateList[index];
        _currentState?.OnEnter();
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    /// <param name="id">目标状态ID</param>
    public void ChangeState(TKey id)
    {
        int index = GetEnumIndex(id);
        
        if (index >= _keyList.Count || !_keyList[index])
        {
            Debug.LogError($"[EUFSM] ChangeState: 状态 {id} 未找到！请先添加该状态。");
            return;
        }
        
        // 如果是同一状态，则不进行切换
        if (_stateList[index] == _currentState) return;
        // 记录上一个状态
        _previousId = _currentId;
        _previousState = _currentState;
        //退出上一次的状态
        _previousState?.OnExit();
        // 进入新状态
        _currentId = id;
        _currentState = _stateList[index];
        //状态更改时执行的方法
        _onStateChanged?.Invoke(_currentId,_previousId);
        _currentState?.OnEnter();
    }
    
    /// <summary>
    /// 添加状态
    /// </summary>
    /// <param name="id">状态ID</param>
    /// <param name="state">状态对象</param>
    public void AddState(TKey id, IState state)
    {
        Add(id, state);
    }

    /// <summary>
    /// 移除状态
    /// </summary>
    /// <param name="stateId">状态ID</param>
    public void RemoveState(TKey stateId)
    {
        Remove(stateId);
    }
    
    /// <summary>
    /// 检查是否包含指定状态
    /// </summary>
    /// <param name="stateId">状态ID</param>
    /// <returns>是否存在</returns>
    public bool ContainsState(TKey stateId)
    {
        int index = GetEnumIndex(stateId);
        if (index >= _keyList.Count) return false;
        return _keyList[index];
    }

    /// <summary>
    /// 清理状态机(状态机复用时可以调用)
    /// </summary>
    public void Clear()
    {
        _currentState?.OnExit();
        _keyList.Clear();
        _stateList.Clear();
        _currentState = null;
        _currentId = default;
        _previousState = null;
        _previousId = default;
    }

    /// <summary>
    /// 轮询更新（需要在 MonoBehaviour 的 Update 中调用）
    /// </summary>
    public void Update()
    {
        _currentState?.OnUpdate();
    }

    /// <summary>
    /// 物理更新（需要在 MonoBehaviour 的 FixedUpdate 中调用）
    /// </summary>
    public void FixedUpdate()
    {
        _currentState?.OnFixedUpdate();
    }
}
}



