using System;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class ExpOrbView : MonoBehaviour
    {
        public event Action<ExpOrbView, int> Collected;
        public event Action<ExpOrbView> Released;

        [SerializeField]
        private float _lifeTime = 10f;

        [SerializeField]
        private float _magnetRadius = 0f;

        [SerializeField]
        private float _moveSpeed = 5.8f;

        private Transform _target;
        private int _expValue;
        private float _elapsed;
        private bool _collected;
        private CircleCollider2D _trigger;
        private PlayerView _player;
        private Collider2D _playerBodyCollider;
        private bool _destroyOnCollected = true;

        public void Initialize(int expValue, Transform target)
        {
            _expValue = Mathf.Max(1, expValue);
            _target = target;
            _player = target != null ? target.GetComponent<PlayerView>() : null;
            _playerBodyCollider = _player != null ? _player.GetComponent<Collider2D>() : null;
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

        public void SetDestroyOnCollected(bool destroyOnCollected)
        {
            _destroyOnCollected = destroyOnCollected;
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
                Released?.Invoke(this);
                if (_destroyOnCollected)
                {
                    Destroy(gameObject);
                }
                else
                {
                    gameObject.SetActive(false);
                }
                return;
            }

            if (_target == null)
            {
                return;
            }

            float effectiveMagnetRadius = _magnetRadius;
            if (_player != null && _player.IsExpMagnetActive)
            {
                effectiveMagnetRadius = Mathf.Max(effectiveMagnetRadius, _player.ExpMagnetRadius);
            }

            var delta = _target.position - transform.position;
            if (effectiveMagnetRadius > 0.01f && delta.sqrMagnitude <= effectiveMagnetRadius * effectiveMagnetRadius)
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

            if (_player == null)
            {
                return;
            }

            if (_playerBodyCollider != null)
            {
                if (other != _playerBodyCollider)
                {
                    return;
                }
            }
            else
            {
                var otherPlayer = other.GetComponentInParent<PlayerView>();
                if (otherPlayer != _player)
                {
                    return;
                }
            }

            _collected = true;
            Collected?.Invoke(this, _expValue);
            if (_destroyOnCollected)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
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
