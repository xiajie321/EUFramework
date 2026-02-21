# EU Audio API 文档

## EUAudio 类

`EUFramwork.Extension.EUAudioKit.EUAudio`

音频系统的核心静态管理类。

### 初始化

#### `void Init()`
初始化音频系统。系统会在首次使用时自动初始化，但也可以手动调用以控制初始化时机。如果存在配置文件，会自动加载配置。

#### `void LoadConfig(EUAudioConfig config)`
手动加载指定的配置文件。

### 属性 (Properties)

#### 音量控制
- `float SoundVolume`: 音效音量 (0-1)
- `float BgmVolume`: 背景音乐音量 (0-1)
- `float VoiceVolume`: 语音音量 (0-1)
- `float GlobalVolume`: 全局音量 (0-1)

#### 配置参数
- `int SoundDelayFrame`: 音效播放结束检测的延迟帧数
- `int StartSound`: 初始音效播放器数量
- `int MaxSound`: 最大音效播放器数量

#### AudioSource 参数设置
- `float SoundPitch`: 音效音高
- `float SoundSpatialBlend`: 音效空间混合 (0=2D, 1=3D)
- `int SoundPriority`: 音效优先级
- `float BgmPitch`: BGM音高
- `float BgmSpatialBlend`: BGM空间混合
- `int BgmPriority`: BGM优先级
- `float VoicePitch`: 语音音高
- `float VoiceSpatialBlend`: 语音空间混合
- `int VoicePriority`: 语音优先级

### 方法 (Methods)

#### 音效 (Sound)
- `void PlaySound(AudioClip clip, Vector3 position, Action<AudioClip> onAudioEnd = null)`: 在指定位置播放音效。
- `void PlaySound(AudioClip clip, Action<AudioClip> onAudioEnd = null)`: 在默认位置播放音效。

#### 背景音乐 (BGM)
- `void SetBGM(AudioClip clip, float fadeTime = 0, bool loop = true)`: 设置背景音乐但不播放。
- `void PlayBGM(AudioClip clip, float fadeTime = 0, bool loop = true)`: 设置并播放背景音乐，支持淡入淡出。
- `void PlayBGM()`: 播放已设置的背景音乐。
- `void StopBGM(float fadeTime = 0)`: 停止背景音乐播放，支持淡出。

#### 语音 (Voice)
- `void SetVoice(AudioClip clip, float fadeTime = 0, bool loop = false)`: 设置语音但不播放。
- `void PlayVoice(AudioClip clip, float fadeTime = 0, bool loop = false)`: 设置并播放语音，支持淡入淡出。
- `void PlayVoice()`: 播放已设置的语音。
- `void StopVoice(float fadeTime = 0)`: 停止语音播放，支持淡出。

### 事件监听 (Listeners)

#### 音量变化监听
- `Set/Add/Remove/RemoveAllSoundVolumeChangeListener(Action<float>)`
- `Set/Add/Remove/RemoveAllBgmVolumeChangeListener(Action<float>)`
- `Set/Add/Remove/RemoveAllVoiceVolumeChangeListener(Action<float>)`
- `Set/Add/Remove/RemoveAllGlobalVolumeChangeListener(Action<float>)`

#### 播放状态监听
- `Set/Add/Remove/RemoveAllBgmEndListener(Action<AudioClip>)`: BGM播放结束监听
- `Set/Add/Remove/RemoveAllVoiceEndListener(Action<AudioClip>)`: Voice播放结束监听
- `Set/Add/Remove/RemoveAllBgmChangeListener(Action<AudioClip, AudioClip>)`: BGM切换监听
- `Set/Add/Remove/RemoveAllVoiceChangeListener(Action<AudioClip, AudioClip>)`: Voice切换监听

## EUAudioConfig 类

`EUFramwork.Extension.EUAudioKit.EUAudioConfig`

ScriptableObject 配置类，用于保存默认设置。

### 方法
- `void ApplyConfig()`: 应用配置到 EUAudio 系统。
- `void LoadFromCurrent()`: 从 EUAudio 系统读取当前配置。
