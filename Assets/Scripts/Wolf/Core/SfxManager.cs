using System.Collections.Generic;
using UnityEngine;

namespace Wolf.Protocol
{
    /// <summary>Freesound SFX playback pool.</summary>
    public class SfxManager : MonoBehaviour
    {
        public static SfxManager Instance { get; private set; }

        readonly Dictionary<string, AudioClip> _clips = new();
        readonly List<AudioSource> _pool = new();
        int _next;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            for (int i = 0; i < 16; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _pool.Add(src);
            }

            Load("gun_pistol", "gun_rifle", "gun_smg", "gun_shotgun", "explosion", "melee",
                "hurt", "reload", "pickup", "victory", "death", "heartbeat");
            Alias("shot", "gun_pistol");
            Alias("kill", "explosion");
            Alias("slam", "explosion");
            Alias("vengeance", "heartbeat");
            _clips["hit"] = GeneratePunch(170f, 0.10f, 0.6f, 0.85f);
        }

        void Load(params string[] names)
        {
            foreach (var n in names)
            {
                var clip = Resources.Load<AudioClip>($"Audio/{n}");
                if (clip != null) _clips[n] = clip;
            }
        }

        void Alias(string alias, string source)
        {
            if (_clips.TryGetValue(source, out var c))
                _clips[alias] = c;
        }

        public void Play(string name, float pitch = 1f)
        {
            if (!_clips.TryGetValue(name, out var clip) || clip == null) return;
            var src = _pool[_next];
            _next = (_next + 1) % _pool.Count;
            src.clip = clip;
            src.pitch = pitch;
            src.Play();
        }

        static AudioClip GeneratePunch(float freq, float dur, float vol, float noiseAmt)
        {
            const int rate = 22050;
            int n = Mathf.CeilToInt(rate * dur);
            var samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                float prog = (float)i / n;
                float t = (float)i / rate;
                float env = Mathf.Exp(-prog * 6f);
                float body = Mathf.Sin(2f * Mathf.PI * freq * Mathf.Lerp(1f, 0.4f, prog) * t);
                float noise = Random.Range(-1f, 1f);
                float s = Mathf.Lerp(body, noise, noiseAmt) * env * 2.4f;
                samples[i] = Mathf.Clamp(s * vol, -1f, 1f);
            }
            var clip = AudioClip.Create("hit_punch", n, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
