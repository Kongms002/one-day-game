using System;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class ExpOrbView : MonoBehaviour
    {
        public event Action<ExpOrbView, int> Collected;

        [SerializeField]
        private float _lifeTime = 10f;

        [SerializeField]
        private float _magnetRadius = 2.2f;

        [SerializeField]
        private float _moveSpeed = 5.8f;

        private Transform _target;
        private int _expValue;
        private float _elapsed;
        private bool _collected;
        private CircleCollider2D _trigger;

        public void Initialize(int expValue, Transform target)
        {
            _expValue = Mathf.Max(1, expValue);
            _target = target;
            _elapsed = 0f;
            _collected = false;

            if (_trigger == null)
            {
                _trigger = GetComponent<CircleCollider2D>();
            }

            _trigger.isTrigger = true;
            _trigger.radius = 0.32f;
            EnsureVisibleSprite();
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_collected)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifeTime)
            {
                Destroy(gameObject);
                return;
            }

            if (_target == null)
            {
                return;
            }

            var delta = _target.position - transform.position;
            if (delta.sqrMagnitude <= _magnetRadius * _magnetRadius)
            {
                transform.position = Vector3.MoveTowards(transform.position, _target.position, _moveSpeed * Time.deltaTime);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || _target == null)
            {
                return;
            }

            if (other.transform != _target && !other.transform.IsChildOf(_target))
            {
                return;
            }

            _collected = true;
            Collected?.Invoke(this, _expValue);
            Destroy(gameObject);
        }

        private void EnsureVisibleSprite()
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = RuntimeSpriteLibrary.GetCircle();
            renderer.color = new Color(0.58f, 0.66f, 1f, 1f);
            renderer.sortingOrder = 105;
            transform.localScale = new Vector3(0.26f, 0.26f, 1f);
        }
    }
}
