using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public sealed class DamagePopupView : MonoBehaviour
    {
        public event System.Action<DamagePopupView> Completed;

        [SerializeField]
        private float _lifeTime = 0.55f;

        [SerializeField]
        private float _riseSpeed = 1.8f;

        private TextMesh _textMesh;
        private float _elapsed;
        private Color _originColor;
        private bool _destroyOnComplete = true;

        public void Initialize(string text, Color color)
        {
            _textMesh = GetComponent<TextMesh>();
            if (_textMesh == null)
            {
                _textMesh = gameObject.AddComponent<TextMesh>();
            }

            _textMesh.text = text;
            _textMesh.characterSize = 0.08f;
            _textMesh.fontSize = 54;
            _textMesh.anchor = TextAnchor.MiddleCenter;
            _textMesh.alignment = TextAlignment.Center;
            _textMesh.color = color;
            _originColor = color;
            _elapsed = 0f;
            gameObject.SetActive(true);
        }

        public void SetDestroyOnComplete(bool destroyOnComplete)
        {
            _destroyOnComplete = destroyOnComplete;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            transform.position += new Vector3(0f, _riseSpeed * Time.unscaledDeltaTime, 0f);

            if (_textMesh != null)
            {
                float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _lifeTime));
                var c = _originColor;
                c.a = 1f - t;
                _textMesh.color = c;
            }

            if (_elapsed >= _lifeTime)
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
        }
    }
}
