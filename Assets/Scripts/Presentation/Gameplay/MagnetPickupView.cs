using System;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public sealed class MagnetPickupView : MonoBehaviour
    {
        [SerializeField]
        private float _lifeTime = 8f;

        public event Action<MagnetPickupView> Collected;

        public float Duration { get; private set; }

        public float Radius { get; private set; }

        private void Awake()
        {
            EnsureTriggerCollider();
            EnsureVisibleSprite();
        }

        public void Initialize(float duration, float radius)
        {
            Duration = Mathf.Max(0.5f, duration);
            Radius = Mathf.Max(1.5f, radius);
            _lifeTime = Mathf.Max(0.1f, _lifeTime);
        }

        private void Update()
        {
            _lifeTime -= Time.deltaTime;
            if (_lifeTime <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerView>() == null)
            {
                return;
            }

            Collected?.Invoke(this);
            Destroy(gameObject);
        }

        private void EnsureTriggerCollider()
        {
            var collider2D = GetComponent<Collider2D>();
            if (collider2D == null)
            {
                collider2D = gameObject.AddComponent<CircleCollider2D>();
            }

            collider2D.isTrigger = true;
        }

        private void EnsureVisibleSprite()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = RuntimeSpriteLibrary.GetCircle();
            spriteRenderer.color = new Color(0.58f, 0.82f, 1f, 1f);
            spriteRenderer.sortingOrder = 111;
            transform.localScale = new Vector3(0.46f, 0.46f, 1f);
        }
    }
}
