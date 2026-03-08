using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public sealed class WeaponProjectileView : MonoBehaviour
    {
        public event System.Action<WeaponProjectileView> Completed;

        private EnemyView _target;
        private float _damage;
        private float _speed;
        private float _lifeTime;
        private float _hitRadius;
        private float _knockbackForce;
        private int _enemyMask;
        private float _elapsed;
        private bool _resolved;
        private Vector3 _fallbackDirection;
        private bool _destroyOnComplete = true;

        public void Initialize(
            EnemyView target,
            float damage,
            float speed,
            float lifeTime,
            float hitRadius,
            float knockbackForce,
            int enemyMask,
            Color tint)
        {
            _target = target;
            _damage = Mathf.Max(0.1f, damage);
            _speed = Mathf.Max(1f, speed);
            _lifeTime = Mathf.Max(0.1f, lifeTime);
            _hitRadius = Mathf.Clamp(hitRadius, 0.08f, 0.6f);
            _knockbackForce = Mathf.Max(0.1f, knockbackForce);
            _enemyMask = enemyMask == 0 ? Physics2D.AllLayers : enemyMask;
            _elapsed = 0f;
            _resolved = false;
            _fallbackDirection = target != null
                ? (target.transform.position - transform.position).normalized
                : Vector3.up;

            EnsureRenderer(tint);
            gameObject.SetActive(true);
        }

        public void SetDestroyOnComplete(bool destroyOnComplete)
        {
            _destroyOnComplete = destroyOnComplete;
        }

        private void Update()
        {
            if (_resolved)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifeTime)
            {
                Complete();
                return;
            }

            Vector3 direction;
            if (_target != null && !_target.IsDead)
            {
                direction = (_target.transform.position - transform.position).normalized;
                if (direction.sqrMagnitude > 0.001f)
                {
                    _fallbackDirection = direction;
                }
            }
            else
            {
                direction = _fallbackDirection;
            }

            transform.position += direction * (_speed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            var hit = Physics2D.OverlapCircle(transform.position, _hitRadius, _enemyMask);
            if (hit == null)
            {
                return;
            }

            var enemy = hit.GetComponent<EnemyView>();
            if (enemy == null || enemy.IsDead)
            {
                return;
            }

            _resolved = true;
            enemy.ApplyDamage(_damage);
            enemy.ApplyKnockback(transform.position, _knockbackForce);
            Complete();
        }

        private void Complete()
        {
            Completed?.Invoke(this);
            if (_destroyOnComplete)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void EnsureRenderer(Color tint)
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = RuntimeSpriteLibrary.GetDiamond();
            renderer.color = tint;
            renderer.sortingOrder = 132;
            transform.localScale = new Vector3(0.24f, 0.24f, 1f);
        }
    }
}
