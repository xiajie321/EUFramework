using EUFarmworker.Core.MVC.Abstract;
using EUFarmworker.Core.MVC.CoreTool;
using EUFarmworker.Core.MVC.Interface;
using UnityEngine;

namespace EUFarmworker.Core.MVC.Example.Script
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
            // 注册事件监听
            this.RegisterEvent<TestEvent>(Run);
            
            // 发送事件
            this.SendEvent<TestEvent>(new TestEvent());
            int a= this.SendCommand<TestCommandReturnInt,int>(new TestCommandReturnInt());//调用有返回值的命令
            Debug.Log(a);
            this.SendCommand(new TestCommand());//调用无返回值的命令
            this.SendCommand(new TestCommand()//给命令赋值
            {
                lsValue = -1
            });
        }

        public void Run(TestEvent testEvent)
        {
            Debug.Log("TestEvent");
        }

        private void OnDestroy()
        {
            //注销事件
            this.UnRegisterEvent<TestEvent>(Run);
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
            // this.SendEvent(new TestEvent());//不建议在结构体内使用(因为会产生装箱)
            // this.GetUtility<TestUtility>();//不建议在结构体内使用(因为会产生装箱)
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
            // this.SendEvent(new TestEvent());//不建议在结构体内使用(因为会产生装箱)
            // this.GetModel<TestModel>();//不建议在结构体内使用(因为会产生装箱)
            // this.GetUtility<TestUtility>();//不建议在结构体内使用(因为会产生装箱)
            // this.GetSystem<TestSystem>();//不建议在结构体内使用(因为会产生装箱)
        }
    }
    
    /// <summary>
    /// 测试用的工具
    /// </summary>
    public class TestUtilityBase:AbsUtilityBase//工具本身仅起到辅助作用
    {
        public override void Init()
        {
            Debug.Log("TestUtility");
        }
    }

    public struct TestCommand : ICommand//无返回值的命令
    {
        public int lsValue;
        public void Execute()
        {
            Debug.Log(lsValue);
            //this.SendCommand<TestCommandReturnInt,int>(new TestCommandReturnInt());//不建议在结构体内使用(因为会产生装箱)
            // this.SendQuery<TestQuery,int>(new TestQuery());//不建议在结构体内使用(因为会产生装箱)
            // this.SendEvent<TestEvent>(new TestEvent());//不建议在结构体内使用(因为会产生装箱)
            // this.GetModel<TestModel>();//不建议在结构体内使用(因为会产生装箱)
            // this.GetUtility<TestUtility>();//不建议在结构体内使用(因为会产生装箱)
            // this.GetSystem<TestSystem>();//不建议在结构体内使用(因为会产生装箱)
            
            //this.SendCommand(new TestCommand());//无返回值默认通过泛型确定避免装箱问题
            // this.SendCommand<TestCommand,TestCommandReturnInt,int>(new TestCommandReturnInt());//通过泛型确定避免装箱问题
            // this.SendQuery<TestCommand,TestQuery,int>(new TestQuery());//通过泛型确定避免装箱问题
            // this.SendEvent<TestCommand,TestEvent>(new TestEvent());//通过泛型确定类型避免装箱问题
            // this.GetModel<TestCommand,TestModel>();//通过泛型确定类型避免装箱问题
            // this.GetUtility<TestCommand,TestUtility>();//通过泛型确定类型避免装箱问题
            // this.GetSystem<TestCommand,TestSystem>();//通过泛型确定类型避免装箱问题
        }
    }

    public struct TestCommandReturnInt : ICommand<int>//有返回值的命令
    {
        public int Execute()
        {
            // this.SendCommand<TestCommandReturnInt,int>(new  TestCommandReturnInt());//不建议在结构体内使用(因为会产生装箱)
            // this.SendQuery<TestQuery,int>(new TestQuery());//不建议在结构体内使用(因为会产生装箱)
            // this.SendEvent<TestEvent>(new TestEvent());//不建议在结构体内使用(因为会产生装箱)
            // this.GetModel<TestModel>();//不建议在结构体内使用(因为会产生装箱)
            // this.GetUtility<TestUtility>();//不建议在结构体内使用(因为会产生装箱)
            // this.GetSystem<TestSystem>();//不建议在结构体内使用(因为会产生装箱)
            //
            // this.SendCommand(new TestCommand());//无返回值默认通过泛型确定避免装箱问题
            // this.SendCommand<TestCommandReturnInt,TestCommandReturnInt,int>(new TestCommandReturnInt());//通过泛型确定避免装箱问题
            // this.SendQuery<TestCommandReturnInt,TestQuery,int>(new TestQuery());//通过泛型确定避免装箱问题
            // this.SendEvent<TestCommandReturnInt,TestEvent>(new TestEvent());//通过泛型确定类型避免装箱问题
            // this.GetModel<TestCommandReturnInt,TestModel>();//通过泛型确定类型避免装箱问题
            // this.GetUtility<TestCommandReturnInt,TestUtility>();//通过泛型确定类型避免装箱问题
            // this.GetSystem<TestCommandReturnInt,TestSystem>();//通过泛型确定类型避免装箱问题
            return 1;
        }
    }

    public struct TestQuery : IQuery<int>
    {
        public int Execute()
        {
            // this.SendQuery<TestQuery,int>(new TestQuery());//不建议在结构体内使用(因为会产生装箱)
            // this.GetModel<TestModel>();//不建议在结构体内使用(因为会产生装箱)
            // this.GetUtility<TestUtility>();//不建议在结构体内使用(因为会产生装箱)
            //
            // this.SendQuery<TestQuery,TestQuery,int>(new TestQuery());//通过泛型确定类型避免装箱问题
            // this.GetModel<TestQuery,TestModel>();//通过泛型确定类型避免装箱问题
            // this.GetUtility<TestQuery,TestUtility>();//通过泛型确定类型避免装箱问题
            return 1;
        }
    }
}
