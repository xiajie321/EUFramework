using Cysharp.Threading.Tasks;
using YooAsset;
using UnityEngine;

namespace EUFramework.Extension.EURes
{
    internal class FsmInitializePackage : IStateNode
    {
        private StateMachine _machine;
        public void OnCreate(StateMachine machine)
        {
            _machine = machine;
        }

        public void OnEnter()
        {
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            var playMode = (EPlayMode)_machine.GetBlackboardValue("PlayMode");
            var packageName = (string)_machine.GetBlackboardValue("PackageName");
            Debug.Log($"[Fsm] FsmInitializePackage OnEnter package={packageName} playMode={playMode}");

            // 创建资源包裹类
            var package = YooAssets.TryGetPackage(packageName);
            if (package == null)
                package = YooAssets.CreatePackage(packageName);

            InitializationOperation initializationOperation = null;

            //编辑器模式
            if (playMode == EPlayMode.EditorSimulateMode)
            {
                Debug.Log($"[Fsm] FsmInitializePackage EditorSimulateMode SimulateBuild");
                var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
                var packageRoot = buildResult.PackageRootDirectory;
                var createParameters = new EditorSimulateModeParameters();
                createParameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
                initializationOperation = package.InitializeAsync(createParameters);
            }

            // 单机运行模式
            if (playMode == EPlayMode.OfflinePlayMode)
            {
                Debug.Log($"[Fsm] FsmInitializePackage OfflinePlayMode");
                var createParameters = new OfflinePlayModeParameters();
                createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                initializationOperation = package.InitializeAsync(createParameters);
            }

            // WebGL运行模式
            if (playMode == EPlayMode.WebPlayMode)
            {
                Debug.Log($"[Fsm] FsmInitializePackage WebPlayMode");
#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
            var createParameters = new WebPlayModeParameters();
			string defaultHostServer = GetHostServerURL();
            string fallbackHostServer = GetHostServerURL();
            string packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE"; //注意：如果有子目录，请修改此处！
            IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            createParameters.WebServerFileSystemParameters = WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices);
            initializationOperation = package.InitializeAsync(createParameters);
#else
                var createParameters = new WebPlayModeParameters();
                createParameters.WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
                initializationOperation = package.InitializeAsync(createParameters);
#endif
            }

            // 若未进入任何模式或被空包保护提前返回，避免空引用
            var owner = _machine.Owner as EUResKitPatchOperation;
            if (initializationOperation == null)
            {
                Debug.Log("[Fsm] FsmInitializePackage initializationOperation==null, SetFinish并return");
                owner?.SetFinish();
                return;
            }

            Debug.Log($"[Fsm] FsmInitializePackage 即将 await initializationOperation");
            await initializationOperation;
            Debug.Log($"[Fsm] FsmInitializePackage await 返回 Status={initializationOperation.Status}");

            if (initializationOperation.Status != EOperationStatus.Succeed)
            {
                Debug.Log("[Fsm] FsmInitializePackage 初始化失败，触发 OnInitializePackageFailed");
                owner?.OnInitializePackageFailed?.Invoke();
            }
            else
            {
                owner?.ResetInitRetryCount();
                Debug.Log("[Fsm] FsmInitializePackage 成功，即将 ChangeState FsmRequestPackageVersion");
                _machine.ChangeState<FsmRequestPackageVersion>();
            }
        }

        /// <summary>
        /// 获取资源服务器地址
        /// </summary>
        private string GetHostServerURL()
        {
            // 加载服务器配置
            var config = Resources.Load<EUResServerConfig>("EUResKitSettings/EUResServerConfig");
            if (config == null)
            {
                Debug.LogError("[FsmInitializePackage] 未找到 EUResServerConfig 配置文件，请先创建配置！");
                return string.Empty;
            }

            if (!config.IsValid())
            {
                Debug.LogError("[FsmInitializePackage] EUResServerConfig 配置无效，请检查配置！");
                return string.Empty;
            }

            // 获取服务器地址和版本
            string hostServerURL = config.GetServerUrl();
            string appVersion = config.appVersion;

#if UNITY_EDITOR
            if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
                return $"{hostServerURL}/CDN/Android/{appVersion}";
            else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
                return $"{hostServerURL}/CDN/IPhone/{appVersion}";
            else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
                return $"{hostServerURL}/CDN/WebGL/{appVersion}";
            else
                return $"{hostServerURL}/CDN/PC/{appVersion}";
#else
        if (Application.platform == RuntimePlatform.Android)
            return $"{hostServerURL}/CDN/Android/{appVersion}";
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
            return $"{hostServerURL}/CDN/IPhone/{appVersion}";
        else if (Application.platform == RuntimePlatform.WebGLPlayer)
            return $"{hostServerURL}/CDN/WebGL/{appVersion}";
        else
            return $"{hostServerURL}/CDN/PC/{appVersion}";
#endif
        }

        /// <summary>
        /// 远端资源地址查询服务类
        /// </summary>
        private class RemoteServices : IRemoteServices
        {
            private readonly string _defaultHostServer;
            private readonly string _fallbackHostServer;

            public RemoteServices(string defaultHostServer, string fallbackHostServer)
            {
                _defaultHostServer = defaultHostServer;
                _fallbackHostServer = fallbackHostServer;
            }
            string IRemoteServices.GetRemoteMainURL(string fileName)
            {
                return $"{_defaultHostServer}/{fileName}";
            }
            string IRemoteServices.GetRemoteFallbackURL(string fileName)
            {
                return $"{_fallbackHostServer}/{fileName}";
            }
        }
        public void OnExit()
        {
            
        }

        public void OnUpdate()
        {
        
        }
    }
}
