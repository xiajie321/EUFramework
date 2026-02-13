using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EUFramwork.Extension.EUAudioKit
{
    public class EUAudioSource:MonoBehaviour
    {
        private AudioSource _source;
        private int _index;
        private int _delayFrame;
        public AudioSource Source => _source;
        public int Index
        {
            get => _index;
            internal set => _index = value;
        }
        public int DelayFrame
        {
            get => _delayFrame; 
            set => _delayFrame = value; 
        }
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
        public void Play()
        {
            _playVersion++; // 版本号自增，立即使旧任务失效
            _source.Play();
            PlayAsync(_playVersion).Forget();
        }

        public void Stop()
        {
            _source.Stop();
        }
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
            if (version == _playVersion)
            {
                EUAudio.ReleaseSound(_index);
            }
        }
    }
}