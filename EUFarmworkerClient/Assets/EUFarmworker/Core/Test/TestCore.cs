using EUFarmworker.Core.Abstracts;
using EUFarmworker.Core.Interfaces;
using UnityEngine;

namespace EUFarmworker.Core.Test
{
    public class TestCore:MonoBehaviour,IController
    {
        private void Start()
        {
            EUCore.SetArchitecture(TestArchitecture.Instance);
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