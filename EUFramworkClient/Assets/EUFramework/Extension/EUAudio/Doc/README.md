# EU Audio 音频管理器

## 概述

EU Audio 是一个高性能的音频管理系统，专为 Unity 游戏开发设计。它提供了完整的音频解决方案，包括音效(Sound)、背景音乐(BGM)和语音(Voice)的播放管理，支持对象池优化、音量控制、淡入淡出效果以及完整的生命周期管理。

## 核心特性

- **三种音频类型管理**：独立管理音效、背景音乐、语音
- **高性能对象池系统**：自动管理音效播放器，避免频繁创建销毁开销
- **独立的音量控制系统**：支持音效、BGM、语音单独控制以及全局音量控制
- **音量变化事件监听**：提供丰富的事件回调，方便UI更新
- **淡入淡出效果**：支持BGM和Voice的平滑切换（基于 UniTask）
- **3D 空间音频支持**：支持配置空间混合(Spatial Blend)和3D音效
- **内存优化**：使用 Unity.Collections 优化内部数据结构
- **高精度支持**：可配置音频检测帧率，适配音游等高精度需求
- **配置文件支持**：支持通过 ScriptableObject 配置默认参数

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
        // 系统会在首次使用时自动初始化
        // 也可以手动调用以提前初始化
        EUAudio.Init();
        
        // 播放音效
        EUAudio.PlaySound(soundClip);
        
        // 播放背景音乐（默认循环播放）
        EUAudio.PlayBGM(bgmClip);
        
        // 播放语音
        EUAudio.PlayVoice(voiceClip);
    }
}
```

## 进阶使用

### 淡入淡出 (Cross Fade)

EUAudio 支持 BGM 和 Voice 的平滑过渡。

```csharp
// 2秒内淡入新的 BGM（如果已有 BGM 播放，则先淡出旧的）
EUAudio.PlayBGM(newBgmClip, fadeTime: 2.0f);

// 停止 BGM 并淡出
EUAudio.StopBGM(fadeTime: 1.5f);

// 播放语音并淡入
EUAudio.PlayVoice(voiceClip, fadeTime: 0.5f);
```

### 3D 音效

可以在指定的世界坐标播放音效。

```csharp
// 在指定位置播放音效
EUAudio.PlaySound(soundClip, transform.position);
```

### 事件监听

系统提供了丰富的事件回调，用于监听音量变化或播放结束。

```csharp
void Start()
{
    // 监听全局音量变化
    EUAudio.AddGlobalVolumeChangeListener(OnGlobalVolumeChanged);
    
    // 监听 BGM 播放结束
    EUAudio.AddBgmEndListener(OnBgmEnded);
}

void OnGlobalVolumeChanged(float volume)
{
    Debug.Log($"Global Volume Changed: {volume}");
}

void OnBgmEnded(AudioClip clip)
{
    Debug.Log($"BGM Ended: {clip.name}");
}
```

### 配置文件

系统支持通过 `Resources/EUAudio/EUAudioConfig` 加载默认配置。
你可以创建 `EUAudioConfig` 资产并配置以下参数：

- **音量设置**：Sound, BGM, Voice, Global 音量
- **播放器设置**：初始/最大音效数量，检测帧率
- **AudioSource参数**：音高(Pitch)，空间混合(Spatial Blend)，优先级(Priority)

## 文档说明

- **API文档**：请查阅 [API.md](API.md) 获取详细的接口说明。
- **更新日志**：请查阅 [Update.md](Update.md) 获取版本更新历史。
