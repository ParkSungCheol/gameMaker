#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameMaker.Core;
using GameMaker.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameMaker.Dev
{
    /// <summary>
    /// 에디터 전용 — 스토어 등록용 스크린샷 자동 촬영 투어.
    /// 메뉴 [GameMaker > 스크린샷 투어 실행] 으로 시작하면 각 화면을 '상호작용이 있는 순간'까지 조작해서
    /// c:/GameMaker_app/screenshots 에 PNG 를 저장하고 플레이 모드를 종료한다.
    ///
    ///  01_title                  타이틀
    ///  02_main                   메인 메뉴(5버튼)
    ///  03_map / 03b_map_sub      맵 / 서브스테이지 화살표로 2번째 판 선택
    ///  04_upgrade / 04b_upgrade_tier2 / 04c_upgrade_roleguide   강화 / 다음 등급 / ? 포지션 안내 팝업
    ///  05_gacha / 05b_gacha_rates / 05c_gacha_result            뽑기 / 확률표 팝업 / 뽑기 결과(이름판·배지)
    ///  06_deploy / 06b_deploy_drag                              배치 / 카드를 드래그 중인 순간
    ///  07_codex / 07b_codex_detail / 07c_codex_enemy            도감 / 상세 팝업 / 적군 탭
    ///  battle_stage1..12         각 여행지 첫 판, 전선이 맞붙은 순간
    ///  battle_win                승리 결과 팝업
    ///
    /// 촬영 전 세이브를 백업하고 (돈/보유 유닛/클리어 기록을 보기 좋게 채운 뒤) 끝나면 원래대로 되돌린다.
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

        string backupJson, backupUpgrades;

        void Start() => StartCoroutine(Tour());

        IEnumerator Tour()
        {
            System.IO.Directory.CreateDirectory(Dir);
            Core.Dev.ReleaseLook = true; // 유닛 뷰어 버튼 / x20 배속 / 무료 뽑기 등 개발 흔적 숨김
            PrepareSave();

            yield return new WaitForSecondsRealtime(1.2f); // 타이틀 연출 자리 잡기
            yield return Shot("01_title");

            // ── 메인 ──
            yield return Go(ScreenId.Main, 1.0f);
            yield return Shot("02_main");

            // ── 맵: 기본 / 2번째 서브스테이지 선택 ──
            yield return Go(ScreenId.Map, 0.8f);
            yield return Shot("03_map");
            Click("Next1");
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shot("03b_map_sub");

            // ── 업그레이드: 기본 등급 / 다음 등급 / 포지션 안내 팝업 ──
            yield return Go(ScreenId.Upgrade, 0.8f);
            yield return Shot("04_upgrade");
            Click("NextTier");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("04b_upgrade_tier2");
            Click("RoleHelp");
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("04c_upgrade_roleguide");
            ClosePopup();

            // ── 뽑기: 대기 / 확률표 / 뽑기 결과 ──
            yield return Go(ScreenId.Gacha, 1.0f);
            yield return Shot("05_gacha");
            Click("RateBtn");
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("05b_gacha_rates");
            ClosePopup();
            yield return new WaitForSecondsRealtime(0.2f);
            Click("DrawBtn");
            // 이름판이 박힌 직후(배지까지 뜬 순간)를 잡는다
            for (float t = 0; t < 12f && GameObject.Find("NamePlate") == null; t += 0.1f)
                yield return new WaitForSecondsRealtime(0.1f);
            yield return new WaitForSecondsRealtime(0.7f);
            yield return Shot("05c_gacha_result");

            // ── 배치: 기본 / 드래그 중 ──
            yield return Go(ScreenId.Deploy, 0.8f);
            yield return Shot("06_deploy");
            var drag = FindListDragCard();
            if (drag != null)
            {
                var ev = new PointerEventData(EventSystem.current) { position = new Vector2(Screen.width * 0.5f, Screen.height * 0.56f) };
                ExecuteEvents.Execute(drag, ev, ExecuteEvents.beginDragHandler);
                ExecuteEvents.Execute(drag, ev, ExecuteEvents.dragHandler);
                yield return new WaitForSecondsRealtime(0.3f);
                yield return Shot("06b_deploy_drag");
                ExecuteEvents.Execute(drag, ev, ExecuteEvents.endDragHandler);
            }

            // ── 도감: 아군 / 상세 팝업 / 적군 탭 ──
            yield return Go(ScreenId.Codex, 0.8f);
            yield return Shot("07_codex");
            var card = FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name.StartsWith("Card_"));
            if (card != null) card.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("07b_codex_detail");
            ClosePopup();
            Click("EnemyTab");
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("07c_codex_enemy");

            // ── 전투: 여행지별 첫 판, 전선이 맞붙는 순간 (+ 1번 여행지는 승리 팝업까지) ──
            for (int stage = 1; stage <= 12; stage++)
            {
                ScreenRouter.I.Show(ScreenId.Battlefield, stage * 10 + 1); // 각 여행지 첫 판 (stageId)
                yield return new WaitForSecondsRealtime(0.6f);

                var ctrl = FindFirstObjectByType<Battle.BattlefieldController>();
                SetSpeed(ctrl, 3); // 컨트롤러가 매 프레임 Time.timeScale 을 자기 배속으로 덮어쓰므로 배속 필드를 직접 바꾼다

                // 1) 첫 적이 나타날 때까지 대기 (최대 30초) — 적은 빠르므로 보이자마자 아군을 내보내야 중앙에서 만난다
                for (float t = 0; t < 30f; t += 0.25f)
                {
                    if (EnemyFront(ctrl) < Battle.BattlefieldController.WorldWidth * 0.98f) break;
                    yield return new WaitForSecondsRealtime(0.25f);
                }

                // 2) 지갑을 채워 아군을 내보내고(아군은 오른쪽으로 행군) 전선이 맞붙는 순간을 감지 (최대 18초)
                //    — 성 앞이 아니라 화면 중앙 부근에서 맞붙는 그림이 나온다
                bool engaged = false;
                for (float t = 0; t < 18f; t += 0.4f)
                {
                    FillWallet(ctrl);
                    ClickSpawnButtons(3); // 한 번에 세 종 — 인원 초과 안내가 뜨지 않는 선에서
                    if (AllyFront(ctrl) >= EnemyFront(ctrl) - 190f) { engaged = true; break; }
                    yield return new WaitForSecondsRealtime(0.4f);
                }

                // 3) 맞붙는 순간 바로 정속 전환 — 몬스터가 죽기 전에 촬영 (안내 문구가 사라지는 1초 뒤)
                SetSpeed(ctrl, 1);
                yield return new WaitForSecondsRealtime(engaged ? 1.05f : 0.4f);
                yield return Shot("battle_stage" + stage);

                if (stage == 1)
                {
                    // 적 성을 무너뜨려 승리 결과 팝업 촬영
                    var castle = Party(ctrl, "yourParty").FirstOrDefault(u => u != null && u.IsCastle && !u.Dead);
                    if (castle != null) castle.TakeDamage(999999);
                    yield return new WaitForSecondsRealtime(1.4f);
                    yield return Shot("battle_win");
                    Time.timeScale = 1f;
                }
            }

            Time.timeScale = 1f;
            RestoreSave();
            Core.Dev.ReleaseLook = false;
            UnityEditor.SessionState.SetBool("shotTour", false);
            Debug.Log("[ScreenshotTour] 완료 — " + Dir);
            UnityEditor.EditorApplication.ExitPlaymode();
        }

        // ─────────── 세이브 준비/복구 ───────────

        /// <summary>보기 좋은 상태로: 돈 넉넉히, 뽑기 유닛 여러 종 보유(+강화 중복 포함), 앞쪽 스테이지 클리어·트라이 기록.</summary>
        void PrepareSave()
        {
            var p = DataHub.I.GetPlayer();
            backupJson = JsonUtility.ToJson(p);

            p.money = 2750;
            var rnd = new System.Random(7); // 재현 가능한 보유 구성
            var pool = DataHub.I.GetMonsters().Where(m => m.IsOur && !m.IsCastle && m.tier > 0).ToList();
            foreach (int tier in new[] { 1, 1, 1, 1, 2, 2, 2, 3, 3, 4, 5 })
            {
                var cand = pool.Where(m => m.tier == tier).ToList();
                if (cand.Count == 0) continue;
                var u = cand[rnd.Next(cand.Count)];
                int i = p.gachaNames.IndexOf(u.name);
                if (i < 0) { p.gachaNames.Add(u.name); p.gachaDupes.Add(tier == 1 ? 2 : 0); }
            }
            // 출전: 기본 2 + 뽑은 유닛 3 (성 제외 5칸)
            p.loadout = new List<string> { "ourbasic", "ourtank" };
            p.loadout.AddRange(p.gachaNames.Take(3));

            // 여행지 1~3 전부 + 4-1 클리어, 4-2 는 시작만 (맵에 진행감, 도감 적군은 그만큼만 공개)
            if (p.triedStages == null) p.triedStages = new List<int>();
            var cleared = new List<int>();
            for (int t = 1; t <= 3; t++)
                for (int s = 1; s <= Mathf.Max(1, DataHub.I.GetStage(t).subCount); s++) cleared.Add(t * 10 + s);
            cleared.Add(41);
            foreach (int s in cleared) if (s < p.mapClear.Length) p.mapClear[s] = Mathf.Max(p.mapClear[s], 1);
            foreach (int s in cleared) if (!p.triedStages.Contains(s)) p.triedStages.Add(s);
            if (!p.triedStages.Contains(42)) p.triedStages.Add(42);
            DataHub.I.SavePlayer(p);

            // 골드 강화 레벨: 전부 MAX 가 아니라 진행 중인 모습으로
            var upField = typeof(LocalDataService).GetField("upgrades",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (DataHub.I is LocalDataService local && upField != null)
            {
                var up = (UpgradeState)upField.GetValue(local);
                backupUpgrades = JsonUtility.ToJson(up);
                var shown = new UpgradeState();
                shown.Set("ourcastle", 3); shown.Set("ourbasic", 4); shown.Set("ourtank", 2); shown.Set("ourbattle", 1);
                upField.SetValue(local, shown);
            }
        }

        void RestoreSave()
        {
            if (!string.IsNullOrEmpty(backupJson))
                DataHub.I.SavePlayer(JsonUtility.FromJson<PlayerData>(backupJson));
            var upField = typeof(LocalDataService).GetField("upgrades",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (!string.IsNullOrEmpty(backupUpgrades) && DataHub.I is LocalDataService local && upField != null)
            {
                upField.SetValue(local, JsonUtility.FromJson<UpgradeState>(backupUpgrades));
                PlayerPrefs.SetString("gm_upgrades_json", backupUpgrades); // 촬영 중 저장된 값 덮어쓰기
                PlayerPrefs.Save();
            }
        }

        /// <summary>전투 지갑을 용량까지 채운다 — 촬영용 즉시 소환.</summary>
        static void FillWallet(Battle.BattlefieldController ctrl)
        {
            var tp = typeof(Battle.BattlefieldController);
            var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var cost = tp.GetField("cost", f); var max = tp.GetField("costMax", f);
            if (ctrl != null && cost != null && max != null) cost.SetValue(ctrl, max.GetValue(ctrl));
        }

        // ─────────── 조작 도우미 ───────────

        IEnumerator Go(ScreenId id, float settle)
        {
            ScreenRouter.I.Show(id);
            yield return new WaitForSecondsRealtime(settle);
        }

        static void Click(string objectName)
        {
            var go = GameObject.Find(objectName);
            var b = go != null ? go.GetComponent<Button>() : null;
            if (b != null) b.onClick.Invoke();
            else Debug.LogWarning("[ScreenshotTour] 버튼 없음: " + objectName);
        }

        static void ClosePopup()
        {
            var go = GameObject.Find("PopupOverlay");
            var b = go != null ? go.GetComponent<Button>() : null;
            if (b != null) b.onClick.Invoke();
        }

        /// <summary>배치 화면 보유 목록의 첫 카드(드래그 가능한 이미지) — 슬롯 안 카드는 제외.</summary>
        static GameObject FindListDragCard()
        {
            var grid = GameObject.Find("Grid");
            if (grid == null) return null;
            foreach (var h in grid.GetComponentsInChildren<IBeginDragHandler>())
                return ((MonoBehaviour)h).gameObject;
            return null;
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

        /// <summary>전투 배속(gameSpeed 필드) 직접 설정 — 촬영용 빨리감기/정속.</summary>
        static void SetSpeed(Battle.BattlefieldController ctrl, int speed)
        {
            var f = typeof(Battle.BattlefieldController).GetField("gameSpeed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null && ctrl != null) f.SetValue(ctrl, speed);
            var apply = typeof(Battle.BattlefieldController).GetMethod("ApplySpeed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (apply != null && ctrl != null) apply.Invoke(ctrl, null); else Time.timeScale = speed;
        }


        static List<Battle.Unit> Party(Battle.BattlefieldController ctrl, string field) =>
            (List<Battle.Unit>)typeof(Battle.BattlefieldController)
                .GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(ctrl);

        /// <summary>소환 바의 버튼(출전 편성에 따라 이름이 달라지므로 접두사로 찾는다) — 앞에서 count 개만.</summary>
        static void ClickSpawnButtons(int count = 99)
        {
            int n = 0;
            foreach (var b in FindObjectsByType<Button>(FindObjectsSortMode.None).OrderBy(b => b.transform.position.x))
                if (b.name.StartsWith("Spawn_") && b.interactable && n++ < count) b.onClick.Invoke();
        }

        IEnumerator Shot(string name)
        {
            ScreenCapture.CaptureScreenshot(Dir + "/" + name + ".png");
            yield return new WaitForSecondsRealtime(0.8f); // 비동기 저장 대기
        }
    }
}
#endif
