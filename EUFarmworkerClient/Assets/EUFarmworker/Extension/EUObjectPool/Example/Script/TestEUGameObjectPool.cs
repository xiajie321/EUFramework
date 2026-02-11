using UnityEngine;

namespace EUFarmworker.Extension.EUObjectPool.Example.Script
{
    public class TestEUGameObjectPool:EUAbsGameObjectPoolBase<Test>
    {
        public override Test OnLoadObject()
        {
            return Resources.Load<GameObject>("").GetComponent<Test>();
        }

        public override void OnInit()
        {
          
        }

        public override void OnCreate(Test obj)
        {

        }

        public override void OnGet(Test obj)
        {

        }

        public override void OnRelease(Test obj)
        {
    
        }
    }
}