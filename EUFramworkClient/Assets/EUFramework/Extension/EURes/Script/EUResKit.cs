using System;
using UnityEngine;
using YooAsset;
using Cysharp.Threading.Tasks;

namespace EUFramework.Extension.EURes
{
    /// <summary>
    /// EUResKit 资源管理入口类
    /// 用户可编辑版本 - 包含初始化逻辑
    /// </summary>
    public static partial class EUResKit
    {
        #region 私有字段
        
        private static EUResKitPackageConfig _packageConfig;
        private static GameObject _cachedPopupPrefab;
        private static MonoBehaviour _popupInstance; // 使用 MonoBehaviour 避免编译时依赖
        private static Canvas _popupCanvas;
        private static Action<string, int, int, long, long> _onDownloadProgressChanged;
        private static bool _isYooAssetInitialized = false;
        
        #endregion
        
        #region 下载进度回调
        
        /// <summary>
        /// 设置下载进度回调
        /// </summary>
        /// <param name="callback">
        /// 下载进度回调委托
        /// <para>参数说明：</para>
        /// <list type="bullet">
        /// <item>packageName - 资源包名称</item>
        /// <item>totalCount - 总文件数</item>
        /// <item>currentCount - 当前已下载文件数</item>
        /// <item>totalBytes - 总字节数</item>
        /// <item>currentBytes - 当前已下载字节数</item>
        /// </list>
        /// </param>
        /// <example>
        /// <code>
         /// EUResKit.SetDownloadProgressCallback((packageName, totalCount, currentCount, totalBytes, currentBytes) =>
        /// {
        ///     float progress = (float)currentBytes / totalBytes * 100f;
        ///     Debug.Log($"[{packageName}] 下载进度: {progress:F1}%");
        ///     progressSlider.value = progress / 100f;
        /// });
        /// </code>
        /// </example>
        public static void SetDownloadProgressCallback(Action<string, int, int, long, long> callback)
        {
            _onDownloadProgressChanged = callback;
        }
        
        /// <summary>
        /// 清理过程资源（私有方法，初始化完成后自动调用）
        /// 清理内容：下载进度回调、UI Canvas、Popup 实例等
        /// </summary>
        private static void ClearProcessResources()
        {
            // 清理下载进度回调
            _onDownloadProgressChanged = null;
            
            // 清理 Popup 实例
            if (_popupInstance != null)
            {
                UnityEngine.Object.Destroy(_popupInstance.gameObject);
                _popupInstance = null;
            }
            
            // 清理 Canvas
            if (_popupCanvas != null)
            {
                UnityEngine.Object.Destroy(_popupCanvas.gameObject);
                _popupCanvas = null;
            }
            
            // 清理 Prefab 缓存（可选，Resources 会自动管理）
            _cachedPopupPrefab = null;
        }
        
        #endregion
        
        #region 配置加载
        
        /// <summary>
        /// 加载 Package 配置
        /// </summary>
        private static EUResKitPackageConfig LoadPackageConfig()
        {
            if (_packageConfig == null)
            {
                _packageConfig = Resources.Load<EUResKitPackageConfig>("EUResKitSettings/EUResKitPackageConfig");
                if (_packageConfig == null)
                {
                    Debug.LogError("[EUResKit] 未找到 EUResKitPackageConfig，请先创建配置文件");
                }
            }
            return _packageConfig;
        }
        
        #endregion
        
        #region UI 管理
        
        /// <summary>
        /// 加载并缓存用户操作弹窗 Prefab
        /// </summary>
        private static GameObject LoadUserOperationPopupPrefab()
        {
            if (_cachedPopupPrefab != null)
            {
                return _cachedPopupPrefab;
            }
            
            _cachedPopupPrefab = Resources.Load<GameObject>("EUResKitUI/EUResKitUserOpePopUp");
            if (_cachedPopupPrefab == null)
            {
                Debug.LogError("[EUResKit] 未找到 EUResKitUserOpePopUp Prefab，路径: Resources/EUResKitUI/EUResKitUserOpePopUp");
                return null;
            }
            
            return _cachedPopupPrefab;
        }
        
        /// <summary>
        /// 获取或创建 EventSystem（兼容 Input System）
        /// </summary>
        private static UnityEngine.EventSystems.EventSystem GetOrCreateEventSystem()
        {
            var existingEventSystem = UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (existingEventSystem != null)
            {
                return existingEventSystem;
            }
            
            var eventSystemObj = new GameObject("ResKitEventSystem");
            var eventSystem = eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            
            // 尝试添加 Input System 模块（使用反射避免编译错误）
            bool hasInputSystem = false;
            try
            {
                var inputSystemType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemType != null)
                {
                    eventSystemObj.AddComponent(inputSystemType);
                    hasInputSystem = true;
                }
            }
            catch (System.Exception)
            {
                // 忽略异常，使用传统模块
            }
            
            if (!hasInputSystem)
            {
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            UnityEngine.Object.DontDestroyOnLoad(eventSystemObj);
            return eventSystem;
        }
        
        /// <summary>
        /// 获取或创建 Popup Canvas（自动适配横竖屏）
        /// </summary>
        private static Canvas GetOrCreatePopupCanvas()
        {
            if (_popupCanvas != null)
            {
                return _popupCanvas;
            }
            
            // 查找现有的 Canvas
            var existingCanvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (existingCanvas != null)
            {
                _popupCanvas = existingCanvas;
                GetOrCreateEventSystem();
                return _popupCanvas;
            }
            
            // 确保有 EventSystem
            GetOrCreateEventSystem();
            
            // 创建新的 Canvas
            var canvasObj = new GameObject("ResKitPopupCanvas");
            _popupCanvas = canvasObj.AddComponent<Canvas>();
            _popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _popupCanvas.sortingOrder = 9999;
            
            // 添加 CanvasScaler - 自动适配横竖屏
            var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            
            // 自动检测屏幕方向并适配
            bool isPortrait = Screen.height > Screen.width;
            if (isPortrait)
            {
                // 竖屏模式（如手机竖屏：1080x1920）
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 1f; // 高度优先
            }
            else
            {
                // 横屏模式（如PC/横屏手机：1920x1080）
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0f; // 宽度优先
            }
            
            scaler.referencePixelsPerUnit = 100f;
            
            // 添加 GraphicRaycaster
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // 跨场景保持
            UnityEngine.Object.DontDestroyOnLoad(canvasObj);
            
            return _popupCanvas;
        }
        
        /// <summary>
        /// 获取或创建用户操作弹窗实例（单例）
        /// </summary>
        private static MonoBehaviour GetOrCreatePopupInstance()
        {
            if (_popupInstance != null)
            {
                return _popupInstance;
            }
            
            var prefab = LoadUserOperationPopupPrefab();
            if (prefab == null)
            {
                return null;
            }
            
            var canvas = GetOrCreatePopupCanvas();
            if (canvas == null)
            {
                Debug.LogError("[EUResKit] 无法创建或找到 Canvas");
                return null;
            }
            
            // 实例化并挂载到 Canvas 下
            var instance = UnityEngine.Object.Instantiate(prefab, canvas.transform);
            
            // 使用反射动态查找 EUResKitUserOpePopUp 类型（避免编译时依赖）
            var component = instance.GetComponent<MonoBehaviour>();
            var popupTypeName = "EUFramework.Extension.EURes.EUResKitUserOpePopUp";
            var popupType = System.Type.GetType(popupTypeName);
            
            if (popupType != null)
            {
                component = instance.GetComponent(popupType) as MonoBehaviour;
                if (component == null)
                {
                    component = (MonoBehaviour)instance.AddComponent(popupType);
                }
            }
            else
            {
                Debug.LogWarning($"[EUResKit] 未找到类型 {popupTypeName}，使用默认 MonoBehaviour");
            }
            
            // 跨场景保持
            UnityEngine.Object.DontDestroyOnLoad(instance);
            
            // 初始状态为隐藏
            instance.SetActive(false);
            
            _popupInstance = component;
            return _popupInstance;
        }
        
        #endregion
        
        #region 热更新回调配置
        
        /// <summary>
        /// 配置补丁操作的用户交互回调
        /// </summary>
        internal static void SetupPatchOperationCallbacks(EUResKitPatchOperation patchOperation, string packageName)
        {
            if (patchOperation == null)
            {
                return;
            }
            
            // 懒加载 Popup 实例（使用反射动态调用方法）
            Func<MonoBehaviour> GetPopup = () =>
            {
                var popup = GetOrCreatePopupInstance();
                if (popup == null)
                {
                    Debug.LogError("[EUResKit] 无法创建用户操作弹窗");
                }
                return popup;
            };
            
            // 1. 初始化失败回调
            patchOperation.OnInitializePackageFailed = () =>
            {
                var popup = GetPopup();
                if (popup != null)
                {
                    // 使用反射调用 Show 方法
                    var showMethod = popup.GetType().GetMethod("Show",
                        new System.Type[] { typeof(string), typeof(string), typeof(Action), typeof(Action) });
                    if (showMethod != null)
                    {
                        showMethod.Invoke(popup, new object[] {
                            "初始化失败",
                            $"资源包 [{packageName}] 初始化失败\n是否重试？",
                            (Action)(() => patchOperation.UserRetryInitialize()),
                            (Action)(() => { Debug.LogError($"[EUResKit] 用户取消初始化资源包: {packageName}"); patchOperation.UserCancel(); })
                        });
                    }
                    else
                        patchOperation.UserCancel();
                }
                else
                    patchOperation.UserCancel();
            };
            
            // 2. 版本请求失败回调
            patchOperation.OnPackageVersionRequestFailed = () =>
            {
                var popup = GetPopup();
                if (popup != null)
                {
                    var showMethod = popup.GetType().GetMethod("Show",
                        new System.Type[] { typeof(string), typeof(string), typeof(Action), typeof(Action) });
                    if (showMethod != null)
                    {
                        showMethod.Invoke(popup, new object[] {
                            "版本检查失败",
                            $"无法获取资源包 [{packageName}] 的版本信息\n请检查网络连接后重试",
                            (Action)(() => patchOperation.UserRetryRequestVersion()),
                            (Action)(() => { Debug.LogError($"[EUResKit] 用户取消版本检查: {packageName}"); patchOperation.UserCancel(); })
                        });
                    }
                    else
                        patchOperation.UserCancel();
                }
                else
                    patchOperation.UserCancel();
            };
            
            // 3. 清单更新失败回调
            patchOperation.OnUpdatePackageManifestFailed = () =>
            {
                var popup = GetPopup();
                if (popup != null)
                {
                    var showMethod = popup.GetType().GetMethod("Show",
                        new System.Type[] { typeof(string), typeof(string), typeof(Action), typeof(Action) });
                    if (showMethod != null)
                    {
                        showMethod.Invoke(popup, new object[] {
                            "更新失败",
                            $"资源包 [{packageName}] 清单更新失败\n是否重试？",
                            (Action)(() => patchOperation.UserRetryUpdateManifest()),
                            (Action)(() => { Debug.LogError($"[EUResKit] 用户取消清单更新: {packageName}"); patchOperation.UserCancel(); })
                        });
                    }
                    else
                        patchOperation.UserCancel();
                }
                else
                    patchOperation.UserCancel();
            };
            
            // 4. 发现更新文件回调
            patchOperation.OnFoundUpdateFiles = (totalCount, totalBytes) =>
            {
                var popup = GetPopup();
                if (popup != null)
                {
                    float sizeMB = totalBytes / (1024f * 1024f);
                    var showMethod = popup.GetType().GetMethod("Show",
                        new System.Type[] { typeof(string), typeof(string), typeof(Action), typeof(Action) });
                    if (showMethod != null)
                    {
                        showMethod.Invoke(popup, new object[] {
                            "发现新版本",
                            $"需要下载 {totalCount} 个文件\n大小: {sizeMB:F2} MB\n是否开始下载？",
                            (Action)(() => patchOperation.UserBeginDownloadWebFiles()),
                            (Action)(() => { Debug.LogError($"[EUResKit] 用户取消下载: {packageName}"); patchOperation.UserCancel(); })
                        });
                    }
                    else
                        patchOperation.UserCancel();
                }
                else
                    patchOperation.UserCancel();
            };
            
            // 5. 下载错误回调
            patchOperation.OnDownloadError = (fileName, errorInfo) =>
            {
                var popup = GetPopup();
                if (popup != null)
                {
                    var showMethod = popup.GetType().GetMethod("Show",
                        new System.Type[] { typeof(string), typeof(string), typeof(Action), typeof(Action) });
                    if (showMethod != null)
                    {
                        showMethod.Invoke(popup, new object[] {
                            "下载失败",
                            $"文件下载失败\n文件: {fileName}\n错误: {errorInfo}\n是否重试？",
                            (Action)(() => patchOperation.UserBeginDownloadWebFiles()),
                            (Action)(() => { Debug.LogError($"[EUResKit] 用户取消下载重试: {packageName}"); patchOperation.UserCancel(); })
                        });
                    }
                    else
                        patchOperation.UserCancel();
                }
                else
                    patchOperation.UserCancel();
            };
            
            // 6. 下载进度回调
            patchOperation.OnDownloadUpdate = (totalCount, currentCount, totalBytes, currentBytes) =>
            {
                // 触发下载进度回调，外部可通过 SetDownloadProgressCallback 设置回调来更新 UI
                _onDownloadProgressChanged?.Invoke(packageName, totalCount, currentCount, totalBytes, currentBytes);
            };
        }
        
        #endregion
        
        #region 包初始化
        
        /// <summary>
        /// 初始化所有配置的资源包
        /// 游戏启动时调用此方法，自动从配置读取所有包并初始化
        /// </summary>
        /// <param name="onPackageInitialized">每个包初始化完成后的回调 (packageName, isSuccess)</param>
        /// <param name="onAllCompleted">所有包初始化完成后的回调 (allSuccess)</param>
        public static async UniTask<bool> InitializeAllPackagesAsync(
            Action<string, bool> onPackageInitialized = null,
            Action<bool> onAllCompleted = null)
        {
            // 初始化 YooAsset（只需要调用一次）
            if (!_isYooAssetInitialized)
            {
                YooAssets.Initialize();
                _isYooAssetInitialized = true;
            }

            var config = LoadPackageConfig();
            if (config == null)
            {
                Debug.LogError("[EUResKit] 配置文件加载失败，无法初始化");
                onAllCompleted?.Invoke(false);
                return false;
            }
            var packages = config.GetAllPackages();
            if (packages == null || packages.Count == 0)
            {
                Debug.LogWarning("[EUResKit] 没有配置任何 Package");
                onAllCompleted?.Invoke(false);
                return false;
            }           
            bool allSuccess = true;

            foreach (var packageInfo in packages)
            {
                if(packageInfo.packageName=="Builtin") continue;
                // 调用 Generated 分部类的初始化方法
                bool success = await InitPackageResAsync(packageInfo.packageName, packageInfo.playMode);
                if (success)
                {
                    // 只设置配置中明确标记的默认包
                    if (packageInfo.isDefault)
                    {
                        SetDefaultPackage(packageInfo.packageName);
                    }
                    
                    // 回调：包初始化成功
                    onPackageInitialized?.Invoke(packageInfo.packageName, true);
                }
                else
                {
                    Debug.LogError($"[EUResKit] 包 {packageInfo.packageName} 初始化失败");
                    allSuccess = false;
                    
                    // 回调：包初始化失败
                    onPackageInitialized?.Invoke(packageInfo.packageName, false);
                }
            }
            // 回调：所有包初始化完成
            onAllCompleted?.Invoke(allSuccess);
            
            // 清理过程资源（下载进度回调、UI Canvas、Popup 实例等）
            ClearProcessResources();
            return allSuccess;
        }
        
        #endregion
    }
}
