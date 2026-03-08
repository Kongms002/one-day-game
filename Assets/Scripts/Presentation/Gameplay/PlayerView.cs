using System.Collections.Generic;
using System;
using OneDayGame.Application;
using OneDayGame.Domain.Gameplay;
using OneDayGame.Domain.Input;
using OneDayGame.Domain.Policies;
using OneDayGame.Domain.Weapons;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerView : MonoBehaviour
    {
        private const float WeaponVisualScale = 0.44f;
        private const float WeaponColliderRadius = 0.78f;

        [Header("Combat")]
        [SerializeField]
        private bool _enableEnemyKnockback = true;

        [SerializeField]
        [Min(0f)]
        private float _weaponKnockbackForce = 2.4f;

        [SerializeField]
        [Min(0f)]
        private float _bodyKnockbackForce = 1.1f;

        [SerializeField]
        private Rigidbody2D _rigidbody2D;

        [SerializeField]
        private LayerMask _enemyLayer = -1;

        [SerializeField]
        private LayerMask _medKitLayer = -1;

        [SerializeField]
        public Sprite[] _directionSprites; // 0: South, 1: SouthEast, 2: East, 3: NorthEast, 4: North, 5: NorthWest, 6: West, 7: SouthWest

        private SpriteRenderer _mainSpriteRenderer;

        private IInputPort _inputPort;
        private RunSessionService _runSession;
        private IWeaponPolicy _weaponPolicy;
        private IWeaponLoadoutReadModel _weaponLoadout;
        private Transform _weaponVisual;
        private InputAxis _cachedInputAxis;
        private SpriteRenderer _weaponRenderer;
        private CircleCollider2D _weaponHitCollider;
        private CircleCollider2D _bodyHitCollider;
        private readonly Collider2D[] _weaponHitBuffer = new Collider2D[24];
        private readonly Collider2D[] _bodyHitBuffer = new Collider2D[24];
        private readonly List<Transform> _loadoutWeaponVisuals = new List<Transform>();
        private Transform _weaponVisualsRoot;

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
        private float _expMagnetRemaining;
        private float _expMagnetRadius;
        private readonly WeaponOrchestrator _weaponOrchestrator = new WeaponOrchestrator();
        private Transform _magnetAura;
        private SpriteRenderer _magnetAuraRenderer;
        private Func<Vector2, bool> _walkableResolver;
        private float _lastInputAxisTimestamp = -999f;

        public bool IsExpMagnetActive => _expMagnetRemaining > 0f;

        public float ExpMagnetRadius => Mathf.Max(0f, _expMagnetRadius);

        private void Awake()
        {
            if (_rigidbody2D == null)
            {
                _rigidbody2D = GetComponent<Rigidbody2D>();
            }

            EnsureMovementBody();

            EnsureVisibleSprite();
            EnsureWeaponVisual();
            EnsureBodyHitCollider();
        }

        private void EnsureMovementBody()
        {
            if (_rigidbody2D == null)
            {
                return;
            }

            _rigidbody2D.simulated = true;
            _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
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
            _expMagnetRemaining = 0f;
            _expMagnetRadius = 0f;
            _cachedInputAxis = InputAxis.Zero;
            _lastInputAxisTimestamp = -999f;
            _weaponOrchestrator.Reset();
            EnsureWeaponVisual();
            EnsureBodyHitCollider();
            EnsureMagnetAura();
        }

        public void BindWeaponLoadout(IWeaponLoadoutReadModel weaponLoadout)
        {
            _weaponLoadout = weaponLoadout;
            _weaponOrchestrator.Reset();
            EnsureLoadoutWeaponVisuals();
        }

        public void SetInputAxis(InputAxis axis)
        {
            _cachedInputAxis = axis;
            _lastInputAxisTimestamp = Time.unscaledTime;
        }

        public void BindInputPort(IInputPort inputPort)
        {
            _inputPort = inputPort;
        }

        public void BindWalkableResolver(Func<Vector2, bool> walkableResolver)
        {
            _walkableResolver = walkableResolver;
        }

        public void ApplyDamageMultiplier(float multiplier)
        {
            _damageMultiplier = Mathf.Clamp(_damageMultiplier * Mathf.Max(1f, multiplier), 1f, 50f);
        }

        public void ApplyAttackSpeedMultiplier(float multiplier)
        {
            _attackSpeedMultiplier = Mathf.Clamp(_attackSpeedMultiplier * Mathf.Max(1f, multiplier), 1f, 10f);
        }

        public void ActivateExpMagnet(float duration, float radius)
        {
            _expMagnetRemaining = Mathf.Max(_expMagnetRemaining, Mathf.Max(0.1f, duration));
            _expMagnetRadius = Mathf.Max(_expMagnetRadius, Mathf.Max(1.5f, radius));
            EnsureMagnetAura();
            UpdateMagnetAura();
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
            if (_runSession == null)
            {
                return;
            }

            if (_runSession.IsDead)
            {
                return;
            }

            var target = ResolveMoveTarget();

            if (_expMagnetRemaining > 0f)
            {
                _expMagnetRemaining = Mathf.Max(0f, _expMagnetRemaining - Time.deltaTime);
            }

            UpdateMagnetAura();

            ApplyMovement(target);

            UpdateFacingSprite(target);

            ClampPosition();
            UpdateWeaponVisual(Time.deltaTime);

            if (_weaponLoadout != null && _weaponLoadout.Slots != null)
            {
                ExecuteLoadoutAttacks(Time.deltaTime);
            }
            else if (_weaponPolicy != null)
            {
                _attackCooldown -= Time.deltaTime;
                if (_attackCooldown <= 0f)
                {
                    ExecuteAttack();
                    _attackCooldown = _weaponPolicy.GetPlayerAttackCooldown(_runSession.Stage) / _attackSpeedMultiplier;
                }
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
                TryApplyEnemyKnockback(nearestEnemy, transform.position, _bodyKnockbackForce);
                if (nearestEnemy.Archetype == EnemyArchetype.SelfDestruct)
                {
                    _runSession.ApplyDamage(Mathf.Max(0.2f, nearestEnemy.ContactDamage * 0.65f));
                    nearestEnemy.ApplyDamage(9999f);
                }

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
                TryApplyEnemyKnockback(enemy, transform.position, _weaponKnockbackForce);
            }
        }

        private void UpdateFacingSprite(Vector2 target)
        {
            if (target.sqrMagnitude <= 0.01f || _mainSpriteRenderer == null || _directionSprites == null || _directionSprites.Length != 8)
            {
                return;
            }

            int dirIndex = GetDirectionIndex(target);
            if (_directionSprites[dirIndex] != null)
            {
                _mainSpriteRenderer.sprite = _directionSprites[dirIndex];
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
            Vector3 pos = _rigidbody2D != null
                ? _rigidbody2D.position
                : transform.position;
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

        private Vector2 ResolveMoveTarget()
        {
            float tickAge = Time.unscaledTime - _lastInputAxisTimestamp;
            if (tickAge <= 0.2f)
            {
                return ClampAndNormalize(_cachedInputAxis);
            }

            if (_inputPort != null)
            {
                var moveFromPort = _inputPort.MoveAxis;
                _cachedInputAxis = moveFromPort;
                return ClampAndNormalize(moveFromPort);
            }

            _cachedInputAxis = InputAxis.Zero;
            return Vector2.zero;
        }

        private void ApplyMovement(Vector2 target)
        {
            if (target.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var current = _rigidbody2D != null ? _rigidbody2D.position : (Vector2) transform.position;
            var proposed = ResolveProposedPosition(target, current);
            if ((proposed - current).sqrMagnitude < 0.000001f)
            {
                proposed = current + target * (_moveSpeed * Time.deltaTime);
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                _rigidbody2D.position = proposed;
                return;
            }

            transform.position = new Vector3(proposed.x, proposed.y, transform.position.z);
        }

        private static Vector2 ClampAndNormalize(InputAxis axis)
        {
            var move = new Vector2(axis.X, axis.Y);
            if (move.sqrMagnitude > 1f)
            {
                move = move.normalized;
            }

            return move;
        }

        private Vector2 ResolveProposedPosition(Vector2 target, Vector2 currentPosition)
        {
            var proposed = currentPosition + target * (_moveSpeed * Time.deltaTime);
            bool currentWalkable = IsWalkable(currentPosition);

            if (currentWalkable && !IsWalkable(proposed))
            {
                var xOnly = new Vector2(proposed.x, currentPosition.y);
                var yOnly = new Vector2(currentPosition.x, proposed.y);
                if (IsWalkable(xOnly))
                {
                    return xOnly;
                }

                if (IsWalkable(yOnly))
                {
                    return yOnly;
                }

                return currentPosition;
            }

            if (!currentWalkable)
            {
                return currentPosition + target * (_moveSpeed * Time.deltaTime);
            }

            return proposed;
        }

        private void UpdateWeaponVisual(float deltaTime)
        {
            if (_weaponVisual == null || _weaponPolicy == null || _runSession == null)
            {
                return;
            }

            _weaponOrbitAngle += deltaTime * 220f;
            bool useLoadout = _weaponLoadout != null && _weaponLoadout.Slots != null;

            if (useLoadout)
            {
                if (_weaponVisual != null)
                {
                    _weaponVisual.gameObject.SetActive(false);
                }

                if (_weaponHitCollider != null)
                {
                    _weaponHitCollider.enabled = false;
                }

                EnsureLoadoutWeaponVisuals();
                float orbitRadius = ResolveRotationOrbitRadius(_runSession.Stage);
                float loadoutPulseScale = 1f;
                if (_weaponPulseElapsed > 0f)
                {
                    _weaponPulseElapsed = Mathf.Max(0f, _weaponPulseElapsed - deltaTime);
                    loadoutPulseScale = 1.28f;
                }

                UpdateLoadoutWeaponVisuals(orbitRadius, loadoutPulseScale);
                return;
            }

            if (_weaponVisual != null)
            {
                _weaponVisual.gameObject.SetActive(true);
            }

            if (_weaponHitCollider != null)
            {
                _weaponHitCollider.enabled = true;
            }

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
                var selectedSlot = _weaponLoadout != null ? _weaponLoadout.SelectedSlot : null;
                var selectedIcon = selectedSlot != null && selectedSlot.Definition != null
                    ? WeaponSpriteLibrary.GetWeaponIcon(selectedSlot.Definition.Id)
                    : null;

                _weaponRenderer.sprite = selectedIcon ?? RuntimeSpriteLibrary.GetDiamond();
                _weaponRenderer.color = selectedIcon != null ? Color.white : new Color(1f, 0.9f, 0.25f, 1f);
                _weaponRenderer.sortingOrder = 130;
            }
        }

        private void EnsureVisibleSprite()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (_directionSprites == null || _directionSprites.Length != 8 || _directionSprites[0] == null)
            {
                spriteRenderer.sprite = RuntimeSpriteLibrary.GetCircle();
                spriteRenderer.color = new Color(0.15f, 0.95f, 1f, 1f);
            }
            else
            {
                spriteRenderer.sprite = _directionSprites[0];
            }
            
            spriteRenderer.sortingOrder = 120;
            transform.localScale = new Vector3(0.62f, 0.62f, 1f);
            _mainSpriteRenderer = spriteRenderer;
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

        private int GetDirectionIndex(Vector2 dir)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            if (angle < 0)
                angle += 360f;

            if (angle >= 337.5f || angle < 22.5f) return 2; // East
            if (angle >= 22.5f && angle < 67.5f) return 3; // NorthEast
            if (angle >= 67.5f && angle < 112.5f) return 4; // North
            if (angle >= 112.5f && angle < 157.5f) return 5; // NorthWest
            if (angle >= 157.5f && angle < 202.5f) return 6; // West
            if (angle >= 202.5f && angle < 247.5f) return 7; // SouthWest
            if (angle >= 247.5f && angle < 292.5f) return 0; // South
            if (angle >= 292.5f && angle < 337.5f) return 1; // SouthEast

            return 0;
        }

        private void ExecuteLoadoutAttacks(float deltaTime)
        {
            var slots = _weaponLoadout.Slots;
            if (slots == null)
            {
                return;
            }

            int stage = _runSession.Stage;
            float synergyMultiplier = WeaponSynergyEngine.EvaluateDamageMultiplier(slots);
            _weaponOrchestrator.Tick(
                slots,
                stage,
                _attackSpeedMultiplier,
                deltaTime,
                (slot, stats) => ExecuteWeaponAttack(slot, stats, stage, synergyMultiplier));
        }

        private void ExecuteWeaponAttack(WeaponSlot slot, WeaponStats stats, int stage, float synergyMultiplier)
        {
            float damage = Mathf.Max(0.1f, stats.Damage * _damageMultiplier * Mathf.Max(0.2f, synergyMultiplier));
            float range = Mathf.Max(0.2f, stats.Range);
            int projectileCount = Mathf.Max(1, stats.ProjectileCount);
            _weaponPulseElapsed = 0.09f;

            int enemyMask = _enemyLayer.value == 0 ? Physics2D.AllLayers : _enemyLayer.value;
            var hits = Physics2D.OverlapCircleAll(transform.position, range, enemyMask);
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            if (slot.Definition.Type == WeaponType.Rotation)
            {
                int bladeCount = Mathf.Max(1, projectileCount);
                float orbitRadius = ResolveWeaponOrbitRadius(slot.Definition.Id, range);
                float preciseRadius = Mathf.Clamp(range * 0.26f, 0.16f, 0.46f);
                var hitSet = new HashSet<EnemyView>();
                for (int blade = 0; blade < bladeCount; blade++)
                {
                    float angle = _weaponOrbitAngle + (360f / bladeCount) * blade;
                    float rad = angle * Mathf.Deg2Rad;
                    var center = transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;
                    var preciseHits = Physics2D.OverlapCircleAll(center, preciseRadius, enemyMask);
                    if (preciseHits == null || preciseHits.Length == 0)
                    {
                        continue;
                    }

                    for (int i = 0; i < preciseHits.Length; i++)
                    {
                        var enemy = preciseHits[i] != null ? preciseHits[i].GetComponent<EnemyView>() : null;
                        if (enemy == null || enemy.IsDead || hitSet.Contains(enemy))
                        {
                            continue;
                        }

                        hitSet.Add(enemy);
                        enemy.ApplyDamage(damage);
                        TryApplyEnemyKnockback(enemy, center, _weaponKnockbackForce);
                        ApplyWeaponStatusEffects(slot, enemy, stats);
                    }
                }

                return;
            }

            if (slot.Definition.Type == WeaponType.Area)
            {
                EnemyView centerEnemy = null;
                float nearestSqr = float.MaxValue;
                for (int i = 0; i < hits.Length; i++)
                {
                    var enemy = hits[i] != null ? hits[i].GetComponent<EnemyView>() : null;
                    if (enemy == null || enemy.IsDead)
                    {
                        continue;
                    }

                    float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
                    if (sqr < nearestSqr)
                    {
                        centerEnemy = enemy;
                        nearestSqr = sqr;
                    }

                    enemy.ApplyDamage(damage);
                    TryApplyEnemyKnockback(enemy, transform.position, _weaponKnockbackForce);
                    ApplyWeaponStatusEffects(slot, enemy, stats);
                }

                if (centerEnemy != null)
                {
                    WeaponAreaEffectPool.Spawn(
                        centerEnemy.transform.position,
                        Mathf.Clamp(range * 0.8f, 0.5f, 2.6f),
                        new Color(0.45f, 0.95f, 0.55f, 0.6f),
                        WeaponSpriteLibrary.ResolveAreaVisualIcon(slot.Definition));
                }

                return;
            }

            for (int shot = 0; shot < projectileCount; shot++)
            {
                EnemyView nearest = null;
                float nearestSqr = float.MaxValue;
                for (int i = 0; i < hits.Length; i++)
                {
                    var enemy = hits[i] != null ? hits[i].GetComponent<EnemyView>() : null;
                    if (enemy == null || enemy.IsDead)
                    {
                        continue;
                    }

                    float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
                    if (sqr < nearestSqr)
                    {
                        nearest = enemy;
                        nearestSqr = sqr;
                    }
                }

                if (nearest == null)
                {
                    break;
                }

                SpawnProjectile(slot.Definition, nearest, damage, enemyMask);
                ApplyWeaponStatusEffects(slot, nearest, stats);
                hits = RemoveHitTarget(hits, nearest);
            }
        }

        private static void ApplyWeaponStatusEffects(WeaponSlot slot, EnemyView enemy, WeaponStats stats)
        {
            if (slot == null || enemy == null)
            {
                return;
            }

            switch (slot.Definition.Id)
            {
                case WeaponId.LaserPet:
                    enemy.ApplyPoison(Mathf.Max(1f, stats.Damage * 0.22f), 1.6f);
                    break;
                case WeaponId.RagePet:
                    enemy.ApplySlow(0.55f, 0.74f);
                    break;
                case WeaponId.StunPet:
                    enemy.ApplyStun(0.35f);
                    break;
                case WeaponId.PoisonCloud:
                    enemy.ApplyPoison(Mathf.Max(1f, stats.DotPerSecond), 2.1f);
                    break;
                case WeaponId.BlackHole:
                    enemy.ApplySlow(0.85f, 0.58f);
                    break;
            }
        }

        private void SpawnProjectile(WeaponDefinition definition, EnemyView target, float damage, int enemyMask)
        {
            if (target == null)
            {
                return;
            }
            float speed = definition.Type == WeaponType.Projectile ? 11f : 9f;
            float hitRadius = definition.Id == WeaponId.Boomerang ? 0.3f : 0.22f;
            Color tint = definition.Id == WeaponId.Boomerang
                ? new Color(1f, 0.64f, 0.24f, 1f)
                : new Color(1f, 0.94f, 0.3f, 1f);
            var sprite = WeaponSpriteLibrary.ResolveProjectileIcon(definition);
            WeaponProjectilePool.Spawn(
                transform.position,
                target,
                damage,
                speed,
                2.2f,
                hitRadius,
                ResolveProjectileKnockbackForce(target),
                enemyMask,
                tint,
                sprite);
        }

        private float ResolveProjectileKnockbackForce(EnemyView target)
        {
            if (!_enableEnemyKnockback)
            {
                return 0f;
            }

            if (target != null && target.Data.IsBoss)
            {
                return 0f;
            }

            return Mathf.Max(0f, _weaponKnockbackForce);
        }

        private void TryApplyEnemyKnockback(EnemyView enemy, Vector3 sourcePosition, float force)
        {
            if (!_enableEnemyKnockback || enemy == null || enemy.IsDead)
            {
                return;
            }

            if (enemy.Data.IsBoss)
            {
                return;
            }

            float safeForce = Mathf.Max(0f, force);
            if (safeForce <= 0f)
            {
                return;
            }

            enemy.ApplyKnockback(sourcePosition, safeForce);
        }

        private static Collider2D[] RemoveHitTarget(Collider2D[] hits, EnemyView target)
        {
            if (hits == null || target == null)
            {
                return hits;
            }

            for (int i = 0; i < hits.Length; i++)
            {
                var enemy = hits[i] != null ? hits[i].GetComponent<EnemyView>() : null;
                if (enemy == target)
                {
                    hits[i] = null;
                }
            }

            return hits;
        }

        private void EnsureLoadoutWeaponVisuals()
        {
            int activeCount = GetActiveRotationWeaponCount();
            if (activeCount <= 0)
            {
                for (int i = 0; i < _loadoutWeaponVisuals.Count; i++)
                {
                    if (_loadoutWeaponVisuals[i] != null)
                    {
                        _loadoutWeaponVisuals[i].gameObject.SetActive(false);
                    }
                }

                return;
            }

            if (_weaponVisualsRoot == null)
            {
                var existing = transform.Find("WeaponVisualsRoot");
                if (existing == null)
                {
                    var rootGo = new GameObject("WeaponVisualsRoot");
                    rootGo.transform.SetParent(transform, false);
                    _weaponVisualsRoot = rootGo.transform;
                }
                else
                {
                    _weaponVisualsRoot = existing;
                }
            }

            while (_loadoutWeaponVisuals.Count < activeCount)
            {
                var go = new GameObject($"LoadoutWeaponVisual_{_loadoutWeaponVisuals.Count}");
                go.transform.SetParent(_weaponVisualsRoot, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = RuntimeSpriteLibrary.GetDiamond();
                renderer.sortingOrder = 129;
                renderer.color = Color.white;
                _loadoutWeaponVisuals.Add(go.transform);
            }

            for (int i = 0; i < _loadoutWeaponVisuals.Count; i++)
            {
                if (_loadoutWeaponVisuals[i] != null)
                {
                    _loadoutWeaponVisuals[i].gameObject.SetActive(i < activeCount);
                }
            }
        }

        private void UpdateLoadoutWeaponVisuals(float orbitRadius, float pulseScale)
        {
            int activeCount = GetActiveRotationWeaponCount();
            if (activeCount <= 0)
            {
                return;
            }

            float safeRadius = Mathf.Clamp(orbitRadius * 0.9f, 0.58f, 2f);
            int visualIndex = 0;
            if (_weaponLoadout != null && _weaponLoadout.Slots != null)
            {
                for (int i = 0; i < _weaponLoadout.Slots.Count; i++)
                {
                    var slot = _weaponLoadout.Slots[i];
                    if (slot == null || slot.IsLocked || slot.IsEmpty || slot.Definition.Type != WeaponType.Rotation)
                    {
                        continue;
                    }

                    if (visualIndex >= _loadoutWeaponVisuals.Count)
                    {
                        break;
                    }

                    var visual = _loadoutWeaponVisuals[visualIndex];
                    if (visual == null)
                    {
                        visualIndex++;
                        continue;
                    }

                    float angle = _weaponOrbitAngle + (360f / activeCount) * visualIndex;
                    float rad = angle * Mathf.Deg2Rad;
                    visual.localPosition = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * safeRadius;
                    visual.localRotation = Quaternion.Euler(0f, 0f, -angle * 1.2f);
                    visual.localScale = new Vector3(WeaponVisualScale * pulseScale * 0.82f, WeaponVisualScale * pulseScale * 0.82f, 1f);

                    var renderer = visual.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        var icon = WeaponSpriteLibrary.GetWeaponIcon(slot.Definition.Id);
                        renderer.sprite = icon ?? RuntimeSpriteLibrary.GetDiamond();
                        renderer.color = icon == null ? GetWeaponTint(slot.Definition.Type) : Color.white;
                    }

                    visualIndex++;
                }
            }
        }

        private Vector3 ResolveWeaponVisualPosition(WeaponId weaponId)
        {
            if (_weaponLoadout == null || _weaponLoadout.Slots == null)
            {
                return transform.position;
            }

            int visualIndex = 0;
            for (int i = 0; i < _weaponLoadout.Slots.Count; i++)
            {
                var slot = _weaponLoadout.Slots[i];
                if (slot == null || slot.IsLocked || slot.IsEmpty || slot.Definition.Type != WeaponType.Rotation)
                {
                    continue;
                }

                if (slot.Definition.Id == weaponId)
                {
                    if (visualIndex < _loadoutWeaponVisuals.Count && _loadoutWeaponVisuals[visualIndex] != null)
                    {
                        return _loadoutWeaponVisuals[visualIndex].position;
                    }

                    return transform.position;
                }

                visualIndex++;
            }

            return transform.position;
        }

        private float ResolveWeaponOrbitRadius(WeaponId weaponId, float fallbackRange)
        {
            Vector3 pos = ResolveWeaponVisualPosition(weaponId);
            float distance = Vector3.Distance(transform.position, pos);
            if (distance > 0.05f)
            {
                return distance;
            }

            return Mathf.Clamp(fallbackRange * 0.85f, 0.55f, 2f);
        }

        private float ResolveRotationOrbitRadius(int stage)
        {
            if (_weaponLoadout == null || _weaponLoadout.Slots == null)
            {
                return Mathf.Clamp(_weaponPolicy.GetPlayerAttackRange(stage) * 0.95f, 0.62f, 2f);
            }

            for (int i = 0; i < _weaponLoadout.Slots.Count; i++)
            {
                var slot = _weaponLoadout.Slots[i];
                if (slot == null || slot.IsLocked || slot.IsEmpty || slot.Definition.Type != WeaponType.Rotation)
                {
                    continue;
                }

                var stats = slot.Definition.Evaluate(stage, slot.Level);
                return Mathf.Clamp(stats.Range * 0.85f, 0.62f, 2f);
            }

            return Mathf.Clamp(_weaponPolicy.GetPlayerAttackRange(stage) * 0.95f, 0.62f, 2f);
        }

        private int GetActiveRotationWeaponCount()
        {
            if (_weaponLoadout == null || _weaponLoadout.Slots == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _weaponLoadout.Slots.Count; i++)
            {
                var slot = _weaponLoadout.Slots[i];
                if (slot != null && !slot.IsLocked && !slot.IsEmpty && slot.Definition.Type == WeaponType.Rotation)
                {
                    count++;
                }
            }

            return count;
        }

        private static Color GetWeaponTint(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Projectile:
                    return new Color(1f, 0.78f, 0.2f, 0.95f);
                case WeaponType.Area:
                    return new Color(0.44f, 0.95f, 0.46f, 0.95f);
                case WeaponType.Persistent:
                    return new Color(0.68f, 0.8f, 1f, 0.95f);
                default:
                    return new Color(1f, 0.9f, 0.25f, 0.95f);
            }
        }

        private bool IsWalkable(Vector2 worldPosition)
        {
            if (_walkableResolver == null)
            {
                return true;
            }

            return _walkableResolver(worldPosition);
        }

        private void EnsureMagnetAura()
        {
            if (_magnetAura == null)
            {
                var existing = transform.Find("MagnetAura");
                if (existing == null)
                {
                    var aura = new GameObject("MagnetAura");
                    aura.transform.SetParent(transform, false);
                    _magnetAura = aura.transform;
                }
                else
                {
                    _magnetAura = existing;
                }
            }

            if (_magnetAura != null && _magnetAuraRenderer == null)
            {
                _magnetAuraRenderer = _magnetAura.GetComponent<SpriteRenderer>();
                if (_magnetAuraRenderer == null)
                {
                    _magnetAuraRenderer = _magnetAura.gameObject.AddComponent<SpriteRenderer>();
                }

                _magnetAuraRenderer.sprite = RuntimeSpriteLibrary.GetCircle();
                _magnetAuraRenderer.sortingOrder = 80;
                _magnetAuraRenderer.color = new Color(0.65f, 0.86f, 1f, 0f);
            }
        }

        private void UpdateMagnetAura()
        {
            if (_magnetAura == null || _magnetAuraRenderer == null)
            {
                return;
            }

            bool active = IsExpMagnetActive;
            _magnetAura.gameObject.SetActive(active);
            if (!active)
            {
                return;
            }

            float normalizedRadius = Mathf.Clamp(_expMagnetRadius * 0.22f, 0.45f, 1.8f);
            _magnetAura.localScale = new Vector3(normalizedRadius, normalizedRadius, 1f);
            float pulse = 0.35f + Mathf.Sin(Time.time * 7.5f) * 0.2f;
            _magnetAuraRenderer.color = new Color(0.55f, 0.84f, 1f, pulse);
        }
    }
}
