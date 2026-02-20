using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EUFramwork.Extension.EUAudioKit
{

    public class EUAudioSource:MonoBehaviour
    {
        private AudioSource _source;
        private int _index;
        private int _delayFrame = 10;
        private bool _isSound;
        private Action<AudioClip> _onAudioEnd;
        private Action<AudioClip,AudioClip> _onClipChange;
        
        internal AudioSource Source => _source;
        
        internal int Index
        {
            get => _index;
            set => _index = value;
        }
        
        internal bool IsSound
        {
            get => _isSound;
            set => _isSound = value;
        }
        
        /// <summary>
        /// 用于音频播放结束的监听判断,为了通用优化默认为每10帧检查一次,对于音游这种对于音频精准度要求较高的场景建议将SoundDelayFrame修改为1
        /// </summary>
        public int DelayFrame
        {
            get => _delayFrame; 
            set => _delayFrame = value; 
        }
        
        /// <summary>
        /// 获取或设置音频片段
        /// </summary>
        public AudioClip Clip
        {
            get => _source.clip;
            set => SetClip(value);
        }
        
        /// <summary>
        /// 获取或设置音量
        /// </summary>
        public float Volume
        {
            get => _source.volume;
            set => _source.volume = value;
        }
        
        /// <summary>
        /// 获取或设置音高
        /// </summary>
        public float Pitch
        {
            get => _source.pitch;
            set => _source.pitch = value;
        }
        
        /// <summary>
        /// 获取或设置立体声声像（-1左，0中，1右）
        /// </summary>
        public float StereoPan
        {
            get => _source.panStereo;
            set => _source.panStereo = value;
        }
        
        /// <summary>
        /// 获取或设置空间混合（0为2D，1为3D）
        /// </summary>
        public float SpatialBlend
        {
            get => _source.spatialBlend;
            set => _source.spatialBlend = value;
        }
        
        /// <summary>
        /// 获取或设置混响区域混合
        /// </summary>
        public float ReverbZoneMix
        {
            get => _source.reverbZoneMix;
            set => _source.reverbZoneMix = value;
        }
        
        /// <summary>
        /// 获取或设置多普勒级别
        /// </summary>
        public float DopplerLevel
        {
            get => _source.dopplerLevel;
            set => _source.dopplerLevel = value;
        }
        
        /// <summary>
        /// 获取或设置扩散角度
        /// </summary>
        public float Spread
        {
            get => _source.spread;
            set => _source.spread = value;
        }
        
        /// <summary>
        /// 获取或设置最小距离
        /// </summary>
        public float MinDistance
        {
            get => _source.minDistance;
            set => _source.minDistance = value;
        }
        
        /// <summary>
        /// 获取或设置最大距离
        /// </summary>
        public float MaxDistance
        {
            get => _source.maxDistance;
            set => _source.maxDistance = value;
        }
        
        /// <summary>
        /// 获取或设置优先级（0最高，256最低）
        /// </summary>
        public int Priority
        {
            get => _source.priority;
            set => _source.priority = value;
        }
        
        /// <summary>
        /// 获取或设置是否静音
        /// </summary>
        public bool Mute
        {
            get => _source.mute;
            set => _source.mute = value;
        }
        
        /// <summary>
        /// 获取或设置是否绕过效果
        /// </summary>
        public bool BypassEffects
        {
            get => _source.bypassEffects;
            set => _source.bypassEffects = value;
        }
        
        /// <summary>
        /// 获取或设置是否绕过监听器效果
        /// </summary>
        public bool BypassListenerEffects
        {
            get => _source.bypassListenerEffects;
            set => _source.bypassListenerEffects = value;
        }
        
        /// <summary>
        /// 获取或设置是否绕过混响区域
        /// </summary>
        public bool BypassReverbZones
        {
            get => _source.bypassReverbZones;
            set => _source.bypassReverbZones = value;
        }
        
        /// <summary>
        /// 获取或设置播放时间（秒）
        /// </summary>
        public float Time
        {
            get => _source.time;
            set => _source.time = value;
        }
        
        /// <summary>
        /// 获取或设置播放时间（采样）
        /// </summary>
        public int TimeSamples
        {
            get => _source.timeSamples;
            set => _source.timeSamples = value;
        }
        
        /// <summary>
        /// 获取是否正在播放
        /// </summary>
        public bool IsPlaying => _source.isPlaying;
        
        /// <summary>
        /// 获取是否循环播放
        /// </summary>
        public bool Loop => _source.loop;
        private void Awake()
        {
            Init();
        }
        public void Init()
        {
            _source ??= GetComponent<AudioSource>();
            _source ??= gameObject.AddComponent<AudioSource>();
        }
        private int _playVersion; // 新增版本号字段
        /// <summary>
        /// 播放
        /// </summary>
        public void Play()
        {
            _playVersion++; // 版本号自增，立即使旧任务失效
            _source.Play();
            if(_source.loop) return;
            PlayAsync(_playVersion).Forget();
        }
        /// <summary>
        /// 设置音频并播放
        /// </summary>
        /// <param name="clip"></param>
        public void Play(AudioClip clip)
        {
            SetClip(clip);
            Play();
        }
        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Stop()
        {
            _source.Stop();
        }
        /// <summary>
        /// 设置是否循环播放
        /// </summary>
        /// <param name="loop"></param>
        public void SetLoop(bool loop)
        {
            if(_source.loop == loop) return;
            _source.loop = loop;
        }
        /// <summary>
        /// 设置循环并立即生效
        /// </summary>
        /// <param name="loop"></param>
        public void SetLoopAndWIE(bool loop)
        {
            if(_source.loop == loop) return;
            _source.loop = loop;
            Stop();
            Play();
        }
        /// <summary>
        /// 设置音频文件设置后会停止播放
        /// </summary>
        /// <param name="clip"></param>
        public void SetClip(AudioClip clip)
        {
            if(_source.clip == clip) return;
            AudioClip oldClip = _source.clip;
            _source.clip = clip;
            _onClipChange?.Invoke(oldClip,clip);
        }
        /// <summary>
        /// 设置音量大小
        /// </summary>
        /// <param name="volume"></param>
        public void SetVolume(float volume)
        {
            _source.volume = volume;
        }
        /// <summary>
        /// 设置音频播放完毕后的事件
        /// </summary>
        /// <param name="onAudioEnd">ThisEndAudioClip</param>
        public void SetAudioEndListener(Action<AudioClip> onAudioEnd)=>_onAudioEnd = onAudioEnd;
        /// <summary>
        /// 添加音频播放完毕后的事件
        /// </summary>
        /// <param name="onAudioEnd">ThisEndAudioClip</param>
        public void AddAudioEndListener(Action<AudioClip> onAudioEnd)=>_onAudioEnd += onAudioEnd;
        /// <summary>
        /// 注销音频播放完毕后的事件
        /// </summary>
        /// <param name="onAudioEnd">ThisEndAudioClip</param>
        public void RemoveAudioEndListener(Action<AudioClip> onAudioEnd)=>_onAudioEnd -= onAudioEnd;
        /// <summary>
        /// 注销所有音频播放完毕后的事件
        /// </summary>
        /// <param name="onAudioEnd">ThisEndAudioClip</param>
        public void RemoveAllAudioEndListener() => _onAudioEnd = null;
        /// <summary>
        /// 设置切换音频时的事件
        /// </summary>
        /// <param name="onAudioClipChange">OldAudioClip,NewAudioClip</param>
        public void SetAudioClipChangeListener(Action<AudioClip,AudioClip> onAudioClipChange)=>_onClipChange = onAudioClipChange;
        /// <summary>
        /// 添加切换音频时的事件
        /// </summary>
        /// <param name="onAudioClipChange">OldAudioClip,NewAudioClip</param>
        public void AddAudioClipChangeListener(Action<AudioClip,AudioClip> onAudioClipChange)=>_onClipChange += onAudioClipChange;
        /// <summary>
        /// 注销切换音频时的事件
        /// </summary>
        /// <param name="onAudioClipChange">OldAudioClip,NewAudioClip</param>
        public void RemoveAudioClipChangeListener(Action<AudioClip,AudioClip> onAudioClipChange)=>_onClipChange -= onAudioClipChange;
        /// <summary>
        /// 注销所有切换音频时的事件
        /// </summary>
        /// <param name="onAudioClipChange">OldAudioClip,NewAudioClip</param>
        public void RemoveAllAudioClipChangeListener() => _onClipChange = null;
        
        //通过脏标记的方式取消上一次任务。
        private async UniTask PlayAsync(int version)
        {
            while (_source.isPlaying && version == _playVersion)
            {
                if(DelayFrame <= 1)
                    await UniTask.Yield();
                else
                    await UniTask.DelayFrame(_delayFrame);
            }
            if(version != _playVersion) return;
            _onAudioEnd?.Invoke(_source.clip);
            if (!IsSound) return;
            EUAudio.ReleaseSound(_index);
            
        }
    }
}
