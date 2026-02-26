using System;
using System.Collections.Generic;
using System.Linq;
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
        private PlayerInputController _mainPlayerInputController; //主控玩家控制器
        private List<PlayerInputController> _playerInputControllers; //用来记录玩家控制器进入的先后顺序
        private Dictionary<PlayerInputController, int> _playerInputControllerMapId; //给控制器标记Id
        private Dictionary<int, PlayerInputController> _playerInputControllerMap; //标记对应ID的控件
        private Dictionary<int, InputDevice> _playerInputDevices;//用于存储设备
        private Dictionary<int, int> _idAndDevicesId;//用于处理设备与控制器的映射关系方便通过控制器id快速找到对应设备
        private Dictionary<int, int> _devicesIdAndId;//用于处理设备与控制器的映射关系方便通过设备id快速找到对应控制器
        private int _maxPlayerInputControllers = 4;
        private bool _isNewGamepadAddPlayerController = true; //在新的游戏手柄接入时添加玩家控制器
        private bool _isFirstGamepadForMainPlayerController = true; //将第一个接入的手柄设为主玩家控制器。

        public int MaxPlayerInputControllers
        {
            get => _maxPlayerInputControllers;
            set
            {
                if (_maxPlayerInputControllers == value) return;
                if (_maxPlayerInputControllers > value && _playerInputControllers.Count > 0)
                {
                    int ls = _maxPlayerInputControllers - value;
                    for (int i = 0; i < ls; i++)
                    {
                        PlayerInputController ls2 = _playerInputControllers[^1];
                        _playerInputControllerMap.Remove(_playerInputControllerMapId[ls2]);
                        _playerInputControllerMapId.Remove(ls2);
                        _playerInputControllers.RemoveAt(_playerInputControllers.Count - 1);
                    }
                }

                _maxPlayerInputControllers = value;
            }
        } //设置一台机器所能容纳的最大玩家控制器数量

        public int CurrentPlayerInputController
        {
            get => _playerInputControllers.Count;
        } //当前已经连接的玩家控制器数量
        public bool IsNewGamepadAddPlayerController
        {
            get => _isNewGamepadAddPlayerController;
            set
            {
                if (_isNewGamepadAddPlayerController == value) return;
                IsNewGamepadAddPlayerController = value;
            }
        }

        public bool IsFirstGamepadForMainPlayerController
        {
            get => _isFirstGamepadForMainPlayerController;
            set
            {
                if (_isFirstGamepadForMainPlayerController == value) return;
                _isFirstGamepadForMainPlayerController = value;
            }
        }

        private int _id = 0;

        private void Init()
        {
            _playerInputControllers = new(_maxPlayerInputControllers);
            _playerInputControllerMap = new(_maxPlayerInputControllers);
            _playerInputControllerMapId = new(_maxPlayerInputControllers);
            _playerInputDevices = new(_maxPlayerInputControllers);
            _idAndDevicesId = new(_maxPlayerInputControllers);
            _devicesIdAndId = new(_maxPlayerInputControllers);
            AddPlayerInputController(_id++);
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void OnDeviceChange(InputDevice inputDevice, InputDeviceChange change)
        {
            if (inputDevice is not Gamepad) return;
            if (change == InputDeviceChange.Added)
            {
                //TODO 需要添加在新的手柄加入时的操作
                AddPlayerInputDevice(inputDevice.deviceId, inputDevice);
            }

            if (change == InputDeviceChange.Removed)
            {
                RemovePlayerInputDevice(inputDevice.deviceId);
            }
#if UNITY_EDITOR
            Debug.Log($"[EUInputController] 当前设备数量:{GetPlayerInputDeviceCount()}");
#endif
        }

        #region 玩家输入控制器相关

        //TODO 需要处理设备接入时的手柄绑定问题
        //TODO 设置手柄时添加在字典中添加对应引用
        private void AddPlayerInputController(int playerId)
        {
            if (_playerInputControllerMap.ContainsKey(playerId)) return;
            var ls = new PlayerInputController();
            _playerInputControllerMap.Add(playerId, ls);
            _playerInputControllerMapId.Add(ls, playerId);
            _playerInputControllers.Add(ls);
            _idAndDevicesId.Add(playerId,-1);
        }

        private void RemovePlayerInputController(int playerId)
        {
            if (!_playerInputControllerMap.TryGetValue(playerId, out var ls)) return;
            _playerInputControllerMapId.Remove(ls);
            _playerInputControllerMap.Remove(playerId);
            _playerInputControllers.Remove(ls);
            if (_idAndDevicesId[playerId] != -1)
                _devicesIdAndId[_idAndDevicesId[playerId]] = -1;
            _idAndDevicesId.Remove(playerId);
            
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
            if (!_playerInputDevices.TryAdd(deviceId, inputDevice)) return;
            _devicesIdAndId.Add(deviceId, -1);//添加映射关系
            _onAddedDevice?.Invoke(inputDevice);
        }

        private void RemovePlayerInputDevice(int deviceId)
        {
            if (!_playerInputDevices.Remove(deviceId, out var inputDevice)) return;
            if (_devicesIdAndId[deviceId] != -1) _idAndDevicesId[_devicesIdAndId[deviceId]] = -1;
            _devicesIdAndId.Remove(inputDevice.deviceId);//移除映射关系
            _onRemovedDevice?.Invoke(inputDevice);
        }

        /// <summary>
        /// 获取设备数量
        /// </summary>
        /// <returns>设备数量</returns>
        public int GetPlayerInputDeviceCount() => _playerInputDevices.Count;

        /// <summary>
        /// 获取玩家输入设备列表(注意:该方法会产生少量GC高频调用慎用)
        /// </summary>
        public InputDevice[] GetPlayerInputDeviceList() => _playerInputDevices.Values.ToArray();

        /// <summary>
        /// 获取玩家输入设备字典(注意:该方法会产生少量GC高频调用慎用)
        /// </summary>
        public Dictionary<int, InputDevice> GetPlayerInputDeviceDictionary() =>
            new Dictionary<int, InputDevice>(_playerInputDevices); //确保外部不会直接修改原先的字典

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