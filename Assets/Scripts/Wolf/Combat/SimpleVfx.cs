using UnityEngine;

namespace Wolf.Protocol
{
    public class SimpleVfx : MonoBehaviour
    {
        string _kind;
        Color _color;
        float _t;
        const float Duration = 0.35f;

        public static void Spawn(Transform parent, Vector3 pos, string kind, Color color)
        {
            var go = new GameObject($"VFX_{kind}");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var v = go.AddComponent<SimpleVfx>();
            v._kind = kind;
            v._color = color;
        }

        void Update()
        {
            _t += Time.deltaTime;
            if (_t >= Duration) { Destroy(gameObject); return; }
            float a = 1f - _t / Duration;
            transform.localScale = Vector3.one * (0.3f + _t * 2f);
        }

        void OnRenderObject()
        {
            // Drawn via gizmo-style fallback in editor; runtime uses sprite child.
        }

        void Start()
        {
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSprite.White;
            sr.color = _color;
            sr.sortingOrder = 10;
            transform.localScale = Vector3.one * 0.2f;
        }
    }
}
