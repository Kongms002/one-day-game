using OneDayGame.Application;
using OneDayGame.Domain.Input;
using OneDayGame.Domain.Policies;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerView : MonoBehaviour
    {
        private const float WeaponVisualScale = 0.44f;
        private const float WeaponColliderRadius = 0.78f;
        private const float WeaponKnockbackForce = 2.4f;
        private const float BodyKnockbackForce = 1.1f;

        [SerializeField]
        private Rigidbody2D _rigidbody2D;

        [SerializeField]
        private LayerMask _enemyLayer = -1;

        [SerializeField]
        private LayerMask _medKitLayer = -1;

        private IInputPort _inputPort;
        private RunSessionService _runSession;
        private IWeaponPolicy _weaponPolicy;
        private Transform _weaponVisual;
        private SpriteRenderer _weaponRenderer;
        private CircleCollider2D _weaponHitCollider;
        private CircleCollider2D _bodyHitCollider;
        private readonly Collider2D[] _weaponHitBuffer = new Collider2D[24];
        private readonly Collider2D[] _bodyHitBuffer = new Collider2D[24];

        private float _attackCooldown;
        private Vector2 _playerBoundsMin;
        private Vector2 _playerBoundsMax;
        private float _contactDamageElapsed;
        private float _moveSpeed;
        private float _touchDamageInterval;
        private float _weaponOrbitAngle;
        private float _weaponPulseElapsed;
        private float _damageMultiplier = 1f;
        private float _attackSpeedMultiplier = 1f;

        private void Awake()
        {
            if (_rigidbody2D == null)
            {
                _rigidbody2D = GetComponent<Rigidbody2D>();
            }

            EnsureVisibleSprite();
            EnsureWeaponVisual();
            EnsureBodyHitCollider();
        }

        public void Initialize(
            IInputPort inputPort,
            RunSessionService runSession,
            IWeaponPolicy weaponPolicy,
            float moveSpeed,
            float touchDamageInterval)
        {
            _inputPort = inputPort;
            _runSession = runSession;
            _weaponPolicy = weaponPolicy;
            _contactDamageElapsed = 0f;
            _attackCooldown = 0f;
            _moveSpeed = Mathf.Max(0.1f, moveSpeed);
            _touchDamageInterval = Mathf.Max(0.05f, touchDamageInterval);
            _playerBoundsMin = Vector2.zero;
            _playerBoundsMax = Vector2.zero;
            _weaponOrbitAngle = 0f;
            _weaponPulseElapsed = 0f;
            _damageMultiplier = 1f;
            _attackSpeedMultiplier = 1f;
            EnsureWeaponVisual();
            EnsureBodyHitCollider();
        }

        public void ApplyDamageMultiplier(float multiplier)
        {
            _damageMultiplier = Mathf.Clamp(_damageMultiplier * Mathf.Max(1f, multiplier), 1f, 50f);
        }

        public void ApplyAttackSpeedMultiplier(float multiplier)
        {
            _attackSpeedMultiplier = Mathf.Clamp(_attackSpeedMultiplier * Mathf.Max(1f, multiplier), 1f, 10f);
        }

        public void SetAreaBounds(float minX, float maxX, float minY, float maxY)
        {
            _playerBoundsMin = new Vector2(minX, minY);
            _playerBoundsMax = new Vector2(maxX, maxY);
        }

        public void ResetPosition(Vector3 position)
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.position = position;
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
            else
            {
                transform.position = position;
            }
        }

        private void Update()
        {
            if (_runSession == null || _inputPort == null || _weaponPolicy == null)
            {
                return;
            }

            if (_runSession.IsDead)
            {
                return;
            }

            var move = _inputPort.MoveAxis;
            var target = new Vector2(move.X, move.Y);

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = target * _moveSpeed;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, transform.position + new Vector3(target.x, target.y, 0f), _moveSpeed * Time.deltaTime);
            }

            ClampPosition();
            UpdateWeaponVisual(Time.deltaTime);

            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown <= 0f)
            {
                ExecuteAttack();
                _attackCooldown = _weaponPolicy.GetPlayerAttackCooldown(_runSession.Stage) / _attackSpeedMultiplier;
            }

            if (_contactDamageElapsed > 0f)
            {
                _contactDamageElapsed -= Time.deltaTime;
                return;
            }

            if (_bodyHitCollider == null)
            {
                return;
            }

            var filter = CreateEnemyContactFilter();
            int overlapCount = _bodyHitCollider.Overlap(filter, _bodyHitBuffer);
            float nearestSqr = float.MaxValue;
            EnemyView nearestEnemy = null;
            for (int i = 0; i < overlapCount; i++)
            {
                var collider = _bodyHitBuffer[i];
                var enemy = collider != null ? collider.GetComponent<EnemyView>() : null;
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                float sqrDistance = (enemy.transform.position - transform.position).sqrMagnitude;
                if (nearestEnemy == null || sqrDistance < nearestSqr)
                {
                    nearestEnemy = enemy;
                    nearestSqr = sqrDistance;
                }
            }

            if (nearestEnemy != null)
            {
                _runSession.ApplyDamage(Mathf.Max(0.1f, nearestEnemy.ContactDamage));
                nearestEnemy.ApplyKnockback(transform.position, BodyKnockbackForce);
                _contactDamageElapsed = _touchDamageInterval;
            }
        }

        private void ExecuteAttack()
        {
            var stage = _runSession.Stage;
            float damage = _weaponPolicy.GetPlayerAttackDamage(stage) * _damageMultiplier;
            _weaponPulseElapsed = 0.09f;

            if (_weaponHitCollider == null)
            {
                return;
            }

            var filter = CreateEnemyContactFilter();
            int hitCount = _weaponHitCollider.Overlap(filter, _weaponHitBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                var enemy = _weaponHitBuffer[i] != null ? _weaponHitBuffer[i].GetComponent<EnemyView>() : null;
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                enemy.ApplyDamage(damage);
                enemy.ApplyKnockback(transform.position, WeaponKnockbackForce);
            }
        }

        public void ApplyUltimate(float radius, float multiplier)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyView>();
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                enemy.ApplyDamage(999f * multiplier);
            }
        }

        private void ClampPosition()
        {
            var pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, _playerBoundsMin.x, _playerBoundsMax.x);
            pos.y = Mathf.Clamp(pos.y, _playerBoundsMin.y, _playerBoundsMax.y);

            if (_rigidbody2D != null)
            {
                _rigidbody2D.position = pos;
            }
            else
            {
                transform.position = pos;
            }
        }

        private void UpdateWeaponVisual(float deltaTime)
        {
            if (_weaponVisual == null || _weaponPolicy == null || _runSession == null)
            {
                return;
            }

            _weaponOrbitAngle += deltaTime * 220f;
            float attackRange = _weaponPolicy.GetPlayerAttackRange(_runSession.Stage);
            float radius = Mathf.Clamp(attackRange * 0.98f, 0.72f, 2.5f);
            float angleRad = _weaponOrbitAngle * Mathf.Deg2Rad;
            var offset = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f) * radius;
            _weaponVisual.localPosition = offset;
            _weaponVisual.localRotation = Quaternion.Euler(0f, 0f, _weaponOrbitAngle * -1.4f);

            if (_weaponHitCollider != null)
            {
                _weaponHitCollider.radius = Mathf.Max(0.15f, attackRange * 0.72f);
            }

            float pulseScale = 1f;
            if (_weaponPulseElapsed > 0f)
            {
                _weaponPulseElapsed = Mathf.Max(0f, _weaponPulseElapsed - deltaTime);
                pulseScale = 1.28f;
            }

            _weaponVisual.localScale = new Vector3(WeaponVisualScale * pulseScale, WeaponVisualScale * pulseScale, 1f);
        }

        private void EnsureWeaponVisual()
        {
            if (_weaponVisual == null)
            {
                var go = transform.Find("WeaponVisual");
                if (go == null)
                {
                    var weapon = new GameObject("WeaponVisual");
                    weapon.transform.SetParent(transform, false);
                    _weaponVisual = weapon.transform;
                }
                else
                {
                    _weaponVisual = go;
                }
            }

            if (_weaponVisual != null && _weaponRenderer == null)
            {
                _weaponRenderer = _weaponVisual.GetComponent<SpriteRenderer>();
                if (_weaponRenderer == null)
                {
                    _weaponRenderer = _weaponVisual.gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (_weaponVisual != null && _weaponHitCollider == null)
            {
                _weaponHitCollider = _weaponVisual.GetComponent<CircleCollider2D>();
                if (_weaponHitCollider == null)
                {
                    _weaponHitCollider = _weaponVisual.gameObject.AddComponent<CircleCollider2D>();
                }

                _weaponHitCollider.isTrigger = true;
                _weaponHitCollider.radius = WeaponColliderRadius;
            }

            if (_weaponRenderer != null)
            {
                _weaponRenderer.sprite = RuntimeSpriteLibrary.GetDiamond();
                _weaponRenderer.sortingOrder = 130;
                _weaponRenderer.color = new Color(1f, 0.9f, 0.25f, 1f);
            }
        }

        private void EnsureVisibleSprite()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = RuntimeSpriteLibrary.GetCircle();
            spriteRenderer.color = new Color(0.15f, 0.95f, 1f, 1f);
            spriteRenderer.sortingOrder = 120;
            transform.localScale = new Vector3(0.62f, 0.62f, 1f);
        }

        private void EnsureBodyHitCollider()
        {
            if (_bodyHitCollider == null)
            {
                _bodyHitCollider = GetComponent<CircleCollider2D>();
                if (_bodyHitCollider == null)
                {
                    _bodyHitCollider = gameObject.AddComponent<CircleCollider2D>();
                }
            }

            _bodyHitCollider.isTrigger = true;
            _bodyHitCollider.radius = 0.22f;
        }

        private ContactFilter2D CreateEnemyContactFilter()
        {
            int enemyMask = _enemyLayer.value;
            if (enemyMask == 0)
            {
                enemyMask = Physics2D.AllLayers;
            }

            var filter = new ContactFilter2D();
            filter.SetLayerMask(enemyMask);
            filter.useLayerMask = true;
            filter.useTriggers = true;
            return filter;
        }
    }
}
