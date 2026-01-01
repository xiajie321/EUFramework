using EUFarmworker.Core.Abstracts;
using EUFarmworker.Core.Interfaces;
using EUFarmworker.Core.Tools;
using UnityEngine;

namespace EUFarmworker.Core.Test
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
        private void Start()
        {
            // 初始化架构
            EUCore.SetArchitecture(TestArchitecture.Instance);
            
            // 注册事件监听
            this.RegisterEvent<TestEvent>(e =>
            {
                Debug.Log("TestEvent");
            });
            
            // 发送事件
            this.SendEvent<TestEvent>(new TestEvent());
        }
    }

    /// <summary>
    /// 测试用的架构实现
    /// </summary>
    public class TestArchitecture : Architecture<TestArchitecture>
    {
        protected override void Init()
        {
            // 注册模块
            RegisterModel(new TestModel());
            RegisterSystem(new TestSystem());
            RegisterUtility(new TestUtility());
            
        }
    }

    /// <summary>
    /// 测试用的数据模型
    /// </summary>
    public class TestModel : AbstractModel
    {
        public override void Init()
        {
            Debug.Log("TestModel");   
        }
    }

    /// <summary>
    /// 测试用的系统
    /// </summary>
    public class TestSystem : AbstractSystem
    {
        public override void Init()
        {
            Debug.Log("TestSystem");
        }
    }
    
    /// <summary>
    /// 测试用的工具
    /// </summary>
    public class TestUtility:AbstractUtility
    {
        public override void Init()
        {
            Debug.Log("TestUtility");
        }
    }
}
