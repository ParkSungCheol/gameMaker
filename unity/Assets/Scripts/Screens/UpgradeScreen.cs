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
    /// 업그레이드 — 야외 훈련소 화면 그대로: 유닛들이 풀밭 위에 크게 서서 걷는다.
    /// 등급(기본~전설)은 상단 ◀▶ 로 전환, 등급 안에서는 가로 스크롤(드래그/휠)로 넘긴다.
    /// - 골드 강화: 나무 버튼(가격), 레벨 10 = MAX 로 잠김 (이후 중복 뽑기 +N 로만)
    /// - 중복 뽑기: 레벨 배지 옆 노란 +N
    /// - 미보유: 검은 실루엣 + 자물쇠 + '뽑기로 획득'
    /// </summary>
    public class UpgradeScreen : MonoBehaviour
    {
        const float Gap = 340f;                 // 유닛 간격 (원래 5칸 화면과 동일)
        static readonly string[] TierNames = { "기본", "일반", "고급", "희귀", "영웅", "전설" };

        class Group { public string title; public List<MonsterData> units; }

        Canvas canvas;
        RectTransform content;
        ScrollRect scroll;
        Text moneyText, tierText, toastText;
        float toastUntil;
        List<Group> groups;
        int page;
        readonly Dictionary<string, Image> upBtnImages = new Dictionary<string, Image>();
        readonly Dictionary<string, int> upBtnPrice = new Dictionary<string, int>();

        void Start()
        {
            canvas = Ui.CreateCanvas(transform, "UpgradeCanvas");
            MenuBackdrop.Build(this, canvas);

            var title = Ui.OutlinedLabel(canvas.transform, "업그레이드", 52, Color.white, "Title");
            Ui.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -55));

            var moneyPanel = Ui.Image(canvas.transform, SpriteBank.GetEnv("panel_parchment"), "MoneyPanel");
            Ui.Place((RectTransform)moneyPanel.transform, new Vector2(0f, 1f), new Vector2(160, -55), new Vector2(280, 82));
            moneyText = Ui.CenteredIconValue(moneyPanel.transform, SpriteBank.GetEnv("icon_coin"),
                "0", 42, Color.white, "Money");
            Ui.Place((RectTransform)moneyText.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0, 2));

            var back = Ui.CircleIconButton(canvas.transform, "icon_return", 92,
                () => ScreenRouter.I.Show(ScreenId.Main), "BackButton");
            Ui.Place((RectTransform)back.transform, new Vector2(1f, 0f), new Vector2(-58, 50));

            // 등급 전환 ◀▶ + 등급명
            var prev = Ui.CircleIconButton(canvas.transform, "icon_return", 64,
                () => { page = (page + groups.Count - 1) % groups.Count; Rebuild(); }, "PrevTier");
            Ui.Place((RectTransform)prev.transform, new Vector2(0.5f, 1f), new Vector2(-260, -135));
            var next = Ui.CircleIconButton(canvas.transform, "icon_return", 64,
                () => { page = (page + 1) % groups.Count; Rebuild(); }, "NextTier");
            ((RectTransform)next.transform).localScale = new Vector3(-1f, 1f, 1f);
            Ui.Place((RectTransform)next.transform, new Vector2(0.5f, 1f), new Vector2(260, -135));
            tierText = Ui.OutlinedLabel(canvas.transform, "", 40, new Color(1f, 0.9f, 0.5f), "TierName");
            tierText.raycastTarget = false;
            Ui.Place((RectTransform)tierText.transform, new Vector2(0.5f, 1f), new Vector2(0, -135), new Vector2(460, 52));

            toastText = Ui.OutlinedLabel(canvas.transform, "", 28, new Color(1f, 0.6f, 0.5f), "Toast");
            toastText.raycastTarget = false;
            Ui.Place((RectTransform)toastText.transform, new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(900, 40));

            // 가로 스크롤 영역 — 화면 전체 높이, 좌우 드래그/휠로 유닛들을 넘긴다
            var viewport = Ui.Panel(canvas.transform, new Color(0, 0, 0, 0), "Viewport");
            Ui.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = Ui.Panel(viewport, new Color(0, 0, 0, 0), "Content");
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            // 뷰포트(투명 전체 패널)가 위 UI 의 클릭을 가로채지 않도록 배경 바로 위 층으로
            viewport.SetSiblingIndex(title.transform.GetSiblingIndex());

            // 하단 가로 스크롤바 — 각진 트랙 + 금색 손잡이, 항상 표시 (페이드 없음)
            var sb = Ui.CleanScrollbar(canvas.transform, true, new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(1200, 16));
            scroll.horizontalScrollbar = sb;
            scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            // 직업(포지션) 안내 — 등급명 오른쪽 ? 버튼
            var helpBtn = Ui.HelpButton(canvas.transform, 64, ShowRoleGuide, "RoleHelp");
            Ui.Place((RectTransform)helpBtn.transform, new Vector2(0.5f, 1f), new Vector2(345, -135));

            // 그룹: 기본(성+기본 4종) + 뽑기 등급 1~5
            var all = DataHub.I.GetMonsters().Where(m => m.IsOur).ToList();
            groups = new List<Group>();
            var basics = all.Where(m => m.IsCastle && m.name == "ourcastle" || (!m.IsCastle && m.tier == 0)).ToList();
            groups.Add(new Group { title = "기본", units = basics });
            for (int t = 1; t <= 5; t++)
            {
                var units = all.Where(m => !m.IsCastle && m.tier == t).ToList();
                if (units.Count > 0) groups.Add(new Group { title = TierNames[t], units = units });
            }

            Rebuild();
        }

        void Update()
        {
            moneyText.text = DataHub.I.GetPlayer().money.ToString();
            int money = DataHub.I.GetPlayer().money;
            foreach (var kv in upBtnImages)
            {
                if (kv.Value == null) continue;
                bool ok = money >= upBtnPrice[kv.Key];
                kv.Value.color = ok ? Color.white : new Color(0.45f, 0.45f, 0.45f);
            }
            if (toastText.text.Length > 0 && Time.time > toastUntil) toastText.text = "";
        }

        void Toast(string msg)
        {
            toastText.text = msg;
            toastUntil = Time.time + 1.6f;
        }

        /// <summary>직업(포지션) 안내 팝업 — 승/패 팝업과 같은 보드.</summary>
        void ShowRoleGuide()
        {
            var board = Ui.Popup(canvas.transform, "포지션 안내", new Vector2(1040, 900));
            string[][] rows = {
                new[] { "탱커",   "근접 · 단일", "체력이 높고 공격은 약하다. 맨 앞에서 적을 막아 뒤를 지킨다." },
                new[] { "전사",   "근접 · 단일", "체력과 공격이 균형. 어디에 넣어도 무난한 근접 주력." },
                new[] { "암살자", "근접 · 단일", "빠르고 공격이 강하지만 체력이 약하다. 먼저 때리고 먼저 죽는 유리칼." },
                new[] { "궁수",   "원거리 · 단일", "멀리서 화살로 한 명씩 정확히. 앞에 탱커가 있어야 산다." },
                new[] { "마법사", "원거리 · 범위", "구슬이 떨어진 자리 주변을 한꺼번에 태운다. 뭉친 적에게 강하다." },
            };
            for (int i = 0; i < rows.Length; i++)
            {
                float y = -130f - i * 150f; // 행 간격 150: 배지 / 타입 / 설명이 서로 안 겹치게
                var rc = RoleColor(rows[i][0]);
                var pill = Ui.RoundedPanel(board, new Color(rc.r, rc.g, rc.b, 0.95f), "Pill" + i);
                Ui.Place((RectTransform)pill.transform, new Vector2(0f, 1f), new Vector2(70, y - 4), new Vector2(130, 46));
                var pt = Ui.Label(pill.transform, rows[i][0], 24, Color.white, "PillText");
                pt.alignment = TextAnchor.MiddleCenter;
                Ui.Stretch(pt.rectTransform);
                var kind = Ui.OutlinedLabel(board, rows[i][1], 22, new Color(1f, 0.85f, 0.45f), "Kind" + i);
                kind.alignment = TextAnchor.MiddleLeft;
                Ui.Place((RectTransform)kind.transform, new Vector2(0f, 1f), new Vector2(230, y), new Vector2(300, 36));
                var body = Ui.OutlinedLabel(board, rows[i][2], 24, Color.white, "Body" + i);
                body.alignment = TextAnchor.UpperLeft;
                body.horizontalOverflow = HorizontalWrapMode.Wrap;
                Ui.Place((RectTransform)body.transform, new Vector2(0f, 1f), new Vector2(230, y - 44), new Vector2(740, 80));
                if (i < rows.Length - 1)
                {
                    var sep = Ui.Panel(board, new Color(1f, 1f, 1f, 0.12f), "Sep" + i);
                    Ui.Place(sep, new Vector2(0f, 1f), new Vector2(70, y - 128), new Vector2(900, 1));
                }
            }
        }

        internal static Color RoleColor(string role) =>
            role == "탱커" ? new Color(0.3f, 0.5f, 0.85f)
            : role == "암살자" ? new Color(0.55f, 0.38f, 0.8f)
            : role == "궁수" ? new Color(0.3f, 0.65f, 0.32f)
            : role == "마법사" ? new Color(0.8f, 0.32f, 0.55f)
            : new Color(0.8f, 0.45f, 0.18f); // 전사

        int PriceOf(MonsterData m) =>
            Core.Dev.FreeUpgrade ? 0 : (m.IsCastle ? 50 : m.cost) * (DataHub.I.GetUpgradeCount(m.name) + 1);

        void Rebuild()
        {
            StopAllCoroutines(); // 걷기 애니메이션 코루틴 정리
            foreach (Transform c in content) Destroy(c.gameObject);
            upBtnImages.Clear();
            upBtnPrice.Clear();

            var g = groups[Mathf.Clamp(page, 0, groups.Count - 1)];
            tierText.text = g.title + " (" + g.units.Count + "종)"; // 화살표는 실제 버튼이 담당

            float width = Mathf.Max(1920f, 170f * 2f + (g.units.Count - 1) * Gap + 120f);
            content.sizeDelta = new Vector2(width, 0);
            content.anchoredPosition = Vector2.zero; // 스크롤 처음으로

            for (int i = 0; i < g.units.Count; i++)
                BuildEntry(g.units[i], 230f + i * Gap);
        }

        /// <summary>유닛 1명 — 원래 업그레이드 화면과 같은 큰 구성 (풀밭 위에서 걷는다).</summary>
        void BuildEntry(MonsterData m, float x)
        {
            bool owned = m.IsCastle || DataHub.I.OwnsUnit(m.name);
            int goldLv = DataHub.I.GetUpgradeCount(m.name);
            int dupes = DataHub.I.GetDupeCount(m.name);
            bool maxed = goldLv >= LocalDataService.MaxGoldLevel;

            // 유닛: 풀밭 위에 서서 걷는 애니메이션 (미보유 = 검은 실루엣)
            var frames = SpriteBank.GetFrames(m.SpriteName, "move");
            var img = Ui.Image(content, frames.Length > 0 ? frames[0] : null, "Img_" + m.name);
            var imgRt = (RectTransform)img.transform;
            imgRt.anchorMin = imgRt.anchorMax = new Vector2(0f, 0f);
            imgRt.pivot = new Vector2(0.5f, 0f);
            imgRt.anchoredPosition = new Vector2(x, MenuBackdrop.GroundTop - 18f);
            imgRt.sizeDelta = new Vector2(230, 230);
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (owned)
            {
                if (frames.Length > 1) StartCoroutine(MenuBackdrop.CycleFrames(img, frames, 0.16f));
            }
            else
            {
                img.color = new Color(0.1f, 0.1f, 0.14f, 0.95f);
            }

            var nameLabel = Ui.OutlinedLabel(content, m.IsCastle ? "성" : m.DisplayName, 40,
                owned ? Color.white : new Color(1f, 1f, 1f, 0.55f), "Name_" + m.name);
            var nameRt = (RectTransform)nameLabel.transform;
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(0f, 0f);
            nameRt.pivot = new Vector2(0.5f, 0.5f);
            nameRt.anchoredPosition = new Vector2(x, 455f);
            nameRt.sizeDelta = new Vector2(320f, 50f);

            // 도감 병맛 설명 — 가격 버튼 아래 섹션
            if (!string.IsNullOrEmpty(m.desc))
            {
                var desc = Ui.OutlinedLabel(content, m.desc, 20, new Color(1f, 0.95f, 0.8f, 0.92f), "Desc_" + m.name);
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                desc.alignment = TextAnchor.UpperCenter;
                var descRt = (RectTransform)desc.transform;
                descRt.anchorMin = descRt.anchorMax = new Vector2(0f, 0f);
                descRt.pivot = new Vector2(0.5f, 1f);
                descRt.anchoredPosition = new Vector2(x, 760f);
                descRt.sizeDelta = new Vector2(310f, 60f);
            }

            if (!owned)
            {
                // 미보유: 자물쇠는 캐릭터를 가리지 않게 '뽑기로 획득' 텍스트 바로 위에
                var lockImg = Ui.Image(content, SpriteBank.GetEnv("icon_lock"), "Lock_" + m.name);
                lockImg.preserveAspect = true;
                var lockRt = (RectTransform)lockImg.transform;
                lockRt.anchorMin = lockRt.anchorMax = new Vector2(0f, 0f);
                lockRt.pivot = new Vector2(0.5f, 0.5f);
                lockRt.anchoredPosition = new Vector2(x, 692f);
                lockRt.sizeDelta = new Vector2(64, 64);

                var hint = Ui.OutlinedLabel(content, "뽑기로 획득", 30, new Color(1f, 1f, 1f, 0.7f), "Hint_" + m.name);
                var hintRt = (RectTransform)hint.transform;
                hintRt.anchorMin = hintRt.anchorMax = new Vector2(0f, 0f);
                hintRt.pivot = new Vector2(0.5f, 0.5f);
                hintRt.anchoredPosition = new Vector2(x, 620f);
                hintRt.sizeDelta = new Vector2(300f, 44f);
                return;
            }

            // 역할(포지션) 배지 + 핵심 스탯 한 줄 — 한눈에 원/근·단일/범위·역할이 읽힌다
            if (!m.IsCastle && !string.IsNullOrEmpty(m.role))
            {
                string tag = m.role == "궁수" ? "궁수 · 원거리"
                           : m.role == "마법사" ? "마법사 · 범위"
                           : m.role + " · 근접";
                var rc = RoleColor(m.role);
                var pill = Ui.RoundedPanel(content, new Color(rc.r, rc.g, rc.b, 0.92f), "Role_" + m.name);
                var pillRt = (RectTransform)pill.transform;
                pillRt.anchorMin = pillRt.anchorMax = new Vector2(0f, 0f);
                pillRt.pivot = new Vector2(0.5f, 0.5f);
                pillRt.anchoredPosition = new Vector2(x, 504f);
                pillRt.sizeDelta = new Vector2(196f, 38f);
                var pillText = Ui.Label(pill.transform, tag, 22, Color.white, "RoleText");
                pillText.alignment = TextAnchor.MiddleCenter;
                Ui.Stretch(pillText.rectTransform);

                int lvl = goldLv + dupes;
                float mult = 1f + 0.2f * lvl;
                var statLabel = Ui.OutlinedLabel(content,
                    "HP " + Mathf.RoundToInt(m.hp * mult) + "  ·  공격 " + Mathf.RoundToInt(m.attack * mult),
                    24, new Color(1f, 1f, 1f, 0.9f), "Stat_" + m.name);
                var statRt = (RectTransform)statLabel.transform;
                statRt.anchorMin = statRt.anchorMax = new Vector2(0f, 0f);
                statRt.pivot = new Vector2(0.5f, 0.5f);
                statRt.anchoredPosition = new Vector2(x, 544f);
                statRt.sizeDelta = new Vector2(320f, 30f);
            }

            // 레벨 뱃지: 초록 ↑N (MAX = 주황), 중복은 옆에 노란 +N
            var lvBadge = Ui.RoundedPanel(content, maxed
                ? new Color(0.85f, 0.5f, 0.15f, 0.95f)
                : new Color(0.15f, 0.55f, 0.28f, 0.95f), "LvBadge_" + m.name);
            var lvRt = (RectTransform)lvBadge.transform;
            lvRt.anchorMin = lvRt.anchorMax = new Vector2(0f, 0f);
            lvRt.pivot = new Vector2(0.5f, 0.5f);
            lvRt.anchoredPosition = new Vector2(dupes > 0 ? x - 40f : x, 588f);
            lvRt.sizeDelta = new Vector2(120, 52);
            var lvText = Ui.CenteredIconValue(lvBadge.transform, SpriteBank.GetEnv("icon_arrowup"),
                maxed ? "MAX" : goldLv.ToString(), 32, Color.white, "Lv_" + m.name);
            Ui.Place((RectTransform)lvText.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0, 1));

            if (dupes > 0)
            {
                var dupBadge = Ui.RoundedPanel(content, new Color(0.9f, 0.75f, 0.15f, 0.95f), "DupBadge_" + m.name);
                var dupRt = (RectTransform)dupBadge.transform;
                dupRt.anchorMin = dupRt.anchorMax = new Vector2(0f, 0f);
                dupRt.pivot = new Vector2(0.5f, 0.5f);
                dupRt.anchoredPosition = new Vector2(x + 55f, 588f);
                dupRt.sizeDelta = new Vector2(76, 52);
                var dupText = Ui.Label(dupBadge.transform, "+" + dupes, 30, new Color(0.25f, 0.15f, 0f), "Dup");
                dupText.alignment = TextAnchor.MiddleCenter;
                Ui.Stretch(dupText.rectTransform);
            }

            // 가격 버튼: 나무 버튼 + [코인 + 가격] — MAX 면 잠김 표시
            var btn = Ui.ImageButton(content, SpriteBank.GetEnv("btn_wood"), new Vector2(230, 104),
                () => TryUpgrade(m), "Btn_" + m.name);
            var btnRt = (RectTransform)btn.transform;
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = new Vector2(x, 668f);
            Ui.PressedSwap(btn, SpriteBank.GetEnv("btn_wood_pressed"));

            if (maxed)
            {
                btn.GetComponent<Image>().color = new Color(0.5f, 0.45f, 0.4f);
                var maxText = Ui.OutlinedLabel(btn.transform, "MAX", 40, new Color(1f, 0.7f, 0.25f), "Max");
                Ui.Place((RectTransform)maxText.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 4));
            }
            else
            {
                int price = PriceOf(m);
                var priceText = Ui.CenteredIconValue(btn.transform, SpriteBank.GetEnv("icon_coin"),
                    price.ToString(), 36, new Color(1f, 0.9f, 0.3f), "Price_" + m.name);
                Ui.Place((RectTransform)priceText.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0, 4));
                upBtnImages[m.name] = btn.GetComponent<Image>();
                upBtnPrice[m.name] = price;
            }
        }

        void TryUpgrade(MonsterData m)
        {
            if (DataHub.I.GetUpgradeCount(m.name) >= LocalDataService.MaxGoldLevel)
            {
                Toast("최대 강화입니다. 중복 뽑기로만 강화할 수 있습니다.");
                return;
            }
            try
            {
                DataHub.I.Upgrade(m.name);
                float keep = content.anchoredPosition.x;
                Rebuild();
                content.anchoredPosition = new Vector2(keep, 0); // 스크롤 위치 유지
            }
            catch (GameException e)
            {
                Toast(e.Message);
            }
        }
    }
}
