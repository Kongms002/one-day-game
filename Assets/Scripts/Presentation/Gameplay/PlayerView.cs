using OneDayGame.Application;
using OneDayGame.Domain.Input;
using OneDayGame.Domain.Policies;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody2D _rigidbody2D;

        [SerializeField]
        private LayerMask _enemyLayer = -1;

        [SerializeField]
        private LayerMask _medKitLayer = -1;

        private IInputPort _inputPort;
        private RunSessionService _runSession;
        private IWeaponPolicy _weaponPolicy;

        private float _attackCooldown;
        private Vector2 _playerBoundsMin;
        private Vector2 _playerBoundsMax;
        private float _contactDamageElapsed;
        private float _moveSpeed;
        private float _touchDamageInterval;

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

            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown <= 0f && _inputPort.AnyActionPressed)
            {
                ExecuteAttack();
                _attackCooldown = _weaponPolicy.GetPlayerAttackCooldown(_runSession.Stage);
            }

            if (_contactDamageElapsed > 0f)
            {
                _contactDamageElapsed -= Time.deltaTime;
                return;
            }

            var colliders = Physics2D.OverlapCircleAll(transform.position, 0.4f, _enemyLayer);
            foreach (var enemyCollider in colliders)
            {
                var enemy = enemyCollider.GetComponent<EnemyView>();
                if (enemy != null)
                {
                    _runSession.ApplyDamage(Mathf.Max(0.1f, enemy.ContactDamage));
                    _contactDamageElapsed = _touchDamageInterval;
                }
            }
        }

        private void ExecuteAttack()
        {
            var stage = _runSession.Stage;
            float range = _weaponPolicy.GetPlayerAttackRange(stage);
            float damage = _weaponPolicy.GetPlayerAttackDamage(stage);

            var hits = Physics2D.OverlapCircleAll(transform.position, range, _enemyLayer);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyView>();
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                enemy.ApplyDamage(damage);
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
    }
}
