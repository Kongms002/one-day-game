using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public FloatingJoystick joystick;
    public float speed = 5f;

    void Update()
    {
        Vector3 dir = new Vector3(joystick.Horizontal, joystick.Vertical, 0f);

        if (dir.sqrMagnitude < 0.01f)
            return;

        dir.Normalize();
        transform.position += dir * speed * Time.deltaTime;
    }
}