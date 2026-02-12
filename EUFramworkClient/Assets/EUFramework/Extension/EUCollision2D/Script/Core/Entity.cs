using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace EUFarmworker.Extension.EUCollision2D.Script.Core
{
    /// <summary>
    /// 碰撞实体的核心数据结构，用于 JobSystem 中的高效计算。
    /// 使用 Explicit 布局以模拟 C++ 中的 Union (联合体)，优化内存占用。
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    public struct Entity
    {
        /// <summary>
        /// 实体唯一标识，对应在对象列表中的下标
        /// </summary>
        [FieldOffset(0)] public int Id;

        /// <summary>
        /// 形状类型
        /// </summary>
        [FieldOffset(4)] public EntityType Type;

        /// <summary>
        /// 碰撞图层信息，用于图层过滤
        /// </summary>
        [FieldOffset(8)] public int Layer;

        /// <summary>
        /// 实体在世界空间的位置（通常对应 Transform.position）
        /// </summary>
        [FieldOffset(16)] public float2 Position;

        /// <summary>
        /// 碰撞形状相对于 Position 的偏移
        /// </summary>
        [FieldOffset(24)] public float2 Offset;

        /// <summary>
        /// 矩形形状数据。由于 Box 和 Circle 不会同时生效，使用相同的 FieldOffset 以共用内存。
        /// </summary>
        [FieldOffset(32)] public Box Box; 

        /// <summary>
        /// 圆形形状数据。由于 Box 和 Circle 不会同时生效，使用相同的 FieldOffset 以共用内存。
        /// </summary>
        [FieldOffset(32)] public Circle Circle;
    }

    /// <summary>
    /// 支持的碰撞形状类型
    /// </summary>
    public enum EntityType : int
    {
        /// <summary> 轴对齐包围盒 (AABB) </summary>
        Box,
        /// <summary> 圆形 </summary>
        Circle,
        /// <summary> 射线 (预留) </summary>
        Ray,
        /// <summary> 点 </summary>
        Dot
    }
}
