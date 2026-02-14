using System.Collections.Generic;
using EUFramwork.Extension.EUCollision2DKit.Collision;
using EUFramwork.Extension.EUCollision2DKit.Core;
using Unity.Collections;
using UnityEngine.Jobs;

namespace EUFramwork.Extension.EUCollision2DKit.CollisionAlgorithm
{
    /// <summary>
    /// 空间哈希网格碰撞算法实现。
    /// 将 2D 空间划分为固定大小的网格，通过将对象映射到网格中来减少需要进行精确碰撞检测的对象对数量。
    /// 适用于对象分布均匀且大小相近的场景。
    /// </summary>
    internal class CollisionAlgorithmHash : ICollisionAlgorithm
    {
        /// <summary> 网格单元格大小 </summary>
        private float _gridCellSize = 2f;

        /// <summary>
        /// 获取或设置网格单元格大小
        /// </summary>
        public float GridCellSize
        {
            get => _gridCellSize;
            set => _gridCellSize = value;
        }
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
        
        public void Update(in List<EUAbsCollision2D> collisions,ref NativeArray<Entity> entitiys,ref TransformAccessArray transformAccessArray,in int entityCount)
        {
            //EUCollision2DCore.EndHandle 使用这个方法向核心传递你当前最后的Job句柄Core会自动处理何时结束调用
        }

        public void FrameSplitting(in List<EUAbsCollision2D> collisions,ref NativeArray<Entity> entitiys,ref TransformAccessArray transformAccessArray,in int entityCount,in int perFrameMaxObjectSum)
        {
            //EUCollision2DCore.EndHandle 使用这个方法向核心传递你当前最后的Job句柄Core会自动处理何时结束调用
        }

        public void RunCollisionObjectsLogic(in List<EUAbsCollision2D> collisions)
        {
            //这里会回到主线程进行方法回调
        }
        
    }
}
