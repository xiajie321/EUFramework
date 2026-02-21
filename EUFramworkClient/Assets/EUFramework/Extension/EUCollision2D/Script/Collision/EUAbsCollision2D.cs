using System;
using EUFramwork.Extension.EUCollision2DKit.Core;
using UnityEngine;

namespace EUFramwork.Extension.EUCollision2DKit.Collision
{
    /// <summary>
    /// 所有碰撞体组件的抽象基类。
    /// 负责持有底层的 Entity 数据并管理碰撞事件分发。
    /// </summary>
    public class EUAbsCollision2D : MonoBehaviour
    {
        /// <summary> 底层实体数据，由子类填充并由算法读取 </summary>
        internal Entity Entity;

        /// <summary> 当另一个碰撞体进入时触发 </summary>
        public event Action<EUAbsCollision2D> OnEnterTrigger;
        /// <summary> 当另一个碰撞体离开时触发 </summary>
        public event Action<EUAbsCollision2D> OnExitTrigger;

        /// <summary>
        /// 由碰撞算法在检测到碰撞开始时调用
        /// </summary>
        internal void CollisionEnter(EUAbsCollision2D collision)
        {
            OnEnterTrigger?.Invoke(collision);
        }

        /// <summary>
        /// 由碰撞算法在检测到碰撞结束时调用
        /// </summary>
        internal void CollisionExit(EUAbsCollision2D collision)
        {
            OnExitTrigger?.Invoke(collision);
        }
        
        /// <summary>
        /// 提交更新数据指令到核心类。当子类修改了形状参数（如宽、高、半径）时需调用。
        /// </summary>
        protected void UpdateData()
        {
            EUCollision2DCore.UpdateObjectDataCommand(this);
        }

        /// <summary>
        /// 组件禁用时，自动从碰撞系统中移除
        /// </summary>
        private void OnDisable()
        {
            EUCollision2DCore.RemoveObjectCommand(this);
        }
    }
}
