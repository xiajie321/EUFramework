using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EUFramework.Extension.EUInputController
{
    public struct EUMainInputControllerChangeData
    {
        public PlayerInputController LastPlayerInputController;
        public PlayerInputController CurrentPlayerInputController;
    }

    public class EUInputController
    {
        private EUInputController()
        {
        }

        private static EUInputController _instance;

        public static EUInputController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EUInputController();
                    _instance.Init();
                }

                return _instance;
            }
        }

        private Action<InputDevice> _onAddedDevice;
        private Action<InputDevice> _onRemovedDevice;
        private Action<EUMainInputControllerChangeData> _onMainInputControllerChange;
        private Action<InputDevice> _onPlayerInputOfDeviceChange;//TODO 玩家控制器输入设备改变事件
        private PlayerInputController _mainPlayerInputController; //主控玩家控制器
        private List<PlayerInputController> _playerInputControllerList; //用来记录玩家控制器进入的先后顺序
        private List<InputDevice> _playerInputDeviceList;//用来记录玩家控制设备进入的先后顺序
        private Dictionary<PlayerInputController, int> _playerInputControllerMapId; //给控制器标记Id
        private Dictionary<int, PlayerInputController> _playerInputControllerMap; //标记对应ID的控件
        private Dictionary<int, InputDevice> _playerInputDeviceMap;//用于存储设备
        private Dictionary<int, int> _idAndDevicesIdMap;//用于处理设备与控制器的映射关系方便通过控制器id快速找到对应设备
        private Dictionary<int, int> _devicesIdAndIdMap;//用于处理设备与控制器的映射关系方便通过设备id快速找到对应控制器
        private int _maxPlayerInputControllers = 4;

        public int MaxPlayerInputControllers
        {
            get => _maxPlayerInputControllers;
            set
            {
                if (_maxPlayerInputControllers == value) return;
                if (_maxPlayerInputControllers > value && _playerInputControllerList.Count > 0)
                {
                    int ls = _maxPlayerInputControllers - value;
                    for (int i = 1; i < ls; i++)
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
        public int CurrentPlayerInputControllerCount
        {
            get => _playerInputControllerList.Count;
        }
        /// <summary>
        /// 当前已连接的输入设备数量
        /// </summary>
        public int CurrentPlayerInputDeviceCount
        {
            get => _playerInputDeviceList.Count;
        }

        private int _id = 0;
        
        private void Init()
        {
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
            
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void OnDeviceChange(InputDevice inputDevice, InputDeviceChange change)
        {
            if (inputDevice is not Gamepad) return;
            if (change == InputDeviceChange.Added)
            {
                AddPlayerInputDevice(inputDevice.deviceId, inputDevice);
            }

            if (change == InputDeviceChange.Removed)
            {
                RemovePlayerInputDevice(inputDevice.deviceId);
            }
#if UNITY_EDITOR
            Debug.Log($"[EUInputController] 当前手柄设备数量:{GetPlayerInputDeviceCount()} ");
#endif
        }

        #region 玩家输入控制器相关
        
        private void AddPlayerInputController(int playerId)
        {
            if(CurrentPlayerInputControllerCount >= _maxPlayerInputControllers) return;
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
        /// <param name="playerId"></param>
        public void RemovePlayerInputController(int playerId)
        {
            if(CurrentPlayerInputControllerCount <= 1) return;
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
        /// <param name="playerInputController"></param>
        public void RemovePlayerInputController(PlayerInputController playerInputController)
        {
            RemovePlayerInputController(GetPlayerInputControllerId(playerInputController));
        }
        /// <summary>
        /// 添加玩家输入控制器
        /// </summary>
        /// <returns></returns>
        public int AddPlayerInputController()
        {
            AddPlayerInputController(_id++);
            return _id;
        }
        /// <summary>
        /// 绑定玩家输入控制器的输入设备
        /// </summary>
        /// <param name="playerInputController">玩家输入控制器引用</param>
        /// <param name="inputDevice">输入设备的引用</param>
        public void SetPlayerInputControllerOfDevice(PlayerInputController playerInputController,InputDevice inputDevice)
        {
            int id = GetPlayerInputControllerId(playerInputController);
            if (inputDevice is Gamepad device)
            {
                playerInputController.BindGamepad(device);
                if (_devicesIdAndIdMap[inputDevice.deviceId] != -1)//该设备原先有对应的控制器
                {
                    SetPlayerInputControllerOfDevice(_playerInputControllerMap[_devicesIdAndIdMap[inputDevice.deviceId]],null);//设置该设备原先的控制器为空
                }
                _idAndDevicesIdMap[id] = inputDevice.deviceId;
                _devicesIdAndIdMap[inputDevice.deviceId] = id;
                return;
            }

            if (inputDevice != null) return;
            if (_idAndDevicesIdMap[id] != -1)//该控制器原先有对应的设备
            {
                _devicesIdAndIdMap[_idAndDevicesIdMap[id]] = -1;//将该设备原先的控制器标记为无对应引用
                _idAndDevicesIdMap[id] = -1;//标记当前控制器对应的设备为无对应引用
            }
            playerInputController.BindGamepad(null);
        }
        /// <summary>
        /// 绑定玩家输入控制器的输入设备
        /// </summary>
        /// <param name="playerId">玩家输入控制器Id</param>
        /// <param name="inputDevice">输入设备的引用</param>
        public void SetPlayerInputControllerOfDevice(int playerId, InputDevice inputDevice)
        {
            PlayerInputController playerInputController = GetPlayerInputController(playerId);
            SetPlayerInputControllerOfDevice(playerInputController, inputDevice);
        }
        /// <summary>
        /// 获取当前的主玩家控制器
        /// </summary>
        /// <returns></returns>
        public PlayerInputController GetMainPlayerInputController()
        {
            return _mainPlayerInputController;
        }

        /// <summary>
        /// 设置主玩家控制器
        /// </summary>
        /// <param name="playerInputController"></param>
        public void SetMainPlayerInputController(PlayerInputController playerInputController)
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
        /// </summary>
        /// <param name="playerId"></param>
        public void SetMainPlayerInputController(int playerId)
        {
            if (!_playerInputControllerMap.TryGetValue(playerId, out var value)) return;
            if(value == _mainPlayerInputController) return;
            SetMainPlayerInputController(value);
        }

        /// <summary>
        /// 获取玩家控制器引用
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public PlayerInputController GetPlayerInputController(int playerId)
        {
            return _playerInputControllerMap[playerId];
        }
        
        /// <summary>
        /// 获取玩家输入控制器列表(注意:该方法会产生少量GC高频调用慎用)
        /// </summary>
        public PlayerInputController[] GetPlayerInputControllerList()=>new List<PlayerInputController>(_playerInputControllerList).ToArray();
        
        /// <summary>
        /// 获取空闲玩家输入控制器列表(注意:该方法会产生少量GC高频调用慎用)
        /// </summary>
        public PlayerInputController[] GetIdlePlayerInputControllerList()
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
        /// <param name="playerInputController"></param>
        /// <returns></returns>
        public int GetPlayerInputControllerId(PlayerInputController playerInputController)
        {
            return _playerInputControllerMapId[playerInputController];
        }

        /// <summary>
        /// 添加主玩家控制器改变事件
        /// </summary>
        public void AddMainPlayerInputControllerChangeListener(
            Action<EUMainInputControllerChangeData> onMainInputControllerChangeAction) =>
            _onMainInputControllerChange += onMainInputControllerChangeAction;

        /// <summary>
        /// 移除主玩家控制器改变事件
        /// </summary>
        public void RemoveMainPlayerInputControllerChangeListener(
            Action<EUMainInputControllerChangeData> onMainInputControllerChangeAction) =>
            _onMainInputControllerChange -= onMainInputControllerChangeAction;

        /// <summary>
        /// 移除所有主玩家控制器改变事件
        /// </summary>
        public void RemoveAllMainPlayerInputControllerChangeListener() => _onMainInputControllerChange = null;

        #endregion

        #region 玩家输入控制器设备相关

        private void AddPlayerInputDevice(int deviceId, InputDevice inputDevice)
        {
            if (!_playerInputDeviceMap.TryAdd(deviceId, inputDevice)) return;
            _playerInputDeviceList.Add(inputDevice);
            _devicesIdAndIdMap.Add(deviceId, -1);//添加映射关系
            _onAddedDevice?.Invoke(inputDevice);
        }

        private void RemovePlayerInputDevice(int deviceId)
        {
            if (!_playerInputDeviceMap.Remove(deviceId, out var inputDevice)) return;
            if (_devicesIdAndIdMap[deviceId] != -1)
            {
                SetPlayerInputControllerOfDevice(_devicesIdAndIdMap[deviceId],null);
            }
            _playerInputDeviceList.Remove(inputDevice);
            _devicesIdAndIdMap.Remove(inputDevice.deviceId);//移除映射关系
            _onRemovedDevice?.Invoke(inputDevice);
        }

        /// <summary>
        /// 获取设备数量
        /// </summary>
        /// <returns>设备数量</returns>
        public int GetPlayerInputDeviceCount() => _playerInputDeviceMap.Count;

        /// <summary>
        /// 获取玩家输入设备列表(注意:该方法会产生少量GC高频调用慎用)
        /// </summary>
        public InputDevice[] GetPlayerInputDeviceList() => new List<InputDevice>(_playerInputDeviceList).ToArray();
        /// <summary>
        /// 获取空闲玩家输入设备列表(注意:该方法会产生少量GC高频调用慎用)
        /// </summary>
        /// <returns></returns>
        public InputDevice[] GetIdlePlayerInputDeviceList()
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
        public Dictionary<int, InputDevice> GetPlayerInputDeviceDictionary() =>
            new Dictionary<int, InputDevice>(_playerInputDeviceMap); //确保外部不会直接修改原先的字典

        /// <summary>
        /// 添加玩家输入设备接入事件监听
        /// </summary>
        public void AddPlayerInputDeviceAddedListener(Action<InputDevice> onInputDeviceAdded)
        {
            _onAddedDevice += onInputDeviceAdded;
        }

        /// <summary>
        /// 移除玩家输入设备接入事件监听
        /// </summary>
        public void RemovePlayerInputDeviceAddedListener(Action<InputDevice> onInputDeviceAdded)
        {
            _onAddedDevice -= onInputDeviceAdded;
        }

        /// <summary>
        /// 清空玩家输入接入事件监听
        /// </summary>
        public void RemoveAllPlayerInputDeviceAddedListener() => _onAddedDevice = null;

        /// <summary>
        /// 添加玩家输入设备移除事件监听
        /// </summary>
        public void AddPlayerInputDeviceRemovedListener(Action<InputDevice> onInputDeviceRemoved)
        {
            _onRemovedDevice += onInputDeviceRemoved;
        }

        /// <summary>
        /// 移除玩家输入设备移除事件监听
        /// </summary>
        public void RemovePlayerInputDeviceRemovedListener(Action<InputDevice> onInputDeviceRemoved)
        {
            _onRemovedDevice -= onInputDeviceRemoved;
        }

        /// <summary>
        /// 清空玩家输入移除事件监听
        /// </summary>
        public void RemoveAllPlayerInputDeviceRemovedListener() => _onRemovedDevice = null;

        #endregion
    }
}