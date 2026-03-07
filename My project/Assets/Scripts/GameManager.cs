using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public GameObject Title;
    public GameObject tapToStartUI;
    public GameObject joystickInput;
    //public GameObject enemySpawner;

    private bool gameStarted = false;

    void Start()
    {
        Title.SetActive(true);
        tapToStartUI.SetActive(true);
        joystickInput.SetActive(false);
        //enemySpawner.SetActive(false);
    }

    void Update()
    {
        if (gameStarted)
            return;

        // 터치
        if (Touchscreen.current != null)
        {
            if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                StartGame();
            }
        }
        // 마우스 (에디터 테스트)
        else if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartGame();
        }
    }

    void StartGame()
    {
        gameStarted = true;

        tapToStartUI.SetActive(false);
        joystickInput.SetActive(true);
        Title.SetActive(false);
        //enemySpawner.SetActive(true);
    }
}