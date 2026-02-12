using System.Collections.Generic;
using EUFramwork.Extension.EUCollision2DKit.Collision;
using EUFramwork.Extension.EUCollision2DKit.Core;
using Unity.Collections;
using UnityEngine.Jobs;

namespace EUFramwork.Extension.EUCollision2DKit.CollisionAlgorithm
{
    /// <summary>
    /// 多级包围盒 (Multi-level Boundary Volumes) 碰撞算法实现。
    /// 旨在处理具有多种尺寸的对象，通过分层结构进一步优化宽阶段的裁剪效率。
    /// </summary>
    internal class CollisionAlgorithmMBP:ICollisionAlgorithm
    {
        public void OnMaxObjectSumChanged(ref NativeArray<Entity> entitiys, int maxObjectSum)
        {
            
        }

        public void ConfigDataInit()
        {
            
        }

        public void GcContainerInit()
        {
           
        }

        public void NativeContainerInit()
        {
            
        }

        public void NativeDispose()
        {
            
        }

        public void Update(in List<AbsEUCollision2D> collisions,ref NativeArray<Entity> entitiys,ref TransformAccessArray transformAccessArray,in int entityCount)
        {
            //EUCollision2DCore.EndHandle 使用这个方法向核心传递你当前最后的Job句柄Core会自动处理何时结束调用
        }

        public void FrameSplitting(in List<AbsEUCollision2D> collisions,ref NativeArray<Entity> entitiys,ref TransformAccessArray transformAccessArray,in int entityCount,in int perFrameMaxObjectSum)
        {
            //EUCollision2DCore.EndHandle 使用这个方法向核心传递你当前最后的Job句柄Core会自动处理何时结束调用
        }

        public void RunCollisionObjectsLogic(in List<AbsEUCollision2D> collisions)
        {
            //这里会回到主线程进行方法回调
        }
    }
}
