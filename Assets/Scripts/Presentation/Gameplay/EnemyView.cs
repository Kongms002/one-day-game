using System;
using OneDayGame.Domain.Gameplay;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
        public sealed class EnemyView : MonoBehaviour
    {
        private static bool s_showHpBarInDev = true;
        private const float MinKnockback = 0.5f;
        private const float MaxKnockback = 8f;
        private const float KnockbackDuration = 0.18f;

        public event Action<EnemyView> EnemyDied;

        [SerializeField]
        private bool _enableHpBar = true;

        [SerializeField]
        private Vector2 _hpBarOffset = new Vector2(0f, 0.68f);

        [SerializeField]
        private Vector2 _hpBarSize = new Vector2(0.78f, 0.11f);

        private Transform _target;
        private EnemyData _data;
        private float _hp;
        private int _scoreValue;
        private bool _isDead;
        private Transform _hpBarRoot;
        private Transform _hpBarFill;
        private float _maxHp;
        private EnemyArchetype _archetype;
        private CircleCollider2D _bodyCollider;
        private Rigidbody2D _rigidbody2D;
        private Vector2 _knockbackVelocity;
        private float _knockbackTimeRemaining;
        private SpriteRenderer _spriteRenderer;
        private Sprite _animFrameA;
        private Sprite _animFrameB;
        private Sprite _hitFrame;
        private Sprite _deathFrame;
        private float _animInterval;
        private float _animElapsed;
        private float _hitVisualDuration = 0.08f;
        private float _hitVisualElapsed;
        private float _deathVisualDuration = 0.35f;
        private float _deathVisualElapsed;
        private bool _isAnimating;
        private bool _animFrameToggle;
        private bool _deathNotified;
        private bool _destroyOnDeath = true;
        private SpriteRenderer _fallbackRenderer;
        private TextMesh _fallbackText;
        private float _stunRemaining;
        private float _slowRemaining;
        private float _slowMultiplier = 1f;
        private float _poisonRemaining;
        private float _poisonPerSecond;
        private float _selfDestructRange = 0.55f;

        public static void SetHpBarVisible(bool visible)
        {
            s_showHpBarInDev = visible;
        }

        public float ContactDamage => _data.ContactDamage;

        public int ScoreValue => _scoreValue;

        public float MoveSpeed => _data.MoveSpeed;

        public EnemyArchetype Archetype => _archetype;

        public EnemyData Data => _data;

        public void Initialize(EnemyData data, Transform target)
        {
            _data = data;
            _hp = data.MaxHp;
            _maxHp = Mathf.Max(1f, data.MaxHp);
            _target = target;
            _scoreValue = data.ScoreValue;
            _archetype = data.Archetype;
            _isDead = false;
            _isAnimating = false;
            _animElapsed = 0f;
            _animFrameToggle = false;
            _hitVisualElapsed = 0f;
            _deathVisualElapsed = 0f;
            _deathNotified = false;
            ClearFallbackVisual();
            gameObject.SetActive(true);
            EnsureVisibleSprite();
            EnsureRigidbody();
            EnsureBodyCollider();
            if (_bodyCollider != null)
            {
                _bodyCollider.enabled = true;
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.simulated = true;
                _rigidbody2D.linearVelocity = Vector2.zero;
            }

            _knockbackVelocity = Vector2.zero;
            _knockbackTimeRemaining = 0f;
            _stunRemaining = 0f;
            _slowRemaining = 0f;
            _slowMultiplier = 1f;
            _poisonRemaining = 0f;
            _poisonPerSecond = 0f;
            EnsureHpBar();
            RefreshHpBar();
        }

        public void ConfigureStateAnimation(Sprite moveFrameA, Sprite moveFrameB, Sprite hitFrame, Sprite deathFrame, float moveInterval, float hitDuration, float deathDuration)
        {
            if (moveFrameA == null || moveFrameB == null)
            {
                _isAnimating = false;
                return;
            }

            _animFrameA = moveFrameA;
            _animFrameB = moveFrameB;
            _hitFrame = hitFrame;
            _deathFrame = deathFrame;
            _animInterval = Mathf.Max(0.04f, moveInterval);
            _hitVisualDuration = Mathf.Max(0.03f, hitDuration);
            _deathVisualDuration = Mathf.Max(0.12f, deathDuration);
            _animElapsed = 0f;
            _hitVisualElapsed = 0f;
            _deathVisualElapsed = 0f;
            _animFrameToggle = false;
            _isAnimating = true;
            ClearFallbackVisual();

            EnsureVisibleSprite();
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = _animFrameA;
                _spriteRenderer.color = Color.white;
                _spriteRenderer.sortingOrder = 90;
            }
        }

        public void EnableSlimeAnimation(Sprite frameA, Sprite frameB, float interval)
        {
            ConfigureStateAnimation(frameA, frameB, null, null, interval, 0.08f, 0.35f);
        }

        public void SetDestroyOnDeath(bool destroyOnDeath)
        {
            _destroyOnDeath = destroyOnDeath;
        }

        public void ConfigureFallbackVisual(int enemySerial)
        {
            EnsureFallbackVisualObjects();

            if (_fallbackRenderer != null)
            {
                _fallbackRenderer.enabled = true;
                _fallbackRenderer.sprite = RuntimeSpriteLibrary.GetSquare();
                _fallbackRenderer.color = new Color(0.92f, 0.2f, 0.22f, 0.9f);
                _fallbackRenderer.sortingOrder = 90;
            }

            if (_fallbackText != null)
            {
                _fallbackText.gameObject.SetActive(true);
                _fallbackText.text = enemySerial.ToString();
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = false;
            }

            _isAnimating = false;
            _hitFrame = null;
            _deathFrame = null;
            transform.localScale = new Vector3(0.72f, 0.72f, 1f);
        }

        public void ClearFallbackVisual()
        {
            if (_fallbackRenderer != null)
            {
                _fallbackRenderer.enabled = false;
            }

            if (_fallbackText != null)
            {
                _fallbackText.gameObject.SetActive(false);
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = true;
            }
        }

        private void Update()
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            if (_isDead)
            {
                UpdateDeathVisual();
                return;
            }

            if (_poisonRemaining > 0f)
            {
                float tick = Mathf.Min(_poisonRemaining, Time.deltaTime);
                _poisonRemaining = Mathf.Max(0f, _poisonRemaining - Time.deltaTime);
                if (tick > 0f && _poisonPerSecond > 0f)
                {
                    ApplyDamage(_poisonPerSecond * tick);
                }
            }

            if (_stunRemaining > 0f)
            {
                _stunRemaining = Mathf.Max(0f, _stunRemaining - Time.deltaTime);
            }

            if (_slowRemaining > 0f)
            {
                _slowRemaining = Mathf.Max(0f, _slowRemaining - Time.deltaTime);
                if (_slowRemaining <= 0f)
                {
                    _slowMultiplier = 1f;
                }
            }

            if (_hitVisualElapsed > 0f)
            {
                _hitVisualElapsed = Mathf.Max(0f, _hitVisualElapsed - Time.deltaTime);
                if (_hitFrame != null)
                {
                    _spriteRenderer.sprite = _hitFrame;
                }

                _spriteRenderer.color = new Color(1f, 1f, 1f, 0.7f);
                return;
            }

            _spriteRenderer.color = Color.white;

            if (!_isAnimating)
            {
                return;
            }

            _animElapsed += Time.deltaTime;
            if (_animElapsed < _animInterval)
            {
                return;
            }

            _animElapsed = 0f;
            _animFrameToggle = !_animFrameToggle;
            _spriteRenderer.sprite = _animFrameToggle ? _animFrameB : _animFrameA;
        }

        public void ApplyDamage(float value)
        {
            if (_isDead)
            {
                return;
            }

            _hp -= Mathf.Max(0f, value);
            _hitVisualElapsed = _hitVisualDuration;
            SpawnDamagePopup(Mathf.Max(0f, value));
            RefreshHpBar();
            if (_hp <= 0f)
            {
                Die();
            }
        }

        public void ApplyKnockback(Vector3 sourcePosition, float force)
        {
            if (_isDead || _rigidbody2D == null || !isActiveAndEnabled)
            {
                return;
            }

            Vector2 direction = (Vector2) transform.position - (Vector2) sourcePosition;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.up;
            }

            float clampedForce = Mathf.Clamp(force, MinKnockback, MaxKnockback);
            _knockbackVelocity = direction.normalized * clampedForce;
            _knockbackTimeRemaining = KnockbackDuration;
        }

        public void ApplySlow(float duration, float speedMultiplier)
        {
            if (_isDead)
            {
                return;
            }

            _slowRemaining = Mathf.Max(_slowRemaining, Mathf.Max(0.05f, duration));
            _slowMultiplier = Mathf.Clamp(speedMultiplier, 0.2f, 1f);
        }

        public void ApplyStun(float duration)
        {
            if (_isDead)
            {
                return;
            }

            _stunRemaining = Mathf.Max(_stunRemaining, Mathf.Max(0.05f, duration));
        }

        public void ApplyPoison(float damagePerSecond, float duration)
        {
            if (_isDead)
            {
                return;
            }

            _poisonPerSecond = Mathf.Max(_poisonPerSecond, Mathf.Max(0f, damagePerSecond));
            _poisonRemaining = Mathf.Max(_poisonRemaining, Mathf.Max(0.05f, duration));
        }

        private void FixedUpdate()
        {
            if (_isDead || _target == null)
            {
                return;
            }

            if (_stunRemaining > 0f)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                return;
            }

            if (_knockbackTimeRemaining > 0f)
            {
                var decay = _knockbackTimeRemaining / KnockbackDuration;
                _rigidbody2D.linearVelocity = Vector2.Lerp(_knockbackVelocity, Vector2.zero, 1f - Mathf.Clamp01(decay));
                _knockbackTimeRemaining = Mathf.Max(0f, _knockbackTimeRemaining - Time.fixedDeltaTime);
                return;
            }

            var targetPos = new Vector2(_target.position.x, _target.position.y);
            var toTarget = targetPos - _rigidbody2D.position;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                return;
            }

            float speed = _data.MoveSpeed;
            if (_archetype == EnemyArchetype.Swift)
            {
                speed *= 1.2f;
            }
            else if (_archetype == EnemyArchetype.Berserker)
            {
                float hpRatio = Mathf.Clamp01(_hp / Mathf.Max(1f, _maxHp));
                speed *= Mathf.Lerp(1.05f, 1.55f, 1f - hpRatio);
            }
            else if (_archetype == EnemyArchetype.SelfDestruct)
            {
                speed *= 1.25f;
                if (toTarget.sqrMagnitude <= _selfDestructRange * _selfDestructRange)
                {
                    _hp = 0f;
                    Die();
                    return;
                }
            }
            else if (_archetype == EnemyArchetype.Swarm)
            {
                speed *= 1.18f;
            }

            speed *= _slowMultiplier;

            _rigidbody2D.linearVelocity = toTarget.normalized * speed;
        }

        private void Die()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            if (_hpBarRoot != null)
            {
                _hpBarRoot.gameObject.SetActive(false);
            }

            if (_bodyCollider != null)
            {
                _bodyCollider.enabled = false;
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                _rigidbody2D.simulated = false;
            }

            if (_deathFrame != null && _spriteRenderer != null)
            {
                _spriteRenderer.sprite = _deathFrame;
            }

            _deathVisualElapsed = _deathVisualDuration;
        }

        public bool IsDead => _isDead;

        private void EnsureVisibleSprite()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
                if (_spriteRenderer == null)
                {
                    _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (_isAnimating && _animFrameA != null)
            {
                _spriteRenderer.sprite = _animFrameToggle ? _animFrameB : _animFrameA;
                _spriteRenderer.color = Color.white;
                _spriteRenderer.sortingOrder = 90;
                transform.localScale = new Vector3(1.12f, 1.12f, 1f);
                return;
            }

            _spriteRenderer.sprite = RuntimeSpriteLibrary.GetDiamond();
            switch (_archetype)
            {
                case EnemyArchetype.Tank:
                    _spriteRenderer.color = new Color(0.82f, 0.42f, 0.16f, 1f);
                    transform.localScale = new Vector3(0.92f, 0.92f, 1f);
                    break;
                case EnemyArchetype.Swift:
                    _spriteRenderer.color = new Color(1f, 0.74f, 0.18f, 1f);
                    transform.localScale = new Vector3(0.62f, 0.62f, 1f);
                    break;
                case EnemyArchetype.Berserker:
                    _spriteRenderer.color = new Color(0.98f, 0.08f, 0.32f, 1f);
                    transform.localScale = new Vector3(0.78f, 0.78f, 1f);
                    break;
                case EnemyArchetype.SelfDestruct:
                    _spriteRenderer.color = new Color(1f, 0.38f, 0.14f, 1f);
                    transform.localScale = new Vector3(0.68f, 0.68f, 1f);
                    break;
                case EnemyArchetype.Multiply:
                    _spriteRenderer.color = new Color(0.76f, 0.34f, 1f, 1f);
                    transform.localScale = new Vector3(0.7f, 0.7f, 1f);
                    break;
                case EnemyArchetype.Swarm:
                    _spriteRenderer.color = new Color(0.25f, 1f, 0.88f, 1f);
                    transform.localScale = new Vector3(0.58f, 0.58f, 1f);
                    break;
                default:
                    _spriteRenderer.color = new Color(1f, 0.18f, 0.15f, 1f);
                    transform.localScale = new Vector3(0.74f, 0.74f, 1f);
                    break;
            }
            _spriteRenderer.sortingOrder = 90;
        }

        private void UpdateDeathVisual()
        {
            if (_deathVisualElapsed > 0f)
            {
                _deathVisualElapsed = Mathf.Max(0f, _deathVisualElapsed - Time.deltaTime);
                if (_spriteRenderer != null)
                {
                    float flicker = Mathf.Abs(Mathf.Sin(Time.time * 28f));
                    _spriteRenderer.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.2f, 1f, flicker));
                }

                return;
            }

            if (_deathNotified)
            {
                return;
            }

            _deathNotified = true;
            EnemyDied?.Invoke(this);
            if (_destroyOnDeath)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void EnsureHpBar()
        {
            if (_hpBarRoot == null)
            {
                var existing = transform.Find("HpBarRoot");
                if (existing == null)
                {
                    var root = new GameObject("HpBarRoot");
                    root.transform.SetParent(transform, false);
                    _hpBarRoot = root.transform;
                }
                else
                {
                    _hpBarRoot = existing;
                }
            }

            if (_hpBarRoot == null)
            {
                return;
            }

            _hpBarRoot.localPosition = new Vector3(_hpBarOffset.x, _hpBarOffset.y, 0f);

            var bg = _hpBarRoot.Find("Bg");
            if (bg == null)
            {
                var go = new GameObject("Bg");
                go.transform.SetParent(_hpBarRoot, false);
                bg = go.transform;
            }

            var bgRenderer = bg.GetComponent<SpriteRenderer>();
            if (bgRenderer == null)
            {
                bgRenderer = bg.gameObject.AddComponent<SpriteRenderer>();
            }

            bgRenderer.sprite = RuntimeSpriteLibrary.GetSquare();
            bgRenderer.color = new Color(0f, 0f, 0f, 0.75f);
            bgRenderer.sortingOrder = 98;
            bg.localScale = new Vector3(_hpBarSize.x, _hpBarSize.y, 1f);

            if (_hpBarFill == null)
            {
                var fill = _hpBarRoot.Find("Fill");
                if (fill == null)
                {
                    var go = new GameObject("Fill");
                    go.transform.SetParent(_hpBarRoot, false);
                    fill = go.transform;
                }

                _hpBarFill = fill;
            }

            var fillRenderer = _hpBarFill.GetComponent<SpriteRenderer>();
            if (fillRenderer == null)
            {
                fillRenderer = _hpBarFill.gameObject.AddComponent<SpriteRenderer>();
            }

            fillRenderer.sprite = RuntimeSpriteLibrary.GetSquare();
            fillRenderer.color = new Color(0.35f, 1f, 0.34f, 1f);
            fillRenderer.sortingOrder = 99;

            bool visible = _enableHpBar && s_showHpBarInDev;
            _hpBarRoot.gameObject.SetActive(visible);
        }

        private void EnsureBodyCollider()
        {
            if (_bodyCollider == null)
            {
                _bodyCollider = GetComponent<CircleCollider2D>();
                if (_bodyCollider == null)
                {
                    _bodyCollider = gameObject.AddComponent<CircleCollider2D>();
                }
            }

            _bodyCollider.isTrigger = true;
            _bodyCollider.radius = Mathf.Max(0.12f, _data.ContactRadius);
        }

        private void EnsureRigidbody()
        {
            if (_rigidbody2D == null)
            {
                _rigidbody2D = GetComponent<Rigidbody2D>();
            }

            if (_rigidbody2D == null)
            {
                _rigidbody2D = gameObject.AddComponent<Rigidbody2D>();
            }

            _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.freezeRotation = true;
        }

        private void SpawnDamagePopup(float damage)
        {
            DamagePopupPool.Spawn(
                transform.position + new Vector3(0f, 0.56f, 0f),
                $"-{Mathf.RoundToInt(damage)}",
                new Color(1f, 0.93f, 0.3f, 1f));
        }

        private void RefreshHpBar()
        {
            if (_hpBarFill == null || _hpBarRoot == null)
            {
                return;
            }

            float ratio = Mathf.Clamp01(_hp / Mathf.Max(1f, _maxHp));
            _hpBarFill.localScale = new Vector3(_hpBarSize.x * ratio, _hpBarSize.y * 0.78f, 1f);
            _hpBarFill.localPosition = new Vector3((-_hpBarSize.x * 0.5f) + (_hpBarSize.x * ratio * 0.5f), 0f, 0f);
        }

        private void EnsureFallbackVisualObjects()
        {
            if (_fallbackRenderer == null)
            {
                var fallback = transform.Find("FallbackShape");
                if (fallback == null)
                {
                    var fallbackGo = new GameObject("FallbackShape");
                    fallbackGo.transform.SetParent(transform, false);
                    fallback = fallbackGo.transform;
                }

                _fallbackRenderer = fallback.GetComponent<SpriteRenderer>();
                if (_fallbackRenderer == null)
                {
                    _fallbackRenderer = fallback.gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (_fallbackText == null)
            {
                var text = transform.Find("FallbackText");
                if (text == null)
                {
                    var textGo = new GameObject("FallbackText");
                    textGo.transform.SetParent(transform, false);
                    textGo.transform.localPosition = Vector3.zero;
                    text = textGo.transform;
                }

                _fallbackText = text.GetComponent<TextMesh>();
                if (_fallbackText == null)
                {
                    _fallbackText = text.gameObject.AddComponent<TextMesh>();
                }

                _fallbackText.fontSize = 48;
                _fallbackText.characterSize = 0.08f;
                _fallbackText.anchor = TextAnchor.MiddleCenter;
                _fallbackText.alignment = TextAlignment.Center;
                _fallbackText.color = Color.white;
            }
        }
    }
}
