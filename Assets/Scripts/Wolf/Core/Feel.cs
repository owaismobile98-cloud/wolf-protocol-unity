using System;
using System.Collections;
using UnityEngine;

namespace Wolf.Protocol
{
    /// <summary>
    /// Hit-pause + hitstun constants verified from Ra Ra BOOM teardown.
    /// </summary>
    public class Feel : MonoBehaviour
    {
        public static Feel Instance { get; private set; }

        public enum PauseTier { None = 0, Light = 1, Medium = 2, Heavy = 3 }
        public enum StunLevel { Low = 0, Normal = 1, High = 2, Ultimate = 3 }

        public static readonly float[] PauseSeconds = { 0f, 0.07f, 0.11f, 0.20f };
        public const float PauseScale = 0.02f;
        public static readonly float[] StunMult = { 0.85f, 1.1f, 1.35f, 1.6f };
        public const float StunBase = 0.32f;

        bool _pausing;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Pause(PauseTier tier)
        {
            if (tier == PauseTier.None || _pausing) return;
            StartCoroutine(PauseRoutine((int)tier));
        }

        IEnumerator PauseRoutine(int tier)
        {
            _pausing = true;
            float seconds = PauseSeconds[Mathf.Clamp(tier, 0, 3)];
            Time.timeScale = PauseScale;
            yield return new WaitForSecondsRealtime(seconds);
            Time.timeScale = 1f;
            _pausing = false;
        }

        public static float Hitstun(StunLevel level, int combo)
        {
            int idx = Mathf.Clamp((int)level, 0, 3);
            float mult = StunMult[idx] / Mathf.Pow(2f, combo / 2f);
            return StunBase * mult;
        }
    }
}
