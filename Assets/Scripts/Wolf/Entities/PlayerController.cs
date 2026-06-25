using UnityEngine;
using UnityEngine.InputSystem;

namespace Wolf.Protocol
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public const float Speed = 220f;
        public const float DashSpeed = 620f;

        public float MaxHealth = 100f;
        public float Health = 100f;
        public float Fury = 0f;
        public float MaxFury = 100f;

        public Vector2 AimDir { get; private set; } = Vector2.right;
        public int Facing => AimDir.x >= 0 ? 1 : -1;

        Rigidbody2D _rb;
        Camera _cam;
        CameraRig _cameraRig;
        Vector2 _moveDir = Vector2.right;
        bool _dead;
        bool _furyActive;
        float _furyTime;
        float _shake;
        float _hurt;
        float _melee;
        float _dash;
        float _dashCd;
        int _weaponIdx;
        readonly int[] _mag = new int[4];
        readonly int[] _reserve = new int[4];
        float _fireCd;
        float _reloadT;
        bool _fireQueued;
        SpriteRenderer _sprite;
        Animator _anim;

        public bool IsDashing => _dash > 0f;
        public bool IsFury => _furyActive;
        public float DamageMult => _furyActive ? 2f : 1f;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            gameObject.layer = LayerMask.NameToLayer(WolfLayers.Player);

            if (GetComponent<BoxCollider2D>() == null)
            {
                var col = gameObject.AddComponent<BoxCollider2D>();
                col.size = new Vector2(28f, 44f);
            }

            _sprite = GetComponentInChildren<SpriteRenderer>();
            if (_sprite == null) _sprite = gameObject.AddComponent<SpriteRenderer>();
            if (_sprite.sprite == null)
            {
                _sprite.sprite = PlaceholderSprite.White;
            }
            _sprite.color = new Color(0.3f, 0.75f, 1f);

            _anim = GetComponentInChildren<Animator>();

            if (GetComponent<YSorter>() == null)
                gameObject.AddComponent<YSorter>();

            for (int i = 0; i < WeaponTable.All.Length; i++)
            {
                _mag[i] = WeaponTable.All[i].Mag;
                _reserve[i] = WeaponTable.All[i].Reserve;
            }
        }

        public void BindCamera(Camera cam)
        {
            _cam = cam;
            _cameraRig = cam != null ? cam.GetComponent<CameraRig>() : null;
        }

        public void BindCameraRig(CameraRig rig)
        {
            _cameraRig = rig;
            _cam = rig != null ? rig.Camera : null;
        }

        public void AddShake(float amount) => _shake = Mathf.Max(_shake, amount);

        public void AddFury(float amount)
        {
            if (!_furyActive)
                Fury = Mathf.Clamp(Fury + amount, 0f, MaxFury);
        }

        public void Heal(float amount) => Health = Mathf.Clamp(Health + amount, 0f, MaxHealth);

        public void TakeDamage(float amount, Vector2 dir = default)
        {
            if (_dead || _dash > 0f || _furyActive) return;
            Health -= amount;
            _hurt = 0.18f;
            AddShake(4f);
            SfxManager.Instance?.Play("hurt");
            _rb.linearVelocity += dir.normalized * 120f;
            if (Health <= 0f)
            {
                Health = 0f;
                _dead = true;
                FindAnyObjectByType<GameManager>()?.OnPlayerDied();
            }
        }

        void Update()
        {
            if (_dead) return;

            var input = ReadMove();
            if (input.sqrMagnitude > 0.01f) _moveDir = input.normalized;

            _dashCd = Mathf.Max(_dashCd - Time.deltaTime, 0f);
            if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && _dash <= 0f && _dashCd <= 0f)
            {
                _dash = 0.16f;
                _dashCd = 0.6f;
            }

            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame && !_furyActive && Fury >= MaxFury)
                ActivateVengeance();

            float furySpeed = 1f;
            if (_furyActive)
            {
                _furyTime -= Time.deltaTime;
                Fury = MaxFury * (_furyTime / 5f);
                furySpeed = 1.5f;
                if (_furyTime <= 0f) { _furyActive = false; Fury = 0f; }
            }

            if (_dash > 0f)
            {
                _dash -= Time.deltaTime;
                _rb.linearVelocity = _moveDir * DashSpeed;
            }
            else
            {
                _rb.linearVelocity = input * Speed * furySpeed;
            }

            if (Mouse.current != null)
            {
                var mouseWorld = _cam != null ? _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue()) : (Vector3)transform.position + Vector3.right;
                var toMouse = (Vector2)mouseWorld - (Vector2)transform.position;
                if (toMouse.sqrMagnitude > 1f) AimDir = toMouse.normalized;
            }

            UpdateWeapon();
            _hurt = Mathf.Max(0f, _hurt - Time.deltaTime);
            _melee = Mathf.Max(0f, _melee - Time.deltaTime);
            UpdateVisuals();
            UpdateCameraShake();
        }

        Vector2 ReadMove()
        {
            if (Keyboard.current == null) return Vector2.zero;
            float x = (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f);
            float y = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);
            var v = new Vector2(x, y);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        void UpdateVisuals()
        {
            if (_sprite == null) return;
            _sprite.flipX = Facing < 0;
            if (_hurt > 0f) _sprite.color = new Color(1.2f, 1.2f, 1.2f);
            else if (_dead) _sprite.color = new Color(0.45f, 0.45f, 0.5f);
            else if (_dash > 0f) _sprite.color = new Color(0.75f, 1.35f, 1.35f);
            else if (_furyActive) _sprite.color = new Color(1.35f, 0.45f, 0.35f);
            // We removed the hardcoded cyan override here so prefab colors (like cyan vs red) stay intact

            if (_anim != null && _anim.runtimeAnimatorController != null)
            {
                if (_dead) _anim.Play("death");
                else if (_melee > 0f) _anim.Play("attack"); // May not exist, unity will ignore or warn
                else if (_rb.linearVelocity.sqrMagnitude > 0.01f) _anim.Play("run");
                else _anim.Play("idle");
            }
        }

        void UpdateCameraShake()
        {
            if (_cameraRig != null)
            {
                var lead = Vector2.Lerp(Vector2.zero, AimDir * 48f, 4f * Time.deltaTime);
                _cameraRig.SetAimLead(lead);
                if (_shake > 0f)
                {
                    _cameraRig.AddShake(_shake);
                    _shake = Mathf.MoveTowards(_shake, 0f, 40f * Time.deltaTime);
                }
                return;
            }

            if (_cam == null) return;
            var fallbackLead = Vector2.Lerp(Vector2.zero, AimDir * 48f, 4f * Time.deltaTime);
            var fallbackJitter = Vector2.zero;
            if (_shake > 0f)
            {
                fallbackJitter = new Vector2(Random.Range(-_shake, _shake), Random.Range(-_shake, _shake));
                _shake = Mathf.MoveTowards(_shake, 0f, 40f * Time.deltaTime);
            }
            _cam.transform.position = (Vector2)transform.position + fallbackLead + (Vector3)fallbackJitter;
        }

        void UpdateWeapon()
        {
            if (Keyboard.current == null) return;

            for (int i = 0; i < WeaponTable.All.Length; i++)
            {
                if (Keyboard.current[UnityEngine.InputSystem.Key.Digit1 + i].wasPressedThisFrame && _weaponIdx != i)
                {
                    _weaponIdx = i;
                    _reloadT = 0f;
                    _fireCd = 0.12f;
                }
            }
            _fireCd = Mathf.Max(_fireCd - Time.deltaTime, 0f);

            if (_reloadT > 0f)
            {
                _reloadT -= Time.deltaTime;
                if (_reloadT <= 0f) FinishReload();
            }
            else if (Keyboard.current.rKey.wasPressedThisFrame && _mag[_weaponIdx] < WeaponTable.All[_weaponIdx].Mag && _reserve[_weaponIdx] > 0)
            {
                StartReload();
            }

            var w = WeaponTable.All[_weaponIdx];
            bool want = w.Auto && Mouse.current != null ? Mouse.current.leftButton.isPressed : _fireQueued;
            _fireQueued = false;
            if (want && _fireCd <= 0f && _reloadT <= 0f)
            {
                if (_mag[_weaponIdx] > 0) FireWeapon();
                else StartReload();
            }
        }

        void LateUpdate()
        {
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) _fireQueued = true;
                if (Mouse.current.rightButton.wasPressedThisFrame) DoMelee();
            }
        }

        void DoMelee()
        {
            _melee = 0.25f; // extended slightly for animation to play
            AddShake(2f);
            SfxManager.Instance?.Play("melee");
            // Fallback if no animator
            if (_anim == null || _anim.runtimeAnimatorController == null) OnMeleeHit(); 
        }

        // ANIMATION EVENT
        public void OnMeleeHit()
        {
            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, 64f, WolfLayers.EnemyMask))
            {
                var e = hit.GetComponent<EnemyController>();
                if (e == null) continue;
                var toE = (Vector2)e.transform.position - (Vector2)transform.position;
                if (Vector2.Dot(toE.normalized, AimDir) > 0.3f)
                {
                    e.TakeDamage(34f * DamageMult, toE, Feel.PauseTier.Medium, Feel.StunLevel.High);
                    SimpleVfx.Spawn(transform.parent, e.transform.position, "spark", new Color(1f, 0.95f, 0.7f));
                }
            }
        }

        void ActivateVengeance()
        {
            _furyActive = true;
            _furyTime = 5f;
            AddShake(7f);
            SfxManager.Instance?.Play("vengeance");
            VengeanceShockwave();
        }

        void VengeanceShockwave()
        {
            SimpleVfx.Spawn(transform.parent, transform.position, "burst", new Color(1f, 0.4f, 0.3f));
            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, 220f, WolfLayers.EnemyMask))
            {
                var e = hit.GetComponent<EnemyController>();
                if (e == null) continue;
                var toE = (Vector2)e.transform.position - (Vector2)transform.position;
                e.TakeDamage(40f * DamageMult, toE, Feel.PauseTier.Heavy, Feel.StunLevel.Ultimate);
            }
        }

        void FireWeapon()
        {
            var w = WeaponTable.All[_weaponIdx];
            _mag[_weaponIdx]--;
            _fireCd = w.Rate;
            for (int p = 0; p < w.Pellets; p++)
            {
                float ang = Mathf.Atan2(AimDir.y, AimDir.x) + Random.Range(-w.Spread, w.Spread);
                var d = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                var go = new GameObject("Bullet");
                go.transform.SetParent(transform.parent);
                var bullet = go.AddComponent<Bullet>();
                bullet.Init(d, w.Speed, w.Damage, this, w.PauseTier, w.StunLevel);
                go.transform.position = transform.position + (Vector3)(d * 34f);
            }
            SimpleVfx.Spawn(transform.parent, transform.position + (Vector3)(AimDir * 34f), "muzzle", _furyActive ? new Color(1f, 0.4f, 0.3f) : new Color(1f, 0.9f, 0.5f));
            AddShake(w.Name == "SHOTGUN" ? 2.5f : 0.8f);
            SfxManager.Instance?.Play(new[] { "gun_pistol", "gun_rifle", "gun_smg", "gun_shotgun" }[_weaponIdx]);
            if (_mag[_weaponIdx] <= 0) StartReload();
        }

        void StartReload()
        {
            if (_reloadT > 0f || _reserve[_weaponIdx] <= 0) return;
            _reloadT = WeaponTable.All[_weaponIdx].Reload;
            SfxManager.Instance?.Play("reload");
        }

        void FinishReload()
        {
            var w = WeaponTable.All[_weaponIdx];
            int need = w.Mag - _mag[_weaponIdx];
            int take = Mathf.Min(need, _reserve[_weaponIdx]);
            _mag[_weaponIdx] += take;
            _reserve[_weaponIdx] -= take;
        }

        public string WeaponText()
        {
            var w = WeaponTable.All[_weaponIdx];
            if (_reloadT > 0f) return $"{w.Name}  RELOADING…";
            return $"{w.Name}  {_mag[_weaponIdx]} / {_reserve[_weaponIdx]}";
        }
    }
}
