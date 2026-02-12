using EUFarmworker.Extension.EUCollision2DKit.Core;
using EUFramwork.Extension.EUCollision2DKit.Core;
using Unity.Mathematics;
using UnityEngine;

namespace EUFramwork.Extension.EUCollision2DKit.Collision
{
    /// <summary>
    /// 2D 点碰撞体组件。
    /// </summary>
    public class EUDotCollision2D : AbsEUCollision2D
    {
        /// <summary> 点相对于物体的偏移量 </summary>
        [SerializeField] private float2 offset;

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
            Entity.Type = EntityType.Dot;
            Entity.Offset = offset;
            Entity.Position = new float2(transform.position.x, transform.position.y);
        }
        private void OnEnable()
        {
            EUCollision2DCore.AddObjectCommand(this);
        }
#if UNITY_EDITOR
        private void FixedUpdate()
        {
            Entity.Offset = offset;
            UpdateData();
        }

        // 添加Gizmos绘制方法
        private void OnDrawGizmosSelected()
        {
            // 绘制绿色点
            Gizmos.color = Color.green;
            
            // 计算世界空间的位置
            Vector3 worldPosition = transform.position + new Vector3(offset.x, offset.y, 0);
            
            // 绘制一个小十字表示点
            float size = 0.1f; // 点的大小
            
            Gizmos.DrawLine(
                worldPosition - new Vector3(size, 0, 0),
                worldPosition + new Vector3(size, 0, 0)
            );
            
            Gizmos.DrawLine(
                worldPosition - new Vector3(0, size, 0),
                worldPosition + new Vector3(0, size, 0)
            );
            
            // 也可以绘制一个小圆点
            Gizmos.DrawSphere(worldPosition, size * 0.5f);
        }
#endif
    }
}
