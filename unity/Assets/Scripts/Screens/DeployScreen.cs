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
        Text pageText, toastText, slotHeader;
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

            var back = Ui.CircleIconButton(canvas.transform, "icon_return", 92,
                () => ScreenRouter.I.Show(ScreenId.Main), "BackButton");
            Ui.Place((RectTransform)back.transform, new Vector2(1f, 0f), new Vector2(-58, 50));

            // ── 상단 섹션: 출전 배치 (전용 배경 밴드로 아래와 확실히 구분) ──
            var topBand = Ui.Panel(canvas.transform, new Color(0, 0, 0, 0), "TopBand");
            Ui.Place(topBand, new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(1320, 330));
            slotHeader = Ui.OutlinedLabel(topBand, "", 30, new Color(1f, 0.9f, 0.5f), "SlotHeader");
            Ui.Place((RectTransform)slotHeader.transform, new Vector2(0.5f, 1f), new Vector2(0, -26), new Vector2(600, 38));

            slotRow = Ui.Panel(topBand, new Color(0, 0, 0, 0), "SlotRow");
            Ui.Place(slotRow, new Vector2(0.5f, 0.5f), new Vector2(0, -18), new Vector2(1200, 250));

            // ── 하단 섹션: 보유 유닛 (별도 배경 밴드) ──
            var botBand = Ui.Panel(canvas.transform, new Color(0, 0, 0, 0), "BotBand");
            Ui.Place(botBand, new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(1820, 480));
            var botHeader = Ui.OutlinedLabel(botBand, "보유 유닛 — 위 칸으로 드래그해 배치, 배치된 유닛은 클릭으로 해제",
                24, new Color(1f, 1f, 1f, 0.8f), "BotHeader");
            Ui.Place((RectTransform)botHeader.transform, new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(1300, 34));


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
            Ui.Place((RectTransform)toastText.transform, new Vector2(0.5f, 0f), new Vector2(0, 12), new Vector2(900, 40));

            grid = Ui.Panel(canvas.transform, new Color(0, 0, 0, 0), "Grid");
            Ui.Place(grid, new Vector2(0.5f, 0f), new Vector2(0, 105), new Vector2(1760, 400));

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
            slotHeader.text = "출전 배치  " + slots.Count(s => s != null) + "/" + LocalDataService.LoadoutMax;
            float gap = 1200f / LocalDataService.LoadoutMax;
            for (int i = 0; i < LocalDataService.LoadoutMax; i++)
            {
                bool filled = slots[i] != null;

                // 금테(테두리) + 진한 속판 — 배경과 확실히 구분되는 출전 슬롯
                // 메인 메뉴 버튼과 같은 나무 프레임 (원본 비율 150:140 유지)
                var frameImg = Ui.Image(slotRow, SpriteBank.GetEnv("btn_wood"), "Slot" + i);
                frameImg.color = filled ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.85f);
                var rt = (RectTransform)frameImg.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2((i - (LocalDataService.LoadoutMax - 1) * 0.5f) * gap, 0);
                float fw = gap - 14;
                rt.sizeDelta = new Vector2(fw, fw * 140f / 150f);
                slotRects[i] = rt;
                var inner = rt; // 카드 내용은 프레임 안쪽에 직접

                // 슬롯 번호
                var num = Ui.Label(rt, (i + 1).ToString(), 20, new Color(1f, 1f, 1f, 0.5f), "Num");
                num.alignment = TextAnchor.MiddleCenter;
                Ui.Place(num.rectTransform, new Vector2(0f, 1f), new Vector2(20, -18), new Vector2(30, 26));

                if (!filled)
                {
                    var plus = Ui.Label(inner, "+", 66, new Color(1f, 1f, 1f, 0.3f), "Plus");
                    plus.alignment = TextAnchor.MiddleCenter;
                    Ui.Stretch(plus.rectTransform);
                    var hint = Ui.Label(inner, "드래그로 배치", 16, new Color(1f, 1f, 1f, 0.35f), "Hint");
                    hint.alignment = TextAnchor.MiddleCenter;
                    Ui.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 42), new Vector2(gap - 60, 22));
                    continue;
                }

                var m = DataHub.I.FindMonster(slots[i]);
                if (m == null) { slots[i] = null; continue; }
                BuildUnitCard(inner, m, inner.sizeDelta * 0.84f, i); // 나무 테두리 안쪽 여백
                // 나무 박스 어디를 클릭해도 해제되도록 — 박스 전체가 유닛 단위
                var boxDrag = frameImg.gameObject.AddComponent<DragCard>();
                boxDrag.Init(this, m, i);
            }
        }

        // ─────────── 보유 목록 ───────────

        void RebuildList()
        {
            foreach (Transform c in grid) Destroy(c.gameObject);
            var units = pages[Mathf.Clamp(page, 0, pages.Count - 1)];
            int tier = units[0].tier;
            pageText.text = TierNames[tier] + "  " + (page + 1) + "/" + pages.Count + "  (" + units.Count + "종)";

            float cellW = 1760f / Cols, cellH = 400f / Rows;
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
                var card = Ui.Panel(slot.transform, deployed
                    ? new Color(1f, 0.85f, 0.35f, 0.2f)   // 배치됨 = 은은한 금빛만
                    : new Color(0, 0, 0, 0), "Card");    // 나머지는 배경 그대로
                Ui.Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cellW - 10, cellH - 10));
                BuildUnitCard(card, m, new Vector2(cellW - 10, cellH - 10), -1);
                if (deployed)
                {
                    var tag = Ui.Label(card, "배치됨", 16, new Color(1f, 0.85f, 0.4f), "Tag");
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

            // 역할 미니 배지 (좌상단) — 색으로 포지션이 읽힌다
            if (!string.IsNullOrEmpty(m.role))
            {
                var rc = m.role == "탱커" ? new Color(0.3f, 0.5f, 0.85f)
                       : m.role == "암살자" ? new Color(0.55f, 0.38f, 0.8f)
                       : m.role == "궁수" ? new Color(0.3f, 0.65f, 0.32f)
                       : m.role == "마법사" ? new Color(0.8f, 0.32f, 0.55f)
                       : new Color(0.8f, 0.45f, 0.18f);
                var pill = Ui.RoundedPanel(parent, new Color(rc.r, rc.g, rc.b, 0.92f), "RolePill");
                Ui.Place((RectTransform)pill.transform, new Vector2(0f, 1f), new Vector2(40, -17), new Vector2(64, 24));
                var pt = Ui.Label(pill.transform, m.role, 14, Color.white, "RoleText");
                pt.alignment = TextAnchor.MiddleCenter;
                Ui.Stretch(pt.rectTransform);
            }

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

        /// <summary>배치된 유닛 클릭 = 배치 해제 (슬롯/보유 목록 어디서든).</summary>
        public void HandleClick(MonsterData m)
        {
            int idx = System.Array.IndexOf(slots, m.name);
            if (idx < 0) return; // 미배치 유닛 클릭은 무시 (배치는 드래그로)
            if (slots.Count(s => s != null) <= 1) { Toast("최소 1명은 배치해야 합니다."); return; }
            slots[idx] = null;
            Save();
            RebuildSlots();
            RebuildList();
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
        class DragCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
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

            public void OnPointerClick(PointerEventData e)
            {
                if (e.dragging) return; // 드래그 후 릴리즈는 클릭 아님
                screen.HandleClick(data);
            }
        }
    }
}
