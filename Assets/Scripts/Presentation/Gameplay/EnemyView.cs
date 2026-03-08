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
        private float _destroyDelay = 1.2f;

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
        private CircleCollider2D _bodyCollider;
        private Rigidbody2D _rigidbody2D;
        private Vector2 _knockbackVelocity;
        private float _knockbackTimeRemaining;

        public static void SetHpBarVisible(bool visible)
        {
            s_showHpBarInDev = visible;
        }

        public float ContactDamage => _data.ContactDamage;

        public int ScoreValue => _scoreValue;

        public float MoveSpeed => _data.MoveSpeed;

        public void Initialize(EnemyData data, Transform target)
        {
            _data = data;
            _hp = data.MaxHp;
            _maxHp = Mathf.Max(1f, data.MaxHp);
            _target = target;
            _scoreValue = data.ScoreValue;
            _isDead = false;
            gameObject.SetActive(true);
            EnsureVisibleSprite();
            EnsureRigidbody();
            EnsureBodyCollider();
            EnsureHpBar();
            RefreshHpBar();
        }

        public void ApplyDamage(float value)
        {
            if (_isDead)
            {
                return;
            }

            _hp -= Mathf.Max(0f, value);
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

        private void FixedUpdate()
        {
            if (_isDead || _target == null)
            {
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

            _rigidbody2D.linearVelocity = toTarget.normalized * _data.MoveSpeed;
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
            EnemyDied?.Invoke(this);
            Destroy(gameObject, _destroyDelay);
        }

        public bool IsDead => _isDead;

        private void EnsureVisibleSprite()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = RuntimeSpriteLibrary.GetDiamond();
            spriteRenderer.color = new Color(1f, 0.18f, 0.15f, 1f);
            spriteRenderer.sortingOrder = 90;
            transform.localScale = new Vector3(0.74f, 0.74f, 1f);
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
            var popupObject = new GameObject("DamagePopup");
            popupObject.transform.position = transform.position + new Vector3(0f, 0.56f, 0f);
            var popup = popupObject.AddComponent<DamagePopupView>();
            popup.Initialize($"-{Mathf.RoundToInt(damage)}", new Color(1f, 0.93f, 0.3f, 1f));
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
    }
}
