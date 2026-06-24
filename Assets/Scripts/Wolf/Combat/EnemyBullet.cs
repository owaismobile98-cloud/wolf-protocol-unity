using UnityEngine;

namespace Wolf.Protocol
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyBullet : MonoBehaviour
    {
        Vector2 _dir = Vector2.right;
        const float Speed = 360f;
        const float Damage = 9f;
        float _life = 2.2f;

        public void Init(Vector2 dir)
        {
            _dir = dir;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        void Awake()
        {
            var rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.isKinematic = true;
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(10f, 4f);
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSprite.White;
            sr.color = new Color(1f, 0.35f, 0.3f);
            transform.localScale = new Vector3(0.2f, 0.2f, 1f);
        }

        void Update()
        {
            transform.position += (Vector3)(_dir * Speed * Time.deltaTime);
            _life -= Time.deltaTime;
            if (_life <= 0f) Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Enemy")) return;
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(Damage, _dir);
                SimpleVfx.Spawn(transform.parent, transform.position, "impact", new Color(1f, 0.4f, 0.35f));
                Destroy(gameObject);
            }
        }
    }
}
