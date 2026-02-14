using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EUFramwork.Extension.EUAudioKit
{
    /// <summary>
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
        private static GameObject _root;
        private static EUAudioSource _bgm1; //背景音乐
        private static EUAudioSource _bgm2; //背景音乐()
        private static EUAudioSource _voice;
        private static Action<float> _onSoundVolumeChange;
        private static Action<float> _onBgmVolumeChange;
        private static Action<float> _onVoiceVolumeChange;
        private static Action<float> _onGlobalVolumeChange;
        private static readonly List<EUAudioSource> _sound = new();
        private static readonly Stack<int> _soundPool = new(); //可以使用的音效播放器
        private static NativeList<int> _useSound; //已经使用的音效播放
        private static NativeHashMap<int, int> _useSoundIndex; //映射用于快速查找删除

        /// <summary>
        /// 用于Sound的监听判断,为了通用优化,对于音游这种对于音频精准度要求较高的场景建议将SoundDelayFrame修改为1
        /// </summary>
        public static int SoundDelayFrame
        {
            get => _soundDelayFrame;
            set => _soundDelayFrame = value;
        }

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

        public static float BgmVolume
        {
            get => _bgmVolume;
            set
            {
                if (!_init) Init();
                _bgmVolume = math.clamp(value, 0, 1);
                _bgm1.Source.volume = _bgmVolume * _globalVolume;
                _onBgmVolumeChange?.Invoke(_bgmVolume);
            }
        }

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

        public static int StartSound
        {
            get => _startSound;
            set => _startSound = value;
        }

        public static int MaxSound
        {
            get => _maxSound;
            set => _maxSound = value;
        }

        public static void Init()
        {
            if (_init) return;
            _init = true;
            _root = new GameObject("[EUAudio]");
            Object.DontDestroyOnLoad(_root);
            NativeInit();
            EUAudioSourceInit();
        }

        private static void NativeInit()
        {
            _useSound = new(_maxSound, Allocator.Persistent);
            _useSoundIndex = new(_maxSound, Allocator.Persistent);
        }

        private static void EUAudioSourceInit()
        {
            _bgm1 = new GameObject($"EUBGM").AddComponent<EUAudioSource>();
            _voice = new GameObject($"EUVoice").AddComponent<EUAudioSource>();
            _bgm1.transform.SetParent(_root.transform);
            _voice.transform.SetParent(_root.transform);
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

            _bgm1.Source.volume = _bgmVolume * _globalVolume;
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

        public static void NativeDisposable()//静态类在应用程序结束前都不会消失所以这个可能不会调用到,但为了可能的情况还是保留。
        {
            if (_useSound.IsCreated) _useSound.Dispose();
            if (_useSoundIndex.IsCreated) _useSoundIndex.Dispose();
        }

        public static void PlaySound(AudioClip clip, Vector3 position, float volume = -1,Action<AudioClip> onAudioEnd=null)
        {
            if (!GetSound(out var ls)) return;
            var lsSource = ls.Source;
            ls.DelayFrame = _soundDelayFrame;
            ls.transform.position = position;
            if(onAudioEnd != null) ls.SetAudioEndListener(onAudioEnd);
            lsSource.clip = clip;
            var lsVolume = volume * _globalVolume;
            if (lsVolume < 0) lsVolume = _soundVolume * _globalVolume;
            lsSource.volume = lsVolume;
            ls.Play();
        }
        
        public static void PlaySound(AudioClip clip, float volume = -1,Action<AudioClip> onAudioEnd=null)
        {
            PlaySound(clip, Vector3.zero, volume, onAudioEnd);
        }

        public static void PlaySound(AudioClip clip,Action<AudioClip> onAudioEnd = null)
        {
            PlaySound(clip, Vector3.zero,-1, onAudioEnd);
        }

        public static void SetBGM(AudioClip clip, float fadeTime = 0,float volume = -1 ,bool loop = true)
        {
            
        }
        public static void PlayBGM(AudioClip clip, float fadeTime = 0,float volume = -1 ,bool loop = true)
        {
            //设置并播放    
        }

        public static void PlayBGM()
        {
        }
        public static void StopBGM()
        {
            
        }

        public static void SetVoice(AudioClip clip, float fadeTime = 0, float volume = -1, bool loop = false)
        {
            
        }

        public static void PlayVoice(AudioClip clip, float fadeTime = 0 ,float volume = -1 ,bool loop = false)
        {
            //设置并播放
        }
        
        public static void PlayVoice()
        {
        }

        public static void StopVoice()
        {
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
    }
}