using System.Collections.Generic;
using System.Linq;
using GameMaker.Battle;
using GameMaker.Core;
using GameMaker.Data;
using GameMaker.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameMaker.Screens
{
    /// <summary>
    /// 배치 — 냥코대전쟁식 출전 편성:
    /// 위에 출전 슬롯 5칸, 아래에 보유 유닛 목록(등급별 페이지).
    /// 보유 유닛을 슬롯으로 드래그해 배치, 슬롯끼리 드래그로 교체,
    /// 슬롯 밖으로 드래그하면 해제 (최소 1명). 성은 항상 출전이라 목록에 없다.
    /// </summary>
    public class DeployScreen : MonoBehaviour
    {
        const int Cols = 8, Rows = 2;
        static readonly string[] TierNames = { "기본", "일반", "고급", "희귀", "영웅", "전설" };
        static readonly Color[] TierColors =
        {
            new Color(0.75f, 0.75f, 0.75f),
            new Color(0.88f, 0.88f, 0.88f),
            new Color(0.45f, 0.9f, 0.5f),
            new Color(0.4f, 0.7f, 1f),
            new Color(0.8f, 0.5f, 1f),
            new Color(1f, 0.84f, 0.3f),
        };

        Canvas canvas;
        RectTransform grid;
        Text pageText, toastText;
        float toastUntil;
        List<List<MonsterData>> pages; // 등급별 보유 유닛
        int page;
        string[] slots;                 // 출전 5칸 (null = 빈 칸)
        readonly RectTransform[] slotRects = new RectTransform[LocalDataService.LoadoutMax];
        RectTransform slotRow;

        void Start()
        {
            var loadout = DataHub.I.GetLoadout();
            slots = new string[LocalDataService.LoadoutMax];
            for (int i = 0; i < loadout.Count && i < slots.Length; i++) slots[i] = loadout[i];

            canvas = Ui.CreateCanvas(transform, "DeployCanvas");
            MenuBackdrop.Build(this, canvas, dim: 0.55f, withGround: false);

            var title = Ui.OutlinedLabel(canvas.transform, "배치", 48, Color.white, "Title");
            Ui.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -50));
            var hintLabel = Ui.OutlinedLabel(canvas.transform, "보유 유닛을 위 칸으로 끌어다 배치하세요 (슬롯 밖으로 끌면 해제)",
                24, new Color(1f, 1f, 1f, 0.75f), "Hint");
            Ui.Place((RectTransform)hintLabel.transform, new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(1100, 34));

            var back = Ui.CircleIconButton(canvas.transform, "icon_return", 92,
                () => ScreenRouter.I.Show(ScreenId.Main), "BackButton");
            Ui.Place((RectTransform)back.transform, new Vector2(1f, 0f), new Vector2(-58, 50));

            // 출전 슬롯 5칸
            slotRow = Ui.Panel(canvas.transform, new Color(0, 0, 0, 0), "SlotRow");
            Ui.Place(slotRow, new Vector2(0.5f, 1f), new Vector2(0, -270), new Vector2(1150, 260));

            // 보유 목록 (등급별 페이지)
            var prev = Ui.CircleIconButton(canvas.transform, "icon_return", 64,
                () => { page = (page + pages.Count - 1) % pages.Count; RebuildList(); }, "PrevPage");
            Ui.Place((RectTransform)prev.transform, new Vector2(0.5f, 0f), new Vector2(-390, 55));
            var next = Ui.CircleIconButton(canvas.transform, "icon_return", 64,
                () => { page = (page + 1) % pages.Count; RebuildList(); }, "NextPage");
            ((RectTransform)next.transform).localScale = new Vector3(-1f, 1f, 1f);
            Ui.Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(390, 55));
            pageText = Ui.OutlinedLabel(canvas.transform, "", 32, Color.white, "Page");
            pageText.raycastTarget = false;
            Ui.Place((RectTransform)pageText.transform, new Vector2(0.5f, 0f), new Vector2(0, 55), new Vector2(640, 42));

            toastText = Ui.OutlinedLabel(canvas.transform, "", 28, new Color(1f, 0.6f, 0.5f), "Toast");
            toastText.raycastTarget = false;
            Ui.Place((RectTransform)toastText.transform, new Vector2(0.5f, 0f), new Vector2(0, 104), new Vector2(900, 40));

            grid = Ui.Panel(canvas.transform, new Color(0, 0, 0, 0), "Grid");
            Ui.Place(grid, new Vector2(0.5f, 0.5f), new Vector2(0, -140), new Vector2(1760, 480));

            // 보유 유닛만 (성 제외 — 성은 항상 출전)
            var owned = DataHub.I.GetMonsters()
                .Where(m => m.IsOur && !m.IsCastle && DataHub.I.OwnsUnit(m.name)).ToList();
            pages = new List<List<MonsterData>>();
            for (int t = 0; t <= 5; t++)
            {
                var units = owned.Where(m => m.tier == t).ToList();
                if (units.Count > 0) pages.Add(units);
            }

            RebuildSlots();
            RebuildList();
        }

        void Update()
        {
            if (toastText.text.Length > 0 && Time.time > toastUntil) toastText.text = "";
        }

        void Toast(string msg)
        {
            toastText.text = msg;
            toastUntil = Time.time + 1.6f;
        }

        void Save()
        {
            var list = slots.Where(s => s != null).ToList();
            DataHub.I.SetLoadout(list);
        }

        // ─────────── 출전 슬롯 ───────────

        void RebuildSlots()
        {
            foreach (Transform c in slotRow) Destroy(c.gameObject);
            float gap = 1150f / LocalDataService.LoadoutMax;
            for (int i = 0; i < LocalDataService.LoadoutMax; i++)
            {
                var box = Ui.RoundedPanel(slotRow, slots[i] != null
                    ? new Color(1f, 0.85f, 0.35f, 0.2f)
                    : new Color(1f, 1f, 1f, 0.07f), "Slot" + i);
                var rt = (RectTransform)box.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2((i - (LocalDataService.LoadoutMax - 1) * 0.5f) * gap, 0);
                rt.sizeDelta = new Vector2(gap - 16, 244);
                slotRects[i] = rt;

                if (slots[i] == null)
                {
                    var plus = Ui.Label(box.transform, "+", 64, new Color(1f, 1f, 1f, 0.25f), "Plus");
                    plus.alignment = TextAnchor.MiddleCenter;
                    Ui.Stretch(plus.rectTransform);
                    continue;
                }

                var m = DataHub.I.FindMonster(slots[i]);
                if (m == null) { slots[i] = null; continue; }
                BuildUnitCard(box.transform, m, rt.sizeDelta, i);
            }
        }

        // ─────────── 보유 목록 ───────────

        void RebuildList()
        {
            foreach (Transform c in grid) Destroy(c.gameObject);
            var units = pages[Mathf.Clamp(page, 0, pages.Count - 1)];
            int tier = units[0].tier;
            pageText.text = TierNames[tier] + "  " + (page + 1) + "/" + pages.Count + "  (" + units.Count + "종)";

            float cellW = 1760f / Cols, cellH = 480f / Rows;
            for (int i = 0; i < Mathf.Min(Cols * Rows, units.Count); i++)
            {
                var m = units[i];
                var slot = new GameObject("Cell_" + m.name);
                slot.transform.SetParent(grid, false);
                var rt = slot.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2((i % Cols) * cellW, -(i / Cols) * cellH);
                rt.sizeDelta = new Vector2(cellW, cellH);

                bool deployed = System.Array.IndexOf(slots, m.name) >= 0;
                var card = Ui.RoundedPanel(slot.transform, deployed
                    ? new Color(1f, 0.85f, 0.35f, 0.18f)
                    : new Color(1f, 1f, 1f, 0.08f), "Card");
                Ui.Place((RectTransform)card.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(cellW - 10, cellH - 10));
                BuildUnitCard(card.transform, m, new Vector2(cellW - 10, cellH - 10), -1);
                if (deployed)
                {
                    var tag = Ui.Label(card.transform, "배치됨", 16, new Color(1f, 0.85f, 0.4f), "Tag");
                    tag.alignment = TextAnchor.MiddleCenter;
                    Ui.Place(tag.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(100, 22));
                }
            }
        }

        /// <summary>슬롯/목록 공용 유닛 카드 — 드래그 가능. fromSlot -1 = 목록.</summary>
        void BuildUnitCard(Transform parent, MonsterData m, Vector2 size, int fromSlot)
        {
            var frames = SpriteBank.GetFrames(m.SpriteName, "move");
            var img = Ui.Image(parent, frames.Length > 0 ? frames[0] : null, "Sprite");
            img.preserveAspect = true;
            Ui.Place((RectTransform)img.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 18),
                new Vector2(size.x - 36, size.y - 96));

            int tier = Mathf.Clamp(m.tier, 0, 5);
            var nameLabel = Ui.Label(parent, m.DisplayName, 18, TierColors[tier], "Name");
            nameLabel.alignment = TextAnchor.MiddleCenter;
            Ui.Place(nameLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 34), new Vector2(size.x - 8, 24));
            int lv = DataHub.I.GetUpgradeCount(m.name) + DataHub.I.GetDupeCount(m.name);
            var lvLabel = Ui.Label(parent, "Lv." + lv, 15, new Color(1f, 1f, 1f, 0.6f), "Lv");
            lvLabel.alignment = TextAnchor.MiddleCenter;
            Ui.Place(lvLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 12), new Vector2(size.x - 8, 20));

            var drag = img.gameObject.AddComponent<DragCard>();
            drag.Init(this, m, fromSlot);
        }

        // ─────────── 드래그&드롭 처리 ───────────

        public RectTransform CanvasRoot => (RectTransform)canvas.transform;

        /// <summary>드롭 지점이 어느 슬롯인지 (-1 = 슬롯 밖).</summary>
        public int SlotAt(Vector2 screenPos, Camera cam)
        {
            for (int i = 0; i < slotRects.Length; i++)
                if (slotRects[i] != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(slotRects[i], screenPos, cam))
                    return i;
            return -1;
        }

        public void HandleDrop(MonsterData m, int fromSlot, Vector2 screenPos, Camera cam)
        {
            int target = SlotAt(screenPos, cam);

            if (fromSlot >= 0) // 슬롯에서 출발
            {
                if (target < 0) // 슬롯 밖 = 해제
                {
                    if (slots.Count(s => s != null) <= 1) { Toast("최소 1명은 배치해야 합니다."); }
                    else slots[fromSlot] = null;
                }
                else if (target != fromSlot) // 슬롯끼리 = 교체
                {
                    var tmp = slots[target];
                    slots[target] = slots[fromSlot];
                    slots[fromSlot] = tmp;
                }
            }
            else // 목록에서 출발
            {
                if (target < 0) return;
                int already = System.Array.IndexOf(slots, m.name);
                if (already >= 0 && already != target) // 이미 배치된 유닛 = 위치 이동
                {
                    var tmp = slots[target];
                    slots[target] = m.name;
                    slots[already] = tmp;
                }
                else if (already < 0)
                {
                    slots[target] = m.name; // 빈 칸이든 교체든 덮어쓰기
                }
            }

            Save();
            RebuildSlots();
            RebuildList();
        }

        /// <summary>유닛 카드 드래그 — 반투명 고스트가 손가락을 따라온다.</summary>
        class DragCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            DeployScreen screen;
            MonsterData data;
            int fromSlot;
            Image ghost;

            public void Init(DeployScreen s, MonsterData m, int slot)
            {
                screen = s;
                data = m;
                fromSlot = slot;
            }

            public void OnBeginDrag(PointerEventData e)
            {
                var frames = SpriteBank.GetFrames(data.SpriteName, "move");
                ghost = Ui.Image(screen.CanvasRoot, frames.Length > 0 ? frames[0] : null, "Ghost");
                ghost.preserveAspect = true;
                ghost.raycastTarget = false;
                ghost.color = new Color(1f, 1f, 1f, 0.75f);
                ((RectTransform)ghost.transform).sizeDelta = new Vector2(150, 150);
                MoveGhost(e);
            }

            public void OnDrag(PointerEventData e) => MoveGhost(e);

            void MoveGhost(PointerEventData e)
            {
                if (ghost == null) return;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    screen.CanvasRoot, e.position, e.pressEventCamera, out var local);
                ((RectTransform)ghost.transform).anchoredPosition = local;
            }

            public void OnEndDrag(PointerEventData e)
            {
                if (ghost != null) Destroy(ghost.gameObject);
                screen.HandleDrop(data, fromSlot, e.position, e.pressEventCamera);
            }
        }
    }
}
