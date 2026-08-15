using UnityEngine;
using UnityEngine.SceneManagement;

public class TestModeSceneController : MonoBehaviour
{
    [Header("References")]
    public GameObject testModeUIPanel; // optional reference if you want to deactivate/activate
    public string mainMenuSceneName = "MainMenu"; // fallback name

    void Start()
    {
        // Ensure TestModeManager exists in this scene
        if (TestModeManager.Instance == null)
        {
            var go = new GameObject("TestModeManager");
            go.AddComponent<TestModeManager>();
        }

        // Show UI panel by default
        if (testModeUIPanel != null) testModeUIPanel.SetActive(true);
    }

    // hooked to Back button in TestMode scene
    public void OnBackButton()
    {
        var prev = string.IsNullOrEmpty(SceneNavigation.PreviousScene) ? mainMenuSceneName : SceneNavigation.PreviousScene;
        SceneManager.LoadScene(prev);
    }
}
