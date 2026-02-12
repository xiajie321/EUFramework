using System.Collections.Generic;
using EUFarmworker.Extension.EUCollision2D.Script.Collision;
using EUFarmworker.Extension.EUCollision2D.Script.Core;
using Unity.Collections;
using UnityEngine.Jobs;

namespace EUFarmworker.Extension.EUCollision2D.Script.CollisionAlgorithm
{
    /// <summary>
    /// 碰撞检测算法的通用接口。
    /// 定义了算法的生命周期管理、数据同步以及不同模式（全量/分帧）下的更新逻辑。
    /// </summary>
    public interface ICollisionAlgorithm
    {
        /// <summary>
        /// 当系统最大对象容量发生改变（扩容）时触发，用于算法内部容器的重新分配。
        /// </summary>
        /// <param name="entitiys">新的实体原生数组引用</param>
        /// <param name="maxObjectSum">当前最新的最大容量</param>
        public void OnMaxObjectSumChanged(ref NativeArray<Entity> entitiys, int maxObjectSum);

        /// <summary>
        /// 初始化算法相关的配置数据（如网格大小、分层数量等）。
        /// </summary>
        public void ConfigDataInit();

        /// <summary>
        /// 初始化算法所需的托管内存容器（如 List, Dictionary）。
        /// </summary>
        public void GcContainerInit();

        /// <summary>
        /// 初始化算法所需的 Native 原生内存容器，用于 JobSystem。
        /// </summary>
        public void NativeContainerInit();
        
        /// <summary>
        /// 释放算法持有的所有原生内存容器，防止泄漏。
        /// </summary>
        public void NativeDispose();

        /// <summary>
        /// 执行全量碰撞检测逻辑。
        /// </summary>
        /// <param name="collisions">托管堆的碰撞体组件列表</param>
        /// <param name="entitys">传递给 Job 的实体数据数组</param>
        /// <param name="transformAccessArray">用于 Job 同步 Transform 数据的数组</param>
        /// <param name="entityCount">当前活跃的实体数量</param>
        public void Update(in List<AbsEUCollision2D> collisions,ref NativeArray<Entity> entitys,ref TransformAccessArray transformAccessArray, in int entityCount);

        /// <summary>
        /// 执行分帧碰撞检测逻辑，用于平滑大负载时的 CPU 峰值。
        /// </summary>
        /// <param name="collisions">托管堆的碰撞体组件列表</param>
        /// <param name="entitys">实体数据数组</param>
        /// <param name="transformAccessArray">Transform 数据数组</param>
        /// <param name="entityCount">当前活跃的实体数量</param>
        /// <param name="perFrameMaxObjectSum">本帧允许处理的最大对象数量上限</param>
        public void FrameSplitting(in List<AbsEUCollision2D> collisions,ref NativeArray<Entity> entitys,ref TransformAccessArray transformAccessArray, in int entityCount,in int perFrameMaxObjectSum);

        /// <summary>
        /// 在计算完成后，在主线程执行具体的碰撞事件触发（如调用 CollisionEnter/Exit）。
        /// </summary>
        /// <param name="collisions">碰撞体组件列表</param>
        public void RunCollisionObjectsLogic(in List<AbsEUCollision2D> collisions);
    }
}
