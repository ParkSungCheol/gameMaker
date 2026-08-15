using UnityEngine;
using UnityEngine.SceneManagement;

public class TestModeSceneLoader : MonoBehaviour
{
    [Tooltip("Name of the scene that contains the Test Mode UI and demo units.")]
    public string testModeSceneName = "TestMode_Demo";

    // Call from MainMenu button OnClick
    public void OpenTestModeScene()
    {
        // remember current scene so TestMode scene can go back
        SceneNavigation.PreviousScene = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(testModeSceneName))
            SceneManager.LoadScene(testModeSceneName);
    }

    // Optional: go to TestMode scene by name (useful from editor)
    public void OpenTestModeSceneByName(string sceneName)
    {
        SceneNavigation.PreviousScene = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
    }

    // If you need a direct static call from code:
    public static void OpenTestModeSceneStatic(string sceneName = "TestMode_Demo")
    {
        SceneNavigation.PreviousScene = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
    }

    // Convenience: called by UI back button inside TestMode scene to return
    public void BackToPrevious()
    {
        var prev = string.IsNullOrEmpty(SceneNavigation.PreviousScene) ? "MainMenu" : SceneNavigation.PreviousScene;
        SceneManager.LoadScene(prev);
    }
}
