using System;
using OneDayGame.Domain.Gameplay;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public sealed class EnemyView : MonoBehaviour
    {
        public event Action<EnemyView> EnemyDied;

        [SerializeField]
        private float _destroyDelay = 1.2f;

        private Transform _target;
        private EnemyData _data;
        private float _hp;
        private int _scoreValue;
        private bool _isDead;

        public float ContactDamage => _data.ContactDamage;

        public int ScoreValue => _scoreValue;

        public float MoveSpeed => _data.MoveSpeed;

        public void Initialize(EnemyData data, Transform target)
        {
            _data = data;
            _hp = data.MaxHp;
            _target = target;
            _scoreValue = data.ScoreValue;
            _isDead = false;
            gameObject.SetActive(true);
        }

        public void ApplyDamage(float value)
        {
            if (_isDead)
            {
                return;
            }

            _hp -= Mathf.Max(0f, value);
            if (_hp <= 0f)
            {
                Die();
            }
        }

        private void Update()
        {
            if (_isDead || _target == null)
            {
                return;
            }

            var step = _data.MoveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(_target.position.x, _target.position.y, transform.position.z),
                step);
        }

        private void Die()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            EnemyDied?.Invoke(this);
            Destroy(gameObject, _destroyDelay);
        }

        public bool IsDead => _isDead;
    }
}
