# EU Audio API 文档

## EUAudio 类

`EUFramwork.Extension.EUAudioKit.EUAudio`

音频系统的核心静态管理类。

### 初始化

#### `void Init()`
初始化音频系统。系统会在首次使用时自动初始化，但也可以手动调用以控制初始化时机。
- **自动加载配置**：如果 `Resources/EUAudio/EUAudioConfig` 存在，初始化时会自动应用该配置。

#### `void LoadConfig(EUAudioConfig config)`
手动加载指定的配置文件。
- **注意**：必须在 `Init()` 之后调用，或者由 `Init()` 内部自动调用。

### 属性 (Properties)

#### 音量控制
修改以下属性会实时触发音量变化事件，并更新当前正在播放的音频。
- `float SoundVolume`: 音效音量 (0-1)
- `float BgmVolume`: 背景音乐音量 (0-1)
- `float VoiceVolume`: 语音音量 (0-1)
- `float GlobalVolume`: 全局音量 (0-1)，作为所有音频类型的音量乘数。

#### 配置参数
- `int SoundDelayFrame`: 音效播放结束检测的延迟帧数。
  - 默认值：10
  - **优化建议**：对于音游等对音频精度要求极高的场景，建议将其设为 `1`。
- `int StartSound`: 初始创建的音效播放器数量（对象池预热）。
- `int MaxSound`: 最大音效播放器数量（超过此数量的并发音效将不播放或等待）。

#### AudioSource 参数设置
修改以下属性会立即应用到对应的 AudioSource。
- `float SoundPitch` / `BgmPitch` / `VoicePitch`: 音高 (Pitch)
- `float SoundSpatialBlend` / `BgmSpatialBlend` / `VoiceSpatialBlend`: 空间混合 (0=2D, 1=3D)
- `int SoundPriority` / `BgmPriority` / `VoicePriority`: 优先级 (0=最高, 256=最低)

### 方法 (Methods)

#### 音效 (Sound)
- `void PlaySound(AudioClip clip, Vector3 position, Action<AudioClip> onAudioEnd = null)`
  - 在指定的世界坐标播放音效（3D 音效）。
  - `onAudioEnd`: 音频播放结束时的回调。
- `void PlaySound(AudioClip clip, Action<AudioClip> onAudioEnd = null)`
  - 在默认位置 `Vector3.zero` 播放音效（通常用于 2D UI 音效）。

#### 背景音乐 (BGM)
- `void SetBGM(AudioClip clip, float fadeTime = 0, bool loop = true)`
  - 设置背景音乐但不播放。如果当前 BGM 正在播放且 `fadeTime > 0`，会先淡出当前 BGM。
- `void PlayBGM(AudioClip clip, float fadeTime = 0, bool loop = true)`
  - 设置并播放背景音乐。
  - `fadeTime > 0`: 启用淡入淡出（异步操作，不阻塞主线程）。
  - `loop`: 是否循环播放。
- `void PlayBGM()`
  - 播放当前已设置的背景音乐。
- `void StopBGM(float fadeTime = 0)`
  - 停止背景音乐。`fadeTime > 0` 时会先淡出再停止。

#### 语音 (Voice)
- `void SetVoice(AudioClip clip, float fadeTime = 0, bool loop = false)`
  - 设置语音但不播放。
- `void PlayVoice(AudioClip clip, float fadeTime = 0, bool loop = false)`
  - 设置并播放语音。通常语音不循环 (`loop = false`)。
- `void PlayVoice()`
  - 播放当前已设置的语音。
- `void StopVoice(float fadeTime = 0)`
  - 停止语音播放。

### 事件监听 (Listeners)

#### 音量变化监听
当对应的音量属性被修改时触发。
- `SetSoundVolumeChangeListener(Action<float>)`: 设置监听（覆盖之前的）
- `AddSoundVolumeChangeListener(Action<float>)`: 添加监听
- `RemoveSoundVolumeChangeListener(Action<float>)`: 移除监听
- `RemoveAllSoundVolumeChangeListener()`: 移除所有监听
- （BGM, Voice, Global 同理，前缀分别为 `Bgm`, `Voice`, `Global`）

#### 播放状态监听
- `AddBgmEndListener(Action<AudioClip>)`: 当 BGM 播放结束（自然结束或停止）时触发。
- `AddVoiceEndListener(Action<AudioClip>)`: 当 Voice 播放结束时触发。
- `AddBgmChangeListener(Action<AudioClip oldClip, AudioClip newClip>)`: 当 BGM 切换时触发。
- `AddVoiceChangeListener(Action<AudioClip oldClip, AudioClip newClip>)`: 当 Voice 切换时触发。
- （所有监听器均有对应的 `Set`, `Remove`, `RemoveAll` 方法）

## EUAudioConfig 类

`EUFramwork.Extension.EUAudioKit.EUAudioConfig`

ScriptableObject 配置类，用于保存默认设置。建议存放在 `Resources/EUAudio/EUAudioConfig`。

### 方法
- `void ApplyConfig()`: 将当前 ScriptableObject 的配置应用到运行时系统。
- `void LoadFromCurrent()`: 从运行时系统读取当前参数并保存到 ScriptableObject（仅在 Editor 模式下有效，用于保存运行时调整的参数）。
