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
            // 좌우 버튼은 페이지 라벨(폭 640) 바깥에 배치 — 라벨과 겹치면 클릭이 막힌다
            var prev = Ui.CircleIconButton(canvas.transform, "icon_return", 64,
                () => { page = (page + PageCount - 1) % PageCount; Rebuild(); }, "PrevPage");
            Ui.Place((RectTransform)prev.transform, new Vector2(0.5f, 0f), new Vector2(-390, 55));
            var next = Ui.CircleIconButton(canvas.transform, "icon_return", 64,
                () => { page = (page + 1) % PageCount; Rebuild(); }, "NextPage");
            ((RectTransform)next.transform).localScale = new Vector3(-1f, 1f, 1f);
            Ui.Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(390, 55));
            pageText = Ui.OutlinedLabel(canvas.transform, "", 34, Color.white, "Page");
            pageText.raycastTarget = false; // 라벨이 아래 버튼 클릭을 가로채지 않게
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

    /// <summary>유닛 1칸 — 전투(Unit.cs)와 같은 규칙·수치로 모션 재현.
    /// 걷기: 밥(bob)/비행 부유, 공격: 공격주기(진입 즉시 첫 타)·근접 아키타입(런지/덮치기/들이받기/내려찍기)·
    /// 발사체(화살/탄환/돌/구슬)·마법사 메테오·보스 3타째 내리찍기+충격파.
    /// 사망만 소멸 대신 쓰러진 모습을 유지했다 반복. 좌표 오프셋은 전투 픽셀 × k(칸 배율).</summary>
    public class UnitMotionCell : MonoBehaviour
    {
        static readonly string[] Cycle = { "idle", "move", "attack", "defeat" };
        static Sprite[] burstFrames;

        MonsterData data;
        RectTransform card;   // 이펙트 부모 — RectMask2D 로 옆 칸 침범 방지
        RectTransform body;
        Image img;
        string action;
        Sprite[] frames;
        float timer;
        int frame;
        float holdUntil;

        float k;              // 전투 픽셀 → 칸 픽셀 배율
        float th;             // 전투 targetHeight 의 칸 스케일 값 (= 표시 높이)
        float unitScale;      // 스프라이트 월드 크기 → 칸 픽셀 (걷기 첫 프레임 기준 고정)
        Vector2 basePos;
        float sign;           // 좌우 반전 부호 (-1 = 왼쪽 보기)
        float walkPhase;
        float footY;          // 발밑 y — 지면 이펙트 기준
        int strikeCount;
        readonly List<GameObject> fxList = new List<GameObject>();

        public void Init(MonsterData m, string startAction, float cellW, float cellH)
        {
            data = m;

            // 배경 카드 (클릭 영역 + 이펙트 클리핑)
            var cardPanel = Ui.RoundedPanel(transform, new Color(1f, 1f, 1f, 0.08f), "Card");
            card = (RectTransform)cardPanel.transform;
            Ui.Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cellW - 10, cellH - 10));
            cardPanel.gameObject.AddComponent<RectMask2D>();
            var btn = cardPanel.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(CycleAction);

            img = Ui.Image(card, null, "Sprite");
            img.raycastTarget = false;
            // 유닛 크기 비례 표시 (셀 안에서 상대 크기 유지, 너무 작지 않게)
            float h = Mathf.Clamp(m.height * 0.42f, 55f, cellH - 70f);
            body = (RectTransform)img.transform;
            // 지상 유닛은 카드 하단 지면선에 발을 붙이고, 비행 유닛은 중앙에 떠 있게 —
            // 뷰어에서 지상/공중이 한눈에 구분된다
            bool flying = m.fly > 0f;
            float groundLine = -(cellH - 10) / 2f + 58f; // 라벨 바로 위 = 지면선
            basePos = flying ? new Vector2(0, 12) : new Vector2(0, groundLine + h * 0.5f);
            Ui.Place(body, new Vector2(0.5f, 0.5f), basePos, new Vector2(h, h));
            // 전투와 동일하게 "걷기 첫 프레임 기준 고정 배율"로 모든 프레임 표시 —
            // 프레임마다 사각형에 맞춰 늘리면 사망(눕기) 프레임이 갑자기 커진다
            var mv0 = SpriteBank.GetFrames(m.SpriteName, "move");
            unitScale = mv0.Length > 0 ? h / Mathf.Max(mv0[0].bounds.size.y, 0.01f) : h;
            k = h / Mathf.Max(m.height, 1f);
            th = h;
            footY = flying ? basePos.y - h * 0.5f : groundLine;
            // 적군은 전투 화면과 같은 방향(왼쪽 보기)으로
            bool flip = m.facing == "left" ? m.IsOur : !m.IsOur;
            sign = flip ? -1f : 1f;
            body.localScale = new Vector3(sign, 1f, 1f);
            // 전투와 동일: 위치 기반 걸음 위상 (유닛마다 다르게)
            walkPhase = (((RectTransform)transform).anchoredPosition.x + basePos.x) * 0.037f;

            // 한글명 (크게) + 내부 id (작게 — 수정 요청 시 함께 확인용)
            var label = Ui.Label(card, m.DisplayName, 22, new Color(1f, 1f, 1f, 0.95f), "Name");
            label.alignment = TextAnchor.MiddleCenter;
            Ui.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(cellW - 12, 28));
            // 적군은 소속 스테이지(테마-서브)도 함께 — 수정 요청 시 특정용
            string idStr = m.stage > 0 ? (m.stage / 10) + "-" + (m.stage % 10) + " · " + m.name : m.name;
            var idText = Ui.Label(card, idStr, 14, new Color(1f, 1f, 1f, 0.45f), "Id");
            idText.alignment = TextAnchor.MiddleCenter;
            Ui.Place(idText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 10), new Vector2(cellW - 12, 20));

            SetAction(startAction);
        }

        public void SetAction(string a)
        {
            StopAllCoroutines();
            foreach (var fx in fxList) if (fx != null) Destroy(fx);
            fxList.Clear();
            ResetBody();

            action = a;
            strikeCount = 0;
            // "idle" = 걷기 첫 프레임에서 정지 (전용 idle 프레임은 없음)
            frames = SpriteBank.GetFrames(data.SpriteName, a == "attack" || a == "idle" ? "move" : a);
            frame = 0;
            timer = 0;
            holdUntil = 0;
            if (frames.Length > 0) SetSprite(frames[0]);
            if (a == "attack") StartCoroutine(AttackLoop());
            if (a == "defeat") StartCoroutine(Soul()); // 전투와 동일: 죽는 순간 영혼이 떠오른다
        }

        void ResetBody()
        {
            if (body == null) return;
            body.localRotation = Quaternion.identity;
            body.localScale = new Vector3(sign, 1f, 1f);
            body.anchoredPosition = basePos;
        }

        void CycleAction()
        {
            int i = System.Array.IndexOf(Cycle, action);
            SetAction(Cycle[(i + 1) % Cycle.Length]);
        }

        void Update()
        {
            if (frames == null || frames.Length == 0) return;

            if (action == "move")
            {
                StepFrame(true);
                // 전투 MoveForward 와 동일: 비행만 부유, 지상은 발이 땅에 붙은 채 프레임에 맡긴다
                if (data.fly > 0f)
                {
                    float ft = Time.unscaledTime * 2.6f + walkPhase;
                    body.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(ft * 0.7f) * 3f);
                    body.localScale = new Vector3(sign, 1f, 1f);
                    body.anchoredPosition = basePos + new Vector2(0, Mathf.Sin(ft) * 14f * k);
                }
            }
            else if (action == "defeat")
            {
                if (Time.unscaledTime < holdUntil) return;
                timer += Time.unscaledDeltaTime;
                if (timer < 0.1f) return; // 10fps — 전투(SimpleSpriteAnimator 기본값)와 동일
                timer = 0;
                frame++;
                if (frame == 0) StartCoroutine(Soul()); // 반복 재생 시작마다 영혼 연출
                if (frame >= frames.Length)
                {
                    // 전투에선 여기서 유닛이 소멸 — 뷰어는 쓰러진 모습 유지 후 반복
                    frame = -1;
                    holdUntil = Time.unscaledTime + 1.2f;
                    return;
                }
                SetSprite(frames[frame]);
            }
            // idle: 정지 / attack: 코루틴이 스프라이트·모션을 전담
        }

        void StepFrame(bool loop)
        {
            timer += Time.unscaledDeltaTime;
            if (timer < 0.1f) return;
            timer = 0;
            frame = loop ? (frame + 1) % frames.Length : Mathf.Min(frame + 1, frames.Length - 1);
            SetSprite(frames[frame]);
        }

        /// <summary>스프라이트 교체 — 크기는 항상 고정 배율(unitScale)로, 전투의 bodyScale 과 동일 규칙.
        /// 지상 유닛은 프레임 높이가 달라도(슬라임 꿀렁/눕기) 발바닥을 지면선에 고정.
        /// 공격 중에는 모션 코루틴이 위치를 움직이므로 건드리지 않는다.</summary>
        void SetSprite(Sprite s)
        {
            img.sprite = s;
            body.sizeDelta = (Vector2)s.bounds.size * unitScale;
            if (data.fly <= 0f && action != "attack")
                body.anchoredPosition = new Vector2(basePos.x, footY + body.sizeDelta.y * 0.5f);
        }

        // ─────────── 공격 재현 (Unit.cs Strike 계열과 동일 수치) ───────────

        IEnumerator AttackLoop()
        {
            float atkTimer = data.attackInterval; // 사거리 진입 즉시 첫 타 (전투와 동일)
            while (true)
            {
                atkTimer += Time.unscaledDeltaTime;
                if (data.attackInterval > 0 && atkTimer >= data.attackInterval)
                {
                    atkTimer = 0f;
                    yield return Strike();
                }
                yield return null;
            }
        }

        IEnumerator Strike()
        {
            strikeCount++;
            StartCoroutine(PlayAttackAnim());
            if (data.name == "ourmass") yield return Meteor();
            else if (data.aoe > 0 && strikeCount % 3 == 0) yield return BossSlam();
            else if (data.range >= 200 || !string.IsNullOrEmpty(data.projectile)) yield return Projectile();
            else if (data.melee == "pounce") yield return Pounce();
            else if (data.melee == "ram") yield return Ram();
            else if (data.melee == "stomp") yield return Stomp();
            else yield return StyleMotion();
        }

        /// <summary>공격 스타일별 몸 움직임 — 전투(Unit.StyleMotion)와 동일 수치.</summary>
        IEnumerator StyleMotion()
        {
            float fw = -Dir;
            IEnumerator Phase(float dur, System.Action<float> f)
            {
                float t = 0f;
                while (t < dur)
                {
                    t += Time.unscaledDeltaTime;
                    f(Mathf.Clamp01(t / dur));
                    yield return null;
                }
            }
            void Pose(float x, float y, float rot, float sx, float sy)
            {
                body.anchoredPosition = basePos + new Vector2(x * th * Dir, y * th);
                body.localRotation = Quaternion.Euler(0, 0, rot * fw);
                body.localScale = new Vector3(sign * sx, sy, 1f);
            }

            switch (data.atkStyle)
            {
                case "spin":
                    yield return Phase(0.32f, k => Pose(0.1f * Mathf.Sin(k * Mathf.PI), 0, 360f * k, 1, 1));
                    break;
                case "flurry":
                    yield return Phase(0.42f, k => Pose(0.16f * Mathf.Abs(Mathf.Sin(k * Mathf.PI * 3f)), 0, 5f * Mathf.Sin(k * Mathf.PI * 6f), 1, 1));
                    break;
                case "bite":
                    yield return Phase(0.1f, k => Pose(-0.05f * k, 0, -6f * k, 1f, 1f - 0.1f * k));
                    yield return Phase(0.09f, k => Pose(-0.05f + 0.27f * k, 0, -6f + 20f * k, 1f + 0.06f * k, 0.9f + 0.1f * k));
                    yield return Phase(0.14f, k => Pose(0.22f * (1f - k), 0, 14f * (1f - k), 1, 1));
                    break;
                case "peck":
                    yield return Phase(0.08f, k => Pose(-0.04f * k, 0.03f * k, -12f * k, 1, 1));
                    yield return Phase(0.07f, k => Pose(-0.04f + 0.18f * k, 0.03f - 0.06f * k, -12f + 34f * k, 1, 1));
                    yield return Phase(0.12f, k => Pose(0.14f * (1f - k), -0.03f * (1f - k), 22f * (1f - k), 1, 1));
                    break;
                case "horn":
                    yield return Phase(0.12f, k => Pose(-0.08f * k, -0.04f * k, 10f * k, 1, 1));
                    yield return Phase(0.09f, k => Pose(-0.08f + 0.26f * k, -0.04f + 0.1f * k, 10f - 26f * k, 1, 1));
                    yield return Phase(0.14f, k => Pose(0.18f * (1f - k), 0.06f * (1f - k), -16f * (1f - k), 1, 1));
                    break;
                case "buck":
                    yield return Phase(0.1f, k => Pose(0.05f * k, 0, 14f * k, 1, 1));
                    yield return Phase(0.08f, k => Pose(0.05f - 0.2f * k, 0.04f * k, 14f - 34f * k, 1, 1));
                    yield return Phase(0.14f, k => Pose(-0.15f * (1f - k), 0.04f * (1f - k), -20f * (1f - k), 1, 1));
                    break;
                case "trample":
                    yield return Phase(0.13f, k => Pose(0, 0.2f * k, -4f * k, 0.96f, 1f + 0.08f * k));
                    yield return Phase(0.07f, k => Pose(0.04f * k, 0.2f * (1f - k), -4f + 8f * k, 0.96f + 0.1f * k, 1.08f - 0.24f * k));
                    yield return Phase(0.13f, k => Pose(0.04f * (1f - k), 0, 4f * (1f - k), Mathf.Lerp(1.06f, 1f, k), Mathf.Lerp(0.84f, 1f, k)));
                    break;
                case "squash":
                    yield return Phase(0.12f, k => Pose(0, -0.05f * k, 0, 1f + 0.16f * k, 1f - 0.22f * k));
                    yield return Phase(0.09f, k => Pose(0.14f * k, -0.05f + 0.09f * k, 5f * k, 1.16f - 0.22f * k, 0.78f + 0.32f * k));
                    yield return Phase(0.13f, k => Pose(0.14f * (1f - k), 0.04f * (1f - k), 5f * (1f - k), Mathf.Lerp(0.94f, 1f, k), Mathf.Lerp(1.1f, 1f, k)));
                    break;
                case "flap":
                    yield return Phase(0.14f, k => Pose(-0.03f * k, 0.24f * k, -8f * k, 1, 1));
                    yield return Phase(0.1f, k => Pose(-0.03f + 0.23f * k, 0.24f * (1f - k), -8f + 20f * k, 1, 1));
                    yield return Phase(0.13f, k => Pose(0.2f * (1f - k), 0, 12f * (1f - k), 1, 1));
                    break;
                case "cast":
                    yield return Phase(0.16f, k => Pose(0, 0.06f * k, -4f * k, 1f - 0.06f * k, 1f + 0.04f * k));
                    yield return Phase(0.09f, k => Pose(0, 0.06f * (1f - k), -4f + 6f * k, Mathf.Lerp(0.94f, 1.12f, k), Mathf.Lerp(1.04f, 1.08f, k)));
                    yield return Phase(0.13f, k => Pose(0, 0, 2f * (1f - k), Mathf.Lerp(1.12f, 1f, k), Mathf.Lerp(1.08f, 1f, k)));
                    break;
                case "swing":
                    yield return Phase(0.12f, k => Pose(-0.06f * k, 0, -16f * k, 1, 1));
                    yield return Phase(0.09f, k => Pose(-0.06f + 0.26f * k, 0, -16f + 42f * k, 1, 1));
                    yield return Phase(0.15f, k => Pose(0.2f * (1f - k), 0, 26f * (1f - k), 1, 1));
                    break;
                default:
                    yield return Lunge();
                    yield break;
            }
            Pose(0, 0, 0, 1, 1);
        }

        IEnumerator PlayAttackAnim()
        {
            var af = SpriteBank.GetFrames(data.SpriteName, "attack");
            for (int i = 0; i < af.Length; i++)
            {
                SetSprite(af[i]);
                yield return new WaitForSecondsRealtime(0.1f);
            }
            // 전투 AttackTick 과 동일: 다음 공격까지 대기 자세(걷기 첫 프레임)
            var mv = SpriteBank.GetFrames(data.SpriteName, "move");
            if (mv.Length > 0) SetSprite(mv[0]);
        }

        float Dir => data.IsOur ? 1f : -1f;

        IEnumerator Lunge()
        {
            float dist = th * 0.22f;
            float t = 0f;
            while (t < 0.22f)
            {
                t += Time.unscaledDeltaTime;
                float p = t < 0.08f ? t / 0.08f : 1f - (t - 0.08f) / 0.14f;
                body.anchoredPosition = basePos + new Vector2(Dir * dist * Mathf.Clamp01(p), 0);
                yield return null;
            }
            body.anchoredPosition = basePos;
        }

        IEnumerator Pounce()
        {
            float dist = th * 0.45f, rise = th * 0.4f, dur = 0.28f, t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / dur);
                body.anchoredPosition = basePos + new Vector2(Dir * dist * p, rise * 4f * p * (1f - p));
                yield return null;
            }
            t = 0f;
            while (t < 0.12f)
            {
                t += Time.unscaledDeltaTime;
                body.anchoredPosition = basePos + new Vector2(Dir * dist * (1f - Mathf.Clamp01(t / 0.12f)), 0);
                yield return null;
            }
            body.anchoredPosition = basePos;
        }

        IEnumerator Ram()
        {
            float back = th * 0.12f, dist = th * 0.6f, t = 0f;
            while (t < 0.12f) // 움츠리기
            {
                t += Time.unscaledDeltaTime;
                body.anchoredPosition = basePos + new Vector2(-Dir * back * Mathf.Clamp01(t / 0.12f), 0);
                yield return null;
            }
            t = 0f;
            while (t < 0.08f) // 폭발적 돌진
            {
                t += Time.unscaledDeltaTime;
                body.anchoredPosition = basePos + new Vector2(Mathf.Lerp(-Dir * back, Dir * dist, Mathf.Clamp01(t / 0.08f)), 0);
                yield return null;
            }
            t = 0f;
            while (t < 0.16f) // 복귀
            {
                t += Time.unscaledDeltaTime;
                body.anchoredPosition = basePos + new Vector2(Dir * dist * (1f - Mathf.Clamp01(t / 0.16f)), 0);
                yield return null;
            }
            body.anchoredPosition = basePos;
        }

        IEnumerator Stomp()
        {
            float rise = th * 0.28f, t = 0f;
            while (t < 0.18f)
            {
                t += Time.unscaledDeltaTime;
                body.anchoredPosition = basePos + new Vector2(0, rise * Mathf.Clamp01(t / 0.18f));
                yield return null;
            }
            t = 0f;
            while (t < 0.07f)
            {
                t += Time.unscaledDeltaTime;
                body.anchoredPosition = basePos + new Vector2(0, rise * (1f - Mathf.Clamp01(t / 0.07f)));
                yield return null;
            }
            body.anchoredPosition = basePos;
            StartCoroutine(ShockRing(new Vector2(basePos.x + Dir * 60f * k, footY + 22f * k), th * 0.5f));
        }

        IEnumerator BossSlam()
        {
            float centerX = basePos.x + Dir * 200f * k; // 전투: 타겟 위치
            float rise = th * 0.45f, t = 0f;
            while (t < 0.22f) // 떠오르기
            {
                t += Time.unscaledDeltaTime;
                body.anchoredPosition = basePos + new Vector2(0, rise * Mathf.Sin(Mathf.Clamp01(t / 0.22f) * Mathf.PI * 0.5f));
                yield return null;
            }
            t = 0f;
            while (t < 0.09f) // 내리찍기
            {
                t += Time.unscaledDeltaTime;
                body.anchoredPosition = basePos + new Vector2(0, rise * (1f - Mathf.Clamp01(t / 0.09f)));
                yield return null;
            }
            body.anchoredPosition = basePos;
            StartCoroutine(ShockRing(new Vector2(centerX, footY + 30f * k), data.aoe * k));
        }

        IEnumerator Projectile()
        {
            string kind = string.IsNullOrEmpty(data.projectile) ? "arrow" : data.projectile;
            float dur = 0.16f, arc = 0f;
            Image p;
            switch (kind)
            {
                case "bullet":
                    p = Fx(SpriteBank.Circle, new Color(1f, 0.9f, 0.4f), new Vector2(18f, 10f) * k, "Bullet");
                    dur = 0.07f;
                    break;
                case "rock":
                    p = Fx(SpriteBank.Circle, new Color(0.55f, 0.52f, 0.48f), new Vector2(35f, 32f) * k, "Rock");
                    dur = 0.34f;
                    arc = 140f;
                    break;
                case "orb":
                    p = Fx(SpriteBank.Circle, new Color(0.75f, 0.45f, 1f), new Vector2(32f, 32f) * k, "Orb");
                    var glow = Fx(SpriteBank.Circle, new Color(0.6f, 0.3f, 1f, 0.4f), new Vector2(54f, 54f) * k, "Glow");
                    glow.transform.SetParent(p.transform, false);
                    glow.rectTransform.anchoredPosition = Vector2.zero;
                    dur = 0.24f;
                    arc = 60f;
                    break;
                default: // arrow
                    p = Fx(SpriteBank.Arrow, Color.white, SpriteBank.Arrow.bounds.size * 3.2f * k, "Arrow");
                    break;
            }

            // 몸 중심에서 사거리만큼 앞의 가상 타겟으로 (전투와 동일 비례)
            Vector2 from = basePos + new Vector2(0, th * 0.1f);
            Vector2 to = new Vector2(basePos.x + Dir * Mathf.Max(200f, data.range) * k, basePos.y);
            var d = to - from;
            if (kind == "arrow" || kind == "bullet")
                p.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);

            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                if (p == null) yield break;
                float pr = Mathf.Clamp01(t / dur);
                var pos = Vector2.Lerp(from, to, pr);
                pos.y += arc * k * 4f * pr * (1f - pr); // 포물선 궤적
                p.rectTransform.anchoredPosition = pos;
                if (kind == "rock") p.rectTransform.Rotate(0, 0, 620f * Time.unscaledDeltaTime);
                yield return null;
            }
            if (p != null) Destroy(p.gameObject);
            if (kind == "orb") SpawnBurst(to); // 마법구: 착탄 이펙트
        }

        IEnumerator Meteor()
        {
            float tx = basePos.x + Dir * 200f * k; // 전투: 타겟 위치
            var m = Fx(SpriteBank.Circle, new Color(1f, 0.55f, 0.15f), new Vector2(96f, 154f) * k, "Meteor");
            var glow = Fx(SpriteBank.Circle, new Color(1f, 0.35f, 0.08f, 0.4f), new Vector2(144f, 215f) * k, "Glow");
            glow.transform.SetParent(m.transform, false);
            glow.rectTransform.anchoredPosition = Vector2.zero;

            Vector2 from = new Vector2(tx + 70f * k, footY + 780f * k);
            Vector2 to = new Vector2(tx, footY + 40f * k);
            var dir = to - from;
            m.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f);

            float dur = 0.3f, t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                if (m == null) yield break;
                float pr = t / dur;
                m.rectTransform.anchoredPosition = Vector2.Lerp(from, to, pr * pr); // 가속 낙하
                yield return null;
            }
            if (m != null) Destroy(m.gameObject);
            SpawnBurst(to);
        }

        IEnumerator ShockRing(Vector2 center, float radius)
        {
            var ring = Fx(SpriteBank.Circle, new Color(1f, 0.75f, 0.35f, 0.55f), Vector2.zero, "ShockRing");
            ring.rectTransform.anchoredPosition = center;
            float dur = 0.32f, t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                if (ring == null) yield break;
                float p = Mathf.Clamp01(t / dur);
                float dm = radius * 2f * (0.25f + 0.75f * p);
                ring.rectTransform.sizeDelta = new Vector2(dm, dm * 0.38f); // 납작한 지면 링
                ring.color = new Color(1f, 0.75f, 0.35f, 0.55f * (1f - p));
                yield return null;
            }
            if (ring != null) Destroy(ring.gameObject);
        }

        void SpawnBurst(Vector2 pos)
        {
            if (burstFrames == null)
            {
                var list = new List<Sprite>();
                for (int i = 0; i < 8; i++)
                {
                    var s = Resources.Load<Sprite>("Sprites/fx/magicburst_" + i);
                    if (s == null) break;
                    list.Add(s);
                }
                burstFrames = list.ToArray();
            }
            if (burstFrames.Length == 0) return;

            var b = Fx(burstFrames[0], new Color(1f, 0.6f, 0.2f), Vector2.one * 240f * k, "Burst");
            b.preserveAspect = true;
            b.rectTransform.anchoredPosition = pos;
            StartCoroutine(PlayFxOnce(b, burstFrames, 18f));
        }

        IEnumerator PlayFxOnce(Image i, Sprite[] fr, float fps)
        {
            foreach (var s in fr)
            {
                if (i == null) yield break;
                i.sprite = s;
                yield return new WaitForSecondsRealtime(1f / fps);
            }
            if (i != null) Destroy(i.gameObject);
        }

        /// <summary>사망 영혼 연출 — 같은 모습의 반투명 유령이 떠올라 사라진다 (전투 SoulRise 와 동일 수치).</summary>
        IEnumerator Soul()
        {
            var mv = SpriteBank.GetFrames(data.SpriteName, "move");
            if (mv.Length == 0) yield break;
            var soul = Fx(mv[0], new Color(0.85f, 0.95f, 1f, 0.55f),
                (Vector2)mv[0].bounds.size * unitScale * 0.55f, "Soul");
            soul.rectTransform.localScale = new Vector3(sign, 1f, 1f);
            float t = 0f;
            while (t < 0.9f)
            {
                t += Time.unscaledDeltaTime;
                if (soul == null) yield break;
                float p = t / 0.9f;
                soul.rectTransform.anchoredPosition = basePos +
                    new Vector2(Mathf.Sin(t * 7f) * 10f * k, 170f * k * p);
                soul.color = new Color(0.85f, 0.95f, 1f, 0.55f * (1f - p));
                yield return null;
            }
            Destroy(soul.gameObject);
        }

        Image Fx(Sprite s, Color c, Vector2 size, string n)
        {
            var go = new GameObject(n, typeof(RectTransform));
            go.transform.SetParent(card, false);
            var i = go.AddComponent<Image>();
            i.sprite = s;
            i.color = c;
            i.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            fxList.Add(go);
            return i;
        }
    }
}
