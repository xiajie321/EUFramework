using System;
using System.Collections.Generic;
using EUFramework.Core.MVC.Abstract;
using EUFramework.Core.MVC.CoreTool;
using EUFramework.Core.MVC.Interface;
using UnityEngine;

namespace EUFramework.Core.MVC.Example.Script
{
    /// <summary>
    /// 测试用事件结构体
    /// </summary>
    public struct TestEvent
    {
        
    }
    
    /// <summary>
    /// 测试核心功能的 MonoBehaviour
    /// </summary>
    public class TestCore:MonoBehaviour,IController
    {
        private void Awake()
        {
            // 初始化架构
            EUCore.SetArchitecture(TestAbsArchitectureBase.Instance);
        }

        private void Start()
        {
            // 开始性能测试
            RunPerformanceTest();
        }

        private void RunPerformanceTest()
        {
            const int testCount = 1000;
            const int listenerCount = 100; // 模拟高压力多播情况：100个监听者
            Debug.Log($"<color=cyan>--- 开始性能对比测试 (执行次数: {testCount}) ---</color>");

            // --- 1. 单播测试 (Unicast) ---
            {
                var qfSystem = new QFramework_TypeEventSystem();
                
                // EUFramework 使用静态类
                TypeEventSystem.Register<TestEvent>(m => { });
                qfSystem.Register<TestEvent>(m => { });

                // 预热
                for (int i = 0; i < 100; i++)
                {
                    TypeEventSystem.Send(new TestEvent());
                    qfSystem.Send(new TestEvent());
                }

                Debug.Log("<color=white># 单播性能测试 (1个监听者):</color>");
                
                var euSw = new System.Diagnostics.Stopwatch();
                euSw.Start();
                for (int i = 0; i < testCount; i++) TypeEventSystem.Send(new TestEvent());
                euSw.Stop();
                long euTime = euSw.ElapsedMilliseconds;

                var qfSw = new System.Diagnostics.Stopwatch();
                qfSw.Start();
                for (int i = 0; i < testCount; i++) qfSystem.Send(new TestEvent());
                qfSw.Stop();
                long qfTime = qfSw.ElapsedMilliseconds;

                Debug.Log($"EUFramework (Unicast): {euTime} ms");
                Debug.Log($"QFramework (Sim Unicast): {qfTime} ms");
                if (qfTime > 0)
                {
                    Debug.Log($"<color=yellow>单播性能提升: {(float)(qfTime - euTime) / qfTime * 100:F2}%</color>");
                }
                
                // 清理
                TypeEventSystem.Clear();
            }

            Debug.Log("\n");

            // --- 2. 多播测试 (Multicast) ---
            {
                var qfSystem = new QFramework_TypeEventSystem();
                for (int i = 0; i < listenerCount; i++)
                {
                    TypeEventSystem.Register<TestEvent>(m => { });
                    qfSystem.Register<TestEvent>(m => { });
                }

                // 预热
                for (int i = 0; i < 100; i++)
                {
                    TypeEventSystem.Send(new TestEvent());
                    qfSystem.Send(new TestEvent());
                }

                Debug.Log($"<color=white># 多播性能测试 ({listenerCount}个监听者):</color>");

                var euSw = new System.Diagnostics.Stopwatch();
                euSw.Start();
                for (int i = 0; i < testCount; i++) TypeEventSystem.Send(new TestEvent());
                euSw.Stop();
                long euTime = euSw.ElapsedMilliseconds;

                var qfSw = new System.Diagnostics.Stopwatch();
                qfSw.Start();
                for (int i = 0; i < testCount; i++) qfSystem.Send(new TestEvent());
                qfSw.Stop();
                long qfTime = qfSw.ElapsedMilliseconds;

                Debug.Log($"EUFramework (Multicast): {euTime} ms");
                Debug.Log($"QFramework (Sim Multicast): {qfTime} ms");
                if (qfTime > 0)
                {
                    Debug.Log($"<color=yellow>多播性能提升: {(float)(qfTime - euTime) / qfTime * 100:F2}%</color>");
                }
                
                // 清理
                TypeEventSystem.Clear();
            }

            Debug.Log("\n");

            // --- 3. 注册与注销测试 (Register & UnRegister) ---
            {
                var qfSystem = new QFramework_TypeEventSystem();
                Action<TestEvent> onEvent = m => { };

                // 预热
                for (int i = 0; i < 100; i++)
                {
                    TypeEventSystem.Register(onEvent);
                    TypeEventSystem.UnRegister(onEvent);
                    qfSystem.Register(onEvent);
                    qfSystem.UnRegister(onEvent);
                }

                Debug.Log("<color=white># 注册与注销性能测试:</color>");

                var euSw = new System.Diagnostics.Stopwatch();
                euSw.Start();
                for (int i = 0; i < testCount; i++)
                {
                    TypeEventSystem.Register(onEvent);
                    TypeEventSystem.UnRegister(onEvent);
                }
                euSw.Stop();
                long euTime = euSw.ElapsedMilliseconds;

                var qfSw = new System.Diagnostics.Stopwatch();
                qfSw.Start();
                for (int i = 0; i < testCount; i++)
                {
                    qfSystem.Register(onEvent);
                    qfSystem.UnRegister(onEvent);
                }
                qfSw.Stop();
                long qfTime = qfSw.ElapsedMilliseconds;

                Debug.Log($"EUFramework (Reg/UnReg): {euTime} ms");
                Debug.Log($"QFramework (Reg/UnReg): {qfTime} ms");
                if (qfTime > 0)
                {
                    Debug.Log($"<color=yellow>注册注销性能提升: {(float)(qfTime - euTime) / qfTime * 100:F2}%</color>");
                }
                
                // 清理
                TypeEventSystem.Clear();
            }

            Debug.Log("<color=cyan>--- 性能对比测试结束 ---</color>");
        }

        public void Run2(TestEvent testEvent)
        {
            Debug.Log("aaaaaa");
        }
        public void Run(TestEvent testEvent)
        {
            // Debug.Log("TestEvent"); // 屏蔽掉，避免干扰性能测试
        }
    }

    /// <summary>
    /// 模拟 QFramework 的 TypeEventSystem 实现 (基于 Dictionary)
    /// </summary>
    public class QFramework_TypeEventSystem
    {
        private interface IEasyEvent { }
        private class EasyEvent<T> : IEasyEvent 
        { 
            public Action<T> OnEvent = e => { }; 
        }
        
        private readonly System.Collections.Generic.Dictionary<Type, IEasyEvent> mEvents 
            = new System.Collections.Generic.Dictionary<Type, IEasyEvent>();

        public void Register<T>(Action<T> onEvent)
        {
            var type = typeof(T);
            if (!mEvents.TryGetValue(type, out var e))
            {
                e = new EasyEvent<T>();
                mEvents.Add(type, e);
            }
            ((EasyEvent<T>)e).OnEvent += onEvent;
        }

        public void UnRegister<T>(Action<T> onEvent)
        {
            var type = typeof(T);
            if (mEvents.TryGetValue(type, out var e))
            {
                ((EasyEvent<T>)e).OnEvent -= onEvent;
            }
        }

        public void Send<T>(T e)
        {
            var type = typeof(T);
            if (mEvents.TryGetValue(type, out var eventObj))
            {
                ((EasyEvent<T>)eventObj).OnEvent(e);
            }
        }
    }

    /// <summary>
    /// 测试用的架构实现
    /// </summary>
    public class TestAbsArchitectureBase : AbsArchitectureBase<TestAbsArchitectureBase>
    {
        protected override void Init()
        {
            // 注册模块
            RegisterModel(new TestModelBase());
            RegisterSystem(new TestSystemBase());
            RegisterUtility(new TestUtilityBase());
        }
    }

    /// <summary>
    /// 测试用的数据模型
    /// </summary>
    public class TestModelBase : AbsModelBase
    {
        public override void Init()
        {
            Debug.Log("TestModel");   
        }
    }

    /// <summary>
    /// 测试用的系统
    /// </summary>
    public class TestSystemBase : AbsSystemBase
    {
        public override void Init()
        {
            Debug.Log("TestSystem");
        }
    }
    
    /// <summary>
    /// 测试用的工具
    /// </summary>
    public class TestUtilityBase:AbsUtilityBase
    {
        public override void Init()
        {
            Debug.Log("TestUtility");
        }
    }

    public struct TestCommand : ICommand
    {
        public int lsValue;
        public void Execute()
        {
            Debug.Log(lsValue);
        }
    }

    public struct TestCommandReturnInt : ICommand<int>
    {
        public int Execute()
        {
            return 1;
        }
    }

    public struct TestQuery : IQuery<int>
    {
        public int Execute()
        {
            return 1;
        }
    }
}
