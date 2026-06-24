using UnityEngine;

namespace Wolf.Protocol
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Bullet : MonoBehaviour
    {
        Vector2 _dir = Vector2.right;
        float _speed = 720f;
        float _damage = 15f;
        PlayerController _shooter;
        Feel.PauseTier _pauseTier = Feel.PauseTier.Light;
        Feel.StunLevel _stunLevel = Feel.StunLevel.Normal;
        float _life = 1.2f;
        SpriteRenderer _sprite;

        public void Init(Vector2 dir, float speed, float damage, PlayerController shooter, Feel.PauseTier pauseTier, Feel.StunLevel stunLevel)
        {
            _dir = dir;
            _speed = speed;
            _damage = damage;
            _shooter = shooter;
            _pauseTier = pauseTier;
            _stunLevel = stunLevel;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        void Awake()
        {
            var rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.isKinematic = true;
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(12f, 4f);
            _sprite = gameObject.AddComponent<SpriteRenderer>();
            _sprite.sprite = PlaceholderSprite.White;
            _sprite.color = new Color(1f, 0.92f, 0.55f);
            transform.localScale = new Vector3(0.22f, 0.22f, 1f);
        }

        void Update()
        {
            transform.position += (Vector3)(_dir * _speed * Time.deltaTime);
            _life -= Time.deltaTime;
            if (_life <= 0f) Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_shooter != null && other.gameObject == _shooter.gameObject) return;
            var enemy = other.GetComponent<EnemyController>();
            var boss = other.GetComponent<BossController>();
            float mult = _shooter != null ? _shooter.DamageMult : 1f;
            if (enemy != null)
            {
                enemy.TakeDamage(_damage * mult, _dir, _pauseTier, _stunLevel);
                SimpleVfx.Spawn(transform.parent, transform.position, "impact", new Color(1f, 0.85f, 0.5f));
                Destroy(gameObject);
            }
            else if (boss != null)
            {
                boss.TakeDamage(_damage * mult, _dir, _pauseTier, _stunLevel);
                SimpleVfx.Spawn(transform.parent, transform.position, "impact", new Color(1f, 0.85f, 0.5f));
                Destroy(gameObject);
            }
        }
    }
}
