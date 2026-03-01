using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EUFramework.Extension.EUInputControllerKit
{
    public struct EUMainInputControllerChangeData
    {
        public PlayerInputController LastPlayerInputController;
        public PlayerInputController CurrentPlayerInputController;
    }

    public struct EUPlayerInputOfDeviceChangeData
    {
        public PlayerInputController ChangeOfPlayerInputController;//改变的控制器
        public Gamepad LastGamepad;
        public Gamepad CurrentGamepad;
    }

    public static class EUInputController
    {
        private static bool _isInit;
        private static Action<InputDevice> _onAddedDevice;
        private static Action<InputDevice> _onRemovedDevice;
        private static Action<EUMainInputControllerChangeData> _onMainInputControllerChange;
        private static Action<EUPlayerInputOfDeviceChangeData> _onPlayerInputControllerOfDeviceChange;
        private static PlayerInputController _mainPlayerInputController; //主控玩家控制器
        private static List<PlayerInputController> _playerInputControllerList; //用来记录玩家控制器进入的先后顺序
        private static List<InputDevice> _playerInputDeviceList;//用来记录玩家控制设备进入的先后顺序
        private static Dictionary<PlayerInputController, int> _playerInputControllerMapId; //给控制器标记Id
        private static Dictionary<int, PlayerInputController> _playerInputControllerMap; //标记对应ID的控件
        private static Dictionary<int, InputDevice> _playerInputDeviceMap;//用于存储设备
        private static Dictionary<int, int> _idAndDevicesIdMap;//用于处理设备与控制器的映射关系方便通过控制器id快速找到对应设备
        private static Dictionary<int, int> _devicesIdAndIdMap;//用于处理设备与控制器的映射关系方便通过设备id快速找到对应控制器
        private static int _maxPlayerInputControllers = 4;
        internal static Dictionary<PlayerInputController, int> PlayerInputControllerMapId  => _playerInputControllerMapId;
        internal static Dictionary<int,InputDevice>  PlayerInputDeviceMap => _playerInputDeviceMap;
        public static int MaxPlayerInputControllers
        {
            get => _maxPlayerInputControllers;
            set
            {
                if (_maxPlayerInputControllers == value) return;
                if(_maxPlayerInputControllers < 1) return;//最少为1个
                if (_maxPlayerInputControllers > value)
                {
                    int ls = _maxPlayerInputControllers - value;
                    for (int i = 0; i < ls; i++)
                    {
                        PlayerInputController ls2 = _playerInputControllerList[^1];
                        _playerInputControllerMap.Remove(_playerInputControllerMapId[ls2]);
                        _playerInputControllerMapId.Remove(ls2);
                        SetPlayerInputControllerOfDevice(ls2,null);
                        _idAndDevicesIdMap.Remove(GetPlayerInputControllerId(ls2));
                        _playerInputControllerList.RemoveAt(_playerInputControllerList.Count - 1);
                    }
                }

                _maxPlayerInputControllers = value;
            }
        } //设置一台机器所能容纳的最大玩家控制器数量
        /// <summary>
        /// 当前已连接的输入控制器数量
        /// </summary>
        public static int CurrentPlayerInputControllerCount
        {
            get => _playerInputControllerList.Count;
        }
        /// <summary>
        /// 当前已连接的输入设备数量
        /// </summary>
        public static int CurrentPlayerInputDeviceCount
        {
            get => _playerInputDeviceList.Count;
        }

        private static int _id = 0;
        /// <summary>
        /// 初始化
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            if(_isInit) return;
            _isInit = true;
            _id = 0;
            _playerInputControllerList = new(_maxPlayerInputControllers);
            _playerInputDeviceList = new(_maxPlayerInputControllers);
            _playerInputControllerMap = new(_maxPlayerInputControllers);
            _playerInputControllerMapId = new(_maxPlayerInputControllers);
            _playerInputDeviceMap = new(_maxPlayerInputControllers);
            _idAndDevicesIdMap = new(_maxPlayerInputControllers);
            _devicesIdAndIdMap = new(_maxPlayerInputControllers);
            AddPlayerInputController(_id++);//默认会有一个控制器
            
            // 初始化已连接的设备
            foreach (var device in InputSystem.devices)
            {
                if (device is Gamepad)
                {
                    AddPlayerInputDevice(device.deviceId, device);
                }
            }

            InputSystem.onDeviceChange -= OnDeviceChange;//注销事件以确保事件唯一
            InputSystem.onDeviceChange += OnDeviceChange;
#if UNITY_EDITOR
            LogDebugData("初始化");
#endif
        }

        private static void OnDeviceChange(InputDevice inputDevice, InputDeviceChange change)
        {
            if (inputDevice is not Gamepad) return;
            if (change == InputDeviceChange.Added|| change == InputDeviceChange.Reconnected)
            {
                AddPlayerInputDevice(inputDevice.deviceId, inputDevice);
#if UNITY_EDITOR
                LogDebugData($"设备加入:{change}");
#endif
            }

            if (change == InputDeviceChange.Removed|| change == InputDeviceChange.Disconnected)
            {
                RemovePlayerInputDevice(inputDevice.deviceId);
#if UNITY_EDITOR
                LogDebugData($"设备移除:{change}");
#endif
            }

        }

        #region 玩家输入控制器相关
        
        private static void AddPlayerInputController(int playerId)
        {
            if (CurrentPlayerInputControllerCount >= _maxPlayerInputControllers)
            {
#if UNITY_EDITOR
                LogDebugData($"<color=red>已超过当前所能容纳的最大玩家控制器数量:{_maxPlayerInputControllers} , 当前数量:{CurrentPlayerInputControllerCount}</color>");
                return;
#endif
            }
            if (_playerInputControllerMap.ContainsKey(playerId)) return;
            var ls = new PlayerInputController();
            _playerInputControllerMap.Add(playerId, ls);
            _playerInputControllerMapId.Add(ls, playerId);
            _playerInputControllerList.Add(ls);
            _idAndDevicesIdMap.Add(playerId,-1);
            if (_playerInputControllerList.Count == 1)
            {
                SetMainPlayerInputController(ls);
            }
        }
        /// <summary>
        /// 移除玩家输入控制器
        /// </summary>
        public static void RemovePlayerInputController(int playerId)
        {
            if(CurrentPlayerInputControllerCount <= 1) return;
            if(playerId == _mainPlayerInputController.GetPlayerInputControllerId()) return;
            if (!_playerInputControllerMap.TryGetValue(playerId, out var ls)) return;
            _playerInputControllerMapId.Remove(ls);
            _playerInputControllerMap.Remove(playerId);
            _playerInputControllerList.Remove(ls);
            if (_idAndDevicesIdMap[playerId] != -1)
                _devicesIdAndIdMap[_idAndDevicesIdMap[playerId]] = -1;
            _idAndDevicesIdMap.Remove(playerId);
        }

        /// <summary>
        /// 移除玩家输入控制器
        /// </summary>
        public static void RemovePlayerInputController(PlayerInputController playerInputController)
        {
            RemovePlayerInputController(GetPlayerInputControllerId(playerInputController));
        }
        /// <summary>
        /// 添加玩家输入控制器
        /// </summary>
        public static int AddPlayerInputController()
        {
            int id = _id++;
            AddPlayerInputController(id);
            return id;
        }
        /// <summary>
        /// 绑定玩家输入控制器的输入设备
        /// </summary>
        /// <param name="playerInputController">玩家输入控制器引用</param>
        /// <param name="inputDevice">输入设备的引用</param>
        public static void SetPlayerInputControllerOfDevice(PlayerInputController playerInputController,InputDevice inputDevice)
        {
            if(playerInputController.Gamepad == inputDevice) return;
            int id = GetPlayerInputControllerId(playerInputController);
            Gamepad lsGamepad;
            if (inputDevice is null or Keyboard)
            {
                lsGamepad = playerInputController.Gamepad;
                if (_idAndDevicesIdMap[id] != -1)//该控制器原先有对应的设备
                {
                    _devicesIdAndIdMap[_idAndDevicesIdMap[id]] = -1;//将该设备原先的控制器标记为无对应引用
                    _idAndDevicesIdMap[id] = -1;//标记当前控制器对应的设备为无对应引用
                }
                playerInputController.BindGamepad(null);
                
                _onPlayerInputControllerOfDeviceChange?.Invoke(new()
                {
                    ChangeOfPlayerInputController = playerInputController,
                    LastGamepad = lsGamepad,
                    CurrentGamepad = null
                });
#if UNITY_EDITOR
                LogDebugData("绑定设备.null");
#endif
            }

            if (inputDevice is not Gamepad device) return;
            
            // 检查该设备是否已经被其他控制器绑定，如果是，则先解除绑定
            if (_devicesIdAndIdMap.TryGetValue(device.deviceId, out int oldOwnerId) && oldOwnerId != -1 && oldOwnerId != id)
            {
                SetPlayerInputControllerOfDevice(GetPlayerInputController(oldOwnerId), null);
            }
            
            playerInputController.BindGamepad(device);
            lsGamepad = playerInputController.Gamepad;
            if (_idAndDevicesIdMap[id] != -1)//该控制器原先有对应的设备
            {
                var oldDeviceId =  _idAndDevicesIdMap[id];
                _devicesIdAndIdMap[oldDeviceId] = -1;
            }
            _idAndDevicesIdMap[id] = inputDevice.deviceId;
            _devicesIdAndIdMap[inputDevice.deviceId] = id;
            
            _onPlayerInputControllerOfDeviceChange?.Invoke(new()
            {
                ChangeOfPlayerInputController = playerInputController,
                LastGamepad = lsGamepad,
                CurrentGamepad = (Gamepad)inputDevice
            });
#if UNITY_EDITOR
            LogDebugData($"绑定设备.{inputDevice.deviceId}");
#endif
        }
        /// <summary>
        /// 绑定玩家输入控制器的输入设备
        /// </summary>
        /// <param name="playerId">玩家输入控制器Id</param>
        /// <param name="inputDevice">输入设备的引用</param>
        public static void SetPlayerInputControllerOfDevice(int playerId, InputDevice inputDevice)
        {
            PlayerInputController playerInputController = GetPlayerInputController(playerId);
            SetPlayerInputControllerOfDevice(playerInputController, inputDevice);
        }
        /// <summary>
        /// 获取当前的主玩家控制器
        /// </summary>
        /// <returns></returns>
        public static PlayerInputController GetMainPlayerInputController()
        {
            return _mainPlayerInputController;
        }

        /// <summary>
        /// 设置主玩家控制器
        /// </summary>
        public static void SetMainPlayerInputController(PlayerInputController playerInputController)
        {
            if(playerInputController == _mainPlayerInputController) return;
            if (!_playerInputControllerMapId.ContainsKey(playerInputController)) return;
            var last = _mainPlayerInputController;
            _mainPlayerInputController = playerInputController;
            _onMainInputControllerChange?.Invoke(new EUMainInputControllerChangeData()
            {
                LastPlayerInputController = last,
                CurrentPlayerInputController = _mainPlayerInputController
            });
        }

        /// <summary>
        /// 设置主玩家控制器
        public static void SetMainPlayerInputController(int playerId)
        {
            if (!_playerInputControllerMap.TryGetValue(playerId, out var value)) return;
            if(value == _mainPlayerInputController) return;
            SetMainPlayerInputController(value);
        }

        /// <summary>
        /// 获取玩家控制器引用
        /// </summary>
        public static PlayerInputController GetPlayerInputController(int playerId)
        {
            return _playerInputControllerMap[playerId];
        }

        /// <summary>
        /// 获取玩家输入控制器列表(注意:该方法会产生少量GC高频调用慎用)
        /// </summary>
        public static PlayerInputController[] GetPlayerInputControllerList()
        {
            return new List<PlayerInputController>(_playerInputControllerList).ToArray();
        }
        
        /// <summary>
        /// 获取空闲玩家输入控制器列表(注意:该方法会产生少量GC高频调用慎用)
        /// </summary>
        public static PlayerInputController[] GetIdlePlayerInputControllerList()
        {
            var ls = new List<PlayerInputController>();
            foreach (var value in _idAndDevicesIdMap.Keys)
            {
                if (_idAndDevicesIdMap[value] == -1)
                {
                    ls.Add(_playerInputControllerMap[value]);
                }
            }
            return ls.ToArray();
        }
        

        /// <summary>
        /// 获取玩家控制器Id
        /// </summary>
        public static int GetPlayerInputControllerId(PlayerInputController playerInputController)
        {
            return _playerInputControllerMapId[playerInputController];
        }

        /// <summary>
        /// 添加主玩家控制器改变事件
        /// </summary>
        public static void AddMainPlayerInputControllerChangeListener(
            Action<EUMainInputControllerChangeData> onMainInputControllerChangeAction) =>
            _onMainInputControllerChange += onMainInputControllerChangeAction;

        /// <summary>
        /// 移除主玩家控制器改变事件
        /// </summary>
        public static void RemoveMainPlayerInputControllerChangeListener(
            Action<EUMainInputControllerChangeData> onMainInputControllerChangeAction) =>
            _onMainInputControllerChange -= onMainInputControllerChangeAction;

        /// <summary>
        /// 移除所有主玩家控制器改变事件
        /// </summary>
        public static void RemoveAllMainPlayerInputControllerChangeListener() => _onMainInputControllerChange = null;

        /// <summary>
        /// 添加玩家控制器的设备改变的事件
        /// </summary>
        public static void AddPlayerInputControllerOfDeviceChangeListener(
            Action<EUPlayerInputOfDeviceChangeData> onPlayerInputControllerOfDeviceChange) =>
            _onPlayerInputControllerOfDeviceChange += onPlayerInputControllerOfDeviceChange;
        
        /// <summary>
        /// 移除玩家控制器的设备改变的事件
        /// </summary>
        public static void RemovePlayerInputControllerOfDeviceChangeListener(
            Action<EUPlayerInputOfDeviceChangeData> onPlayerInputControllerOfDeviceChange) =>
            _onPlayerInputControllerOfDeviceChange -= onPlayerInputControllerOfDeviceChange;
        
        /// <summary>
        /// 移除所有玩家控制器的设备改变的事件
        /// </summary>
        public static void RemoveAllPlayerInputControllerOfDeviceChangeListener() => _onPlayerInputControllerOfDeviceChange = null;
        #endregion

        #region 玩家输入控制器设备相关

        private static void AddPlayerInputDevice(int deviceId, InputDevice inputDevice)
        {
            if (!_playerInputDeviceMap.TryAdd(deviceId, inputDevice)) return;
            _playerInputDeviceList.Add(inputDevice);
            _devicesIdAndIdMap.Add(deviceId, -1);//添加映射关系
            _onAddedDevice?.Invoke(inputDevice);
        }

        private static void RemovePlayerInputDevice(int deviceId)
        {
            if (!_playerInputDeviceMap.Remove(deviceId, out var inputDevice)) return;
            _onRemovedDevice?.Invoke(inputDevice);
            if (_devicesIdAndIdMap[deviceId] != -1)
            {
                SetPlayerInputControllerOfDevice(_devicesIdAndIdMap[deviceId],null);
            }
            _playerInputDeviceList.Remove(inputDevice);
            _devicesIdAndIdMap.Remove(inputDevice.deviceId);//移除映射关系
        }
        /// <summary>
        /// 获取设备对应的角色控制器(如果返回值为空表示没有设备没有对应的角色控制器)
        /// </summary>
        public static PlayerInputController GetPlayerInputDeviceOfPlayerInputController(int deviceId)
        {
            if (_devicesIdAndIdMap[deviceId] == -1) return null;
            return _playerInputControllerMap[_devicesIdAndIdMap[deviceId]];
        }
        
        /// <summary>
        /// 获取设备对应的角色控制器
        /// </summary>
        public static PlayerInputController GetPlayerInputDeviceOfPlayerInputController(InputDevice inputDevice)
        {
            return GetPlayerInputDeviceOfPlayerInputController(inputDevice.deviceId);
        }

        /// <summary>
        /// 获取设备数量
        /// </summary>
        /// <returns>设备数量</returns>
        public static int GetPlayerInputDeviceCount()
        {
            return _playerInputDeviceMap.Count;
        } 

        /// <summary>
        /// 获取玩家输入设备列表(注意:该方法会产生少量GC高频调用慎用)
        /// </summary>
        public static InputDevice[] GetPlayerInputDeviceList()
        {
            return new List<InputDevice>(_playerInputDeviceList).ToArray();
        } 
        /// <summary>
        /// 获取空闲玩家输入设备列表(注意:该方法会产生少量GC高频调用慎用)
        /// </summary>
        /// <returns></returns>
        public static InputDevice[] GetIdlePlayerInputDeviceList()
        {
            List<InputDevice> inputDevices = new List<InputDevice>();
            foreach (var value in _devicesIdAndIdMap.Keys)
            {
                if (_devicesIdAndIdMap[value] == -1)
                {
                    inputDevices.Add(_playerInputDeviceMap[value]);
                }
            }
            return inputDevices.ToArray();
        }

        /// <summary>
        /// 获取玩家输入设备字典(注意:该方法会产生少量GC高频调用慎用)
        /// </summary>
        public static Dictionary<int, InputDevice> GetPlayerInputDeviceDictionary()
        {
            return new Dictionary<int, InputDevice>(_playerInputDeviceMap);
        }//确保外部不会直接修改原先的字典

        /// <summary>
        /// 添加玩家输入设备接入事件监听
        /// </summary>
        public static void AddPlayerInputDeviceAddedListener(Action<InputDevice> onInputDeviceAdded)
        {
            _onAddedDevice += onInputDeviceAdded;
        }

        /// <summary>
        /// 移除玩家输入设备接入事件监听
        /// </summary>
        public static void RemovePlayerInputDeviceAddedListener(Action<InputDevice> onInputDeviceAdded)
        {
            _onAddedDevice -= onInputDeviceAdded;
        }

        /// <summary>
        /// 清空玩家输入接入事件监听
        /// </summary>
        public static void RemoveAllPlayerInputDeviceAddedListener() => _onAddedDevice = null;

        /// <summary>
        /// 添加玩家输入设备移除事件监听
        /// </summary>
        public static void AddPlayerInputDeviceRemovedListener(Action<InputDevice> onInputDeviceRemoved)
        {
            _onRemovedDevice += onInputDeviceRemoved;
        }

        /// <summary>
        /// 移除玩家输入设备移除事件监听
        /// </summary>
        public static void RemovePlayerInputDeviceRemovedListener(Action<InputDevice> onInputDeviceRemoved)
        {
            _onRemovedDevice -= onInputDeviceRemoved;
        }

        /// <summary>
        /// 清空玩家输入移除事件监听
        /// </summary>
        public static void RemoveAllPlayerInputDeviceRemovedListener() => _onRemovedDevice = null;

        #endregion

#if UNITY_EDITOR
        private static void LogDebugData(string log = "")
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"[EUInputController] {log}");
            
            sb.AppendLine($"[EUInputController] 当前手柄设备数量:{GetPlayerInputDeviceCount()} ");
            
            sb.AppendLine("[EUInputController] 调试数据:");
            
            sb.AppendLine($"_mainPlayerInputController (主玩家控制器): {_mainPlayerInputController}");

            sb.AppendLine($"_playerInputControllerList (玩家控制器列表) 数量: {_playerInputControllerList.Count}");
            for(int i = 0; i < _playerInputControllerList.Count; i++)
            {
                sb.AppendLine($"  索引: {i}, 值: {_playerInputControllerList[i]}");
            }

            sb.AppendLine($"_playerInputDeviceList (玩家输入设备列表) 数量: {_playerInputDeviceList.Count}");
            for(int i = 0; i < _playerInputDeviceList.Count; i++)
            {
                var device = _playerInputDeviceList[i];
                sb.AppendLine($"  索引: {i}, 设备ID: {device.deviceId}, 设备名称: {device.name}");
            }

            sb.AppendLine($"_playerInputControllerMap 数量: {_playerInputControllerMap.Count}");
            foreach(var kvp in _playerInputControllerMap)
            {
                sb.AppendLine($"  键: {kvp.Key}, 值: {kvp.Value}");
            }

            sb.AppendLine($"_playerInputControllerMapId 数量: {_playerInputControllerMapId.Count}");
            foreach(var kvp in _playerInputControllerMapId)
            {
                sb.AppendLine($"  键: {kvp.Key}, 值: {kvp.Value}");
            }

            sb.AppendLine($"_playerInputDeviceMap 数量: {_playerInputDeviceMap.Count}");
            foreach(var kvp in _playerInputDeviceMap)
            {
                sb.AppendLine($"  键: {kvp.Key}, 值: {kvp.Value.name}");
            }

            sb.AppendLine($"_idAndDevicesIdMap 数量: {_idAndDevicesIdMap.Count}");
            foreach(var kvp in _idAndDevicesIdMap)
            {
                sb.AppendLine($"控制器ID: {kvp.Key}, 设备ID: {kvp.Value}");
            }

            sb.AppendLine($"_devicesIdAndIdMap 数量: {_devicesIdAndIdMap.Count}");
            foreach(var kvp in _devicesIdAndIdMap)
            {
                sb.AppendLine($"设备ID: {kvp.Key}, 控制器ID: {kvp.Value}");
            }
            Debug.Log(sb.ToString());
        }
#endif
    }
}