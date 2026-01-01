using EUFarmworker.Core.Abstracts;
using EUFarmworker.Core.Interfaces;
using EUFarmworker.Core.Tools;
using UnityEngine;

namespace EUFarmworker.Core.Test
{
    public struct TestEvent
    {
        
    }
    public class TestCore:MonoBehaviour,IController
    {
        private void Start()
        {
            EUCore.SetArchitecture(TestArchitecture.Instance);
            this.RegisterEvent<TestEvent>(e =>
            {
                Debug.Log("TestEvent");
            });
            this.SendEvent<TestEvent>(new TestEvent());
        }
    }

    public class TestArchitecture : Architecture<TestArchitecture>
    {
        protected override void Init()
        {
            RegisterModel(new TestModel());
            RegisterSystem(new TestSystem());
            RegisterUtility(new TestUtility());
            
        }
    }

    public class TestModel : AbstractModel
    {
        public override void Init()
        {
            Debug.Log("TestModel");   
        }
    }

    public class TestSystem : AbstractSystem
    {
        public override void Init()
        {
            Debug.Log("TestSystem");
        }
    }
    public class TestUtility:AbstractUtility
    {
        public override void Init()
        {
            Debug.Log("TestUtility");
        }
    }
}
