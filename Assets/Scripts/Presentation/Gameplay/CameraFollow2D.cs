using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField]
        private Transform _target;

        [SerializeField]
        private Vector3 _offset = new Vector3(0f, 0f, -10f);

        [SerializeField]
        private float _followLerpSpeed = 10f;

        private bool _hasBounds;
        private float _minX;
        private float _maxX;
        private float _minY;
        private float _maxY;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void SetBounds(float minX, float maxX, float minY, float maxY)
        {
            _minX = Mathf.Min(minX, maxX);
            _maxX = Mathf.Max(minX, maxX);
            _minY = Mathf.Min(minY, maxY);
            _maxY = Mathf.Max(minY, maxY);
            _hasBounds = true;
        }

        public void ClearBounds()
        {
            _hasBounds = false;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            var desiredPosition = _target.position + _offset;
            if (_hasBounds)
            {
                var camera = GetComponent<Camera>();
                float verticalHalf = 0f;
                float horizontalHalf = 0f;
                if (camera != null && camera.orthographic)
                {
                    verticalHalf = camera.orthographicSize;
                    horizontalHalf = verticalHalf * camera.aspect;
                }

                float minX = _minX + horizontalHalf;
                float maxX = _maxX - horizontalHalf;
                float minY = _minY + verticalHalf;
                float maxY = _maxY - verticalHalf;
                if (minX > maxX)
                {
                    float cx = (_minX + _maxX) * 0.5f;
                    minX = cx;
                    maxX = cx;
                }

                if (minY > maxY)
                {
                    float cy = (_minY + _maxY) * 0.5f;
                    minY = cy;
                    maxY = cy;
                }

                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
            }

            var speed = Mathf.Max(0.01f, _followLerpSpeed);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * speed);
        }
    }
}
