using Unity.Burst;
using Unity.Mathematics;

namespace EUFarmworker.Extension.EUCollision2D.Script.Core
{
    
    /// <summary>
    /// 2D 轴对齐包围盒 (AABB)，内存占用 16 字节。
    /// </summary>
    [BurstCompile]
    public struct Box
    {
        /// <summary> 最小点 (左下角) </summary>
        public float2 Min;
        /// <summary> 最大点 (右上角) </summary>
        public float2 Max;
        
        /// <summary>
        /// 获取或设置包围盒的中心点。设置时会保持 Size 不变。
        /// </summary>
        public float2 Center
        {
            readonly get => (Min + Max) / 2f;
            [BurstCompile]
            set
            {
                float2 halfSize = Size / 2f;
                Min = value - halfSize;
                Max = value + halfSize;
            }
        }

        /// <summary>
        /// 获取或设置包围盒的宽度。设置时会以 Center 为中心进行缩放。
        /// </summary>
        public float Width
        {
            readonly get => Max.x - Min.x;
            [BurstCompile]
            set
            {
                float2 center = Center;
                Min.x = center.x - value / 2f;
                Max.x = center.x + value / 2f;
            }
        }

        /// <summary>
        /// 获取或设置包围盒的高度。设置时会以 Center 为中心进行缩放。
        /// </summary>
        public float Height
        {
            readonly get => Max.y - Min.y;
            [BurstCompile]
            set
            {
                float2 center = Center;
                Min.y = center.y - value / 2f;
                Max.y = center.y + value / 2f;
            }
        }

        /// <summary>
        /// 获取或设置包围盒的尺寸。设置时会以 Center 为中心进行缩放。
        /// </summary>
        public float2 Size
        {
            readonly get => new float2(Width, Height);
            [BurstCompile]
            set
            {
                float2 center = Center;
                float2 halfSize = value / 2f;
                Min = center - halfSize;
                Max = center + halfSize;
            }
        }
        /// <summary>
        /// 获取左下角坐标
        /// </summary>
        public readonly float2 BottomLeft => new float2(Min.x, Min.y);
        
        /// <summary>
        /// 获取右下角坐标
        /// </summary>
        public readonly float2 BottomRight => new float2(Max.x, Min.y);
        
        /// <summary>
        /// 获取左上角坐标
        /// </summary>
        public readonly float2 TopLeft => new float2(Min.x, Max.y);
        
        /// <summary>
        /// 获取右上角坐标
        /// </summary>
        public readonly float2 TopRight => new float2(Max.x, Max.y);
        
        /// <summary>
        /// 获取上边界中点坐标
        /// </summary>
        public readonly float2 TopCenter => new float2(Center.x, Max.y);
        
        /// <summary>
        /// 获取下边界中点坐标
        /// </summary>
        public readonly float2 BottomCenter => new float2(Center.x, Min.y);
        
        /// <summary>
        /// 获取左边界中点坐标
        /// </summary>
        public readonly float2 LeftCenter => new float2(Min.x, Center.y);
        
        /// <summary>
        /// 获取右边界中点坐标
        /// </summary>
        public readonly float2 RightCenter => new float2(Max.x, Center.y);

        /// <summary>
        /// 检查点是否在包围盒内（包含边界）
        /// </summary>
        /// <param name="point">要检查的点</param>
        /// <returns>如果点在包围盒内或边界上返回true，否则返回false</returns>
        [BurstCompile]
        public readonly bool Intersects(in float2 point)
        {
            return point.x >= Min.x && point.x <= Max.x && 
                   point.y >= Min.y && point.y <= Max.y;
        }
    
        /// <summary>
        /// 检查点是否在包围盒内（不包含边界）
        /// </summary>
        /// <param name="point">要检查的点</param>
        /// <returns>如果点在包围盒内（不包含边界）返回true，否则返回false</returns>
        [BurstCompile]
        public readonly bool IntersectsExclusive(in float2 point)
        {
            return point.x > Min.x && point.x < Max.x && 
                   point.y > Min.y && point.y < Max.y;
        }
        
        /// <summary>
        /// 检查是否与另一个AABB相交
        /// </summary>
        /// <param name="other">另一个AABB</param>
        /// <returns>如果相交返回true，否则返回false</returns>
        [BurstCompile]
        public readonly bool Intersects(in Box other)
        {
            return !(other.Max.x < Min.x || other.Min.x > Max.x ||
                     other.Max.y < Min.y || other.Min.y > Max.y);
        }

        /// <summary>
        /// 检查是否与圆形相交（包含边界）
        /// </summary>
        /// <param name="circle">要检查的圆形</param>
        /// <returns>如果相交返回true，否则返回false</returns>
        [BurstCompile]
        public readonly bool Intersects(in Circle circle)
        {
            float closestX = math.clamp(circle.Center.x, Min.x, Max.x);
            float closestY = math.clamp(circle.Center.y, Min.y, Max.y);
            
            float distanceSq = math.distancesq(new float2(closestX, closestY), circle.Center);
            
            return distanceSq <= circle.Radius * circle.Radius;
        }
    }
    /// <summary>
    /// 2D 圆形，内存占用 12 字节。
    /// </summary>
    [BurstCompile]
    public struct Circle
    {
        /// <summary> 圆心坐标 </summary>
        public float2 Center;
        /// <summary> 半径 </summary>
        public float Radius;
        
        /// <summary>
        /// 圆顶部的点
        /// </summary>
        public readonly float2 Top => new float2(Center.x, Center.y + Radius);
    
        /// <summary>
        /// 圆底部的点
        /// </summary>
        public readonly float2 Bottom => new float2(Center.x, Center.y - Radius);
    
        /// <summary>
        /// 圆左侧的点
        /// </summary>
        public readonly float2 Left => new float2(Center.x - Radius, Center.y);
    
        /// <summary>
        /// 圆右侧的点
        /// </summary>
        public readonly float2 Right => new float2(Center.x + Radius, Center.y);
        
        /// <summary>
        /// 检查点是否在圆形内（包含边界）
        /// </summary>
        /// <param name="point">要检查的点</param>
        /// <returns>如果点在圆形内或边界上返回true，否则返回false</returns>
        [BurstCompile]
        public readonly bool Intersects(in float2 point)
        {
            return math.distancesq(point, Center) <= Radius * Radius;
        }

        /// <summary>
        /// 检查点是否在圆形内
        /// </summary>
        /// <param name="point">要检查的点</param>
        /// <returns>如果点在圆形内 返回true，否则返回false</returns>
        [BurstCompile]
        public readonly bool IntersectsExclusive(in float2 point)
        {
            return math.distancesq(point, Center) < Radius * Radius;
        }

        /// <summary>
        /// 检查是否与AABB相交
        /// </summary>
        /// <param name="aabb">要检查的AABB</param>
        /// <returns>如果相交 返回true，否则返回false</returns>
        [BurstCompile]
        public readonly bool Intersects(in Box aabb)
        {
            float closestX = math.clamp(Center.x, aabb.Min.x, aabb.Max.x);
            float closestY = math.clamp(Center.y, aabb.Min.y, aabb.Max.y);
            
            float distanceSq = math.distancesq(new float2(closestX, closestY), Center);
            
            return distanceSq <= Radius * Radius;
        }

        /// <summary>
        /// 检查是否与另一个圆形相交
        /// </summary>
        /// <param name="other">另一个圆形</param>
        /// <returns>如果相交 返回true，否则返回false</returns>
        [BurstCompile]
        public readonly bool Intersects(in Circle other)
        {
            float centerDistanceSq = math.distancesq(Center, other.Center);
            float radiusSum = Radius + other.Radius;
            return centerDistanceSq <= radiusSum * radiusSum;
        }
    }
}
