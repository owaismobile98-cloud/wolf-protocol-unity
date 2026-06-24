using UnityEngine;

namespace Wolf.Protocol
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BossController : MonoBehaviour
    {
        public Transform Target;
        public float MaxShield = 80f;
        public float Shield = 80f;
        public float MaxArmor = 120f;
        public float Armor = 120f;
        public float MaxHealth = 180f;
        public float Health = 180f;

        const float DmgCapPerWindow = 70f;
        Vector2 _size = new(72f, 96f);
        float _flash;
        Vector2 _knockback;
        string _state = "approach";
        float _timer = 1f;
        Vector2 _chargeDir = Vector2.right;
        float _touchCd;
        float _dmgWindowStart;
        float _dmgInWindow;
        SpriteRenderer _sprite;
        Rigidbody2D _rb;

        public int Phase
        {
            get
            {
                if (Shield > 0f) return 0;
                if (Armor > 0f) return 1;
                return 2;
            }
        }

        public float TotalFraction => (Shield + Armor + Health) / (MaxShield + MaxArmor + MaxHealth);

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            gameObject.layer = LayerMask.NameToLayer(WolfLayers.Enemy);
            gameObject.tag = "Enemy";

            var col = gameObject.AddComponent<BoxCollider2D>();
            col.size = _size;

            _sprite = gameObject.AddComponent<SpriteRenderer>();
            _sprite.sprite = PlaceholderSprite.White;
            _sprite.color = new Color(0.85f, 0.35f, 0.32f);
            transform.localScale = Vector3.one * 2f;
        }

        void FixedUpdate()
        {
            _flash = Mathf.Max(0f, _flash - Time.fixedDeltaTime);
            _touchCd = Mathf.Max(_touchCd - Time.fixedDeltaTime, 0f);
            _timer -= Time.fixedDeltaTime;

            var toT = Target != null ? (Vector2)Target.position - (Vector2)transform.position : Vector2.zero;

            switch (_state)
            {
                case "approach":
                    _rb.linearVelocity = toT.normalized * 72f + _knockback;
                    if (_timer <= 0f)
                    {
                        if (Phase >= 1 && Random.value < 0.5f)
                        { _state = "slam_tele"; _timer = 0.7f; }
                        else
                        { _state = "charge_tele"; _timer = 0.6f; _chargeDir = toT.normalized; }
                    }
                    break;
                case "charge_tele":
                    _rb.linearVelocity = _knockback * 0.5f;
                    _chargeDir = Vector2.Lerp(_chargeDir, toT.normalized, 0.05f);
                    if (_timer <= 0f) { _state = "charge"; _timer = 0.45f; }
                    break;
                case "charge":
                    _rb.linearVelocity = _chargeDir * (Phase == 2 ? 540f : 430f);
                    TryTouch(toT, 22f);
                    if (_timer <= 0f) { _state = "approach"; _timer = Phase < 2 ? 1.2f : 0.7f; }
                    break;
                case "slam_tele":
                    _rb.linearVelocity = _knockback * 0.5f;
                    if (_timer <= 0f)
                    {
                        _state = "slam"; _timer = 0.25f;
                        SimpleVfx.Spawn(transform.parent, transform.position, "burst", new Color(1f, 0.6f, 0.2f));
                        SfxManager.Instance?.Play("slam", 0.7f);
                        if (toT.magnitude < 135f)
                            Target?.GetComponent<PlayerController>()?.TakeDamage(30f, toT);
                        Target?.GetComponent<PlayerController>()?.AddShake(11f);
                    }
                    break;
                case "slam":
                    _rb.linearVelocity = _knockback * 0.5f;
                    if (_timer <= 0f) { _state = "approach"; _timer = 1f; }
                    break;
            }

            _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, 6f * Time.fixedDeltaTime);
            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            if (_sprite == null) return;
            if (Target != null) _sprite.flipX = Target.position.x < transform.position.x;
            var baseCol = Phase == 2 ? new Color(1.05f, 0.42f, 0.28f) : new Color(0.85f, 0.35f, 0.32f);
            if (_flash > 0f) _sprite.color = new Color(1.4f, 1.4f, 1.4f);
            else if (_state is "charge_tele" or "slam_tele")
                _sprite.color = ((int)(_timer * 12f) % 2 == 0) ? new Color(1.2f, 0.95f, 0.45f) : baseCol;
            else _sprite.color = baseCol;
        }

        void TryTouch(Vector2 toT, float dmg)
        {
            if (toT.magnitude < _size.x * 0.5f + 22f && _touchCd <= 0f)
            {
                Target?.GetComponent<PlayerController>()?.TakeDamage(dmg, toT);
                _touchCd = 0.6f;
            }
        }

        public void TakeDamage(float amount, Vector2 dir, Feel.PauseTier pauseTier = Feel.PauseTier.Light, Feel.StunLevel stunLevel = Feel.StunLevel.Normal)
        {
            _flash = 0.1f;
            _knockback = dir.normalized * 40f;
            Feel.Instance?.Pause(pauseTier);
            ScoreSystem.Instance?.RegisterHit();
            DamageNumber.Spawn(transform.parent, transform.position, Mathf.RoundToInt(amount), new Color(1f, 0.8f, 0.4f));
            SimpleVfx.Spawn(transform.parent, transform.position + (Vector3)(dir.normalized * 0.3f), "blood", new Color(1f, 0.6f, 0.3f));
            Target?.GetComponent<PlayerController>()?.AddFury(3f);

            float now = Time.time;
            if (now - _dmgWindowStart > 0.5f)
            {
                _dmgWindowStart = now;
                _dmgInWindow = 0f;
            }
            amount = Mathf.Min(amount, Mathf.Max(0f, DmgCapPerWindow - _dmgInWindow));
            _dmgInWindow += amount;

            int p0 = Phase;
            if (Shield > 0f) Shield -= amount;
            else if (Armor > 0f) Armor -= amount;
            else Health -= amount;

            if (Shield < 0f) { Armor += Shield; Shield = 0f; }
            if (Armor < 0f) { Health += Armor; Armor = 0f; }

            if (Phase != p0)
            {
                Target?.GetComponent<PlayerController>()?.AddShake(10f);
                SimpleVfx.Spawn(transform.parent, transform.position, "burst", new Color(0.6f, 0.8f, 1f));
            }

            if (Health <= 0f) Die();
        }

        void Die()
        {
            for (int i = 0; i < 6; i++)
            {
                var offset = new Vector3(Random.Range(-0.34f, 0.34f), Random.Range(-0.44f, 0.44f));
                SimpleVfx.Spawn(transform.parent, transform.position + offset, "burst", new Color(1f, 0.5f, 0.2f));
            }
            Target?.GetComponent<PlayerController>()?.AddShake(16f);
            SfxManager.Instance?.Play("kill");
            FindAnyObjectByType<GameManager>()?.OnBossKilled();
            Destroy(gameObject);
        }
    }
}
