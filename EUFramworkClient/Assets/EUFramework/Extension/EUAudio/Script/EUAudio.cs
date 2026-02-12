using System.Collections.Generic;
using UnityEngine;

namespace EUFramwork.Extension.Audio
{
    public static class EUAudio
    {
        private static bool _init = false;
        private static GameObject _root;
        private static readonly List<AudioSource> _sound = new();
        private static readonly Stack<int> _soundPool = new();
        private static readonly AudioSource _bgm;//背景音乐
        private static readonly AudioSource _voice;
        public static void Init()
        {
            if(_init) return;
            _init = true;
            _root = new GameObject("[EUAudio]");
            Object.DontDestroyOnLoad(_root);
        }
    }
}