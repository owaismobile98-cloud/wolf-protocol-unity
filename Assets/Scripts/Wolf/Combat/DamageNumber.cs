using UnityEngine;

namespace Wolf.Protocol
{
    public class DamageNumber : MonoBehaviour
    {
        float _t;
        TextMesh _text;

        public static void Spawn(Transform parent, Vector3 pos, int amount, Color? col = null)
        {
            var go = new GameObject("DamageNumber");
            go.transform.SetParent(parent);
            go.transform.position = pos + new Vector3(-0.06f, 0.28f);
            var dn = go.AddComponent<DamageNumber>();
            dn._text = go.AddComponent<TextMesh>();
            dn._text.text = amount.ToString();
            dn._text.fontSize = 48;
            dn._text.characterSize = 0.05f;
            dn._text.anchor = TextAnchor.MiddleCenter;
            dn._text.color = col ?? new Color(1f, 0.85f, 0.5f);
        }

        void Update()
        {
            _t += Time.deltaTime;
            transform.position += Vector3.up * (42f * Time.deltaTime * 0.01f);
            var c = _text.color;
            c.a = Mathf.Clamp01(1f - _t / 0.7f);
            _text.color = c;
            if (_t >= 0.7f) Destroy(gameObject);
        }
    }
}
