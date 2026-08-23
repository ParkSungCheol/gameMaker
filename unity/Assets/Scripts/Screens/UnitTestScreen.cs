using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameMaker.Battle;
using GameMaker.Core;
using GameMaker.Data;
using GameMaker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameMaker.Screens
{
    /// <summary>
    /// [DEV] 유닛 뷰어 — 모든 아군/적군의 모션(걷기/공격/사망)을 스테이지에 들어가지 않고 한눈에 확인.
    /// - 상단 탭: 아군 / 적군, 모션 버튼: 걷기/공격/사망 (전체 일괄 적용)
    /// - 유닛 클릭: 해당 유닛만 걷기→공격→사망 순환
    /// - ◀ ▶ 페이지 넘김. Core.Dev.UnitViewer 가 true 일 때만 메인 메뉴에 진입 버튼 노출.
    /// </summary>
    public class UnitTestScreen : MonoBehaviour
    {
        const int Cols = 8, Rows = 3;

        /// <summary>한 페이지 = 한 그룹 (아군: 등급, 적군: 테마)</summary>
        class Group
        {
            public string title;
            public List<MonsterData> units;
        }

        static readonly string[] TierNames = { "기본", "일반", "고급", "희귀", "영웅", "전설" };

        Canvas canvas;
        RectTransform grid;
        Text pageText;
        readonly List<Button> actionButtons = new List<Button>();
        readonly List<Button> tabButtons = new List<Button>();

        List<Group> allyGroups, enemyGroups;
        bool showingEnemies = true;
        int page;
        string globalAction = "idle"; // 기본: 가만히 서 있기

        void Start()
        {
            var all = DataHub.I.GetMonsters().Where(m => !m.IsCastle).ToList();

            // 아군: 뽑기 등급별 그룹 (0 기본 ~ 5 전설)
            allyGroups = new List<Group>();
            for (int t = 0; t < TierNames.Length; t++)
            {
                var units = all.Where(m => m.IsOur && m.tier == t).ToList();
                if (units.Count > 0)
                    allyGroups.Add(new Group { title = TierNames[t], units = units });
            }

            // 적군: 테마별 그룹, 테마 안에서는 서브스테이지 순
            enemyGroups = new List<Group>();
            foreach (var g in all.Where(m => !m.IsOur).GroupBy(m => m.stage / 10).OrderBy(x => x.Key))
            {
                var stage = DataHub.I.GetStage(g.Key);
                enemyGroups.Add(new Group
                {
                    title = "테마 " + g.Key + " · " + (stage != null ? stage.label : ""),
                    units = g.OrderBy(m => m.stage).ToList(),
                });
            }

            canvas = Ui.CreateCanvas(transform, "UnitTestCanvas");
            MenuBackdrop.Build(this, canvas, dim: 0.55f, withGround: false);

            var title = Ui.OutlinedLabel(canvas.transform, "유닛 뷰어 (테스트)", 48, Color.white, "Title");
            Ui.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -50));

            var back = Ui.CircleIconButton(canvas.transform, "icon_return", 92,
                () => ScreenRouter.I.Show(ScreenId.Main), "BackButton");
            Ui.Place((RectTransform)back.transform, new Vector2(1f, 0f), new Vector2(-58, 50));

            // 좌상단: 아군/적군 탭
            MakeTab("아군", new Vector2(90, -50), () => { showingEnemies = false; page = 0; Rebuild(); });
            MakeTab("적군", new Vector2(230, -50), () => { showingEnemies = true; page = 0; Rebuild(); });

            // 우상단: 모션 버튼 (전체 적용) — 기본은 대기
            MakeAction("대기", "idle", new Vector2(-700, -50));
            MakeAction("걷기", "move", new Vector2(-560, -50));
            MakeAction("공격", "attack", new Vector2(-420, -50));
            MakeAction("사망", "defeat", new Vector2(-280, -50));

            // 페이지 넘김
            var prev = Ui.CircleIconButton(canvas.transform, "icon_return", 64,
                () => { page = (page + PageCount - 1) % PageCount; Rebuild(); }, "PrevPage");
            Ui.Place((RectTransform)prev.transform, new Vector2(0.5f, 0f), new Vector2(-140, 55));
            var next = Ui.CircleIconButton(canvas.transform, "icon_return", 64,
                () => { page = (page + 1) % PageCount; Rebuild(); }, "NextPage");
            ((RectTransform)next.transform).localScale = new Vector3(-1f, 1f, 1f);
            Ui.Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(140, 55));
            pageText = Ui.OutlinedLabel(canvas.transform, "", 34, Color.white, "Page");
            Ui.Place((RectTransform)pageText.transform, new Vector2(0.5f, 0f), new Vector2(0, 55), new Vector2(640, 44));

            grid = Ui.Panel(canvas.transform, new Color(0, 0, 0, 0), "Grid");
            Ui.Place(grid, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1760, 760));

            Rebuild();
        }

        List<Group> Current => showingEnemies ? enemyGroups : allyGroups;
        int PageCount => Mathf.Max(1, Current.Count);

        void MakeTab(string label, Vector2 pos, System.Action onClick)
        {
            var b = Ui.TextButton(canvas.transform, label, 30, new Vector2(120, 62),
                () => onClick(), new Color(0.35f, 0.3f, 0.22f), label + "Tab");
            Ui.Place((RectTransform)b.transform, new Vector2(0f, 1f), pos);
            tabButtons.Add(b);
        }

        void RefreshTabColors()
        {
            // [0]=아군, [1]=적군 — 선택된 탭 노란색 강조
            for (int i = 0; i < tabButtons.Count; i++)
                tabButtons[i].GetComponent<Image>().color = (i == 1) == showingEnemies
                    ? new Color(0.95f, 0.75f, 0.25f)
                    : new Color(0.35f, 0.3f, 0.22f);
        }

        void MakeAction(string label, string action, Vector2 pos)
        {
            Button b = null;
            b = Ui.TextButton(canvas.transform, label, 30, new Vector2(120, 62), () =>
            {
                globalAction = action;
                RefreshActionColors();
                foreach (var cell in grid.GetComponentsInChildren<UnitMotionCell>())
                    cell.SetAction(action);
            }, new Color(0.25f, 0.35f, 0.25f), label + "Btn");
            Ui.Place((RectTransform)b.transform, new Vector2(1f, 1f), pos);
            actionButtons.Add(b);
        }

        void RefreshActionColors()
        {
            string[] acts = { "idle", "move", "attack", "defeat" };
            for (int i = 0; i < actionButtons.Count && i < acts.Length; i++)
                actionButtons[i].GetComponent<Image>().color = acts[i] == globalAction
                    ? new Color(0.95f, 0.75f, 0.25f)
                    : new Color(0.25f, 0.35f, 0.25f);
        }

        void Rebuild()
        {
            foreach (Transform c in grid) Destroy(c.gameObject);
            RefreshActionColors();
            RefreshTabColors();

            var group = Current[Mathf.Clamp(page, 0, Current.Count - 1)];
            var list = group.units;
            pageText.text = group.title + "  " + (page + 1) + "/" + PageCount + "  (" + list.Count + "종)";

            float cellW = 1760f / Cols, cellH = 760f / Rows;
            for (int i = 0; i < Mathf.Min(Cols * Rows, list.Count); i++)
            {
                var m = list[i];
                var slot = new GameObject("Cell_" + m.name);
                slot.transform.SetParent(grid, false);
                var rt = slot.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2((i % Cols) * cellW, -(i / Cols) * cellH);
                rt.sizeDelta = new Vector2(cellW, cellH);

                var cell = slot.AddComponent<UnitMotionCell>();
                cell.Init(m, globalAction, cellW, cellH);
            }
        }
    }

    /// <summary>유닛 1칸: 스프라이트 모션 재생 + 클릭 시 걷기→공격→사망 순환.</summary>
    public class UnitMotionCell : MonoBehaviour
    {
        static readonly string[] Cycle = { "idle", "move", "attack", "defeat" };

        MonsterData data;
        Image img;
        Text label;
        string action;
        Sprite[] frames;
        float timer;
        int frame;
        float holdUntil; // 사망 모션 후 잠깐 멈췄다 재시작

        public void Init(MonsterData m, string startAction, float cellW, float cellH)
        {
            data = m;

            // 배경 카드 (클릭 영역)
            var card = Ui.RoundedPanel(transform, new Color(1f, 1f, 1f, 0.08f), "Card");
            Ui.Place((RectTransform)card.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(cellW - 10, cellH - 10));
            var btn = card.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(CycleAction);

            img = Ui.Image(card.transform, null, "Sprite");
            img.raycastTarget = false;
            img.preserveAspect = true;
            // 유닛 크기 비례 표시 (셀 안에서 상대 크기 유지, 너무 작지 않게)
            float h = Mathf.Clamp(m.height * 0.42f, 55f, cellH - 70f);
            Ui.Place((RectTransform)img.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 12),
                new Vector2(cellW - 30, h));
            // 적군은 전투 화면과 같은 방향(왼쪽 보기)으로
            bool flip = m.facing == "left" ? m.IsOur : !m.IsOur;
            img.transform.localScale = new Vector3(flip ? -1f : 1f, 1f, 1f);

            // 한글명 (크게) + 내부 id (작게 — 수정 요청 시 함께 확인용)
            label = Ui.Label(card.transform, m.DisplayName, 22, new Color(1f, 1f, 1f, 0.95f), "Name");
            label.alignment = TextAnchor.MiddleCenter;
            Ui.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(cellW - 12, 28));
            // 적군은 소속 스테이지(테마-서브)도 함께 — 수정 요청 시 특정용
            string idStr = m.stage > 0 ? (m.stage / 10) + "-" + (m.stage % 10) + " · " + m.name : m.name;
            var idText = Ui.Label(card.transform, idStr, 14, new Color(1f, 1f, 1f, 0.45f), "Id");
            idText.alignment = TextAnchor.MiddleCenter;
            Ui.Place(idText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 10), new Vector2(cellW - 12, 20));

            SetAction(startAction);
        }

        public void SetAction(string a)
        {
            action = a;
            // "idle" = 걷기 첫 프레임에서 정지 (전용 idle 프레임은 없음)
            frames = SpriteBank.GetFrames(data.SpriteName, a == "idle" ? "move" : a);
            frame = 0;
            timer = 0;
            holdUntil = 0;
            if (frames.Length > 0) img.sprite = frames[0];
        }

        void CycleAction()
        {
            int i = System.Array.IndexOf(Cycle, action);
            SetAction(Cycle[(i + 1) % Cycle.Length]);
        }

        void Update()
        {
            if (action == "idle") return; // 대기: 정지 화면
            if (frames == null || frames.Length == 0) return;
            if (Time.unscaledTime < holdUntil) return;

            timer += Time.unscaledDeltaTime;
            if (timer < 1f / 10f) return; // 10fps — 전투(SimpleSpriteAnimator 기본값)와 동일
            timer = 0;
            frame++;
            if (frame >= frames.Length)
            {
                frame = 0;
                if (action == "defeat") holdUntil = Time.unscaledTime + 0.6f; // 쓰러진 모습 잠깐 유지
            }
            img.sprite = frames[frame];
        }
    }
}
