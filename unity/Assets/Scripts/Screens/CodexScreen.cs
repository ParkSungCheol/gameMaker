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
    /// 도감 — 냥코대전쟁식 캐릭터 도감.
    /// [아군] 탭은 등급별, [적군] 탭은 출현 테마(여행지)별 페이지로 묶고,
    /// 카드를 누르면 팝업(승/패 팝업 프레임 재사용)에 걷는 모습 + 스탯 + 병맛 한 줄 설명.
    /// 아직 못 뽑은 아군은 어둡게 + 자물쇠, 아직 못 간 테마의 적은 실루엣으로만.
    /// </summary>
    public class CodexScreen : MonoBehaviour
    {
        const int Cols = 8, Rows = 3, PerPage = Cols * Rows;
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

        class Page
        {
            public string title;
            public Color color;
            public List<MonsterData> units;
        }

        Canvas canvas;
        RectTransform grid;
        Text pageText, countText;
        Button allyTab, enemyTab;
        bool enemyMode;
        int page;
        List<Page> pages;

        void Start()
        {
            canvas = Ui.CreateCanvas(transform, "CodexCanvas");
            MenuBackdrop.Build(this, canvas, dim: 0.6f, withGround: false);

            var title = Ui.OutlinedLabel(canvas.transform, "도감", 48, Color.white, "Title");
            Ui.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -50));

            countText = Ui.OutlinedLabel(canvas.transform, "", 24, new Color(1f, 1f, 1f, 0.8f), "Count");
            Ui.Place((RectTransform)countText.transform, new Vector2(1f, 1f), new Vector2(-190, -50), new Vector2(300, 34));

            var back = Ui.CircleIconButton(canvas.transform, "icon_return", 92,
                () => ScreenRouter.I.Show(ScreenId.Main), "BackButton");
            Ui.Place((RectTransform)back.transform, new Vector2(1f, 0f), new Vector2(-58, 50));

            // 탭
            allyTab = Ui.TextButton(canvas.transform, "아군", 30, new Vector2(170, 58),
                () => SetMode(false), new Color(0.3f, 0.26f, 0.18f, 0.95f), "AllyTab");
            Ui.Place((RectTransform)allyTab.transform, new Vector2(0f, 1f), new Vector2(130, -55));
            enemyTab = Ui.TextButton(canvas.transform, "적군", 30, new Vector2(170, 58),
                () => SetMode(true), new Color(0.3f, 0.26f, 0.18f, 0.95f), "EnemyTab");
            Ui.Place((RectTransform)enemyTab.transform, new Vector2(0f, 1f), new Vector2(315, -55));

            // 페이지 이동
            var prev = Ui.CircleIconButton(canvas.transform, "icon_return", 64,
                () => { page = (page + pages.Count - 1) % pages.Count; RebuildGrid(); }, "PrevPage");
            Ui.Place((RectTransform)prev.transform, new Vector2(0.5f, 0f), new Vector2(-390, 55));
            var next = Ui.CircleIconButton(canvas.transform, "icon_return", 64,
                () => { page = (page + 1) % pages.Count; RebuildGrid(); }, "NextPage");
            ((RectTransform)next.transform).localScale = new Vector3(-1f, 1f, 1f);
            Ui.Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(390, 55));
            pageText = Ui.OutlinedLabel(canvas.transform, "", 32, Color.white, "Page");
            pageText.raycastTarget = false;
            Ui.Place((RectTransform)pageText.transform, new Vector2(0.5f, 0f), new Vector2(0, 55), new Vector2(640, 42));

            grid = Ui.Panel(canvas.transform, new Color(0, 0, 0, 0), "Grid");
            Ui.Place(grid, new Vector2(0.5f, 0f), new Vector2(0, 120), new Vector2(1760, 780));

            SetMode(false);
        }

        void SetMode(bool enemy)
        {
            enemyMode = enemy;
            page = 0;
            allyTab.GetComponent<Image>().color = enemy ? new Color(0.3f, 0.26f, 0.18f, 0.95f) : new Color(0.75f, 0.55f, 0.2f, 0.95f);
            enemyTab.GetComponent<Image>().color = enemy ? new Color(0.75f, 0.55f, 0.2f, 0.95f) : new Color(0.3f, 0.26f, 0.18f, 0.95f);
            BuildPages();
            RebuildGrid();
        }

        void BuildPages()
        {
            pages = new List<Page>();
            var all = DataHub.I.GetMonsters();
            if (!enemyMode)
            {
                var allies = all.Where(m => m.IsOur).ToList();
                for (int t = 0; t <= 5; t++)
                {
                    var units = allies.Where(m => m.tier == t).ToList();
                    AddChunked(TierNames[t], TierColors[t], units);
                }
                int owned = allies.Count(m => DataHub.I.OwnsUnit(m.name));
                countText.text = "보유 " + owned + " / " + allies.Count;
            }
            else
            {
                var enemies = all.Where(m => !m.IsOur).ToList();
                var themes = enemies.Select(m => m.stage / 10).Distinct().OrderBy(x => x).ToList();
                foreach (int theme in themes)
                {
                    var units = enemies.Where(m => m.stage / 10 == theme)
                        .OrderBy(m => m.stage).ThenBy(m => m.IsCastle ? 1 : 0).ToList();
                    string label = theme == 0 ? "공통" : theme + ". " + DataHub.I.GetStage(theme).label;
                    AddChunked(label, new Color(1f, 0.75f, 0.55f), units);
                }
                countText.text = "적 " + enemies.Count + "종";
            }
            if (pages.Count == 0) pages.Add(new Page { title = "", color = Color.white, units = new List<MonsterData>() });
        }

        void AddChunked(string title, Color color, List<MonsterData> units)
        {
            if (units.Count == 0) return;
            int chunks = (units.Count + PerPage - 1) / PerPage;
            for (int c = 0; c < chunks; c++)
            {
                pages.Add(new Page
                {
                    title = chunks > 1 ? title + " (" + (c + 1) + "/" + chunks + ")" : title,
                    color = color,
                    units = units.Skip(c * PerPage).Take(PerPage).ToList(),
                });
            }
        }

        /// <summary>적군: 해당 테마의 첫 서브스테이지를 한 번이라도 클리어(또는 이전 테마 클리어로 해금)했으면 공개.</summary>
        bool EnemyRevealed(MonsterData m)
        {
            if (Core.Dev.UnlockAllStages) return true;
            int theme = m.stage / 10;
            if (theme <= 1) return true;
            var clear = DataHub.I.GetPlayer().mapClear;
            // 이 테마의 어떤 서브라도 클리어했거나, 이 테마에 도달(이전 테마 마지막 서브 클리어)했으면 공개
            for (int s = theme * 10 + 1; s < theme * 10 + 10 && s < clear.Length; s++) if (clear[s] > 0) return true;
            var prevStage = DataHub.I.GetStage(theme - 1);
            int lastPrev = (theme - 1) * 10 + Mathf.Max(1, prevStage.subCount);
            return lastPrev < clear.Length && clear[lastPrev] > 0;
        }

        void RebuildGrid()
        {
            foreach (Transform c in grid) Destroy(c.gameObject);
            var p = pages[Mathf.Clamp(page, 0, pages.Count - 1)];
            pageText.text = p.title + "   (" + (page + 1) + "/" + pages.Count + ")";
            pageText.color = p.color;

            float cw = 1760f / Cols, ch = 780f / Rows;
            for (int i = 0; i < p.units.Count; i++)
            {
                var m = p.units[i];
                int col = i % Cols, row = i / Cols;
                bool revealed = enemyMode ? EnemyRevealed(m) : (m.tier == 0 || DataHub.I.OwnsUnit(m.name));

                var card = Ui.RoundedPanel(grid, new Color(0f, 0f, 0f, 0.35f), "Card_" + m.name);
                var rt = (RectTransform)card.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2((col + 0.5f) * cw, -(row + 0.5f) * ch);
                rt.sizeDelta = new Vector2(cw - 14, ch - 14);

                var frames = SpriteBank.GetFrames(m.SpriteName, "move");
                var img = Ui.Image(card.transform, frames.Length > 0 ? frames[0] : null, "Img");
                img.preserveAspect = true; img.raycastTarget = false;
                if (m.facing == "left") img.rectTransform.localScale = new Vector3(-1, 1, 1);
                Ui.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 22), new Vector2(cw - 50, ch - 90));
                if (!revealed) img.color = new Color(0.08f, 0.08f, 0.1f, 0.95f); // 실루엣

                string nm = revealed ? m.DisplayName : "???";
                var txt = Ui.OutlinedLabel(card.transform, nm, 24, revealed ? Color.white : new Color(0.7f, 0.7f, 0.7f), "Name");
                txt.raycastTarget = false;
                Ui.Place((RectTransform)txt.transform, new Vector2(0.5f, 0f), new Vector2(0, 10), new Vector2(cw - 20, 32));

                if (!enemyMode && !revealed)
                {
                    var lockImg = Ui.Image(card.transform, SpriteBank.GetEnv("icon_lock"), "Lock");
                    lockImg.preserveAspect = true; lockImg.raycastTarget = false;
                    Ui.Place(lockImg.rectTransform, new Vector2(1f, 1f), new Vector2(-8, -8), new Vector2(40, 40));
                }

                var cm = m; bool rv = revealed;
                var btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = card;
                btn.onClick.AddListener(() => ShowDetail(cm, rv));
            }
        }

        // ─────────── 상세 팝업 ───────────

        void ShowDetail(MonsterData m, bool revealed)
        {
            var board = Ui.Popup(canvas.transform, revealed ? m.DisplayName : "???", new Vector2(1060, 840));

            // 왼쪽: 걷는 모습 (비공개면 실루엣)
            var stage = Ui.RoundedPanel(board, new Color(0f, 0f, 0f, 0.3f), "Stage");
            Ui.Place((RectTransform)stage.transform, new Vector2(0f, 1f), new Vector2(70, -160), new Vector2(380, 440));
            var frames = SpriteBank.GetFrames(m.SpriteName, "move");
            var img = Ui.Image(stage.transform, frames.Length > 0 ? frames[0] : null, "Walk");
            img.preserveAspect = true; img.raycastTarget = false;
            if (m.facing == "left") img.rectTransform.localScale = new Vector3(-1, 1, 1);
            Ui.Place(img.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340, 380));
            if (!revealed) img.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
            else if (frames.Length > 1) StartCoroutine(MenuBackdrop.CycleFrames(img, frames, 0.16f));

            // 오른쪽: 태그 + 스탯
            float x = 490f, y = -165f;
            if (!enemyMode)
            {
                var rc = UpgradeScreen.RoleColor(m.role);
                var pill = Ui.RoundedPanel(board, new Color(rc.r, rc.g, rc.b, 0.95f), "RolePill");
                Ui.Place((RectTransform)pill.transform, new Vector2(0f, 1f), new Vector2(x, y), new Vector2(130, 42));
                var pt = Ui.Label(pill.transform, string.IsNullOrEmpty(m.role) ? "기본" : m.role, 24, Color.white, "RoleText");
                pt.alignment = TextAnchor.MiddleCenter; Ui.Stretch(pt.rectTransform);
                var tierTxt = Ui.OutlinedLabel(board, new string('★', Mathf.Max(0, m.tier)) + " " + TierNames[Mathf.Clamp(m.tier, 0, 5)],
                    26, TierColors[Mathf.Clamp(m.tier, 0, 5)], "Tier");
                tierTxt.alignment = TextAnchor.MiddleLeft;
                Ui.Place((RectTransform)tierTxt.transform, new Vector2(0f, 1f), new Vector2(x + 145, y), new Vector2(380, 42));
            }
            else
            {
                int theme = m.stage / 10, sub = m.stage % 10;
                bool boss = theme > 0 && DataHub.I.GetStage(theme).BossOf(sub) == m.name;
                string where = theme == 0 ? "공통" : "출현  " + theme + "-" + sub + "  " + DataHub.I.GetStage(theme).label;
                var whereTxt = Ui.OutlinedLabel(board, where, 26, new Color(1f, 0.85f, 0.55f), "Where");
                whereTxt.alignment = TextAnchor.MiddleLeft;
                Ui.Place((RectTransform)whereTxt.transform, new Vector2(0f, 1f), new Vector2(x, y), new Vector2(380, 42));
                if (boss)
                {
                    var pill = Ui.RoundedPanel(board, new Color(0.8f, 0.2f, 0.2f, 0.95f), "BossPill");
                    Ui.Place((RectTransform)pill.transform, new Vector2(0f, 1f), new Vector2(x + 400, y), new Vector2(100, 42));
                    var pt = Ui.Label(pill.transform, "보스", 24, Color.white, "BossText");
                    pt.alignment = TextAnchor.MiddleCenter; Ui.Stretch(pt.rectTransform);
                }
            }

            string kind = !string.IsNullOrEmpty(m.projectile) ? (m.aoe > 0 ? "원거리 · 범위" : "원거리 · 단일")
                : (m.aoe > 0 ? "근접 · 범위" : "근접 · 단일");
            if (m.fly > 0) kind += " · 비행";
            var lines = new List<string>
            {
                "타입      " + kind,
                "체력      " + (revealed ? m.hp.ToString() : "?"),
                "공격력    " + (revealed ? m.attack.ToString() : "?"),
                "사거리    " + (revealed ? m.range.ToString() : "?"),
                "이동속도  " + (revealed ? m.moveSpeed.ToString("0") : "?"),
                "공격주기  " + (revealed ? m.attackInterval.ToString("0.0") + "초" : "?"),
            };
            if (!enemyMode && !m.IsCastle)
                lines.Add("소환      " + m.cost + "원 · 쿨타임 " + m.cooldown.ToString("0.0") + "초");
            y -= 56f;
            foreach (var line in lines)
            {
                var t = Ui.OutlinedLabel(board, line, 26, Color.white, "Stat");
                t.alignment = TextAnchor.MiddleLeft;
                Ui.Place((RectTransform)t.transform, new Vector2(0f, 1f), new Vector2(x, y), new Vector2(500, 36));
                y -= 40f;
            }

            // 아래: 병맛 설명
            var descBg = Ui.RoundedPanel(board, new Color(0f, 0f, 0f, 0.3f), "DescBg");
            Ui.Place((RectTransform)descBg.transform, new Vector2(0.5f, 1f), new Vector2(0, -620), new Vector2(920, 110));
            var desc = Ui.OutlinedLabel(board, revealed ? (string.IsNullOrEmpty(m.desc) ? "..." : m.desc) : "아직 만나지 못한 상대다.",
                26, new Color(1f, 0.95f, 0.8f), "Desc");
            desc.alignment = TextAnchor.MiddleCenter;
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            Ui.Place((RectTransform)desc.transform, new Vector2(0.5f, 1f), new Vector2(0, -620), new Vector2(880, 110));
        }
    }
}
