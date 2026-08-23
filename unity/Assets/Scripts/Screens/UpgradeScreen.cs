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

        int PriceOf(MonsterData m) =>
            (m.IsCastle ? 50 : m.cost) * (DataHub.I.GetUpgradeCount(m.name) + 1);

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

            if (!owned)
            {
                // 미보유: 자물쇠 + 안내
                var lockImg = Ui.Image(content, SpriteBank.GetEnv("icon_lock"), "Lock_" + m.name);
                lockImg.preserveAspect = true;
                var lockRt = (RectTransform)lockImg.transform;
                lockRt.anchorMin = lockRt.anchorMax = new Vector2(0f, 0f);
                lockRt.pivot = new Vector2(0.5f, 0.5f);
                lockRt.anchoredPosition = new Vector2(x, 330f);
                lockRt.sizeDelta = new Vector2(64, 64);

                var hint = Ui.OutlinedLabel(content, "뽑기로 획득", 30, new Color(1f, 1f, 1f, 0.7f), "Hint_" + m.name);
                var hintRt = (RectTransform)hint.transform;
                hintRt.anchorMin = hintRt.anchorMax = new Vector2(0f, 0f);
                hintRt.pivot = new Vector2(0.5f, 0.5f);
                hintRt.anchoredPosition = new Vector2(x, 620f);
                hintRt.sizeDelta = new Vector2(300f, 44f);
                return;
            }

            // 레벨 뱃지: 초록 ↑N (MAX = 주황), 중복은 옆에 노란 +N
            var lvBadge = Ui.RoundedPanel(content, maxed
                ? new Color(0.85f, 0.5f, 0.15f, 0.95f)
                : new Color(0.15f, 0.55f, 0.28f, 0.95f), "LvBadge_" + m.name);
            var lvRt = (RectTransform)lvBadge.transform;
            lvRt.anchorMin = lvRt.anchorMax = new Vector2(0f, 0f);
            lvRt.pivot = new Vector2(0.5f, 0.5f);
            lvRt.anchoredPosition = new Vector2(dupes > 0 ? x - 40f : x, 535f);
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
                dupRt.anchoredPosition = new Vector2(x + 55f, 535f);
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
            btnRt.anchoredPosition = new Vector2(x, 640f);
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
