using EUFramwork.Extension.EUCollision2DKit.Core;
using Unity.Mathematics;
using UnityEngine;

namespace EUFramwork.Extension.EUCollision2DKit.Collision
{
    /// <summary>
    /// 2D 圆形碰撞体组件。
    /// </summary>
    public class EUCircleCollision2D : EUAbsCollision2D
    {
        /// <summary> 圆形半径 </summary>
        [SerializeField] private float radius;

        /// <summary> 碰撞体相对于物体的偏移量 </summary>
        [SerializeField] private float2 offset;

        /// <summary>
        /// 获取或设置半径，设置时会同步更新底层 Entity 数据并通知核心类
        /// </summary>
        public float Radius
        {
            get => Entity.Circle.Radius;
            set
            {
#if UNITY_EDITOR
                radius = value;
#endif
                Entity.Circle.Radius = value;
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
            Entity.Type = EntityType.Circle;
            Entity.Circle.Radius = radius;
            Entity.Offset = offset;
            Entity.Position = new float2(transform.position.x, transform.position.y);
            Entity.Circle.Center = new float2(transform.position.x + offset.x, transform.position.y + offset.y);
        }

        private void OnEnable()
        {
            EUCollision2DCore.AddObjectCommand(this);
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            Entity.Circle.Radius = radius;
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
            
            // 绘制圆形框（使用WireSphere绘制2D圆）
            // 注意：WireSphere绘制的是3D球体，我们需要在2D平面上绘制，所以设置Z轴深度为0
            // 为了在2D视图中正确显示，我们可以在XY平面上绘制多个线段来模拟圆
            DrawWireCircle(worldPosition, radius);
        }

        // 辅助方法：在XY平面上绘制圆形
        private void DrawWireCircle(Vector3 center, float radius)
        {
            const int segments = 32;
            float angle = 0f;
            float angleStep = 2 * Mathf.PI / segments;
            
            Vector3 prevPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                angle += angleStep;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }
#endif
    }
}
