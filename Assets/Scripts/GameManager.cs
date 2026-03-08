using UnityEngine;
using OneDayGame.Presentation.Bootstrap;

public class GameManager : MonoBehaviour
{
    public GameObject Title;
    public GameObject tapToStartUI;
    public GameObject joystickInput;
    //public GameObject enemySpawner;

    private bool gameStarted = false;

    void Start()
    {
        EnsureRuntimeBootstrap();
        StartGame();
    }

    void EnsureRuntimeBootstrap()
    {
        if (FindObjectOfType<GameBootstrap>() != null)
        {
            return;
        }

        var bootstrapRoot = new GameObject("GameBootstrap");
        bootstrapRoot.AddComponent<GameBootstrap>();

        var legacyPlayer = FindObjectOfType<PlayerController>();
        if (legacyPlayer != null)
        {
            legacyPlayer.gameObject.SetActive(false);
        }
    }

    void StartGame()
    {
        if (gameStarted)
            return;

        gameStarted = true;

        if (tapToStartUI != null)
            tapToStartUI.SetActive(false);

        if (joystickInput != null)
            joystickInput.SetActive(true);

        if (Title != null)
            Title.SetActive(false);
    }
}
