using UnityEngine;

namespace Wolf.Protocol
{
    /// <summary>Score + combo tracker (autoload equivalent).</summary>
    public class ScoreSystem : MonoBehaviour
    {
        public static ScoreSystem Instance { get; private set; }

        public const float ComboIdleSec = 2f;
        public const int EnemyPoints = 100;
        public const int BossPoints = 5000;

        public int Score { get; private set; }
        public int Combo { get; private set; }

        float _lastHitTime = -999f;

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

        void Update()
        {
            if (Combo > 0 && Time.time - _lastHitTime > ComboIdleSec)
                Combo = 0;
        }

        public void EnterGame()
        {
            Score = 0;
            Combo = 0;
            _lastHitTime = -999f;
        }

        public void RegisterHit()
        {
            Combo = (Time.time - _lastHitTime <= ComboIdleSec) ? Combo + 1 : 1;
            _lastHitTime = Time.time;
        }

        public void AddScore(int basePoints)
        {
            if (Time.time - _lastHitTime <= ComboIdleSec)
                Combo++;
            else
                Combo = 1;
            _lastHitTime = Time.time;
            float mult = 1f + (Combo - 1) * 0.5f;
            Score += Mathf.RoundToInt(basePoints * mult);
        }

        public float ComboMultiplier => 1f + (Combo - 1) * 0.5f;
    }
}
