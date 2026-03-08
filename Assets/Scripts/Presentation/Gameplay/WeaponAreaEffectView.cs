using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public sealed class WeaponAreaEffectView : MonoBehaviour
    {
        public event System.Action<WeaponAreaEffectView> Completed;

        [SerializeField]
        private float _lifeTime = 0.18f;

        private float _elapsed;
        private bool _destroyOnComplete = true;
        private SpriteRenderer _renderer;

        public void Initialize(float radius, Color color, Sprite overrideSprite)
        {
            EnsureRenderer();
            _elapsed = 0f;
            transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            _renderer.color = color;
            _renderer.sprite = overrideSprite != null
                ? overrideSprite
                : RuntimeSpriteLibrary.GetCircle();
            gameObject.SetActive(true);
        }

        public void SetDestroyOnComplete(bool destroyOnComplete)
        {
            _destroyOnComplete = destroyOnComplete;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _lifeTime));
            if (_renderer != null)
            {
                var c = _renderer.color;
                c.a = Mathf.Lerp(0.55f, 0f, t);
                _renderer.color = c;
            }

            if (_elapsed < _lifeTime)
            {
                return;
            }

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

        private void EnsureRenderer()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
                if (_renderer == null)
                {
                    _renderer = gameObject.AddComponent<SpriteRenderer>();
                }

                _renderer.sprite = RuntimeSpriteLibrary.GetCircle();
                _renderer.sortingOrder = 120;
            }
        }
    }
}
