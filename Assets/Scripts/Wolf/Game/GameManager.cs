using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if CINEMACHINE
using Unity.Cinemachine;
#endif

namespace Wolf.Protocol
{
    public class GameManager : MonoBehaviour
    {
        static readonly int[][] Waves =
        {
            new[] { 3, 0 }, new[] { 4, 1 }, new[] { 3, 2 }, new[] { 5, 2 },
            new[] { 4, 3 }, new[] { 6, 3 }, new[] { 5, 4 },
        };

        PlayerController _player;
        BossController _boss;
        int _kills;
        int _wave;
        int _alive;
        string _state = "fight";
        float _banterAlpha;
        string _banterText = "";

        void Awake()
        {
            EnsureServices();
            BuildArena();
            SpawnPlayer();
            SetupCamera();
            SpawnWave();
        }

        void EnsureServices()
        {
            if (Feel.Instance == null) new GameObject("Feel").AddComponent<Feel>();
            if (ScoreSystem.Instance == null) new GameObject("Score").AddComponent<ScoreSystem>();
            if (SfxManager.Instance == null) new GameObject("Sfx").AddComponent<SfxManager>();
            if (Director.Instance == null) new GameObject("Director").AddComponent<Director>();
            ScoreSystem.Instance.EnterGame();
            Director.Instance.OnBanter += OnBanter;
        }

        void BuildArena()
        {
            var floor = new GameObject("Floor");
            floor.transform.SetParent(transform);
            var sr = floor.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSprite.White;
            sr.color = new Color(0.12f, 0.13f, 0.16f);
            floor.transform.localScale = new Vector3(80f, 45f, 1f);
            floor.transform.position = new Vector3(320f, 200f, 10f);
        }

        void SpawnPlayer()
        {
            var go = new GameObject("Player");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(320f, 200f, 0f);
            _player = go.AddComponent<PlayerController>();
        }

        void SetupCamera()
        {
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 200f;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(320f, 200f, -10f);
            _player.BindCamera(cam);

#if CINEMACHINE
            camGo.AddComponent<CinemachineBrain>();
            var vcamGo = new GameObject("CM_PlayerFollow");
            vcamGo.transform.SetParent(transform);
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            vcam.Follow = _player.transform;
            vcam.Lens.OrthographicSize = 200f;
#endif
        }

        void SpawnWave()
        {
            if (_wave >= Waves.Length)
            {
                _state = "victory";
                return;
            }

            _state = "fight";
            var comp = Waves[_wave];
            int runners = comp[0];
            int brutes = comp[1];
            var enc = Director.Instance.EncounterForWave(_wave);
            int shooters = enc.Shooters;
            float speedMult = enc.SpeedMult;
            int total = runners + brutes + shooters;

            for (int i = 0; i < total; i++)
            {
                var go = new GameObject($"Enemy_{i}");
                go.transform.SetParent(transform);
                var e = go.AddComponent<EnemyController>();
                var k = EnemyController.Kind.Runner;
                if (i < brutes) k = EnemyController.Kind.Brute;
                else if (i < brutes + shooters) k = EnemyController.Kind.Shooter;
                e.Configure(k, _wave);
                e.Speed *= speedMult;
                float ang = Mathf.PI * 2f * i / total;
                float dist = Random.Range(240f, 340f);
                go.transform.position = (Vector2)_player.transform.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
                e.Target = _player.transform;
            }

            _alive = total;
            Director.Instance.Say("wave_start", new System.Collections.Generic.Dictionary<string, object>
            {
                ["wave"] = _wave + 1,
                ["runners"] = runners,
                ["brutes"] = brutes,
            });
            _wave++;
        }

        public void OnEnemyKilled()
        {
            ScoreSystem.Instance.AddScore(ScoreSystem.EnemyPoints);
            _kills++;
            _alive--;
            if (_kills > 0 && _kills % 8 == 0)
                Director.Instance.Say("streak", new System.Collections.Generic.Dictionary<string, object> { ["kills"] = _kills });

            if (_alive <= 0)
            {
                if (_wave >= Waves.Length)
                {
                    _state = "clear";
                    StartCoroutine(SpawnBossDelayed(1.5f));
                }
                else
                {
                    _state = "clear";
                    StartCoroutine(SpawnWaveDelayed(2f));
                }
            }
        }

        IEnumerator SpawnWaveDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnWave();
        }

        IEnumerator SpawnBossDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnBoss();
        }

        void SpawnBoss()
        {
            _state = "boss";
            Director.Instance.Say("boss");
            var go = new GameObject("Boss");
            go.transform.SetParent(transform);
            _boss = go.AddComponent<BossController>();
            _boss.Target = _player.transform;
            go.transform.position = _player.transform.position + Vector3.up * 280f;
        }

        public void OnBossKilled()
        {
            ScoreSystem.Instance.AddScore(ScoreSystem.BossPoints);
            _state = "victory";
            Director.Instance.Say("victory");
            SfxManager.Instance?.Play("victory");
        }

        public void OnPlayerDied()
        {
            _state = "dead";
            SfxManager.Instance?.Play("death");
        }

        void OnBanter(string speaker, string line)
        {
            _banterText = $"{speaker}: {line}";
            _banterAlpha = 1f;
            StopAllCoroutines();
            StartCoroutine(FadeBanter());
        }

        IEnumerator FadeBanter()
        {
            yield return new WaitForSeconds(3.5f);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime;
                _banterAlpha = 1f - t;
                yield return null;
            }
            _banterAlpha = 0f;
        }

        void Update()
        {
            if (_player != null && _player.Health < 30f && _state == "fight")
            {
                // low health banter once per wave-ish — keep simple
            }

            if ((_state == "dead" || _state == "victory") && Input.GetKey(KeyCode.R))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            GUI.Label(new Rect(16, 10, 600, 24),
                $"KILLS: {_kills}      WAVE: {Mathf.Min(_wave, Waves.Length)} / {Waves.Length}      ENEMIES: {Mathf.Max(_alive, 0)}", style);

            if (_player != null)
            {
                DrawBar(16, 34, 200, 16, new Color(0.15f, 0.15f, 0.18f), new Color(0.25f, 0.85f, 0.45f),
                    _player.Health / _player.MaxHealth);
                DrawBar(16, 54, 200, 10, new Color(0.15f, 0.12f, 0.12f), new Color(1f, 0.35f, 0.2f),
                    _player.Fury / _player.MaxFury);
                GUI.Label(new Rect(16, 70, 400, 24), _player.WeaponText(), style);
                if (_player.IsFury()) GUI.Label(new Rect(222, 48, 120, 20), "VENGEANCE!", style);
                else if (_player.Fury >= _player.MaxFury) GUI.Label(new Rect(222, 48, 160, 20), "Q: VENGEANCE", style);
            }

            if (ScoreSystem.Instance != null)
            {
                GUI.Label(new Rect(480, 10, 200, 20), $"SCORE: {ScoreSystem.Instance.Score}", style);
                if (ScoreSystem.Instance.Combo > 1)
                    GUI.Label(new Rect(480, 30, 240, 20),
                        $"COMBO x{ScoreSystem.Instance.Combo}  ({ScoreSystem.Instance.ComboMultiplier:F1}x)", style);
            }

            if (_boss != null)
                DrawBar(160, 92, 320, 14, new Color(0.15f, 0.1f, 0.1f), new Color(0.85f, 0.18f, 0.16f), _boss.TotalFraction);

            var statusStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
            string status = _state switch
            {
                "clear" => "ROOM CLEAR  —  GO →",
                "victory" => "VICTORY\nPress R",
                "dead" => "YOU DIED\nPress R",
                _ => "",
            };
            if (!string.IsNullOrEmpty(status))
                GUI.Label(new Rect(190, 150, 400, 80), status, statusStyle);

            if (_banterAlpha > 0f)
            {
                var c = new Color(0.7f, 1f, 0.9f, _banterAlpha);
                var bs = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = c } };
                GUI.Label(new Rect(16, 322, 600, 40), _banterText, bs);
            }
        }

        static void DrawBar(float x, float y, float w, float h, Color bg, Color fg, float frac)
        {
            var prev = GUI.color;
            GUI.color = bg;
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = fg;
            GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(frac), h), Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}
