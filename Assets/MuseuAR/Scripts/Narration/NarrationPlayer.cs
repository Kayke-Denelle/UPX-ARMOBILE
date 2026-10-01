using System;
using UnityEngine;

namespace MuseuAR
{
    /// <summary>
    /// Audioguia: toca uma narração por vez (peça ou curiosidade).
    /// Áudio 2D — pensado para o visitante usar fone de ouvido.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class NarrationPlayer : MonoBehaviour
    {
        public static NarrationPlayer Instance { get; private set; }

        public event Action StateChanged;

        AudioSource source;
        bool paused;
        bool wasPlaying;

        public AudioClip CurrentClip => source ? source.clip : null;
        public bool IsPlaying => source && source.isPlaying;
        public bool IsPaused => paused;

        public float Progress =>
            source && source.clip && source.clip.length > 0f
                ? Mathf.Clamp01(source.time / source.clip.length)
                : 0f;

        void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Play(AudioClip clip)
        {
            if (!clip || !source) return;
            source.Stop();
            source.clip = clip;
            source.time = 0f;
            source.Play();
            paused = false;
            StateChanged?.Invoke();
        }

        /// <summary>Se for outro clipe, começa do zero. Se for o mesmo, alterna pausa/continuar.</summary>
        public void TogglePlayPause(AudioClip clip)
        {
            if (!source) return;
            if (source.clip != clip || (!source.isPlaying && !paused))
            {
                Play(clip);
                return;
            }

            if (paused) { source.UnPause(); paused = false; }
            else { source.Pause(); paused = true; }
            StateChanged?.Invoke();
        }

        public void Stop()
        {
            if (!source) return;
            source.Stop();
            paused = false;
            StateChanged?.Invoke();
        }

        void Update()
        {
            bool playing = source.isPlaying;
            if (wasPlaying && !playing && !paused) StateChanged?.Invoke(); // terminou sozinho
            wasPlaying = playing;
        }
    }
}
