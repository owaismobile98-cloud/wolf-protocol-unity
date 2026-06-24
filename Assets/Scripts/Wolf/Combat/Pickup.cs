using UnityEngine;

namespace Wolf.Protocol
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class Pickup : MonoBehaviour
    {
        public float HealAmount = 25f;
        float _t;
        SpriteRenderer _sprite;

        void Awake()
        {
            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 13f;
            _sprite = gameObject.AddComponent<SpriteRenderer>();
            _sprite.sprite = PlaceholderSprite.White;
            _sprite.color = new Color(0.12f, 0.7f, 0.35f);
            transform.localScale = Vector3.one * 0.2f;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float pulse = 1f + 0.15f * Mathf.Sin(_t * 6f);
            transform.localScale = Vector3.one * 0.2f * pulse;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player == null) return;
            player.Heal(HealAmount);
            SfxManager.Instance?.Play("pickup");
            Destroy(gameObject);
        }
    }
}
