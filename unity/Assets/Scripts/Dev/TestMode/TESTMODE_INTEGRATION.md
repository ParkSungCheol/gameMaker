## Test Mode — Scene integration instructions

This project now includes a Test Mode feature implemented as a separate scene workflow (matches "upgrade" screen UX by switching scenes).

What was added (code only):

- unity/Assets/Scripts/Core/SceneNavigation.cs
  - small static holder to remember which scene opened TestMode so the user can go back.

- unity/Assets/Scripts/Screens/TestModeSceneLoader.cs
  - attach to your MainMenu button (or main menu controller) and call OpenTestModeScene() to load the TestMode scene.
  - it records the current scene name to SceneNavigation.PreviousScene before loading.

- unity/Assets/Scripts/Screens/TestModeSceneController.cs
  - attach in the TestMode scene; it ensures a TestModeManager exists and exposes OnBackButton() for the UI back button.

How to wire this in Unity (recommended, follows repo structure):

1. Create a new scene: `TestMode_Demo` (File → New Scene) and save under `unity/Assets/Scenes/TestMode_Demo.unity`.
2. In that scene place:
   - A Camera and Lighting as needed.
   - A Canvas for UI. Place the TestModeUI panel from `unity/Assets/Scripts/Dev/TestMode` (create the panel and listItem prefab as described in the previous commit). Assign the TestModeUI component references.
   - A GameObject `SceneController` and attach `TestModeSceneController`. Set `testModeUIPanel` reference to your panel. Optionally set `mainMenuSceneName` if your main menu scene name differs.
   - Back button: hook its OnClick to SceneController.OnBackButton().
3. In your main menu scene (e.g., `MainMenu`):
   - Add `TestModeSceneLoader` to the main menu controller object (or a new object).
   - For the menu button that should open Test Mode, add OnClick() => TestModeSceneLoader.OpenTestModeScene().

Notes / Compatibility
- Animator parameters: TestUnitController triggers "Walk", "Attack", "Die". Ensure your unit animators have these triggers (or modify the script to match existing names).
- The code follows the existing `unity/Assets/Scripts/Screens` organization used in the repository.
- This commit does not add binary scene files or prefabs (so you will create the TestMode scene and the UI prefabs in the Unity Editor). If you want, I can add a minimal scene and prefab files, but those are larger and usually edited in-Editor.

If you'd like I can:
- Add a ready-to-open TestMode_Demo.unity scene + a minimal UI prefab so you can press the MainMenu button and test immediately (I can add those files to the branch). OR
- Create a small script to auto-generate a simple TestMode UI at runtime the first time the scene loads (no editor setup required). Which do you prefer?
