# EU Audio 音频管理器

## 概述

EU Audio 是一个高性能的音频管理系统，专为 Unity 游戏开发设计。它提供了完整的音频解决方案，包括音效(Sound)、背景音乐(BGM)和语音(Voice)的播放管理，支持对象池优化、音量控制、淡入淡出效果以及完整的生命周期管理。

## 核心特性

- 三种音频类型管理：音效、背景音乐、语音
- 高性能对象池系统，自动管理音效播放器
- 独立的音量控制系统（音效、BGM、语音、全局音量）
- 音量变化事件监听机制
- 支持淡入淡出效果
- 3D 空间音频支持
- 基于 UniTask 的异步音频处理
- 使用 Unity.Collections 优化内存管理
- 可配置的音频精度（适配音游等高精度场景）

## 快速开始

### 基础使用

```csharp
using EUFramwork.Extension.EUAudioKit;
using UnityEngine;

public class AudioExample : MonoBehaviour
{
    public AudioClip soundClip;
    public AudioClip bgmClip;
    public AudioClip voiceClip;
    
    void Start()
    {
        // 系统会自动初始化，也可以手动初始化
        EUAudio.Init();
        
        // 播放音效
        EUAudio.PlaySound(soundClip);
        
        // 播放背景音乐（循环播放）
        EUAudio.PlayBGM(bgmClip);
        
        // 播放语音
        EUAudio.PlayVoice(voiceClip);
    }
}
```

## API 文档

### 初始化

#### Init()
```csharp
public static void Init()
```
初始化音频系统。系统会在首次使用时自动初始化，但也可以手动调用以控制初始化时机。

**示例：**
```csharp
EUAudio.Init();
```

---

### 音效 (Sound) 管理

#### PlaySound(AudioClip, Vector3, Action<AudioClip>)
```csharp
public static void PlaySound(AudioClip clip, Vector3 position, Action<AudioClip> onAudioEnd = null)
```
在指定位置播放音效。

**参数：**
- `clip`: 要播放的音频片段
- `position`: 播放位置（3D 空间坐标）
- `onAudioEnd`: 音频播放结束时的回调函数（可选）

**示例：**
```csharp
// 在指定位置播放音效
EUAudio.PlaySound(explosionClip, transform.position);

// 播放音效并监听结束事件
EUAudio.PlaySound(footstepClip, playerPosition, (clip) => {
    Debug.Log($"音效播放完成: {clip.name}");
});
```

#### PlaySound(AudioClip, Action<AudioClip>)
```csharp
public static void PlaySound(AudioClip clip, Action<AudioClip> onAudioEnd = null)
```
在默认位置（Vector3.zero）播放音效。

**参数：**
- `clip`: 要播放的音频片段
- `onAudioEnd`: 音频播放结束时的回调函数（可选）

**示例：**
```csharp
// 播放 2D 音效
EUAudio.PlaySound(clickSound);

// 播放音效并处理结束事件
EUAudio.PlaySound(coinSound, (clip) => {
    AddScore(10);
});
```

---

### 背景音乐 (BGM) 管理

#### SetBGM(AudioClip, float, bool)
```csharp
public static void SetBGM(AudioClip clip, float fadeTime = 0, bool loop = true)
```
设置背景音乐但不播放。

**参数：**
- `clip`: 要设置的音频片段
- `fadeTime`: 淡入淡出时间（秒），默认为 0
- `loop`: 是否循环播放，默认为 true

**示例：**
```csharp
// 设置背景音乐
EUAudio.SetBGM(menuBGM);

// 设置背景音乐（不循环）
EUAudio.SetBGM(bossBGM, 0, false);
```

#### PlayBGM(AudioClip, float, bool)
```csharp
public static void PlayBGM(AudioClip clip, float fadeTime = 0, bool loop = true)
```
设置并播放背景音乐，支持淡入淡出效果。

**参数：**
- `clip`: 要播放的音频片段
- `fadeTime`: 淡入淡出时间（秒），默认为 0
- `loop`: 是否循环播放，默认为 true

**示例：**
```csharp
// 直接播放背景音乐
EUAudio.PlayBGM(battleBGM);

// 使用 2 秒淡入淡出效果切换背景音乐
EUAudio.PlayBGM(newBGM, 2f);

// 播放一次性背景音乐（不循环）
EUAudio.PlayBGM(victoryBGM, 1f, false);
```

#### PlayBGM()
```csharp
public static void PlayBGM()
```
播放已设置的背景音乐。

**示例：**
```csharp
// 先设置后播放
EUAudio.SetBGM(bgmClip);
EUAudio.PlayBGM();
```

#### StopBGM(float)
```csharp
public static void StopBGM(float fadeTime = 0)
```
停止背景音乐播放，支持淡出效果。

**参数：**
- `fadeTime`: 淡出时间（秒），默认为 0

**示例：**
```csharp
// 立即停止背景音乐
EUAudio.StopBGM();

// 使用 1.5 秒淡出效果停止背景音乐
EUAudio.StopBGM(1.5f);
```

---

### 语音 (Voice) 管理

#### SetVoice(AudioClip, float, bool)
```csharp
public static void SetVoice(AudioClip clip, float fadeTime = 0, bool loop = false)
```
设置语音但不播放。

**参数：**
- `clip`: 要设置的音频片段
- `fadeTime`: 淡入淡出时间（秒），默认为 0
- `loop`: 是否循环播放，默认为 false

**示例：**
```csharp
// 设置语音
EUAudio.SetVoice(dialogueClip);
```

#### PlayVoice(AudioClip, float, bool)
```csharp
public static void PlayVoice(AudioClip clip, float fadeTime = 0, bool loop = false)
```
设置并播放语音，支持淡入淡出效果。

**参数：**
- `clip`: 要播放的音频片段
- `fadeTime`: 淡入淡出时间（秒），默认为 0
- `loop`: 是否循环播放，默认为 false

**示例：**
```csharp
// 播放对话语音
EUAudio.PlayVoice(npcDialogue);

// 使用淡入效果播放语音
EUAudio.PlayVoice(narratorVoice, 0.5f);

// 播放循环语音（如环境音）
EUAudio.PlayVoice(ambientVoice, 0, true);
```

#### PlayVoice()
```csharp
public static void PlayVoice()
```
播放已设置的语音。

**示例：**
```csharp
// 先设置后播放
EUAudio.SetVoice(voiceClip);
EUAudio.PlayVoice();
```

#### StopVoice(float)
```csharp
public static void StopVoice(float fadeTime = 0)
```
停止语音播放，支持淡出效果。

**参数：**
- `fadeTime`: 淡出时间（秒），默认为 0

**示例：**
```csharp
// 立即停止语音
EUAudio.StopVoice();

// 使用淡出效果停止语音
EUAudio.StopVoice(0.8f);
```

---

### 音量控制

#### SoundVolume
```csharp
public static float SoundVolume { get; set; }
```
音效音量（0-1），修改时会触发音量变化事件。

**示例：**
```csharp
// 设置音效音量为 50%
EUAudio.SoundVolume = 0.5f;

// 获取当前音效音量
float currentVolume = EUAudio.SoundVolume;
```

#### BgmVolume
```csharp
public static float BgmVolume { get; set; }
```
背景音乐音量（0-1），修改时会触发音量变化事件。

**示例：**
```csharp
// 设置背景音乐音量为 70%
EUAudio.BgmVolume = 0.7f;
```

#### VoiceVolume
```csharp
public static float VoiceVolume { get; set; }
```
语音音量（0-1），修改时会触发音量变化事件。

**示例：**
```csharp
// 设置语音音量为 80%
EUAudio.VoiceVolume = 0.8f;
```

#### GlobalVolume
```csharp
public static float GlobalVolume { get; set; }
```
全局音量（0-1），影响所有音频类型，修改时会触发音量变化事件。

**示例：**
```csharp
// 设置全局音量为 60%
EUAudio.GlobalVolume = 0.6f;

// 静音所有音频
EUAudio.GlobalVolume = 0f;
```

---

### 事件监听系统

#### 音量变化事件监听

##### 音效音量变化监听
```csharp
public static void SetSoundVolumeChangeListener(Action<float> action)
public static void AddSoundVolumeChangeListener(Action<float> action)
public static void RemoveSoundVolumeChangeListener(Action<float> action)
public static void RemoveAllSoundVolumeChangeListener()
```

**示例：**
```csharp
// 设置监听器
EUAudio.SetSoundVolumeChangeListener((volume) => {
    Debug.Log($"音效音量变化: {volume}");
    UpdateSoundVolumeUI(volume);
});

// 添加监听器
EUAudio.AddSoundVolumeChangeListener(OnSoundVolumeChanged);

// 移除监听器
EUAudio.RemoveSoundVolumeChangeListener(OnSoundVolumeChanged);

// 移除所有监听器
EUAudio.RemoveAllSoundVolumeChangeListener();
```

##### BGM 音量变化监听
```csharp
public static void SetBgmVolumeChangeListener(Action<float> action)
public static void AddBgmVolumeChangeListener(Action<float> action)
public static void RemoveBgmVolumeChangeListener(Action<float> action)
public static void RemoveAllBgmVolumeChangeListener()
```

**示例：**
```csharp
// 监听 BGM 音量变化
EUAudio.AddBgmVolumeChangeListener((volume) => {
    PlayerPrefs.SetFloat("BGMVolume", volume);
});
```

##### 语音音量变化监听
```csharp
public static void SetVoiceVolumeChangeListener(Action<float> action)
public static void AddVoiceVolumeChangeListener(Action<float> action)
public static void RemoveVoiceVolumeChangeListener(Action<float> action)
public static void RemoveAllVoiceVolumeChangeListener()
```

**示例：**
```csharp
// 监听语音音量变化
EUAudio.AddVoiceVolumeChangeListener((volume) => {
    UpdateVoiceVolumeSlider(volume);
});
```

##### 全局音量变化监听
```csharp
public static void SetGlobalVolumeChangeListener(Action<float> action)
public static void AddGlobalVolumeChangeListener(Action<float> action)
public static void RemoveGlobalVolumeChangeListener(Action<float> action)
public static void RemoveAllGlobalVolumeChangeListener()
```

**示例：**
```csharp
// 监听全局音量变化
EUAudio.AddGlobalVolumeChangeListener((volume) => {
    PlayerPrefs.SetFloat("MasterVolume", volume);
});
```

#### 音频播放结束事件监听

##### BGM 结束监听
```csharp
public static void SetBgmEndListener(Action<AudioClip> action)
public static void AddBgmEndListener(Action<AudioClip> action)
public static void RemoveBgmEndListener(Action<AudioClip> action)
public static void RemoveAllBgmEndListener()
```

**示例：**
```csharp
// 监听 BGM 播放结束
EUAudio.AddBgmEndListener((clip) => {
    Debug.Log($"BGM 播放结束: {clip.name}");
    // 可以在这里播放下一首BGM
    PlayNextBGM();
});
```

##### Voice 结束监听
```csharp
public static void SetVoiceEndListener(Action<AudioClip> action)
public static void AddVoiceEndListener(Action<AudioClip> action)
public static void RemoveVoiceEndListener(Action<AudioClip> action)
public static void RemoveAllVoiceEndListener()
```

**示例：**
```csharp
// 监听语音播放结束
EUAudio.AddVoiceEndListener((clip) => {
    Debug.Log($"语音播放结束: {clip.name}");
    // 可以在这里显示下一句对话
    ShowNextDialogue();
});
```

#### 音频改变事件监听

##### BGM 改变监听
```csharp
public static void SetBgmChangeListener(Action<AudioClip, AudioClip> action)
public static void AddBgmChangeListener(Action<AudioClip, AudioClip> action)
public static void RemoveBgmChangeListener(Action<AudioClip, AudioClip> action)
public static void RemoveAllBgmChangeListener()
```

**示例：**
```csharp
// 监听 BGM 改变
EUAudio.AddBgmChangeListener((oldClip, newClip) => {
    Debug.Log($"BGM 从 {oldClip?.name} 切换到 {newClip?.name}");
    // 可以在这里更新UI显示当前播放的BGM
    UpdateBGMDisplay(newClip);
});
```

##### Voice 改变监听
```csharp
public static void SetVoiceChangeListener(Action<AudioClip, AudioClip> action)
public static void AddVoiceChangeListener(Action<AudioClip, AudioClip> action)
public static void RemoveVoiceChangeListener(Action<AudioClip, AudioClip> action)
public static void RemoveAllVoiceChangeListener()
```

**示例：**
```csharp
// 监听语音改变
EUAudio.AddVoiceChangeListener((oldClip, newClip) => {
    Debug.Log($"语音从 {oldClip?.name} 切换到 {newClip?.name}");
    // 可以在这里更新字幕显示
    UpdateSubtitle(newClip);
});
```

---

### 配置属性

#### SoundDelayFrame
```csharp
public static int SoundDelayFrame { get; set; }
```
音效播放结束检测的延迟帧数。默认为 10 帧，对于音游等高精度场景建议设置为 1。

**示例：**
```csharp
// 设置为高精度模式（适合音游）
EUAudio.SoundDelayFrame = 1;

// 设置为通用优化模式
EUAudio.SoundDelayFrame = 10;
```

#### StartSound
```csharp
public static int StartSound { get; set; }
```
初始音效播放器数量，默认为 10。

**示例：**
```csharp
// 在初始化前设置
EUAudio.StartSound = 15;
EUAudio.Init();
```

#### MaxSound
```csharp
public static int MaxSound { get; set; }
```
最大音效播放器数量，默认为 20。

**示例：**
```csharp
// 在初始化前设置
EUAudio.MaxSound = 30;
EUAudio.Init();
```

---

### 资源管理

#### NativeDisposable()
```csharp
public static void NativeDisposable()
```
释放 Native 容器资源。通常在应用退出时自动调用，但也可以手动调用。

**示例：**
```csharp
void OnApplicationQuit()
{
    EUAudio.NativeDisposable();
}
```

---

## 完整使用示例

### 示例 1：基础音频管理器

```csharp
using UnityEngine;
using EUFramwork.Extension.EUAudioKit;

public class GameAudioManager : MonoBehaviour
{
    [Header("音频片段")]
    public AudioClip menuBGM;
    public AudioClip gameBGM;
    public AudioClip buttonClick;
    public AudioClip coinCollect;
    
    void Start()
    {
        // 初始化音频系统
        EUAudio.Init();
        
        // 从 PlayerPrefs 加载音量设置
        LoadVolumeSettings();
        
        // 注册音量变化监听
        RegisterVolumeListeners();
        
        // 播放菜单背景音乐
        EUAudio.PlayBGM(menuBGM, 1f);
    }
    
    void LoadVolumeSettings()
    {
        EUAudio.SoundVolume = PlayerPrefs.GetFloat("SoundVolume", 1f);
        EUAudio.BgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.7f);
        EUAudio.VoiceVolume = PlayerPrefs.GetFloat("VoiceVolume", 1f);
        EUAudio.GlobalVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
    }
    
    void RegisterVolumeListeners()
    {
        EUAudio.AddSoundVolumeChangeListener(volume => {
            PlayerPrefs.SetFloat("SoundVolume", volume);
        });
        
        EUAudio.AddBgmVolumeChangeListener(volume => {
            PlayerPrefs.SetFloat("BGMVolume", volume);
        });
        
        EUAudio.AddVoiceVolumeChangeListener(volume => {
            PlayerPrefs.SetFloat("VoiceVolume", volume);
        });
        
        EUAudio.AddGlobalVolumeChangeListener(volume => {
            PlayerPrefs.SetFloat("MasterVolume", volume);
        });
    }
    
    public void OnButtonClick()
    {
        EUAudio.PlaySound(buttonClick);
    }
    
    public void OnCoinCollect()
    {
        EUAudio.PlaySound(coinCollect, (clip) => {
            Debug.Log("金币音效播放完成");
        });
    }
    
    public void StartGame()
    {
        // 使用淡入淡出切换到游戏背景音乐
        EUAudio.PlayBGM(gameBGM, 2f);
    }
    
    void OnDestroy()
    {
        // 清理监听器
        EUAudio.RemoveAllSoundVolumeChangeListener();
        EUAudio.RemoveAllBgmVolumeChangeListener();
        EUAudio.RemoveAllVoiceVolumeChangeListener();
        EUAudio.RemoveAllGlobalVolumeChangeListener();
    }
}
```

### 示例 2：音量设置 UI

```csharp
using UnityEngine;
using UnityEngine.UI;
using EUFramwork.Extension.EUAudioKit;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("音量滑块")]
    public Slider masterVolumeSlider;
    public Slider bgmVolumeSlider;
    public Slider soundVolumeSlider;
    public Slider voiceVolumeSlider;
    
    void Start()
    {
        // 初始化滑块值
        masterVolumeSlider.value = EUAudio.GlobalVolume;
        bgmVolumeSlider.value = EUAudio.BgmVolume;
        soundVolumeSlider.value = EUAudio.SoundVolume;
        voiceVolumeSlider.value = EUAudio.VoiceVolume;
        
        // 注册滑块事件
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        soundVolumeSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
        voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
    }
    
    void OnMasterVolumeChanged(float value)
    {
        EUAudio.GlobalVolume = value;
    }
    
    void OnBGMVolumeChanged(float value)
    {
        EUAudio.BgmVolume = value;
    }
    
    void OnSoundVolumeChanged(float value)
    {
        EUAudio.SoundVolume = value;
    }
    
    void OnVoiceVolumeChanged(float value)
    {
        EUAudio.VoiceVolume = value;
    }
}
```

### 示例 3：3D 空间音效

```csharp
using UnityEngine;
using EUFramwork.Extension.EUAudioKit;

public class FootstepController : MonoBehaviour
{
    public AudioClip[] footstepSounds;
    public float stepInterval = 0.5f;
    
    private float stepTimer;
    
    void Update()
    {
        if (IsWalking())
        {
            stepTimer += Time.deltaTime;
            
            if (stepTimer >= stepInterval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }
    
    void PlayFootstep()
    {
        if (footstepSounds.Length > 0)
        {
            // 随机选择一个脚步声
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            
            // 在角色位置播放 3D 音效
            EUAudio.PlaySound(clip, transform.position);
        }
    }
    
    bool IsWalking()
    {
        // 检测角色是否在移动
        return Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
    }
}
```

### 示例 4：音游高精度模式

```csharp
using UnityEngine;
using EUFramwork.Extension.EUAudioKit;

public class RhythmGameManager : MonoBehaviour
{
    public AudioClip musicTrack;
    public AudioClip hitSound;
    public AudioClip missSound;
    
    void Start()
    {
        // 设置为高精度模式（每帧检测）
        EUAudio.SoundDelayFrame = 1;
        
        // 初始化
        EUAudio.Init();
        
        // 播放音乐
        EUAudio.PlayBGM(musicTrack, 0, false);
    }
    
    public void OnNoteHit()
    {
        EUAudio.PlaySound(hitSound, (clip) => {
            // 音效播放完成后的精确回调
            Debug.Log("Hit sound finished at: " + Time.time);
        });
    }
    
    public void OnNoteMiss()
    {
        EUAudio.PlaySound(missSound);
    }
}
```

### 示例 5：场景切换音频管理

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using EUFramwork.Extension.EUAudioKit;

public class SceneAudioManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneAudio
    {
        public string sceneName;
        public AudioClip bgm;
        public float fadeTime = 2f;
    }
    
    public SceneAudio[] sceneAudios;
    
    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // 播放当前场景的背景音乐
        PlaySceneBGM(SceneManager.GetActiveScene().name);
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneBGM(scene.name);
    }
    
    void PlaySceneBGM(string sceneName)
    {
        foreach (var sceneAudio in sceneAudios)
        {
            if (sceneAudio.sceneName == sceneName)
            {
                EUAudio.PlayBGM(sceneAudio.bgm, sceneAudio.fadeTime);
                return;
            }
        }
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
```

---

## 性能优化建议

1. **对象池配置**
   - 根据游戏需求调整 `StartSound` 和 `MaxSound`
   - 音效较多的游戏建议增大对象池容量
   - 音效较少的游戏可以减小对象池以节省内存

2. **精度设置**
   - 普通游戏使用默认的 `SoundDelayFrame = 10`
   - 音游等高精度场景设置 `SoundDelayFrame = 1`

3. **淡入淡出**
   - 合理使用淡入淡出时间，避免过长导致卡顿感
   - 快速切换场景时可以使用较短的淡入淡出时间

4. **事件监听**
   - 及时移除不需要的事件监听器
   - 避免在监听器中执行耗时操作

5. **资源管理**
   - 使用 AudioClip 的 LoadType 合理配置加载方式
   - 大文件使用 Streaming，小文件使用 DecompressOnLoad

---

## 注意事项

1. **依赖项**
   - 需要 UniTask 库支持
   - 需要 Unity.Mathematics 和 Unity.Collections

2. **初始化**
   - 系统会在首次使用时自动初始化
   - 可以在游戏启动时手动调用 `Init()` 以控制初始化时机

3. **音效限制**
   - 同时播放的音效数量受 `MaxSound` 限制
   - 超过限制时新的音效请求会被忽略

4. **淡入淡出**
   - 淡入淡出效果基于 UniTask 异步实现
   - 切换音频时会自动处理前一个音频的淡出

5. **3D 音效**
   - 使用 `PlaySound(clip, position)` 播放 3D 音效
   - 确保场景中有 AudioListener 组件

6. **资源释放**
   - 应用退出时会自动释放 Native 容器
   - 也可以手动调用 `NativeDisposable()` 释放资源

---

## 常见问题

**Q: 为什么音效播放不出来？**
A: 检查以下几点：
- 音量设置是否为 0
- 是否超过了 MaxSound 限制
- AudioClip 是否正确加载
- 场景中是否有 AudioListener

**Q: 如何实现音效的优先级？**
A: 当前版本使用先到先得的策略。如需优先级功能，可以在播放前检查当前使用的音效数量，必要时停止低优先级音效。

**Q: 淡入淡出效果不明显？**
A: 检查淡入淡出时间设置，建议使用 1-3 秒的时间。时间过短可能不明显，时间过长可能影响体验。

**Q: 如何实现音效的随机音高？**
A: 可以通过 EUAudioSource 的 Source 属性访问 AudioSource，然后设置 pitch 值。

**Q: 支持音频混响等效果吗？**
A: 可以通过访问 AudioSource 组件添加 AudioReverbFilter 等效果组件。

---

## 版本历史

### v1.0.0
- 初始版本发布
- 支持音效、BGM、语音三种音频类型
- 实现对象池管理系统
- 支持音量控制和事件监听
- 支持淡入淡出效果
- 支持 3D 空间音频

---

## 技术支持

如有问题或建议，请联系开发者或在项目仓库提交 Issue。

---

## 许可证

本工具遵循项目整体许可证。
