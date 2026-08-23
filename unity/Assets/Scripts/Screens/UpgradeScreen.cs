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
    /// 부대 관리 — 강화 + 배치(출전).
    /// 등급별 페이지(기본/일반/고급/희귀/영웅/전설)에 전체 아군을 카드로 나열:
    /// - 골드 강화: 카드의 [강화] 버튼, 레벨 10에서 MAX 로 잠김 (이후 중복 뽑기 +N 로만)
    /// - 중복 뽑기 강화: 이름 옆 노란 +N 배지 (무제한)
    /// - 배치: [배치] 토글 — 출전 슬롯 최대 6, 최소 1. 배치된 카드는 금테 강조
    /// - 미보유(뽑기 전) 유닛: 실루엣 + 자물쇠, 조작 불가
    /// </summary>
    public class UpgradeScreen : MonoBehaviour
    {
        const int Cols = 8, Rows = 3;
        static readonly string[] TierNames = { "기본", "일반", "고급", "희귀", "영웅", "전설" };

        class Group { public string title; public List<MonsterData> units; }

        Canvas canvas;
        RectTransform grid;
        Text moneyText, pageText, loadoutText, toastText;
        float toastUntil;
        List<Group> groups;
        int page;
        List<string> loadout;
        readonly Dictionary<string, Image> upBtnImages = new Dictionary<string, Image>();
        readonly Dictionary<string, int> upBtnPrice = new Dictionary<string, int>();

        void Start()
        {
            loadout = DataHub.I.GetLoadout();

            canvas = Ui.CreateCanvas(transform, "UpgradeCanvas");
            MenuBackdrop.Build(this, canvas, dim: 0.55f, withGround: false);

            var title = Ui.OutlinedLabel(canvas.transform, "부대 강화 · 배치", 48, Color.white, "Title");
            Ui.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -50));

            var moneyPanel = Ui.Image(canvas.transform, SpriteBank.GetEnv("panel_parchment"), "MoneyPanel");
            Ui.Place((RectTransform)moneyPanel.transform, new Vector2(0f, 1f), new Vector2(160, -55), new Vector2(280, 82));
            moneyText = Ui.CenteredIconValue(moneyPanel.transform, SpriteBank.GetEnv("icon_coin"),
                "0", 42, Color.white, "Money");
            Ui.Place((RectTransform)moneyText.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0, 2));

            loadoutText = Ui.OutlinedLabel(canvas.transform, "", 30, new Color(1f, 0.9f, 0.5f), "LoadoutCount");
            Ui.Place((RectTransform)loadoutText.transform, new Vector2(1f, 1f), new Vector2(-170, -55), new Vector2(280, 40));

            var back = Ui.CircleIconButton(canvas.transform, "icon_return", 92,
                () => ScreenRouter.I.Show(ScreenId.Main), "BackButton");
            Ui.Place((RectTransform)back.transform, new Vector2(1f, 0f), new Vector2(-58, 50));

            var prev = Ui.CircleIconButton(canvas.transform, "icon_return", 64,
                () => { page = (page + groups.Count - 1) % groups.Count; Rebuild(); }, "PrevPage");
            Ui.Place((RectTransform)prev.transform, new Vector2(0.5f, 0f), new Vector2(-390, 55));
            var next = Ui.CircleIconButton(canvas.transform, "icon_return", 64,
                () => { page = (page + 1) % groups.Count; Rebuild(); }, "NextPage");
            ((RectTransform)next.transform).localScale = new Vector3(-1f, 1f, 1f);
            Ui.Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(390, 55));
            pageText = Ui.OutlinedLabel(canvas.transform, "", 34, Color.white, "Page");
            pageText.raycastTarget = false;
            Ui.Place((RectTransform)pageText.transform, new Vector2(0.5f, 0f), new Vector2(0, 55), new Vector2(640, 44));

            toastText = Ui.OutlinedLabel(canvas.transform, "", 28, new Color(1f, 0.6f, 0.5f), "Toast");
            toastText.raycastTarget = false;
            Ui.Place((RectTransform)toastText.transform, new Vector2(0.5f, 0f), new Vector2(0, 108), new Vector2(900, 40));

            grid = Ui.Panel(canvas.transform, new Color(0, 0, 0, 0), "Grid");
            Ui.Place(grid, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1760, 760));

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
            foreach (Transform c in grid) Destroy(c.gameObject);
            upBtnImages.Clear();
            upBtnPrice.Clear();

            var g = groups[Mathf.Clamp(page, 0, groups.Count - 1)];
            pageText.text = g.title + "  " + (page + 1) + "/" + groups.Count + "  (" + g.units.Count + "종)";
            loadoutText.text = "배치 " + loadout.Count + "/" + LocalDataService.LoadoutMax;

            float cellW = 1760f / Cols, cellH = 760f / Rows;
            for (int i = 0; i < Mathf.Min(Cols * Rows, g.units.Count); i++)
            {
                var m = g.units[i];
                var slot = new GameObject("Cell_" + m.name);
                slot.transform.SetParent(grid, false);
                var rt = slot.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2((i % Cols) * cellW, -(i / Cols) * cellH);
                rt.sizeDelta = new Vector2(cellW, cellH);
                BuildCell(slot.transform, m, cellW, cellH);
            }
        }

        void BuildCell(Transform slot, MonsterData m, float cellW, float cellH)
        {
            bool owned = m.IsCastle || DataHub.I.OwnsUnit(m.name);
            bool deployed = loadout.Contains(m.name);
            int goldLv = DataHub.I.GetUpgradeCount(m.name);
            int dupes = DataHub.I.GetDupeCount(m.name);
            bool maxed = goldLv >= LocalDataService.MaxGoldLevel;

            // 카드 — 배치된 유닛은 금테 느낌의 밝은 배경
            var card = Ui.RoundedPanel(slot, deployed
                ? new Color(1f, 0.85f, 0.35f, 0.22f)
                : new Color(1f, 1f, 1f, 0.08f), "Card");
            Ui.Place((RectTransform)card.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(cellW - 10, cellH - 10));

            // 유닛 그림 (미보유 = 검은 실루엣)
            var frames = SpriteBank.GetFrames(m.SpriteName, "move");
            var img = Ui.Image(card.transform, frames.Length > 0 ? frames[0] : null, "Sprite");
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.color = owned ? Color.white : new Color(0.12f, 0.12f, 0.16f, 0.9f);
            Ui.Place((RectTransform)img.transform, new Vector2(0.5f, 1f), new Vector2(0, -52), new Vector2(cellW - 60, 86));

            // 이름 + 배지 (골드 레벨 / MAX / 중복 +N)
            var nameLabel = Ui.Label(card.transform, m.IsCastle ? "성" : m.DisplayName, 20,
                owned ? Color.white : new Color(1f, 1f, 1f, 0.45f), "Name");
            nameLabel.alignment = TextAnchor.MiddleCenter;
            Ui.Place(nameLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -108), new Vector2(cellW - 12, 26));

            if (owned)
            {
                // 골드 강화 배지: 초록 ↑N, MAX 면 주황 MAX
                var lvBadge = Ui.RoundedPanel(card.transform, maxed
                    ? new Color(0.85f, 0.5f, 0.15f, 0.95f)
                    : new Color(0.15f, 0.55f, 0.28f, 0.95f), "LvBadge");
                Ui.Place((RectTransform)lvBadge.transform, new Vector2(0.5f, 1f), new Vector2(dupes > 0 ? -34 : 0, -138),
                    new Vector2(64, 30));
                var lvText = Ui.Label(lvBadge.transform, maxed ? "MAX" : "↑" + goldLv, 18, Color.white, "Lv");
                lvText.alignment = TextAnchor.MiddleCenter;
                Ui.Stretch(lvText.rectTransform);

                // 중복 뽑기 배지: 노란 +N
                if (dupes > 0)
                {
                    var dupBadge = Ui.RoundedPanel(card.transform, new Color(0.9f, 0.75f, 0.15f, 0.95f), "DupBadge");
                    Ui.Place((RectTransform)dupBadge.transform, new Vector2(0.5f, 1f), new Vector2(34, -138), new Vector2(58, 30));
                    var dupText = Ui.Label(dupBadge.transform, "+" + dupes, 18, new Color(0.25f, 0.15f, 0f), "Dup");
                    dupText.alignment = TextAnchor.MiddleCenter;
                    Ui.Stretch(dupText.rectTransform);
                }

                // [강화] — MAX 면 잠김 표시
                int price = PriceOf(m);
                var upBtn = Ui.TextButton(card.transform, maxed ? "MAX" : price + " 강화", 18,
                    new Vector2(m.IsCastle ? cellW - 26 : (cellW - 30) * 0.55f, 42),
                    () => TryUpgrade(m), maxed
                        ? new Color(0.35f, 0.3f, 0.25f, 0.9f)
                        : new Color(0.2f, 0.4f, 0.22f, 0.95f), "UpBtn");
                Ui.Place((RectTransform)upBtn.transform, new Vector2(m.IsCastle ? 0.5f : 0.31f, 0f),
                    new Vector2(0, 28));
                if (!maxed)
                {
                    upBtnImages[m.name] = upBtn.GetComponent<Image>();
                    upBtnPrice[m.name] = price;
                }

                // [배치] 토글 — 성은 항상 출전이라 없음
                if (!m.IsCastle)
                {
                    var depBtn = Ui.TextButton(card.transform, deployed ? "배치됨" : "배치", 18,
                        new Vector2((cellW - 30) * 0.4f, 42),
                        () => ToggleDeploy(m), deployed
                            ? new Color(0.85f, 0.65f, 0.15f, 0.95f)
                            : new Color(0.3f, 0.32f, 0.4f, 0.95f), "DepBtn");
                    Ui.Place((RectTransform)depBtn.transform, new Vector2(0.79f, 0f), new Vector2(0, 28));
                }
            }
            else
            {
                // 미보유: 자물쇠 + 안내
                var lockImg = Ui.Image(card.transform, SpriteBank.GetEnv("icon_lock"), "Lock");
                lockImg.preserveAspect = true;
                Ui.Place((RectTransform)lockImg.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(52, 52));
                var hint = Ui.Label(card.transform, "뽑기로 획득", 17, new Color(1f, 1f, 1f, 0.5f), "Hint");
                hint.alignment = TextAnchor.MiddleCenter;
                Ui.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 24), new Vector2(cellW - 12, 24));
            }
        }

        void TryUpgrade(MonsterData m)
        {
            try
            {
                DataHub.I.Upgrade(m.name);
                Rebuild();
            }
            catch (GameException e)
            {
                Toast(e.Message);
            }
        }

        void ToggleDeploy(MonsterData m)
        {
            if (loadout.Contains(m.name))
            {
                if (loadout.Count <= 1) { Toast("최소 1명은 배치해야 합니다."); return; }
                loadout.Remove(m.name);
            }
            else
            {
                if (loadout.Count >= LocalDataService.LoadoutMax)
                {
                    Toast("배치는 최대 " + LocalDataService.LoadoutMax + "명까지입니다.");
                    return;
                }
                loadout.Add(m.name);
            }
            DataHub.I.SetLoadout(loadout);
            Rebuild();
        }
    }
}
