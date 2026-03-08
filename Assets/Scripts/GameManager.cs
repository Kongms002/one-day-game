using UnityEngine;
using OneDayGame.Presentation.Bootstrap;

public class GameManager : MonoBehaviour
{
    public GameObject Title;
    public GameObject tapToStartUI;
    public GameObject joystickInput;

    void Start()
    {
        DisableLegacyPlayerControllers();
        EnsureRuntimeBootstrap();
        HideLegacyStartUi();
    }

    private void DisableLegacyPlayerControllers()
    {
        var legacyPlayers = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < legacyPlayers.Length; i++)
        {
            var legacyPlayer = legacyPlayers[i];
            if (legacyPlayer != null)
            {
                legacyPlayer.enabled = false;
            }
        }
    }

    void EnsureRuntimeBootstrap()
    {
        if (Object.FindFirstObjectByType<GameBootstrap>() != null)
        {
            return;
        }

        var bootstrapRoot = new GameObject("GameBootstrap");
        bootstrapRoot.AddComponent<GameBootstrap>();
    }

    void HideLegacyStartUi()
    {
        if (tapToStartUI != null)
            tapToStartUI.SetActive(false);

        if (joystickInput != null)
            joystickInput.SetActive(true);

        if (Title != null)
            Title.SetActive(false);

        var byNameTap = GameObject.Find("TapToStart");
        if (byNameTap != null)
        {
            byNameTap.SetActive(false);
        }

        var byNameTitle = GameObject.Find("Title");
        if (byNameTitle != null)
        {
            byNameTitle.SetActive(false);
        }
    }
}
