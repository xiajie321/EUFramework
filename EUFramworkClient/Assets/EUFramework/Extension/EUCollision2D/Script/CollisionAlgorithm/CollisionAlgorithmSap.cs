using System.Collections.Generic;
using EUFramwork.Extension.EUCollision2DKit.Collision;
using EUFramwork.Extension.EUCollision2DKit.Core;
using Unity.Collections;
using UnityEngine.Jobs;

namespace EUFramwork.Extension.EUCollision2DKit.CollisionAlgorithm
{
    /// <summary>
    /// 扫掠裁剪算法 (Sweep and Prune) 碰撞算法实现。
    /// 通过在特定轴上对对象的边界进行排序，利用时间相干性（Temporal Coherence）来高效识别潜在的碰撞对。
    /// 适用于对象位置变化不剧烈的场景。
    /// </summary>
    internal class CollisionAlgorithmSap : ICollisionAlgorithm
    {
        /// <summary> 用于排序的原生实体数组 </summary>
        private NativeArray<Entity> _sapSort;
        /// <summary> 排序数组中的活跃元素数量 </summary>
        private int _sapSortCount;
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
