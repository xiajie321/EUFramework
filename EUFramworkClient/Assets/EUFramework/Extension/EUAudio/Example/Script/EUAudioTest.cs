using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EUFramwork.Extension.EUAudioKit
{
    public class EUAudioTest : MonoBehaviour
    {
        public AudioClip audioClip;
        void Start()
        {
            EUAudio.PlayBGM(audioClip);
            EUAudio.PlayVoice(audioClip);
            EUAudio.PlaySound(audioClip);
        }
        void Update()
        {
        
        }
    }
}
