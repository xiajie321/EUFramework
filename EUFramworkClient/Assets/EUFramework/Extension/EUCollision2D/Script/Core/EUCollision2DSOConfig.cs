using EUFramwork.Extension.EUCollision2DKit.Core;
using Unity.Mathematics;
using UnityEngine;

namespace EUFramework.Extension.EUCollision2D.Script.Core
{
    public class EUCollision2DSOConfig: ScriptableObject
    {
        /// <summary> 更新驱动模式 </summary>
        public RunMode RunMode = RunMode.FixedUpdate;
        /// <summary> 碰撞检测使用的算法类型 </summary>
        public CollisionAlgorithmMode CollisionAlgorithm = CollisionAlgorithmMode.Hash;
        /// <summary> 碰撞检测的坐标平面 </summary>
        public Direction Direction = Direction.XY;
        /// <summary> 预分配的最大对象总数 </summary>
        public int MaxObjectSum = 10000;
        /// <summary> 分帧处理的数量（0或1表示不分帧） </summary>
        public int FrameSplittingSum = 0;
        /// <summary>
        /// 地图中心位置(游戏场景世界坐标)
        /// </summary>
        public float2 MapCenter = new int2(0, 0);
        /// <summary>
        /// 地图大小(x为或者y为负数表示某一个方向上是不限制地图大小的;如果两个值都为负数则表示地图大小不被限制此时_mapCenter固定为0,0)
        /// </summary>
        public int2 MapSize = new int2(100,100);
    }
}