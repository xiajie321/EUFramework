using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;

namespace EUFramwork.Extension.EUAudioKit.Editor
{
    /// <summary>
    /// EUAudio配置窗口
    /// 提供可视化界面来配置音频系统参数
    /// </summary>
    public class EUAudioConfigWindow : EditorWindow
    {
        private string _configPath;
        private string _configFolder;
        
        private EUAudioConfig _currentConfig;
        private Slider _soundVolumeSlider;
        private Slider _bgmVolumeSlider;
        private Slider _voiceVolumeSlider;
        private Slider _globalVolumeSlider;
        private IntegerField _startSoundField;
        private IntegerField _maxSoundField;
        private IntegerField _delayFrameField;
        
        // AudioSource参数UI控件
        private Slider _soundPitchSlider;
        private Slider _soundSpatialBlendSlider;
        private IntegerField _soundPriorityField;
        
        private Slider _bgmPitchSlider;
        private Slider _bgmSpatialBlendSlider;
        private IntegerField _bgmPriorityField;
        
        private Slider _voicePitchSlider;
        private Slider _voiceSpatialBlendSlider;
        private IntegerField _voicePriorityField;
        
        private Label _statusLabel;
        
        [MenuItem("EUFramework/拓展/EUAudio设置")]
        public static void ShowWindow()
        {
            var window = GetWindow<EUAudioConfigWindow>();
            window.titleContent = new GUIContent("EUAudio配置");
            window.minSize = new Vector2(450, 800);
        }
        
        public void CreateGUI()
        {
            // 获取当前脚本所在目录
            var script = MonoScript.FromScriptableObject(this);
            var scriptPath = AssetDatabase.GetAssetPath(script);
            var scriptDirectory = Path.GetDirectoryName(scriptPath);
            
            // 计算配置文件的相对路径（Editor文件夹的上级目录/Resources/EUAudio）
            var extensionDirectory = Path.GetDirectoryName(scriptDirectory); // 获取Extension/EUAudio目录
            _configFolder = Path.Combine(extensionDirectory, "Resources", "EUAudio").Replace("\\", "/");
            _configPath = Path.Combine(_configFolder, "EUAudioConfig.asset").Replace("\\", "/");
            
            // 构建UXML和USS路径
            var uxmlPath = Path.Combine(scriptDirectory, "EUAudioConfigPanel.uxml");
            var ussPath = Path.Combine(scriptDirectory, "EUAudioConfigPanel.uss");
            
            // 加载UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            
            if (visualTree != null)
            {
                visualTree.CloneTree(rootVisualElement);
            }
            else
            {
                Debug.LogError($"无法加载EUAudioConfigPanel.uxml，路径: {uxmlPath}");
                return;
            }
            
            // 加载USS
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogWarning($"无法加载EUAudioConfigPanel.uss，路径: {ussPath}");
            }
            
            // 获取UI元素引用
            _soundVolumeSlider = rootVisualElement.Q<Slider>("sound-volume");
            _bgmVolumeSlider = rootVisualElement.Q<Slider>("bgm-volume");
            _voiceVolumeSlider = rootVisualElement.Q<Slider>("voice-volume");
            _globalVolumeSlider = rootVisualElement.Q<Slider>("global-volume");
            _startSoundField = rootVisualElement.Q<IntegerField>("start-sound");
            _maxSoundField = rootVisualElement.Q<IntegerField>("max-sound");
            _delayFrameField = rootVisualElement.Q<IntegerField>("delay-frame");
            
            // 获取AudioSource参数UI元素
            _soundPitchSlider = rootVisualElement.Q<Slider>("sound-pitch");
            _soundSpatialBlendSlider = rootVisualElement.Q<Slider>("sound-spatial-blend");
            _soundPriorityField = rootVisualElement.Q<IntegerField>("sound-priority");
            
            _bgmPitchSlider = rootVisualElement.Q<Slider>("bgm-pitch");
            _bgmSpatialBlendSlider = rootVisualElement.Q<Slider>("bgm-spatial-blend");
            _bgmPriorityField = rootVisualElement.Q<IntegerField>("bgm-priority");
            
            _voicePitchSlider = rootVisualElement.Q<Slider>("voice-pitch");
            _voiceSpatialBlendSlider = rootVisualElement.Q<Slider>("voice-spatial-blend");
            _voicePriorityField = rootVisualElement.Q<IntegerField>("voice-priority");
            
            _statusLabel = rootVisualElement.Q<Label>("status-label");
            
            // 绑定按钮事件
            var saveBtn = rootVisualElement.Q<Button>("save-btn");
            saveBtn.clicked += OnSaveConfig;
            
            // 加载或创建配置
            LoadOrCreateConfig();
        }
        
        private void LoadOrCreateConfig()
        {
            // 尝试加载现有配置
            _currentConfig = AssetDatabase.LoadAssetAtPath<EUAudioConfig>(_configPath);
            
            if (_currentConfig != null)
            {
                LoadConfigToUI(_currentConfig);
                ShowStatus("配置已加载", false);
            }
            else
            {
                // 配置不存在，使用默认值
                ShowStatus("配置文件不存在，将在保存时自动创建", false);
            }
        }
        
        private void LoadConfigToUI(EUAudioConfig config)
        {
            _soundVolumeSlider.value = config.soundVolume;
            _bgmVolumeSlider.value = config.bgmVolume;
            _voiceVolumeSlider.value = config.voiceVolume;
            _globalVolumeSlider.value = config.globalVolume;
            _startSoundField.value = config.startSound;
            _maxSoundField.value = config.maxSound;
            _delayFrameField.value = config.soundDelayFrame;
            
            // 加载AudioSource参数
            _soundPitchSlider.value = config.soundPitch;
            _soundSpatialBlendSlider.value = config.soundSpatialBlend;
            _soundPriorityField.value = config.soundPriority;
            
            _bgmPitchSlider.value = config.bgmPitch;
            _bgmSpatialBlendSlider.value = config.bgmSpatialBlend;
            _bgmPriorityField.value = config.bgmPriority;
            
            _voicePitchSlider.value = config.voicePitch;
            _voiceSpatialBlendSlider.value = config.voiceSpatialBlend;
            _voicePriorityField.value = config.voicePriority;
        }
        
        private void SaveUIToConfig()
        {
            if (_currentConfig == null) return;
            
            _currentConfig.soundVolume = _soundVolumeSlider.value;
            _currentConfig.bgmVolume = _bgmVolumeSlider.value;
            _currentConfig.voiceVolume = _voiceVolumeSlider.value;
            _currentConfig.globalVolume = _globalVolumeSlider.value;
            _currentConfig.startSound = _startSoundField.value;
            _currentConfig.maxSound = _maxSoundField.value;
            _currentConfig.soundDelayFrame = _delayFrameField.value;
            
            // 保存AudioSource参数
            _currentConfig.soundPitch = _soundPitchSlider.value;
            _currentConfig.soundSpatialBlend = _soundSpatialBlendSlider.value;
            _currentConfig.soundPriority = _soundPriorityField.value;
            
            _currentConfig.bgmPitch = _bgmPitchSlider.value;
            _currentConfig.bgmSpatialBlend = _bgmSpatialBlendSlider.value;
            _currentConfig.bgmPriority = _bgmPriorityField.value;
            
            _currentConfig.voicePitch = _voicePitchSlider.value;
            _currentConfig.voiceSpatialBlend = _voiceSpatialBlendSlider.value;
            _currentConfig.voicePriority = _voicePriorityField.value;
        }
        
        private void OnSaveConfig()
        {
            // 如果配置不存在，先创建
            if (_currentConfig == null)
            {
                // 确保目录存在
                if (!AssetDatabase.IsValidFolder(_configFolder))
                {
                    string[] folders = _configFolder.Split('/');
                    string currentPath = folders[0];
                    
                    for (int i = 1; i < folders.Length; i++)
                    {
                        string newPath = currentPath + "/" + folders[i];
                        if (!AssetDatabase.IsValidFolder(newPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, folders[i]);
                        }
                        currentPath = newPath;
                    }
                }
                
                // 创建配置文件
                _currentConfig = CreateInstance<EUAudioConfig>();
                AssetDatabase.CreateAsset(_currentConfig, _configPath);
            }
            
            // 保存UI数据到配置
            SaveUIToConfig();
            EditorUtility.SetDirty(_currentConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            ShowStatus($"配置已保存到: {_configPath}", false);
        }
        
        private void ShowStatus(string message, bool isError)
        {
            _statusLabel.text = message;
            _statusLabel.RemoveFromClassList("status-success");
            _statusLabel.RemoveFromClassList("status-error");
            _statusLabel.AddToClassList(isError ? "status-error" : "status-success");
        }
    }
}
