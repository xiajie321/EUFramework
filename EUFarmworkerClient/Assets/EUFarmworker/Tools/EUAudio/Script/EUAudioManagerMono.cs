using System;
using System.Collections.Generic;
using UnityEngine;
namespace EUFarmworker.Tools.EUAudio.Script
{
    public enum BGMPlayMode
    {
        Single,//单播(单曲播放完毕后停止)
        OrderLoop,//顺序循环
        SingleLoop,//单曲循环
        Random,//随机
    }

    public class BGMClip:IEquatable<BGMClip>
    {
        public AudioClip Clip;//BGM音效
        public float Volume = 1f;//单独BGM默认音量
        public float Offset;//时间轴中偏移
        public float FadeInTime;//淡入时间
        public float FadeOutTime;//淡出时间
        public bool Equals(BGMClip other)
        {
            if (other == null) return false;
            return Clip.name.Equals(other.Clip.name);
        }
    }
    /// <summary>
    /// 这里只是一个运行时,一般来说用户控制音频应该是在EUAudio.cs中控制
    /// </summary>
    public class EUAudioManagerMono :MonoBehaviour
    {
//         private AudioSource _bgm;//BGM同一时间仅能播放一个
//         private List<BGMClip> _bgmList;
//         private Dictionary<string, BGMClip> _bgmDic;//用于去除重复的BGM
//         private List<AudioSource> _sound;//一个音效一个AudioSource
//         private Queue<int> _okayUseSoundQueue;//空闲的音效播放器(如果为0后执行AddSound会对_sound进行扩容) <-使用UniTask异步将已经播放完毕的AudioSource放入队列
//         private float _BGMVolume = 1f;//BGM默认音量
//         private float _SoundVolume = 1f;//音效默认音量
//         
//         private int _soundCount = 10;
//         private BGMPlayMode _playMode = BGMPlayMode.Single;
//         
//
//         public float BGMVolume
//         {
//             get=>_BGMVolume;
//             set
//             {
//                 _BGMVolume = value;
//             }
//         }
//
//         public float SoundVolume
//         {
//             get => _SoundVolume;
//             set => _SoundVolume = value;
//         }
//         public int SoundCount
//         {
//             get=>_sound.Count;
//             set=>_soundCount=value;
//         }
//         private void Awake()
//         {
//             _bgm = gameObject.AddComponent<AudioSource>();
//             _bgmList = new();
//             _bgmDic = new();
//             _sound = new(_soundCount);
//             _okayUseSoundQueue = new(_soundCount);
//         }
//         
//         
//
//         public void SetBGMPlayMode(BGMPlayMode mode)
//         {
//         }
//
//         public bool GetBGMPlayMode() 
//         {
//             
//         }
//         
//         public void SetBGM(AudioClip clip,float volume = -1)//设置BGM(如果引用列表已经存在则会直接播放)
//         {
//             
//         }
//
//         public void SetBGM(BGMClip clip)
//         {
//          
//         }
//
//         public void AddBGM(BGMClip clip)
//         {
//
//         }
//
//         public void InsertBGM(int index, BGMClip clip)
//         {
//             
//         }
//
//         public void AddBGM(AudioClip clip,float volume = -1)
//         {
//           
//         }
//
//         public void InsertBGM(int index, AudioClip clip,float volume = -1)//插入BGM
//         {
//            
//         }
//
//         public void SetCurrentBGMVolume(float volume)
//         {
//         
//         }
//
//         public bool BGMContains(AudioClip clip)
//         {
//            
//         }
//
//         public bool BGMContains(string clipName)
//         {
//            
//         }
//
//         public List<BGMClip> GetBGMList()
//         {
//             
//         }
//
//         public void ClearBGM()//清除所有的BGM引用(主要作用,防止在释放资源时报错)
//         {
//             
//         }
//
//         public void StopBGM()//暂停BGM
//         {
//             
//         }
//
//         public void PlayBGM()//播放BGM
//         {
//             
//         }
//         public void AddSound(AudioClip clip,float volume = -1,Vector2 position = default)//volume = -1表示默认音量
//         {
//            
//         }
//
//         public void StopAllSound()//暂停所有音效(主要作用,防止在释放资源时报错)
//         {
//             
//         }
    }
}
