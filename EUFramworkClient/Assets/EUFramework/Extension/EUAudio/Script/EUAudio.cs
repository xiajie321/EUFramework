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
        private static GameObject _root;
        private static EUAudioSource _bgm;//背景音乐(可能有过渡)
        private static EUAudioSource _voice;
        private static Action<float> _onChangeVolume;
        private static Action<float> _onChangeBgmVolume;
        private static Action<float> _onChangeVoiceVolume;
        private static readonly List<EUAudioSource> _sound = new();
        private static readonly Stack<int> _soundPool = new();//可以使用的音效播放器
        private static readonly List<int> _useSound = new();//已经使用的音效播放
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
                _soundVolume = math.clamp(value,0,1);
                for (int i = 0; i < _useSound.Count; i++)
                {
                    _sound[_useSound[i]].Source.volume = _soundVolume;
                }
                _onChangeVolume?.Invoke(value);
            } 
        }

        public static float BgmVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = math.clamp(value,0,1);
                _bgm.Source.volume = _bgmVolume;
                _onChangeBgmVolume?.Invoke(value);
            } 
        }

        public static float VoiceVolume
        {
            get => _voiceVolume;
            set
            {
                _voiceVolume = math.clamp(value,0,1);
                _voice.Source.volume = _bgmVolume;
                _onChangeVoiceVolume?.Invoke(value);
            } 
        }
        
        public static void Init()
        {
            if(_init) return;
            _init = true;
            _root = new GameObject("[EUAudio]");
            Object.DontDestroyOnLoad(_root);
            EUAudioSourceInit();
        }
        
        private static void EUAudioSourceInit()
        {
            _bgm = new GameObject($"EUBGM").AddComponent<EUAudioSource>();
            _voice = new GameObject($"EUVoice").AddComponent<EUAudioSource>();
            _bgm.transform.SetParent(_root.transform);
            _voice.transform.SetParent(_root.transform);
            for (int i = 0; i < _startSound; i++)
            {
                if(i >= _maxSound) return;//不会超过最大数量
                EUAudioSourceCreate();
            }
        }

        private static int _createObjectSum;
        /// <summary>
        /// 创建EUAudioSource对象
        /// </summary>
        private static void EUAudioSourceCreate()
        {
            _createObjectSum++;
            int index = _sound.Count;
            EUAudioSource ls = new GameObject($"EUSound {_sound.Count+1}").AddComponent<EUAudioSource>();
            ls.transform.SetParent(_root.transform);
            ls.Init();
            ls.Index = index;
            _sound.Add(ls);
            _soundPool.Push(index);
        }
        
        private static bool GetSound(out EUAudioSource euAudioSource)
        {
            bool poolHave = _soundPool.Count <= 0;//对象池没有对象闲置时该参数为真
            if (poolHave && _createObjectSum >= _maxSound)
            {
                euAudioSource = null;
                return false;
            }
            if (poolHave) EUAudioSourceCreate();
            euAudioSource = _sound[_soundPool.Pop()];
            _useSound.Add(euAudioSource.Index);//将已经使用的音效对象添加到已经使用的列表中。
            #if UNITY_EDITOR
            euAudioSource.gameObject.SetActive(true);
            #endif
            return true;
        }
        internal static void ReleaseSound(int index)
        {
            #if UNITY_EDITOR
            _sound[index].gameObject.SetActive(false);
            #endif
            _soundPool.Push(index);
            _useSound.RemoveAtSwapBack(index);
        }
        internal static void ReleaseSound(EUAudioSource euAudioSource)
        {
            #if UNITY_EDITOR
            euAudioSource.gameObject.SetActive(false);
            #endif
            int index = euAudioSource.Index;
            _soundPool.Push(index);
            _useSound.RemoveAtSwapBack(index);
        }
        public static void PlaySound(AudioClip clip,Vector3 position,float volume = -1,bool loop = false)
        {
            if(!GetSound(out var ls)) return;
            var lsSource = ls.Source;
            ls.DelayFrame = _soundDelayFrame;
            ls.transform.position = position;
            lsSource.clip = clip;
            lsSource.loop = loop;
            var lsVolume = volume;
            if (lsVolume < 0)
            {
                lsVolume = _soundVolume;
            }
            lsSource.volume = lsVolume;
            ls.Play();
        }
        public static void PlaySound(AudioClip clip, Vector3 position, bool loop = false)
        {
            PlaySound(clip, position,-1, loop);
        }
        public static void PlaySound(AudioClip clip, float volume=-1, bool loop = false)
        {
            PlaySound(clip,Vector3.zero,volume, loop);
        }
        public static void PlaySound(AudioClip clip, bool loop = false)
        {
            PlaySound(clip,Vector3.zero,-1, loop);
        }
    }
}