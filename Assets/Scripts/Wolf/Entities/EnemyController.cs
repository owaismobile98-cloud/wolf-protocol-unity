using UnityEngine;

namespace Wolf.Protocol
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : MonoBehaviour
    {
        public enum Kind { Runner, Brute, Shooter }

        public Kind EnemyKind = Kind.Runner;
        public Transform Target;
        public float Speed = 110f;
        public float MaxShield;
        public float Shield;
        public float MaxHealth = 30f;
        public float Health = 30f;
        public float ContactDamage = 8f;

        Vector2 _size = new(28f, 44f);
        Color _color = new(0.9f, 0.25f, 0.2f);
        float _flash;
        Vector2 _knockback;
        float _touchCd;
        float _hitstun;
        bool _dying;
        float _preferredRange = 2.4f;
        float _orbitAngle;
        float _fireCd;
        float _telegraph;
        SpriteRenderer _sprite;
        Rigidbody2D _rb;

        public void Configure(Kind k, int wave)
        {
            EnemyKind = k;
            if (k == Kind.Shooter)
            {
                Speed = 95f + wave * 3f;
                MaxHealth = 20f + wave * 3f;
                MaxShield = 0f;
                ContactDamage = 6f;
                _size = new Vector2(26f, 42f);
                _color = new Color(0.95f, 0.55f, 0.2f);
                _preferredRange = 230f + Random.Range(-25f, 35f);
                _orbitAngle = Random.Range(0f, Mathf.PI * 2f);
                _fireCd = Random.Range(0.8f, 1.8f);
            }
            else if (k == Kind.Brute)
            {
                Speed = 64f;
                MaxHealth = 70f + wave * 10f;
                MaxShield = 25f;
                ContactDamage = 16f;
                _size = new Vector2(40f, 58f);
                _color = new Color(0.62f, 0.1f, 0.1f);
            }
            else
            {
                Speed = 120f + wave * 4f;
                MaxHealth = 26f + wave * 4f;
                MaxShield = 0f;
                ContactDamage = 8f;
                _size = new Vector2(26f, 42f);
                _color = new Color(0.95f, 0.28f, 0.22f);
            }
            Health = MaxHealth;
            Shield = MaxShield;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            gameObject.layer = LayerMask.NameToLayer(WolfLayers.Enemy);
            gameObject.tag = "Enemy";

            var col = gameObject.AddComponent<BoxCollider2D>();
            col.size = _size;

            _sprite = gameObject.GetComponent<SpriteRenderer>();
            if (_sprite == null)
            {
                _sprite = gameObject.AddComponent<SpriteRenderer>();
                _sprite.sprite = PlaceholderSprite.White;
                _sprite.color = _color;
            }
            if (EnemyKind == Kind.Brute) transform.localScale = Vector3.one * 1.35f;
            if (GetComponent<YSorter>() == null)
                gameObject.AddComponent<YSorter>();
        }

        void FixedUpdate()
        {
            if (_dying) return;

            if (_hitstun > 0f)
            {
                _hitstun -= Time.fixedDeltaTime;
                _rb.linearVelocity = _knockback;
                _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, 7f * Time.fixedDeltaTime);
                _flash = Mathf.Max(0f, _flash - Time.fixedDeltaTime);
                UpdateVisuals();
                return;
            }

            if (EnemyKind == Kind.Shooter)
            {
                ShooterUpdate();
                return;
            }

            var chase = Vector2.zero;
            if (Target != null)
            {
                var toT = (Vector2)Target.position - (Vector2)transform.position;
                chase = toT.normalized * Speed;
                _touchCd = Mathf.Max(_touchCd - Time.fixedDeltaTime, 0f);
                if (toT.magnitude < _size.x * 0.5f + 22f && _touchCd <= 0f)
                {
                    Target.GetComponent<PlayerController>()?.TakeDamage(ContactDamage, toT);
                    _touchCd = 0.7f;
                    _knockback = -toT.normalized * 120f;
                }
            }
            _rb.linearVelocity = chase + _knockback;
            _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, 7f * Time.fixedDeltaTime);
            _flash = Mathf.Max(0f, _flash - Time.fixedDeltaTime);
            UpdateVisuals();
        }

        void ShooterUpdate()
        {
            if (Target == null)
            {
                _rb.linearVelocity = _knockback;
                return;
            }

            _orbitAngle += Time.fixedDeltaTime * 0.5f;
            var slot = (Vector2)Target.position + new Vector2(Mathf.Cos(_orbitAngle), Mathf.Sin(_orbitAngle)) * _preferredRange;
            var toSlot = slot - (Vector2)transform.position;
            var move = _telegraph > 0f ? Vector2.zero : Vector2.ClampMagnitude(toSlot, Speed);
            _rb.linearVelocity = move + _knockback;
            _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, 7f * Time.fixedDeltaTime);
            _touchCd = Mathf.Max(_touchCd - Time.fixedDeltaTime, 0f);
            _flash = Mathf.Max(0f, _flash - Time.fixedDeltaTime);

            _fireCd = Mathf.Max(_fireCd - Time.fixedDeltaTime, 0f);
            float dist = Vector2.Distance(Target.position, transform.position);
            if (_telegraph > 0f)
            {
                _telegraph -= Time.fixedDeltaTime;
                if (_telegraph <= 0f) FireAtPlayer();
            }
            else if (_fireCd <= 0f && dist < _preferredRange + 120f)
            {
                _telegraph = 0.35f;
            }
            UpdateVisuals();
        }

        void FireAtPlayer()
        {
            if (Target == null) return;
            var d = ((Vector2)Target.position - (Vector2)transform.position).normalized;
            var go = new GameObject("EnemyBullet");
            go.transform.SetParent(transform.parent);
            var bullet = go.AddComponent<EnemyBullet>();
            bullet.Init(d);
            go.transform.position = transform.position + (Vector3)(d * 26f);
            SfxManager.Instance?.Play("gun_pistol");
            _fireCd = Random.Range(1.4f, 2.4f);
        }

        void UpdateVisuals()
        {
            if (_sprite == null || Target == null) return;
            _sprite.flipX = Target.position.x < transform.position.x;
            if (_flash > 0f) _sprite.color = new Color(2f, 2f, 2f);
            else if (_telegraph > 0f) _sprite.color = new Color(1.6f, 0.9f, 0.3f);
            else _sprite.color = _color;
        }

        public void TakeDamage(float amount, Vector2 dir, Feel.PauseTier pauseTier = Feel.PauseTier.Light, Feel.StunLevel stunLevel = Feel.StunLevel.Normal)
        {
            _flash = 0.12f;
            _knockback = dir.normalized * (EnemyKind == Kind.Brute ? 120f : 200f);
            int lvl = Mathf.Max((int)stunLevel - (EnemyKind == Kind.Brute ? 1 : 0), 0);
            _hitstun = Feel.Hitstun((Feel.StunLevel)lvl, ScoreSystem.Instance?.Combo ?? 0);
            ScoreSystem.Instance?.RegisterHit();
            Feel.Instance?.Pause(pauseTier);
            DamageNumber.Spawn(transform.parent, transform.position, Mathf.RoundToInt(amount));
            SimpleVfx.Spawn(transform.parent, transform.position, "blood", new Color(1f, 0.6f, 0.3f));
            SfxManager.Instance?.Play("hit");

            var player = Target?.GetComponent<PlayerController>();
            player?.AddShake(3f);
            player?.AddFury(4f);

            if (Shield > 0f)
            {
                Shield -= amount;
                if (Shield < 0f) { Health += Shield; Shield = 0f; }
            }
            else Health -= amount;

            if (Health <= 0f) Die();
        }

        void Die()
        {
            if (_dying) return;
            _dying = true;
            gameObject.tag = "Untagged";
            GetComponent<Collider2D>().enabled = false;
            SimpleVfx.Spawn(transform.parent, transform.position, "burst", _color * 1.2f);
            SfxManager.Instance?.Play("kill");
            Target?.GetComponent<PlayerController>()?.AddShake(8f);
            Target?.GetComponent<PlayerController>()?.AddFury(18f);

            if (Random.value < 0.25f)
            {
                var p = new GameObject("Pickup");
                p.transform.SetParent(transform.parent);
                p.transform.position = transform.position;
                p.AddComponent<Pickup>();
            }

            FindAnyObjectByType<GameManager>()?.OnEnemyKilled();
            Destroy(gameObject, 0.55f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            var half = _size * 0.5f;
            var top = transform.position + Vector3.up * (half.y + 0.08f);
            Gizmos.DrawLine(top + Vector3.left * half.x, top + Vector3.right * half.x);
        }
    }
}
