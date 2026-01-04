using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EUFarmworker.Core.Abstracts;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EUFarmworker.Tools.EUAudio.Script
{
    public class EUAudio : AbstractUtility
    {
        private EUAudioManagerMono _root;

        public override void Init()
        {
            _root = new GameObject("EUAudio").AddComponent<EUAudioManagerMono>();
            Object.DontDestroyOnLoad(_root.gameObject);
        }
    }
}