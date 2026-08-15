using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class TestModeSceneLoader : MonoBehaviour
{
    [Tooltip("Name of the scene that contains the Test Mode UI and demo units.")]
    public string testModeSceneName = "TestMode_Demo";

    // Call from MainMenu button OnClick
    public void OpenTestModeScene()
    {
        // remember current scene so TestMode scene can go back
        SceneNavigation.PreviousScene = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(testModeSceneName)) return;

        if (IsSceneInBuild(testModeSceneName))
        {
            SceneManager.LoadScene(testModeSceneName);
            return;
        }

        Debug.LogError($"Scene '{testModeSceneName}' couldn't be loaded because it is not in Build Settings. Add it via File -> Build Settings -> Add Open Scenes, or include it in the active build profile.");

#if UNITY_EDITOR
        // Editor convenience: try to find the scene asset and offer to open it.
        var scenePath = FindSceneAssetPath(testModeSceneName);
        if (!string.IsNullOrEmpty(scenePath))
        {
            if (EditorUtility.DisplayDialog("Test Mode scene missing from Build Settings",
                $"Scene '{testModeSceneName}' exists at:\n{scenePath}\n\nOpen it now and add to Build Settings?", "Open", "Ignore"))
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }
#endif
    }

    bool IsSceneInBuild(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (Path.GetFileNameWithoutExtension(path).Equals(sceneName)) return true;
        }
        return false;
    }

#if UNITY_EDITOR
    string FindSceneAssetPath(string sceneName)
    {
        var guids = AssetDatabase.FindAssets("t:Scene " + sceneName);
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileNameWithoutExtension(p).Equals(sceneName)) return p;
        }

        var allGuids = AssetDatabase.FindAssets("t:Scene");
        foreach (var g in allGuids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileNameWithoutExtension(p).Equals(sceneName)) return p;
        }
        return null;
    }
#endif
}
