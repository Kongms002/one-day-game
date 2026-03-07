using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ImagePlayerController : MonoBehaviour
{
    [SerializeField]
    private float _moveSpeed = 5f;

    private Rigidbody2D _rb;
    private Vector2 _input;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        _input = new Vector2(h, v).normalized;
    }

    private void FixedUpdate()
    {
        if (_rb == null)
        {
            return;
        }

        _rb.linearVelocity = _input * _moveSpeed;
    }
}
