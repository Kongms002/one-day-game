using System;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public sealed class MedKitView : MonoBehaviour
    {
        [SerializeField]
        private float _lifeTime = 7f;

        public event Action<MedKitView> MedKitCollected;

        public float HealAmount { get; private set; }

        public int ScoreReward { get; private set; }

        private void Awake()
        {
            EnsureTriggerCollider();
            EnsureVisibleSprite();
        }

        public void Initialize(float healAmount, int scoreReward)
        {
            HealAmount = Mathf.Max(0f, healAmount);
            ScoreReward = Math.Max(0, scoreReward);
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

            if (other.GetComponentInParent<PlayerView>() != null)
            {
                MedKitCollected?.Invoke(this);
                Destroy(gameObject);
            }
        }

        private void EnsureTriggerCollider()
        {
            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null)
            {
                collider2D.isTrigger = true;
            }
        }

        private void EnsureVisibleSprite()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = RuntimeSpriteLibrary.GetPlus();
            spriteRenderer.color = new Color(0.35f, 1f, 0.38f, 1f);
            spriteRenderer.sortingOrder = 110;
            transform.localScale = new Vector3(0.52f, 0.52f, 1f);
        }
    }
}
