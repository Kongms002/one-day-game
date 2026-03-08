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

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            var desiredPosition = _target.position + _offset;
            var speed = Mathf.Max(0.01f, _followLerpSpeed);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * speed);
        }
    }
}
