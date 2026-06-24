using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Wolf.Protocol
{
    /// <summary>
    /// AI Director — static fallback banter + optional LLM upgrade (Groq/Cerebras/Fireworks).
    /// </summary>
    public class Director : MonoBehaviour
    {
        public static Director Instance { get; private set; }

        public event Action<string, string> OnBanter;

        const string Ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) WolfProtocol/1.0";
        static readonly string[] Squad = { "Razor", "Specter", "Volt", "Salem", "Maker" };
        static readonly string SecretsPath = @"D:\Pro Jack\secrets\.env";

        struct Gateway
        {
            public string KeyName;
            public string Url;
            public string Model;
        }

        static readonly Gateway[] Gateways =
        {
            new() { KeyName = "GROQ_API_KEY", Url = "https://api.groq.com/openai/v1/chat/completions", Model = "llama-3.3-70b-versatile" },
            new() { KeyName = "CEREBRAS_API_KEY", Url = "https://api.cerebras.ai/v1/chat/completions", Model = "gpt-oss-120b" },
            new() { KeyName = "FIREWORKS_API_KEY", Url = "https://api.fireworks.ai/inference/v1/chat/completions", Model = "accounts/fireworks/models/gpt-oss-120b" },
        };

        static readonly Dictionary<string, string[][]> StaticBanter = new()
        {
            ["wave_start"] = new[]
            {
                new[] { "Razor", "Eyes up. Movement on the perimeter." },
                new[] { "Specter", "Contacts inbound. Stay in the pocket." },
                new[] { "Volt", "Light 'em up the second they break cover." },
            },
            ["low_health"] = new[]
            {
                new[] { "Maker", "Razor's bleeding out — somebody cover him!" },
                new[] { "Salem", "Patch up. We don't leave wolves behind." },
            },
            ["streak"] = new[]
            {
                new[] { "Volt", "That's how the Wolf Team does it!" },
                new[] { "Specter", "Clean work. Keep the chain going." },
            },
            ["boss"] = new[]
            {
                new[] { "Razor", "Iron-Sides. So Command sent their dog." },
                new[] { "Salem", "Big armor, bigger ego. Crack the shield first." },
            },
            ["victory"] = new[]
            {
                new[] { "Razor", "Sector's ours. On to the next lie they told us." },
            },
        };

        static readonly Dictionary<string, string> LineVo = new()
        {
            ["Eyes up. Movement on the perimeter."] = "razor_wave",
            ["Contacts inbound. Stay in the pocket."] = "specter_contact",
            ["That's how the Wolf Team does it!"] = "volt_streak",
            ["Patch up. We don't leave wolves behind."] = "salem_cover",
            ["Iron-Sides. So Command sent their dog."] = "razor_boss",
            ["Sector's ours. On to the next lie they told us."] = "razor_victory",
        };

        readonly Dictionary<string, string> _keys = new();
        AudioSource _vo;
        bool _busy;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _keys.Clear();
            LoadKeys();
            _vo = gameObject.AddComponent<AudioSource>();
        }

        void LoadKeys()
        {
            foreach (var g in Gateways)
            {
                var env = Environment.GetEnvironmentVariable(g.KeyName);
                if (!string.IsNullOrEmpty(env))
                    _keys[g.KeyName] = env;
            }

            if (!File.Exists(SecretsPath)) return;
            foreach (var line in File.ReadAllLines(SecretsPath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("#") || !trimmed.Contains('=')) continue;
                var eq = trimmed.IndexOf('=');
                _keys[trimmed[..eq].Trim()] = trimmed[(eq + 1)..].Trim();
            }
        }

        public void Say(string evt, Dictionary<string, object> ctx = null)
        {
            EmitStatic(evt);
            if (!_busy && HasKey())
                StartCoroutine(RequestLlm(evt, ctx ?? new Dictionary<string, object>()));
        }

        public EncounterSpec EncounterForWave(int wave)
        {
            return new EncounterSpec
            {
                Runners = Mathf.Clamp(2 + wave, 2, 8),
                Brutes = Mathf.Clamp(wave / 2, 0, 4),
                Shooters = Mathf.Clamp(wave - 1, 0, 4),
                SpeedMult = Mathf.Clamp(1f + wave * 0.04f, 1f, 1.4f),
                Taunt = "The regime sends more meat.",
            };
        }

        void EmitStatic(string evt)
        {
            if (!StaticBanter.TryGetValue(evt, out var bank) || bank.Length == 0)
                bank = StaticBanter["wave_start"];
            var pick = bank[UnityEngine.Random.Range(0, bank.Length)];
            Emit(pick[0], pick[1]);
        }

        void Emit(string speaker, string line)
        {
            OnBanter?.Invoke(speaker, line);
            if (LineVo.TryGetValue(line, out var clipName))
            {
                var clip = Resources.Load<AudioClip>($"Audio/vo/{clipName}");
                if (clip != null)
                {
                    _vo.clip = clip;
                    _vo.Play();
                }
            }
        }

        bool HasKey()
        {
            foreach (var g in Gateways)
                if (_keys.ContainsKey(g.KeyName)) return true;
            return false;
        }

        Gateway ActiveGateway()
        {
            foreach (var g in Gateways)
                if (_keys.TryGetValue(g.KeyName, out _))
                    return g;
            return default;
        }

        IEnumerator RequestLlm(string evt, Dictionary<string, object> ctx)
        {
            var g = ActiveGateway();
            if (string.IsNullOrEmpty(g.Url)) yield break;

            _busy = true;
            var sys = "You write ONE line of in-combat radio banter for THE WOLF TEAM, a gritty Delta squad "
                      + "gone rogue against a corrupt regime (cyberpunk, Streets-of-Rage x cover-shooter). Members: "
                      + string.Join(", ", Squad)
                      + ". Output STRICT JSON only: {\"speaker\":\"<name>\",\"line\":\"<<=90 chars, punchy, profanity-light>\"}. No extra text.";
            var usr = $"EVENT: {evt}. CONTEXT: {MiniJson(ctx)}";
            var body = "{\"model\":\"" + g.Model + "\",\"temperature\":0.9,\"max_tokens\":60,\"messages\":["
                       + "{\"role\":\"system\",\"content\":" + JsonQuote(sys) + "},"
                       + "{\"role\":\"user\",\"content\":" + JsonQuote(usr) + "}]}";

            using var req = new UnityWebRequest(g.Url, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", "Bearer " + _keys[g.KeyName]);
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("User-Agent", Ua);

            yield return req.SendWebRequest();
            _busy = false;

            if (req.result != UnityWebRequest.Result.Success) yield break;
            TryParseLlm(req.downloadHandler.text);
        }

        void TryParseLlm(string txt)
        {
            var content = ExtractJsonField(txt, "content");
            if (string.IsNullOrEmpty(content)) return;
            int lb = content.IndexOf('{');
            int rb = content.LastIndexOf('}');
            if (lb < 0 || rb <= lb) return;
            var inner = content.Substring(lb, rb - lb + 1);
            var speaker = ExtractJsonField(inner, "speaker")?.Trim();
            var line = ExtractJsonField(inner, "line")?.Trim();
            if (string.IsNullOrEmpty(speaker) || string.IsNullOrEmpty(line)) return;
            if (Array.IndexOf(Squad, speaker) < 0) speaker = Squad[0];
            if (line.Length > 90) line = line[..90];
            Emit(speaker, line);
        }

        static string ExtractJsonField(string json, string key)
        {
            var needle = $"\"{key}\":";
            int i = json.IndexOf(needle, StringComparison.Ordinal);
            if (i < 0) return null;
            i += needle.Length;
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            if (i >= json.Length) return null;
            if (json[i] == '"')
            {
                i++;
                var sb = new StringBuilder();
                while (i < json.Length)
                {
                    if (json[i] == '\\' && i + 1 < json.Length) { sb.Append(json[i + 1]); i += 2; continue; }
                    if (json[i] == '"') break;
                    sb.Append(json[i++]);
                }
                return sb.ToString();
            }
            int end = json.IndexOfAny(new[] { ',', '}' }, i);
            return end < 0 ? json[i..].Trim() : json[i..end].Trim().Trim('"');
        }

        static string JsonQuote(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        static string MiniJson(Dictionary<string, object> ctx)
        {
            if (ctx == null || ctx.Count == 0) return "{}";
            var parts = new List<string>();
            foreach (var kv in ctx)
                parts.Add($"\"{kv.Key}\":{kv.Value}");
            return "{" + string.Join(",", parts) + "}";
        }
    }

    public struct EncounterSpec
    {
        public int Runners;
        public int Brutes;
        public int Shooters;
        public float SpeedMult;
        public string Taunt;
    }
}
