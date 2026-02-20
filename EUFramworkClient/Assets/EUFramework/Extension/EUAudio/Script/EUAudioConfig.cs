using UnityEngine;

namespace EUFramwork.Extension.EUAudioKit
{
    /// <summary>
    /// EUAudio配置ScriptableObject
    /// 用于保存音频系统的默认设置
    /// </summary>
    //[CreateAssetMenu(fileName = "EUAudioConfig", menuName = "EUFramework/Audio/Audio Config", order = 1)]
    public class EUAudioConfig : ScriptableObject
    {
        [Header("音量设置")]
        [Tooltip("音效音量 (0-1)")]
        [Range(0f, 1f)]
        public float soundVolume = 1.0f;
        
        [Tooltip("背景音乐音量 (0-1)")]
        [Range(0f, 1f)]
        public float bgmVolume = 1.0f;
        
        [Tooltip("语音音量 (0-1)")]
        [Range(0f, 1f)]
        public float voiceVolume = 1.0f;
        
        [Tooltip("全局音量 (0-1)")]
        [Range(0f, 1f)]
        public float globalVolume = 1.0f;
        
        [Header("音效播放器设置")]
        [Tooltip("初始音效播放器数量")]
        [Range(1, 50)]
        public int startSound = 10;
        
        [Tooltip("最大音效播放器数量")]
        [Range(1, 100)]
        public int maxSound = 20;
        
        [Tooltip("音效播放结束检测的延迟帧数\n对于音游等对音频精准度要求高的场景建议设置为1")]
        [Range(1, 60)]
        public int soundDelayFrame = 10;
        
        [Header("Sound AudioSource参数")]
        [Tooltip("音效音高")]
        [Range(0.1f, 3f)]
        public float soundPitch = 1.0f;
        
        [Tooltip("音效空间混合 (0=2D, 1=3D)")]
        [Range(0f, 1f)]
        public float soundSpatialBlend = 0f;
        
        [Tooltip("音效优先级 (0最高, 256最低)")]
        [Range(0, 256)]
        public int soundPriority = 128;
        
        [Header("BGM AudioSource参数")]
        [Tooltip("BGM音高")]
        [Range(0.1f, 3f)]
        public float bgmPitch = 1.0f;
        
        [Tooltip("BGM空间混合 (0=2D, 1=3D)")]
        [Range(0f, 1f)]
        public float bgmSpatialBlend = 0f;
        
        [Tooltip("BGM优先级 (0最高, 256最低)")]
        [Range(0, 256)]
        public int bgmPriority = 128;
        
        [Header("Voice AudioSource参数")]
        [Tooltip("语音音高")]
        [Range(0.1f, 3f)]
        public float voicePitch = 1.0f;
        
        [Tooltip("语音空间混合 (0=2D, 1=3D)")]
        [Range(0f, 1f)]
        public float voiceSpatialBlend = 0f;
        
        [Tooltip("语音优先级 (0最高, 256最低)")]
        [Range(0, 256)]
        public int voicePriority = 128;
        
        /// <summary>
        /// 应用配置到EUAudio系统
        /// </summary>
        public void ApplyConfig()
        {
            EUAudio.StartSound = startSound;
            EUAudio.MaxSound = maxSound;
            EUAudio.SoundDelayFrame = soundDelayFrame;
            EUAudio.SoundVolume = soundVolume;
            EUAudio.BgmVolume = bgmVolume;
            EUAudio.VoiceVolume = voiceVolume;
            EUAudio.GlobalVolume = globalVolume;
            
            // 应用AudioSource参数
            EUAudio.SoundPitch = soundPitch;
            EUAudio.SoundSpatialBlend = soundSpatialBlend;
            EUAudio.SoundPriority = soundPriority;
            
            EUAudio.BgmPitch = bgmPitch;
            EUAudio.BgmSpatialBlend = bgmSpatialBlend;
            EUAudio.BgmPriority = bgmPriority;
            
            EUAudio.VoicePitch = voicePitch;
            EUAudio.VoiceSpatialBlend = voiceSpatialBlend;
            EUAudio.VoicePriority = voicePriority;
        }
        
        /// <summary>
        /// 从EUAudio系统读取当前配置
        /// </summary>
        public void LoadFromCurrent()
        {
            soundVolume = EUAudio.SoundVolume;
            bgmVolume = EUAudio.BgmVolume;
            voiceVolume = EUAudio.VoiceVolume;
            globalVolume = EUAudio.GlobalVolume;
            startSound = EUAudio.StartSound;
            maxSound = EUAudio.MaxSound;
            soundDelayFrame = EUAudio.SoundDelayFrame;
            
            // 读取AudioSource参数
            soundPitch = EUAudio.SoundPitch;
            soundSpatialBlend = EUAudio.SoundSpatialBlend;
            soundPriority = EUAudio.SoundPriority;
            
            bgmPitch = EUAudio.BgmPitch;
            bgmSpatialBlend = EUAudio.BgmSpatialBlend;
            bgmPriority = EUAudio.BgmPriority;
            
            voicePitch = EUAudio.VoicePitch;
            voiceSpatialBlend = EUAudio.VoiceSpatialBlend;
            voicePriority = EUAudio.VoicePriority;
        }
    }
}
