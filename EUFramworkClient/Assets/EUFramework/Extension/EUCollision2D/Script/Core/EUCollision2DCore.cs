using System;
using System.Collections.Generic;
using EUFramework.Extension.EUCollision2D.Script.Core;
using EUFramwork.Extension.EUCollision2DKit.Collision;
using EUFramwork.Extension.EUCollision2DKit.CollisionAlgorithm;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Object = UnityEngine.Object;

namespace EUFramwork.Extension.EUCollision2DKit.Core
{
    #region 数据结构定义

    /// <summary>
    /// 碰撞系统的运行驱动模式
    /// </summary>
    public enum RunMode
    {
        /// <summary> 在 FixedUpdate 中更新（推荐用于物理同步） </summary>
        FixedUpdate,
        /// <summary> 在 Update 中更新 </summary>
        Update,
        /// <summary> 在 LateUpdate 中更新 </summary>
        LateUpdate,
        /// <summary> 自定义驱动模式，需手动调用 UpdateLogic </summary>
        Script 
    }

    /// <summary>
    /// 碰撞检测使用的宽阶段算法类型
    /// </summary>
    public enum CollisionAlgorithmMode
    {
        /// <summary> 空间哈希网格 (Spatial Hash) </summary>
        Hash,
        /// <summary> 扫掠裁剪算法 (Sweep and Prune) </summary>
        Sap,
        /// <summary> 多级包围盒 (Multi-level Boundary Volumes / MBP) </summary>
        MBP,
    }

    /// <summary>
    /// 碰撞计算的平面方向
    /// </summary>
    public enum Direction
    {
        /// <summary> XY 平面 (2D 默认) </summary>
        XY,
        /// <summary> XZ 平面 (3D 顶视图) </summary>
        XZ,
        /// <summary> YZ 平面 (3D 侧视图) </summary>
        YZ,
    }

    /// <summary>
    /// 碰撞对象操作指令类型
    /// </summary>
    public enum ObjectCommandType
    {
        /// <summary> 添加新对象 </summary>
        Add,
        /// <summary> 移除对象 </summary>
        Remove,
        /// <summary> 更新对象数据 </summary>
        Update,
    }

    /// <summary>
    /// 封装了一个针对碰撞对象的延迟操作指令
    /// </summary>
    public struct ObjectCommand
    {
        /// <summary> 目标碰撞对象组件 </summary>
        public EUAbsCollision2D Collision2D;
        /// <summary> 指令类型 </summary>
        public ObjectCommandType CommandType;
    }

    /// <summary>
    /// 表示一对发生碰撞的对象下标对
    /// </summary>
    public struct CollisionObject : IEquatable<CollisionObject>
    {
        public override bool Equals(object obj)
        {
            return obj is CollisionObject other && Equals(other);
        }

        /// <summary> 第一个对象的下标 </summary>
        public int AIndex;
        /// <summary> 第二个对象的下标 </summary>
        public int BIndex;

        public bool Equals(CollisionObject obj)
        {
            return obj.AIndex == AIndex && obj.BIndex == BIndex;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AIndex, BIndex);
        }

        public static bool operator ==(CollisionObject a, CollisionObject b) => a.Equals(b);
        public static bool operator !=(CollisionObject a, CollisionObject b) => !a.Equals(b);
    }

    #endregion

    /// <summary>
    /// EUCollision2D 系统核心管理类。
    /// 负责碰撞系统的全局配置、对象管理、指令队列处理以及驱动碰撞算法更新。
    /// 采用延迟指令队列和 NativeArray 来保证与 Unity JobSystem 的兼容性。
    /// </summary>
    public static class EUCollision2DCore
    {
        private static EUCollision2DSOConfig _config;
        /// <summary> 运行时驱动脚本实例 </summary>
        private static EUCollision2DRunTime _core;
        /// <summary> 当前使用的碰撞算法实现 </summary>
        private static ICollisionAlgorithm _algorithm;
        /// <summary> 系统是否已初始化 </summary>
        private static bool _init;

        #region 可配置的字段

        /// <summary> 更新驱动模式 </summary>
        private static RunMode _runMode = RunMode.FixedUpdate;
        /// <summary> 碰撞检测使用的算法类型 </summary>
        private static CollisionAlgorithmMode _collisionAlgorithm = CollisionAlgorithmMode.Hash;
        /// <summary> 碰撞检测的坐标平面 </summary>
        private static Direction _direction = Direction.XY;
        /// <summary> 预分配的最大对象总数 </summary>
        private static int _maxObjectSum = 10000;
        /// <summary> 分帧处理的数量（0或1表示不分帧） </summary>
        private static int _frameSplittingSum = 0;
        /// <summary>
        /// 地图中心位置(游戏场景世界坐标)
        /// </summary>
        private static float2 _mapCenter = new int2(0, 0);
        /// <summary>
        /// 地图大小(x为或者y为负数表示某一个方向上是不限制地图大小的;如果两个值都为负数则表示地图大小不被限制此时_mapCenter固定为0,0)
        /// </summary>
        private static int2 _mapSize = new int2(100,100);
        #endregion

        /// <summary> 管理所有碰撞对象组件的列表，用于触发回调事件 </summary>
        private static List<EUAbsCollision2D> _objects;
        /// <summary> 延迟执行的指令队列，避免在 Job 运行期间直接修改集合导致冲突 </summary>
        private static Queue<ObjectCommand> _commandQueue;
        
        private static NativeHashMap<int,UnsafeHashSet<int>> _layerObject;//TODO 需要传递

        /// <summary>
        /// 存储对应位掩码信息
        /// </summary>
        private static NativeArray<int> _layerMasks;//TODO 需要公开
        
        /// <summary> 传递给 JobSystem 的原生实体数据数组 </summary>
        private static NativeArray<Entity> _entitys;
        /// <summary> 用于批量获取 Transform 数据的原生数组 </summary>
        private static TransformAccessArray _transforms;
        /// <summary> 数组中当前活跃的实体数量 </summary>
        private static int _entityCount;

        /// <summary> 碰撞算法实现是否已完成初始化 </summary>
        private static bool _collisionAlgorithmInit;

        #region 公开的属性
        
        private static RunMode _bdRunMode = RunMode.FixedUpdate;//用于比对运行模式的字段
        
        private static bool _runModeChange = false;//运行模式改变
        /// <summary>
        /// 运行模式设置
        /// </summary>
        public static RunMode RunMode
        {
            get => _runMode;
            set
            {
                if(value == _runMode) return;
                _bdRunMode = value;
                _runModeChange = true;
            }
        }

        public static Direction Direction
        {
            get=> _direction;
            internal set => _direction = value;
        }//设置碰撞方向
        
        private static bool _collisionAlgorithmChange = false;
        
        /// <summary>
        /// 碰撞算法更换(更换的时候会把原先的游戏对象信息保留)
        /// </summary>
        public static CollisionAlgorithmMode CollisionAlgorithm
        {
            get => _collisionAlgorithm;
            set
            {
                if (_collisionAlgorithm == value && _collisionAlgorithmInit) return;
                _collisionAlgorithmChange = true;
            }
        } //碰撞算法更换
        
        /// <summary>
        /// 最大对象数量
        /// </summary>
        public static int MaxObjectSum
        {
            get => _maxObjectSum;
            private set//不允许在外部调用(原因:可能会在JobSystem运行时被调用导致引发数据竞争的问题)
            {
                if (_maxObjectSum == value) return;
                if (value < _maxObjectSum)return;
                NativeArray<Entity> newEntitys = new(value,Allocator.Persistent);
                if (_entitys.IsCreated)
                {
                    unsafe
                    {
                        void* srcPtr = _entitys.GetUnsafeReadOnlyPtr();
                        void* dstPtr = newEntitys.GetUnsafePtr();
                        UnsafeUtility.MemCpy(dstPtr, srcPtr, sizeof(Entity) * _entityCount);
                    }
                    _entitys.Dispose();
                }
                
                _entitys = newEntitys;
                TransformAccessArray newTransforms = new(value);
                if (_transforms.isCreated)
                {
                    // 按照当前活跃实体数量，将旧的 Transform 重新填入新数组
                    for (int i = 0; i < _entityCount; i++)
                    {
                        newTransforms.Add(_objects[i].transform);
                    }
                    _transforms.Dispose();
                }
                _transforms = newTransforms;
                
                _maxObjectSum = value;
                _algorithm?.OnMaxObjectSumChanged(ref _entitys,value);//扩容后执行的方法
            }
        } //默认最大对象数量
        
        /// <summary>
        /// 分帧数量
        /// </summary>
        public static int FrameSplittingSum //设置分帧参数
        {
            get => _frameSplittingSum;
            set
            {
                if (_frameSplittingSum == value) return;
                _frameSplittingSum = value;
            }
        }
        /// <summary>
        /// 分帧时最大执行对象命令的数量
        /// </summary>
        public static int PerFrameMaxObjectCommandSum =>  _commandQueue.Count / _frameSplittingSum + 1;
        /// <summary>
        /// 分帧时最大计算对象的数量
        /// </summary>
        public static int PerFrameMaxObjectSum => _entityCount / _frameSplittingSum + 1;

        /// <summary>
        /// 地图中心(用于计算碰撞检测)
        /// </summary>
        public static float2 MapCenter => _mapCenter;
        
        /// <summary>
        /// 地图大小(计算碰撞检测的边界范围,超出这个边界就不会进行检测)
        /// </summary>
        public static int2 MapSize => _mapSize;

        #endregion

        #region 初始化

        /// <summary>
        /// 懒加载初始化碰撞系统
        /// </summary>
        private static void Init()
        {
            if (_init) return;
            _init = true;
            ConfigDataInit();
            GcContainerInit();
            NativeContainerInit();
            RunTimeInit();
        }

        /// <summary>
        /// 初始化配置数据并选择碰撞算法
        /// </summary>
        private static void ConfigDataInit()
        {
            _config ??= Resources.Load<EUCollision2DSOConfig>("EUCollision2D/EUCollision2DConfig");
            _config ??= ScriptableObject.CreateInstance<EUCollision2DSOConfig>();
            _runMode = _config.RunMode;
            _frameSplittingSum = _config.FrameSplittingSum;
            _maxObjectSum = _config.MaxObjectSum;
            _collisionAlgorithm = _config.CollisionAlgorithm;
            _direction = _config.Direction;
            _mapCenter = _config.MapCenter;
            _mapSize = _config.MapSize;
            
            FrameSplittingSum = _frameSplittingSum;
            CollisionAlgorithmUpdate(_collisionAlgorithm);
            _collisionAlgorithmInit = true;
        }

        /// <summary>
        /// 初始化托管堆上的容器（List, Queue）
        /// </summary>
        private static void GcContainerInit()
        {
            _objects = new(_maxObjectSum);
            _commandQueue = new(_maxObjectSum);
        }

        /// <summary>
        /// 初始化原生内存容器（NativeArray, TransformAccessArray）
        /// </summary>
        private static void NativeContainerInit()
        {
            _layerObject = new NativeHashMap<int, UnsafeHashSet<int>>(32,Allocator.Persistent);
            for (int i = 0; i < 32; i++)
            {
                _layerObject.Add(i,new(10,Allocator.Persistent));
            }
            _layerMasks = new(_config.LayerMasks,Allocator.Persistent);
            _entitys = new(_maxObjectSum, Allocator.Persistent);
            _transforms = new(_maxObjectSum);
        }

        /// <summary>
        /// 释放原生内存，防止内存泄漏
        /// </summary>
        private static void NativeDispose()
        {
            if (_layerObject.IsCreated)
            {
                using (var ls = _layerObject.GetValueArray(Allocator.Temp))
                {
                    foreach (var i in ls)
                    {
                        if(i.IsCreated) i.Dispose();
                    }
                }
                _layerObject.Dispose();
            }
            if(_layerMasks.IsCreated) _layerMasks.Dispose();
            if (_entitys.IsCreated) _entitys.Dispose();
            if (_transforms.isCreated) _transforms.Dispose();
            _algorithm?.NativeDispose();
        }

        /// <summary>
        /// 创建隐藏的运行时驱动对象，确保系统持续运行
        /// </summary>
        private static void RunTimeInit()
        {
            _core = new GameObject("EUCollision2DRunTime").AddComponent<EUCollision2DRunTime>();
            Object.DontDestroyOnLoad(_core.gameObject);
        }

        #endregion

        #region 碰撞对象数据增删改（线程安全指令）

        /// <summary>
        /// 提交一个添加碰撞对象的指令
        /// </summary>
        public static void AddObjectCommand(EUAbsCollision2D collision2D)
        {
            if (!_init) Init();
            _commandQueue.Enqueue(new()
            {
                Collision2D = collision2D,
                CommandType = ObjectCommandType.Add
            });
        }

        /// <summary>
        /// 提交一个移除碰撞对象的指令
        /// </summary>
        public static void RemoveObjectCommand(EUAbsCollision2D collision2D)
        {
            if (!_init) Init();
            _commandQueue.Enqueue(new()
            {
                Collision2D = collision2D,
                CommandType = ObjectCommandType.Remove
            });
        }

        /// <summary>
        /// 提交一个更新对象数据的指令（例如当碰撞形状参数改变时）
        /// </summary>
        public static void UpdateObjectDataCommand(EUAbsCollision2D collision2D)
        {
            if (!_init) Init();
            _commandQueue.Enqueue(new()
            {
                Collision2D = collision2D,
                CommandType = ObjectCommandType.Update
            });
        }

        /// <summary>
        /// 实际执行添加对象的逻辑，处理数组扩容和下标分配
        /// </summary>
        private static void AddObject(EUAbsCollision2D collision2D)
        {
            if (_entityCount + 1 >= _maxObjectSum) MaxObjectSum *= 2; //两倍扩容
            collision2D.Entity.Id = _objects.Count;
            _objects.Add(collision2D);
            _transforms.Add(collision2D.transform);
            _entitys[_entityCount] = collision2D.Entity;
            _layerObject[collision2D.Entity.Layer].Add(_entityCount);//在对应图层添加对应的实体
            _entityCount++;
        }

        /// <summary>
        /// 实际执行移除对象的逻辑，使用 SwapBack (交换末尾删除) 优化 O(1) 复杂度
        /// </summary>
        private static void RemoveObject(EUAbsCollision2D collision2D)
        {
            int index = collision2D.Entity.Id;
            _objects[^1].Entity.Id = index; //将最后一个对象的Entity下标改为被删除的那个对象的下标;
            _objects.RemoveAtSwapBack(index); //移除当前对象
            _transforms.RemoveAtSwapBack(index);
            Entity entity = _entitys[_entityCount - 1];
            entity.Id = index;
            _entitys[index] = entity;
            _layerObject[collision2D.Entity.Layer].Remove(index);//在对应图层移除对应的实体
            _entityCount--;
        }

        /// <summary>
        /// 实际执行数据更新逻辑
        /// </summary>
        private static void UpdateObject(EUAbsCollision2D collision2D)
        {
            if (collision2D.ChangeLayer)
            {
                _layerObject[(int)collision2D.LastLayer].Remove(collision2D.Entity.Id);
                _layerObject[collision2D.Entity.Layer].Add(collision2D.Entity.Id);
                collision2D.ChangeLayer = false;
            }
            _entitys[collision2D.Entity.Id] = collision2D.Entity; //更新数据到实体数组上
        }

        /// <summary>
        /// 处理指令队列中的操作命令
        /// </summary>
        /// <param name="length">本帧处理的最大指令数量</param>
        private static void ObjectCommandUpdate(int length)
        {
            if (_collisionAlgorithmChange)
            {
                CollisionAlgorithmUpdate(_collisionAlgorithm);
            }
            int sum = 0;
            while (_commandQueue.Count > 0 && sum < length)
            {
                var command = _commandQueue.Dequeue();
                switch (command.CommandType)
                {
                    case ObjectCommandType.Add:
                        AddObject(command.Collision2D);
                        break;
                    case ObjectCommandType.Remove:
                        RemoveObject(command.Collision2D);
                        break;
                    case ObjectCommandType.Update:
                        UpdateObject(command.Collision2D);
                        break;
                    default:
                        break;
                }
                sum++;
            }
        }
        /// <summary>
        /// 切换碰撞检测算法，并重新初始化算法所需的容器
        /// </summary>
        private static void SetCollisionAlgorithm<T>(T algorithm) where T : class, ICollisionAlgorithm
        {
            if (_algorithm?.GetType() == algorithm?.GetType()) return;
            _algorithm?.NativeDispose();
            _algorithm = algorithm;
            _algorithm?.ConfigDataInit();
            _algorithm?.GcContainerInit();
            _algorithm?.NativeContainerInit();
        }

        /// <summary>
        /// 根据配置更新碰撞算法实现
        /// </summary>
        private static void CollisionAlgorithmUpdate(CollisionAlgorithmMode collisionAlgorithm)
        {
            _collisionAlgorithmChange = false;
            _collisionAlgorithm = collisionAlgorithm;
            if (_collisionAlgorithm == CollisionAlgorithmMode.Hash)
            {
                SetCollisionAlgorithm(new CollisionAlgorithmHash());
            }
            else if (_collisionAlgorithm == CollisionAlgorithmMode.Sap)
            {
                SetCollisionAlgorithm(new CollisionAlgorithmSap());
            }
            else if (CollisionAlgorithm == CollisionAlgorithmMode.MBP)
            {
                SetCollisionAlgorithm(new CollisionAlgorithmMBP());
            }
        }

        #endregion

        #region 具体碰撞逻辑

        /// <summary> 上一次提交的 Job 句柄 </summary>
        private static JobHandle _endHandle;

        /// <summary>
        /// 获取或设置 Job 句柄（内部使用，通常由具体算法实现更新）
        /// </summary>
        public static JobHandle EndHandle
        {
            get => _endHandle;
            internal set => _endHandle = value;
        }

        /// <summary>
        /// 碰撞系统的主逻辑更新方法，由驱动对象调用
        /// </summary>
        public static void UpdateLogic()
        {
            if (!_init) return;
            if (!_endHandle.IsCompleted) return; // 如果上一帧的任务没跑完，跳过本帧
            _endHandle.Complete(); // 强制完成上一帧任务（通常此时已经完成）
            
            // 1. 处理碰撞事件的回调逻辑
            RunCollisionObjectsLogic();
            
            // 2. 根据设置选择全量更新或分帧更新
            if (_frameSplittingSum <= 1)
            {
                Logic();
                return;
            }

            FrameSplittingLogic();
        }

        /// <summary>
        /// 全量更新逻辑（不分帧）
        /// </summary>
        private static void Logic()
        {
            ObjectCommandUpdate(_commandQueue.Count);
            _algorithm.Update(_objects,ref _entitys,ref _transforms,_entityCount);
            
            // 检查运行模式切换
            if(!_runModeChange) return;
            _runModeChange = false;
            _runMode = _bdRunMode;
        }
        

        /// <summary>
        /// 分帧更新逻辑，通过限制每帧处理的对象数量来平滑性能开销
        /// </summary>
        private static void FrameSplittingLogic()
        {
            ObjectCommandUpdate(PerFrameMaxObjectCommandSum);
            _algorithm.FrameSplitting(_objects,ref _entitys,ref _transforms,_entityCount, PerFrameMaxObjectSum);
            
            // 检查运行模式切换
            if(!_runModeChange) return;
            _runModeChange = false;
            _runMode = _bdRunMode;
        }

        /// <summary>
        /// 遍历发生碰撞的对象对并触发 Enter/Exit 事件
        /// </summary>
        private static void RunCollisionObjectsLogic()
        {
            _algorithm.RunCollisionObjectsLogic(_objects);
        }

        #endregion

        #region Unity生命周期

        internal static void Update()
        {
            if (_runMode != RunMode.Update) return;
            UpdateLogic();
        }

        internal static void FixedUpdate()
        {
            if (_runMode != RunMode.FixedUpdate) return;
            UpdateLogic();
        }

        internal static void LateUpdate()
        {
            if (_runMode != RunMode.LateUpdate) return;
            UpdateLogic();
        }

        internal static void OnDestroy()
        {
            EndHandle.Complete();//等待执行完毕并释放最后的句柄
            NativeDispose();
        }

        #endregion
    }
}
