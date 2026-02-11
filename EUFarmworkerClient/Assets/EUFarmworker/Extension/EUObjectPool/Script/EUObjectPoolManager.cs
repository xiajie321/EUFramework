using System.Collections;
using System.Collections.Generic;
using EUFarmworker.Extension.EUObjectPool.Example.Script;
using UnityEngine;

namespace EUFarmworker.Extension.EUObjectPool
{
    public static class EUObjectPoolManager
    {
        //TODO 这里通过代码生成(一个用于标记的特性来标记需要生成的对象池代码,仅在编辑器生效)
        public static readonly TestEUGameObjectPool TestEuGameObjectPool = new TestEUGameObjectPool();
    }
}