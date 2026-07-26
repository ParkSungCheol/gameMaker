using UnityEditor;
using UnityEngine;

namespace GameMaker.Dev
{
    /// <summary>스크린샷 투어 시작 메뉴 — Game 뷰를 1920x1080 으로 맞추고 플레이 모드 진입.</summary>
    public static class ScreenshotTourMenu
    {
        [MenuItem("GameMaker/스크린샷 투어 실행")]
        public static void Run()
        {
            try
            {
                PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "ShotTour");
            }
            catch (System.Exception)
            {
                Debug.LogWarning("[ScreenshotTour] Game 뷰 해상도를 1920x1080 으로 직접 맞춰주세요.");
            }
            SessionState.SetBool("shotTour", true);
            EditorApplication.EnterPlaymode();
        }
    }
}
