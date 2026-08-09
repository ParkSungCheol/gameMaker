#if UNITY_EDITOR
using System.Collections;
using GameMaker.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GameMaker.Dev
{
    /// <summary>
    /// 에디터 전용 — 스토어 등록용 스크린샷 자동 촬영 투어.
    /// 메뉴 [GameMaker > 스크린샷 투어 실행] 으로 시작하면
    /// 타이틀 → 메인 → 맵 → 업그레이드 → 9개 전투(교전 중) 순서로
    /// c:/GameMaker_app/screenshots 에 PNG 를 저장하고 플레이 모드를 종료한다.
    /// </summary>
    public class ScreenshotTour : MonoBehaviour
    {
        const string Dir = "c:/GameMaker_app/screenshots";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!UnityEditor.SessionState.GetBool("shotTour", false)) return;
            var go = new GameObject("ScreenshotTour");
            DontDestroyOnLoad(go);
            go.AddComponent<ScreenshotTour>();
        }

        void Start() => StartCoroutine(Tour());

        IEnumerator Tour()
        {
            System.IO.Directory.CreateDirectory(Dir);

            yield return new WaitForSecondsRealtime(1.2f); // 타이틀 연출 자리 잡기
            yield return Shot("01_title");

            ScreenRouter.I.Show(ScreenId.Main);
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Shot("02_main");

            ScreenRouter.I.Show(ScreenId.Map);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("03_map");

            ScreenRouter.I.Show(ScreenId.Upgrade);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("04_upgrade");

            for (int stage = 1; stage <= 12; stage++) // 테마 번호 → 각 테마 첫 서브스테이지(t-1)
            {
                ScreenRouter.I.Show(ScreenId.Battlefield, stage);
                yield return new WaitForSecondsRealtime(0.6f);
                Time.timeScale = 3f;

                var ctrl = FindFirstObjectByType<Battle.BattlefieldController>();

                // 1) 적 전선이 화면 중앙(약 60% 지점)을 넘어올 때까지 대기 (최대 25초)
                for (float t = 0; t < 25f; t += 0.5f)
                {
                    if (EnemyFront(ctrl) < Battle.BattlefieldController.WorldWidth * 0.62f) break;
                    yield return new WaitForSecondsRealtime(0.5f);
                }

                // 2) 아군을 계속 투입하며 전선이 맞붙는 순간을 감지 (최대 18초)
                bool engaged = false;
                for (float t = 0; t < 18f; t += 0.4f)
                {
                    ClickSpawnButtons();
                    if (AllyFront(ctrl) >= EnemyFront(ctrl) - 190f) { engaged = true; break; }
                    yield return new WaitForSecondsRealtime(0.4f);
                }

                // 3) 맞붙는 순간 바로 정속 전환 — 몬스터가 죽기 전에 촬영
                if (engaged) ClickSpawnButtons();
                Time.timeScale = 1f;
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Shot("battle_stage" + stage);
            }

            Time.timeScale = 1f;
            UnityEditor.SessionState.SetBool("shotTour", false);
            Debug.Log("[ScreenshotTour] 완료 — " + Dir);
            UnityEditor.EditorApplication.ExitPlaymode();
        }

        /// <summary>적 전선(가장 왼쪽까지 진군한 적의 x). 적이 없으면 매우 큰 값.</summary>
        static float EnemyFront(Battle.BattlefieldController ctrl)
        {
            float front = float.MaxValue;
            foreach (var u in Party(ctrl, "yourParty"))
                if (u != null && !u.Dead && !u.IsCastle) front = Mathf.Min(front, u.X);
            return front;
        }

        /// <summary>아군 전선(가장 오른쪽까지 진군한 아군의 x). 아군이 없으면 매우 작은 값.</summary>
        static float AllyFront(Battle.BattlefieldController ctrl)
        {
            float front = float.MinValue;
            foreach (var u in Party(ctrl, "ourParty"))
                if (u != null && !u.Dead && !u.IsCastle) front = Mathf.Max(front, u.X);
            return front;
        }

        static System.Collections.Generic.List<Battle.Unit> Party(Battle.BattlefieldController ctrl, string field) =>
            (System.Collections.Generic.List<Battle.Unit>)typeof(Battle.BattlefieldController)
                .GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(ctrl);

        void ClickSpawnButtons()
        {
            foreach (var name in new[] { "Spawn_ourbasic", "Spawn_ourtank", "Spawn_ourbattle", "Spawn_ourmass" })
            {
                var go = GameObject.Find(name);
                var b = go != null ? go.GetComponent<Button>() : null;
                if (b != null) b.onClick.Invoke();
            }
        }

        IEnumerator Shot(string name)
        {
            ScreenCapture.CaptureScreenshot(Dir + "/" + name + ".png");
            yield return new WaitForSecondsRealtime(0.8f); // 비동기 저장 대기
        }
    }
}
#endif
