using UnityEngine;
using UnityEngine.EventSystems;

namespace GameMaker.Core
{
    /// <summary>
    /// 씬 파일 없이 어떤 씬에서 Play 를 눌러도 게임이 뜨도록 하는 진입점.
    /// (레거시의 WaitingActivity(런처) 역할)
    /// </summary>
    public static class GameBootstrap
    {
        static bool booted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (booted) return;
            booted = true;

            // 카메라 보장
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.backgroundColor = new Color(0.09f, 0.10f, 0.14f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // 입력 이벤트 시스템 보장
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // 화면 라우터 (레거시 Activity 전환에 대응)
            var router = new GameObject("ScreenRouter").AddComponent<ScreenRouter>();
            Object.DontDestroyOnLoad(router.gameObject);
            router.Show(ScreenId.Waiting);
        }
    }
}
