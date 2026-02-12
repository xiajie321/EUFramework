using EUFarmworker.Extension.EUCollision2D.Core;
using EUFramwork.Extension.EUCollision2D.Core;
using Unity.Mathematics;
using UnityEngine;

namespace EUFramwork.Extension.EUCollision2D.Collision
{
    /// <summary>
    /// 2D 矩形碰撞体组件 (AABB)。
    /// </summary>
    public class EUBoxCollision2D : AbsEUCollision2D
    {
        /// <summary> 碰撞体宽度 </summary>
        [SerializeField] private float width;
        /// <summary> 碰撞体高度 </summary>
        [SerializeField] private float height;
        /// <summary> 碰撞体相对于物体的偏移量 </summary>
        [SerializeField] private float2 offset;

        /// <summary>
        /// 获取或设置宽度，设置时会同步更新底层 Entity 数据并通知核心类
        /// </summary>
        public float Width
        {
            get => Entity.Box.Width;
            set
            {
#if UNITY_EDITOR
                width = value;
#endif
                Entity.Box.Width = value;
                UpdateData();
            }
        }

        /// <summary>
        /// 获取或设置高度，设置时会同步更新底层 Entity 数据并通知核心类
        /// </summary>
        public float Height
        {
            get => Entity.Box.Height;
            set
            {
#if UNITY_EDITOR
                height = value;
#endif
                Entity.Box.Height = value;
                UpdateData();
            }
        }

        /// <summary>
        /// 获取或设置偏移量，设置时会同步更新底层 Entity 数据并通知核心类
        /// </summary>
        public float2 Offset
        {
            get => Entity.Offset;
            set
            {
#if UNITY_EDITOR
                offset = value;
#endif
                Entity.Offset = value;
                UpdateData();
            }
        }

        private void Awake()
        {
            Entity.Type = EntityType.Box;
            Entity.Box.Width = width;
            Entity.Box.Height = height;
            Entity.Offset = offset;
            Entity.Position = new float2(transform.position.x, transform.position.y);
            Entity.Box.Center = new float2(transform.position.x + offset.x, transform.position.y + offset.y);
        }

        private void OnEnable()
        {
            EUCollision2DCore.AddObjectCommand(this);
        }

#if UNITY_EDITOR
        private void FixedUpdate()
        {
            Entity.Box.Width = width;
            Entity.Box.Height = height;
            Entity.Offset = offset;
            UpdateData();
        }

        // 添加Gizmos绘制方法
        private void OnDrawGizmosSelected()
        {
            // 绘制绿色边框
            Gizmos.color = Color.green;
            
            // 计算世界空间的位置
            Vector3 worldPosition = transform.position + new Vector3(offset.x, offset.y, 0);
            
            // 绘制矩形框
            Gizmos.DrawWireCube(worldPosition, new Vector3(width, height, 0));
        }
#endif
    }
}
