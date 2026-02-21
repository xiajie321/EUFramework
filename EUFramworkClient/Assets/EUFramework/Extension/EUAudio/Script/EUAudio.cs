using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EUFramwork.Extension.EUAudioKit
{
    /// <summary>
    /// EU Audio 音频管理器
    /// 提供音效(Sound)、背景音乐(BGM)、语音(Voice)的统一管理
    /// 为了通用优化,对于音游这种对于音频精准度要求较高的场景建议将DelayFrame修改为1
    /// </summary>
    public static class EUAudio
    {
        private static bool _init = false;
        private static int _startSound = 10;
        private static int _maxSound = 20;
        private static int _soundDelayFrame = 10;
        private static float _soundVolume = 1.0f;
        private static float _bgmVolume = 1.0f;
        private static float _voiceVolume = 1.0f;
        private static float _globalVolume = 1.0f;
        
        // AudioSource参数
        private static float _soundPitch = 1.0f;
        private static float _soundSpatialBlend = 0f;
        private static int _soundPriority = 128;
        
        private static float _bgmPitch = 1.0f;
        private static float _bgmSpatialBlend = 0f;
        private static int _bgmPriority = 128;
        
        private static float _voicePitch = 1.0f;
        private static float _voiceSpatialBlend = 0f;
        private static int _voicePriority = 128;
        
        private static GameObject _root;
        private static EUAudioSource _bgm; //背景音乐
        private static EUAudioSource _bgmGd; //背景音乐(用于过渡)
        private static EUAudioSource _voice;
        private static EUAudioSource _voiceGd;
        private static Action<float> _onSoundVolumeChange;
        private static Action<float> _onBgmVolumeChange;
        private static Action<float> _onVoiceVolumeChange;
        private static Action<float> _onGlobalVolumeChange;
        private static Action<AudioClip> _onBgmEnd;
        private static Action<AudioClip> _onVoiceEnd;
        private static Action<AudioClip, AudioClip> _onBgmChange;
        private static Action<AudioClip, AudioClip> _onVoiceChange;
        private static readonly List<EUAudioSource> _sound = new();
        private static readonly Stack<int> _soundPool = new(); //可以使用的音效播放器
        private static NativeList<int> _useSound; //已经使用的音效播放
        private static NativeHashMap<int, int> _useSoundIndex; //映射用于快速查找删除

        /// <summary>
        /// 音效播放结束检测的延迟帧数
        /// 用于Sound的监听判断,为了通用优化,对于音游这种对于音频精准度要求较高的场景建议将SoundDelayFrame修改为1
        /// </summary>
        public static int SoundDelayFrame
        {
            get => _soundDelayFrame;
            set => _soundDelayFrame = value;
        }

        /// <summary>
        /// 音效音量(0-1)
        /// 修改时会触发音量变化事件并更新所有正在播放的音效音量
        /// </summary>
        public static float SoundVolume
        {
            get => _soundVolume;
            set
            {
                if (!_init) Init();
                _soundVolume = math.clamp(value, 0, 1);
                float ls = _soundVolume * _globalVolume;
                for (int i = 0; i < _useSound.Length; i++)
                {
                    _sound[_useSound[i]].Source.volume = ls;
                }
                _onSoundVolumeChange?.Invoke(_soundVolume);
            }
        }

        /// <summary>
        /// 背景音乐音量(0-1)
        /// 修改时会触发音量变化事件并更新BGM音量
        /// </summary>
        public static float BgmVolume
        {
            get => _bgmVolume;
            set
            {
                if (!_init) Init();
                _bgmVolume = math.clamp(value, 0, 1);
                _bgm.Source.volume = _bgmVolume * _globalVolume;
                _onBgmVolumeChange?.Invoke(_bgmVolume);
            }
        }

        /// <summary>
        /// 语音音量(0-1)
        /// 修改时会触发音量变化事件并更新Voice音量
        /// </summary>
        public static float VoiceVolume
        {
            get => _voiceVolume;
            set
            {
                if (!_init) Init();
                _voiceVolume = math.clamp(value, 0, 1);
                _voice.Source.volume = _voiceVolume * _globalVolume;
                _onVoiceVolumeChange?.Invoke(_voiceVolume);
            }
        }

        /// <summary>
        /// 全局音量(0-1)
        /// 影响所有音频类型,修改时会触发音量变化事件并更新所有音频音量
        /// </summary>
        public static float GlobalVolume
        {
            get => _globalVolume;
            set
            {
                if (!_init) Init();
                _globalVolume = math.clamp(value, 0, 1);
                UpdateAllVolume();
                _onGlobalVolumeChange?.Invoke(_globalVolume);
            }
        }

        /// <summary>
        /// 初始音效播放器数量
        /// 需要在Init()之前设置
        /// </summary>
        public static int StartSound
        {
            get => _startSound;
            set => _startSound = value;
        }

        /// <summary>
        /// 最大音效播放器数量
        /// 需要在Init()之前设置
        /// </summary>
        public static int MaxSound
        {
            get => _maxSound;
            set => _maxSound = value;
        }
        
        /// <summary>
        /// 音效音高
        /// </summary>
        public static float SoundPitch
        {
            get => _soundPitch;
            set => _soundPitch = value;
        }
        
        /// <summary>
        /// 音效空间混合 (0=2D, 1=3D)
        /// </summary>
        public static float SoundSpatialBlend
        {
            get => _soundSpatialBlend;
            set => _soundSpatialBlend = value;
        }
        
        /// <summary>
        /// 音效优先级 (0最高, 256最低)
        /// </summary>
        public static int SoundPriority
        {
            get => _soundPriority;
            set => _soundPriority = value;
        }
        
        /// <summary>
        /// BGM音高
        /// </summary>
        public static float BgmPitch
        {
            get => _bgmPitch;
            set
            {
                if (!_init) Init();
                _bgmPitch = value;
                _bgm.Source.pitch = _bgmPitch;
            }
        }
        
        /// <summary>
        /// BGM空间混合 (0=2D, 1=3D)
        /// </summary>
        public static float BgmSpatialBlend
        {
            get => _bgmSpatialBlend;
            set
            {
                if (!_init) Init();
                _bgmSpatialBlend = value;
                _bgm.Source.spatialBlend = _bgmSpatialBlend;
            }
        }
        
        /// <summary>
        /// BGM优先级 (0最高, 256最低)
        /// </summary>
        public static int BgmPriority
        {
            get => _bgmPriority;
            set
            {
                if (!_init) Init();
                _bgmPriority = value;
                _bgm.Source.priority = _bgmPriority;
            }
        }
        
        /// <summary>
        /// 语音音高
        /// </summary>
        public static float VoicePitch
        {
            get => _voicePitch;
            set
            {
                if (!_init) Init();
                _voicePitch = value;
                _voice.Source.pitch = _voicePitch;
            }
        }
        
        /// <summary>
        /// 语音空间混合 (0=2D, 1=3D)
        /// </summary>
        public static float VoiceSpatialBlend
        {
            get => _voiceSpatialBlend;
            set
            {
                if (!_init) Init();
                _voiceSpatialBlend = value;
                _voice.Source.spatialBlend = _voiceSpatialBlend;
            }
        }
        
        /// <summary>
        /// 语音优先级 (0最高, 256最低)
        /// </summary>
        public static int VoicePriority
        {
            get => _voicePriority;
            set
            {
                if (!_init) Init();
                _voicePriority = value;
                _voice.Source.priority = _voicePriority;
            }
        }

        /// <summary>
        /// 初始化音频系统
        /// 系统会在首次使用时自动初始化,也可以手动调用以控制初始化时机
        /// 如果存在配置文件,会自动加载配置
        /// </summary>
        public static void Init()
        {
            if (_init) return;
            _init = true;
            
            // 尝试加载默认配置(只设置backing field,不访问AudioSource)
            LoadDefaultConfig();
            
            _root = new GameObject("[EUAudio]");
            Object.DontDestroyOnLoad(_root);
            NativeInit();
            EUAudioSourceInit();
        }
        
        /// <summary>
        /// 加载默认配置文件
        /// 会在Resources/EUAudio目录下查找名为"EUAudioConfig"的配置文件
        /// 注意：此方法在Init()早期调用，AudioSource尚未创建，
        /// 因此只能设置backing field，不能使用会访问AudioSource的属性setter
        /// </summary>
        private static void LoadDefaultConfig()
        {
            var config = Resources.Load<EUAudioConfig>("EUAudio/EUAudioConfig");
            if (config != null)
            {
                // [修复] 只设置backing field，不触发属性setter
                // 原代码调用config.ApplyConfig()会通过setter访问尚未初始化的_bgm/_voice/_useSound导致NullReferenceException
                _startSound = config.startSound;
                _maxSound = config.maxSound;
                _soundDelayFrame = config.soundDelayFrame;
                _soundVolume = config.soundVolume;
                _bgmVolume = config.bgmVolume;
                _voiceVolume = config.voiceVolume;
                _globalVolume = config.globalVolume;
                _soundPitch = config.soundPitch;
                _soundSpatialBlend = config.soundSpatialBlend;
                _soundPriority = config.soundPriority;
                _bgmPitch = config.bgmPitch;
                _bgmSpatialBlend = config.bgmSpatialBlend;
                _bgmPriority = config.bgmPriority;
                _voicePitch = config.voicePitch;
                _voiceSpatialBlend = config.voiceSpatialBlend;
                _voicePriority = config.voicePriority;
#if UNITY_EDITOR
                Debug.Log($"[EUAudio] 已加载默认配置: {config.name}");
#endif
            }
        }
        
        /// <summary>
        /// 手动加载指定的配置文件
        /// 注意：此方法只能在Init()完成后调用
        /// </summary>
        /// <param name="config">要加载的配置文件</param>
        public static void LoadConfig(EUAudioConfig config)
        {
            if (!_init) Init();
            if (config != null)
            {
                config.ApplyConfig();
            }
        }

        private static void NativeInit()
        {
            _useSound = new(_maxSound, Allocator.Persistent);
            _useSoundIndex = new(_maxSound, Allocator.Persistent);
        }

        private static void EUAudioSourceInit()
        {
            _bgm = new GameObject($"EUBGM").AddComponent<EUAudioSource>();
            _voice = new GameObject($"EUVoice").AddComponent<EUAudioSource>();
            _bgm.transform.SetParent(_root.transform);
            _voice.transform.SetParent(_root.transform);
            
            // 应用BGM的AudioSource参数
            _bgm.Source.pitch = _bgmPitch;
            _bgm.Source.spatialBlend = _bgmSpatialBlend;
            _bgm.Source.priority = _bgmPriority;
            _bgm.Source.volume = _bgmVolume * _globalVolume;
            
            // 应用Voice的AudioSource参数
            _voice.Source.pitch = _voicePitch;
            _voice.Source.spatialBlend = _voiceSpatialBlend;
            _voice.Source.priority = _voicePriority;
            _voice.Source.volume = _voiceVolume * _globalVolume;
            
            // 设置 BGM 和 Voice 的结束监听
            _bgm.SetAudioEndListener((clip) => _onBgmEnd?.Invoke(clip));
            _voice.SetAudioEndListener((clip) => _onVoiceEnd?.Invoke(clip));
            
            // 设置 BGM 和 Voice 的音频改变监听
            _bgm.SetAudioClipChangeListener((oldClip, newClip) => _onBgmChange?.Invoke(oldClip, newClip));
            _voice.SetAudioClipChangeListener((oldClip, newClip) => _onVoiceChange?.Invoke(oldClip, newClip));
            
            for (int i = 0; i < _startSound; i++)
            {
                if (i >= _maxSound) return; //不会超过最大数量
                EUAudioSourceSoundCreate();
            }
        }

        private static void UpdateAllVolume()
        {
            float ls = _soundVolume * _globalVolume;
            for (int i = 0; i < _useSound.Length; i++)
            {
                _sound[_useSound[i]].Source.volume = ls;
            }

            _bgm.Source.volume = _bgmVolume * _globalVolume;
            _voice.Source.volume = _voiceVolume * _globalVolume;
        }

        private static int _createObjectSum;

        /// <summary>
        /// 创建EUAudioSource对象
        /// </summary>
        private static void EUAudioSourceSoundCreate()
        {
            _createObjectSum++;
            int index = _sound.Count;
            EUAudioSource ls = new GameObject($"EUSound {_sound.Count + 1}").AddComponent<EUAudioSource>();
            ls.IsSound = true;
            ls.transform.SetParent(_root.transform);
            ls.Init();
            ls.Index = index;
            _sound.Add(ls);
            _soundPool.Push(index);
        }

        private static bool GetSound(out EUAudioSource euAudioSource)
        {
            if (!_init) Init();
            bool poolHave = _soundPool.Count <= 0; //对象池没有对象闲置时该参数为真
            if (poolHave && _createObjectSum >= _maxSound)
            {
                euAudioSource = null;
                return false;
            }
            if (poolHave) EUAudioSourceSoundCreate();
            euAudioSource = _sound[_soundPool.Pop()];
            int useIndex = _useSound.Length;
            int euAudioSourceIndex = euAudioSource.Index;
            if (_useSoundIndex.ContainsKey(euAudioSourceIndex))
                _useSoundIndex[euAudioSourceIndex] = useIndex;
            else
                _useSoundIndex.Add(euAudioSourceIndex, useIndex);
            _useSound.Add(euAudioSourceIndex); //将已经使用的音效对象添加到已经使用的列表中。
#if UNITY_EDITOR
            euAudioSource.gameObject.SetActive(true);
#endif
            return true;
        }

        internal static void ReleaseSound(int index)
        {
            if (!_init) Init();
#if UNITY_EDITOR
            _sound[index].gameObject.SetActive(false);
#endif
            _soundPool.Push(index);
            int useIndex = _useSoundIndex[index];
            int euAudioSourceIndex = _useSound[^1]; //将最后那一个数据的下标变成当前数据的下标
            if (_useSoundIndex.ContainsKey(euAudioSourceIndex)) _useSoundIndex[euAudioSourceIndex] = useIndex;
            else _useSoundIndex.Add(euAudioSourceIndex, useIndex);
            _useSound.RemoveAtSwapBack(useIndex);
        }

        internal static void ReleaseSound(EUAudioSource euAudioSource)
        {
            ReleaseSound(euAudioSource.Index);
        }

        /// <summary>
        /// 释放Native容器资源
        /// 静态类在应用程序结束前都不会消失所以这个可能不会调用到,但为了可能的情况还是保留
        /// </summary>
        public static void NativeDisposable()
        {
            if (_useSound.IsCreated) _useSound.Dispose();
            if (_useSoundIndex.IsCreated) _useSoundIndex.Dispose();
        }

        /// <summary>
        /// 在指定位置播放音效
        /// </summary>
        /// <param name="clip">要播放的音频片段</param>
        /// <param name="position">播放位置(3D空间坐标)</param>
        /// <param name="onAudioEnd">音频播放结束时的回调函数(可选)</param>
        public static void PlaySound(AudioClip clip, Vector3 position,Action<AudioClip> onAudioEnd=null)
        {
            if (!GetSound(out var ls)) return;
            var lsSource = ls.Source;
            ls.DelayFrame = _soundDelayFrame;
            ls.transform.position = position;
            // [修复] 无论onAudioEnd是否为null都要设置,避免复用时残留上次的回调
            ls.SetAudioEndListener(onAudioEnd);
            
            // 应用Sound的AudioSource参数
            lsSource.pitch = _soundPitch;
            lsSource.spatialBlend = _soundSpatialBlend;
            lsSource.priority = _soundPriority;
            // [修复] 设置音效音量,原代码缺失导致音效以残留音量或默认值播放
            lsSource.volume = _soundVolume * _globalVolume;
            
            lsSource.clip = clip;
            ls.Play();
        }

        /// <summary>
        /// 在默认位置(Vector3.zero)播放音效
        /// </summary>
        /// <param name="clip">要播放的音频片段</param>
        /// <param name="onAudioEnd">音频播放结束时的回调函数(可选)</param>
        public static void PlaySound(AudioClip clip,Action<AudioClip> onAudioEnd = null)
        {
            PlaySound(clip, Vector3.zero, onAudioEnd);
        }

        /// <summary>
        /// 设置背景音乐但不播放
        /// 如果当前BGM正在播放且fadeTime大于0,会先淡出当前BGM再设置新的音频
        /// </summary>
        /// <param name="clip">要设置的音频片段</param>
        /// <param name="fadeTime">淡出时间(秒),默认为0</param>
        /// <param name="loop">是否循环播放,默认为true</param>
        public static void SetBGM(AudioClip clip, float fadeTime = 0,bool loop = true)
        {
            if (!_init) Init();
            if (fadeTime > 0 && _bgm.Source.isPlaying)
            {
                SetBGMWithFade(clip, fadeTime, loop).Forget();
            }
            else
            {
                _bgm.SetClip(clip);
                _bgm.SetLoop(loop);
            }
        }
        
        /// <summary>
        /// 设置并播放背景音乐,支持淡入淡出效果
        /// </summary>
        /// <param name="clip">要播放的音频片段</param>
        /// <param name="fadeTime">淡入淡出时间(秒),默认为0</param>
        /// <param name="loop">是否循环播放,默认为true</param>
        public static void PlayBGM(AudioClip clip, float fadeTime = 0,bool loop = true)
        {
            if (!_init) Init();
            if (fadeTime <= 0)
            {
                _bgm.SetClip(clip);
                _bgm.SetLoop(loop);
                _bgm.Play();
            }
            else
            {
                PlayBGMWithFade(clip, fadeTime, loop).Forget();
            }
        }

        /// <summary>
        /// 播放已设置的背景音乐
        /// </summary>
        public static void PlayBGM()
        {
            if (!_init) Init();
            _bgm.Play();
        }
        
        /// <summary>
        /// 停止背景音乐播放,支持淡出效果
        /// </summary>
        /// <param name="fadeTime">淡出时间(秒),默认为0</param>
        public static void StopBGM(float fadeTime = 0)
        {
            if (!_init) Init();
            if (fadeTime <= 0)
            {
                _bgm.Stop();
            }
            else
            {
                StopBGMWithFade(fadeTime).Forget();
            }
        }

        /// <summary>
        /// 设置语音但不播放
        /// 如果当前语音正在播放且fadeTime大于0,会先淡出当前语音再设置新的音频
        /// </summary>
        /// <param name="clip">要设置的音频片段</param>
        /// <param name="fadeTime">淡出时间(秒),默认为0</param>
        /// <param name="loop">是否循环播放,默认为false</param>
        public static void SetVoice(AudioClip clip, float fadeTime = 0, bool loop = false)
        {
            if (!_init) Init();
            if (fadeTime > 0 && _voice.Source.isPlaying)
            {
                SetVoiceWithFade(clip, fadeTime, loop).Forget();
            }
            else
            {
                _voice.SetClip(clip);
                _voice.SetLoop(loop);
            }
        }

        /// <summary>
        /// 设置并播放语音,支持淡入淡出效果
        /// </summary>
        /// <param name="clip">要播放的音频片段</param>
        /// <param name="fadeTime">淡入淡出时间(秒),默认为0</param>
        /// <param name="loop">是否循环播放,默认为false</param>
        public static void PlayVoice(AudioClip clip, float fadeTime = 0 ,bool loop = false)
        {
            if (!_init) Init();
            if (fadeTime <= 0)
            {
                _voice.SetClip(clip);
                _voice.SetLoop(loop);
                _voice.Play();
            }
            else
            {
                PlayVoiceWithFade(clip, fadeTime, loop).Forget();
            }
        }
        
        /// <summary>
        /// 播放已设置的语音
        /// </summary>
        public static void PlayVoice()
        {
            if (!_init) Init();
            _voice.Play();
        }

        /// <summary>
        /// 停止语音播放,支持淡出效果
        /// </summary>
        /// <param name="fadeTime">淡出时间(秒),默认为0</param>
        public static void StopVoice(float fadeTime = 0)
        {
            if (!_init) Init();
            if (fadeTime <= 0)
            {
                _voice.Stop();
            }
            else
            {
                StopVoiceWithFade(fadeTime).Forget();
            }
        }
        
        private static async UniTaskVoid SetBGMWithFade(AudioClip clip, float fadeTime, bool loop)
        {
            float startVolume = _bgm.Source.volume;
            float elapsed = 0;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                _bgm.Source.volume = math.lerp(startVolume, 0, elapsed / fadeTime);
                await UniTask.Yield();
            }
            _bgm.Stop();
            _bgm.Source.volume = _bgmVolume * _globalVolume;
            _bgm.SetClip(clip);
            _bgm.SetLoop(loop);
        }
        
        private static async UniTaskVoid SetVoiceWithFade(AudioClip clip, float fadeTime, bool loop)
        {
            float startVolume = _voice.Source.volume;
            float elapsed = 0;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                _voice.Source.volume = math.lerp(startVolume, 0, elapsed / fadeTime);
                await UniTask.Yield();
            }
            _voice.Stop();
            _voice.Source.volume = _voiceVolume * _globalVolume;
            _voice.SetClip(clip);
            _voice.SetLoop(loop);
        }
        
        private static async UniTaskVoid PlayBGMWithFade(AudioClip clip, float fadeTime, bool loop)
        {
            if (_bgm.Source.isPlaying)
            {
                float startVolume = _bgm.Source.volume;
                float elapsed = 0;
                while (elapsed < fadeTime / 2)
                {
                    elapsed += Time.deltaTime;
                    _bgm.Source.volume = math.lerp(startVolume, 0, elapsed / (fadeTime / 2));
                    await UniTask.Yield();
                }
                _bgm.Stop();
            }
            
            _bgm.SetClip(clip);
            _bgm.SetLoop(loop);
            _bgm.Source.volume = 0;
            _bgm.Play();
            
            float targetVolume = _bgmVolume * _globalVolume;
            float elapsed2 = 0;
            while (elapsed2 < fadeTime / 2)
            {
                elapsed2 += Time.deltaTime;
                _bgm.Source.volume = math.lerp(0, targetVolume, elapsed2 / (fadeTime / 2));
                await UniTask.Yield();
            }
            _bgm.Source.volume = targetVolume;
        }
        
        private static async UniTaskVoid StopBGMWithFade(float fadeTime)
        {
            if (!_bgm.Source.isPlaying) return;
            
            float startVolume = _bgm.Source.volume;
            float elapsed = 0;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                _bgm.Source.volume = math.lerp(startVolume, 0, elapsed / fadeTime);
                await UniTask.Yield();
            }
            _bgm.Stop();
            _bgm.Source.volume = _bgmVolume * _globalVolume;
        }
        
        private static async UniTaskVoid PlayVoiceWithFade(AudioClip clip, float fadeTime, bool loop)
        {
            if (_voice.Source.isPlaying)
            {
                float startVolume = _voice.Source.volume;
                float elapsed = 0;
                while (elapsed < fadeTime / 2)
                {
                    elapsed += Time.deltaTime;
                    _voice.Source.volume = math.lerp(startVolume, 0, elapsed / (fadeTime / 2));
                    await UniTask.Yield();
                }
                _voice.Stop();
            }
            
            _voice.SetClip(clip);
            _voice.SetLoop(loop);
            _voice.Source.volume = 0;
            _voice.Play();
            
            float targetVolume = _voiceVolume * _globalVolume;
            float elapsed2 = 0;
            while (elapsed2 < fadeTime / 2)
            {
                elapsed2 += Time.deltaTime;
                _voice.Source.volume = math.lerp(0, targetVolume, elapsed2 / (fadeTime / 2));
                await UniTask.Yield();
            }
            _voice.Source.volume = targetVolume;
        }
        
        private static async UniTaskVoid StopVoiceWithFade(float fadeTime)
        {
            if (!_voice.Source.isPlaying) return;
            
            float startVolume = _voice.Source.volume;
            float elapsed = 0;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                _voice.Source.volume = math.lerp(startVolume, 0, elapsed / fadeTime);
                await UniTask.Yield();
            }
            _voice.Stop();
            _voice.Source.volume = _voiceVolume * _globalVolume;
        }
        
        public static void SetSoundVolumeChangeListener(Action<float> action) => _onSoundVolumeChange = action;
        public static void AddSoundVolumeChangeListener(Action<float> action) => _onSoundVolumeChange += action;
        public static void RemoveSoundVolumeChangeListener(Action<float> action) => _onSoundVolumeChange -= action;
        public static void RemoveAllSoundVolumeChangeListener() => _onSoundVolumeChange = null;

        public static void SetBgmVolumeChangeListener(Action<float> action) => _onBgmVolumeChange = action;
        public static void AddBgmVolumeChangeListener(Action<float> action) => _onBgmVolumeChange += action;
        public static void RemoveBgmVolumeChangeListener(Action<float> action) => _onBgmVolumeChange -= action;
        public static void RemoveAllBgmVolumeChangeListener() => _onBgmVolumeChange = null;

        public static void SetVoiceVolumeChangeListener(Action<float> action) => _onVoiceVolumeChange = action;
        public static void AddVoiceVolumeChangeListener(Action<float> action) => _onVoiceVolumeChange += action;
        public static void RemoveVoiceVolumeChangeListener(Action<float> action) => _onVoiceVolumeChange -= action;
        public static void  RemoveAllVoiceVolumeChangeListener() => _onVoiceVolumeChange = null;

        public static void SetGlobalVolumeChangeListener(Action<float> action) => _onGlobalVolumeChange = action;
        public static void AddGlobalVolumeChangeListener(Action<float> action) => _onGlobalVolumeChange += action;
        public static void RemoveGlobalVolumeChangeListener(Action<float> action) => _onGlobalVolumeChange -= action;
        public static void RemoveAllGlobalVolumeChangeListener() => _onGlobalVolumeChange = null;
        
        /// <summary>
        /// 设置BGM结束监听器
        /// </summary>
        /// <param name="action">监听器回调,参数为播放结束的AudioClip</param>
        public static void SetBgmEndListener(Action<AudioClip> action) => _onBgmEnd = action;
        
        /// <summary>
        /// 添加BGM结束监听器
        /// </summary>
        /// <param name="action">监听器回调,参数为播放结束的AudioClip</param>
        public static void AddBgmEndListener(Action<AudioClip> action) => _onBgmEnd += action;
        
        /// <summary>
        /// 移除BGM结束监听器
        /// </summary>
        /// <param name="action">要移除的监听器回调</param>
        public static void RemoveBgmEndListener(Action<AudioClip> action) => _onBgmEnd -= action;
        
        /// <summary>
        /// 移除所有BGM结束监听器
        /// </summary>
        public static void RemoveAllBgmEndListener() => _onBgmEnd = null;
        
        /// <summary>
        /// 设置Voice结束监听器
        /// </summary>
        /// <param name="action">监听器回调,参数为播放结束的AudioClip</param>
        public static void SetVoiceEndListener(Action<AudioClip> action) => _onVoiceEnd = action;
        
        /// <summary>
        /// 添加Voice结束监听器
        /// </summary>
        /// <param name="action">监听器回调,参数为播放结束的AudioClip</param>
        public static void AddVoiceEndListener(Action<AudioClip> action) => _onVoiceEnd += action;
        
        /// <summary>
        /// 移除Voice结束监听器
        /// </summary>
        /// <param name="action">要移除的监听器回调</param>
        public static void RemoveVoiceEndListener(Action<AudioClip> action) => _onVoiceEnd -= action;
        
        /// <summary>
        /// 移除所有Voice结束监听器
        /// </summary>
        public static void RemoveAllVoiceEndListener() => _onVoiceEnd = null;
        
        /// <summary>
        /// 设置BGM改变监听器
        /// </summary>
        /// <param name="action">监听器回调,参数为旧AudioClip和新AudioClip</param>
        public static void SetBgmChangeListener(Action<AudioClip, AudioClip> action) => _onBgmChange = action;
        
        /// <summary>
        /// 添加BGM改变监听器
        /// </summary>
        /// <param name="action">监听器回调,参数为旧AudioClip和新AudioClip</param>
        public static void AddBgmChangeListener(Action<AudioClip, AudioClip> action) => _onBgmChange += action;
        
        /// <summary>
        /// 移除BGM改变监听器
        /// </summary>
        /// <param name="action">要移除的监听器回调</param>
        public static void RemoveBgmChangeListener(Action<AudioClip, AudioClip> action) => _onBgmChange -= action;
        
        /// <summary>
        /// 移除所有BGM改变监听器
        /// </summary>
        public static void RemoveAllBgmChangeListener() => _onBgmChange = null;
        
        /// <summary>
        /// 设置Voice改变监听器
        /// </summary>
        /// <param name="action">监听器回调,参数为旧AudioClip和新AudioClip</param>
        public static void SetVoiceChangeListener(Action<AudioClip, AudioClip> action) => _onVoiceChange = action;
        
        /// <summary>
        /// 添加Voice改变监听器
        /// </summary>
        /// <param name="action">监听器回调,参数为旧AudioClip和新AudioClip</param>
        public static void AddVoiceChangeListener(Action<AudioClip, AudioClip> action) => _onVoiceChange += action;
        
        /// <summary>
        /// 移除Voice改变监听器
        /// </summary>
        /// <param name="action">要移除的监听器回调</param>
        public static void RemoveVoiceChangeListener(Action<AudioClip, AudioClip> action) => _onVoiceChange -= action;
        
        /// <summary>
        /// 移除所有Voice改变监听器
        /// </summary>
        public static void RemoveAllVoiceChangeListener() => _onVoiceChange = null;
    }
}
