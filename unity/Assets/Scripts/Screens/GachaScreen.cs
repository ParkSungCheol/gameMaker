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
    /// 뽑기 — 냥코대전쟁류 가챠 연출을 참고한 시퀀스:
    /// 배경 암전 → 상자가 3단으로 점점 격하게 요동(등급 힌트 색 글로우) →
    /// 섬광 + 개봉 + 이중 파동 링 + 광선 방사 → 회전 선버스트를 배경으로
    /// 검은 실루엣이 떠오르고 → 재섬광과 함께 컬러 공개 + 콘페티 + 반짝임 →
    /// ★이 하나씩 박히고 이름이 펀치 인. 영웅+ 화면 진동, 전설은 2차 광선 + 금빛 세례.
    /// 테스트 모드(Dev.FreeGacha)에서는 무료.
    /// </summary>
    public class GachaScreen : MonoBehaviour
    {
        /// <summary>1회 비용 — 테스트 모드(Dev.FreeGacha)에서는 0 (무한 뽑기).</summary>
        public static int Cost => Core.Dev.FreeGacha ? 0 : 100;

        static readonly Color[] TierColors =
        {
            Color.white,
            new Color(0.88f, 0.88f, 0.88f), // 1 일반
            new Color(0.45f, 0.9f, 0.5f),   // 2 고급
            new Color(0.4f, 0.7f, 1f),      // 3 희귀
            new Color(0.8f, 0.5f, 1f),      // 4 영웅
            new Color(1f, 0.84f, 0.3f),     // 5 전설
        };
        static readonly string[] TierNames = { "", "일반", "고급", "희귀", "영웅", "전설" };

        Canvas canvas;
        RectTransform root;       // 화면 진동용 컨테이너
        Text moneyText;
        Image chest, whiteFlash, dimmer;
        Button drawBtn;
        Image drawBtnImg;
        RectTransform resultRoot; // 결과 연출 (매 뽑기마다 갈아엎음 — 자식 코루틴은 null 가드로 종료)
        bool drawing;
        static Sprite[] burstFrames;

        void Start()
        {
            canvas = Ui.CreateCanvas(transform, "GachaCanvas");
            MenuBackdrop.Build(this, canvas, dim: 0.7f, withGround: false);

            // 연출용 암전막 — 캔버스 전체(좌우 여백 없이)를 덮는다
            dimmer = Ui.Image(canvas.transform, null, "Dimmer");
            dimmer.color = new Color(0, 0, 0, 0);
            dimmer.raycastTarget = false;
            Ui.Stretch((RectTransform)dimmer.transform);

            root = Ui.Panel(canvas.transform, new Color(0, 0, 0, 0), "Root");
            Ui.Place(root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080));

            // ── 상시 배경: 바닥 조명 + 피어오르는 금가루 (요란한 효과는 뽑기 순간에만) ──
            var floorGlow = Ui.Image(root, SpriteBank.Circle, "FloorGlow");
            floorGlow.raycastTarget = false;
            floorGlow.color = new Color(1f, 0.85f, 0.45f, 0.2f);
            Ui.Place((RectTransform)floorGlow.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -300), new Vector2(560, 130));
            StartCoroutine(GoldDust());

            var title = Ui.OutlinedLabel(root, "뽑기", 52, Color.white, "Title");
            Ui.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -55));
            // 확률표 ? 버튼
            var rateBtn = Ui.TextButton(root, "확률 ?", 28, new Vector2(130, 54), ShowRates,
                new Color(0.3f, 0.26f, 0.18f, 0.95f), "RateBtn");
            Ui.Place((RectTransform)rateBtn.transform, new Vector2(0.5f, 1f), new Vector2(190, -55));

            var moneyPanel = Ui.Image(root, SpriteBank.GetEnv("panel_parchment"), "MoneyPanel");
            Ui.Place((RectTransform)moneyPanel.transform, new Vector2(0f, 1f), new Vector2(160, -55), new Vector2(280, 82));
            moneyText = Ui.CenteredIconValue(moneyPanel.transform, SpriteBank.GetEnv("icon_coin"),
                "0", 42, Color.white, "Money");
            Ui.Place((RectTransform)moneyText.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0, 2));

            var back = Ui.CircleIconButton(root, "icon_return", 92,
                () => { if (!drawing) ScreenRouter.I.Show(ScreenId.Main); }, "BackButton");
            Ui.Place((RectTransform)back.transform, new Vector2(1f, 0f), new Vector2(-58, 50));

            resultRoot = Ui.Panel(root, new Color(0, 0, 0, 0), "Result");
            Ui.Place(resultRoot, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1100, 820));

            // 보물상자 (황금 상자, 열림 애니메이션 보유) — 가만히 놓여 있다
            chest = Ui.Image(root, SpriteBank.GetEnv("gacha_chest_0"), "Chest");
            chest.preserveAspect = true;
            chest.raycastTarget = false;
            Ui.Place((RectTransform)chest.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -190), new Vector2(320, 320));

            // 뽑기 버튼 — 우측 하단, 되돌아가기 왼쪽 (결과 무대를 가리지 않게)
            drawBtn = Ui.ImageButton(root, SpriteBank.GetEnv("btn_wood"), new Vector2(300, 122), TryDraw, "DrawBtn");
            Ui.Place((RectTransform)drawBtn.transform, new Vector2(1f, 0f), new Vector2(-290, 62));
            Ui.PressedSwap(drawBtn, SpriteBank.GetEnv("btn_wood_pressed"));
            drawBtnImg = drawBtn.GetComponent<Image>();
            var priceText = Ui.CenteredIconValue(drawBtn.transform, SpriteBank.GetEnv("icon_coin"),
                Cost + "  뽑기", 38, new Color(1f, 0.9f, 0.3f), "Price");
            Ui.Place((RectTransform)priceText.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0, 4));

            whiteFlash = Ui.Image(canvas.transform, null, "Flash");
            whiteFlash.color = new Color(1f, 1f, 1f, 0f);
            whiteFlash.raycastTarget = false;
            Ui.Stretch((RectTransform)whiteFlash.transform);

            RefreshMoney();
        }

        void RefreshMoney() => moneyText.text = DataHub.I.GetPlayer().money.ToString();

        static readonly int[] TierRates = { 0, 40, 30, 18, 9, 3 };

        /// <summary>확률표 팝업 — 등급별 확률 + 등급 안 유닛별(1/n) 확률을 스크롤 목록으로.</summary>
        void ShowRates()
        {
            if (drawing) return;
            var board = Ui.Popup(canvas.transform, "뽑기 확률", new Vector2(1180, 900));
            var viewport = Ui.Panel(board, new Color(0, 0, 0, 0), "RateViewport");
            Ui.Place(viewport, new Vector2(0.5f, 1f), new Vector2(-20, -120), new Vector2(1020, 740));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Ui.Panel(viewport, new Color(0, 0, 0, 0), "Content");
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            var sr = viewport.gameObject.AddComponent<ScrollRect>();
            sr.viewport = viewport; sr.content = content; sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 40f;
            var vbar = Ui.CleanScrollbar(board, false, new Vector2(1f, 1f), new Vector2(-44, -120), new Vector2(16, 740));
            sr.verticalScrollbar = vbar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            var all = DataHub.I.GetMonsters().Where(m => m.IsOur && !m.IsCastle && m.tier > 0).ToList();
            float y = -8f;
            for (int t = 5; t >= 1; t--) // 희귀한 등급부터
            {
                var units = all.Where(m => m.tier == t).ToList();
                if (units.Count == 0) continue;
                float each = TierRates[t] / (float)units.Count;
                var head = Ui.OutlinedLabel(content, new string('★', t) + " " + TierNames[t] + "  " + TierRates[t] + "%   (" +
                    units.Count + "종 · 1종당 " + each.ToString("0.00") + "%)", 28, TierColors[t], "Head" + t);
                head.alignment = TextAnchor.MiddleLeft;
                Ui.Place((RectTransform)head.transform, new Vector2(0f, 1f), new Vector2(24, y), new Vector2(980, 44));
                var rule = Ui.Panel(content, new Color(TierColors[t].r, TierColors[t].g, TierColors[t].b, 0.45f), "Rule" + t);
                Ui.Place(rule, new Vector2(0f, 1f), new Vector2(24, y - 46), new Vector2(960, 1));
                y -= 58f;
                foreach (var u in units)
                {
                    var fr = SpriteBank.GetFrames(u.SpriteName, "move");
                    var img = Ui.Image(content, fr.Length > 0 ? fr[0] : null, "Img_" + u.name);
                    img.preserveAspect = true; img.raycastTarget = false;
                    Ui.Place((RectTransform)img.transform, new Vector2(0f, 1f), new Vector2(60, y), new Vector2(46, 46));
                    var nm = Ui.OutlinedLabel(content, u.DisplayName, 24, Color.white, "Nm_" + u.name);
                    nm.alignment = TextAnchor.MiddleLeft;
                    Ui.Place((RectTransform)nm.transform, new Vector2(0f, 1f), new Vector2(120, y - 8), new Vector2(300, 32));
                    var role = Ui.OutlinedLabel(content, u.role, 22, new Color(1f, 0.9f, 0.6f), "Role_" + u.name);
                    role.alignment = TextAnchor.MiddleLeft;
                    Ui.Place((RectTransform)role.transform, new Vector2(0f, 1f), new Vector2(480, y - 8), new Vector2(160, 32));
                    var pr = Ui.OutlinedLabel(content, each.ToString("0.00") + "%", 24, Color.white, "Pr_" + u.name);
                    pr.alignment = TextAnchor.MiddleRight;
                    Ui.Place((RectTransform)pr.transform, new Vector2(1f, 1f), new Vector2(-30, y - 8), new Vector2(160, 32));
                    y -= 50f;
                }
                y -= 26f;
            }
            content.sizeDelta = new Vector2(0, -y + 20f);
        }

        void TryDraw()
        {
            if (drawing) return;
            GachaResult result;
            try { result = DataHub.I.DrawGacha(Cost); }
            catch (GameException)
            {
                Ui.Flash(this, drawBtnImg, new Color(0.9f, 0.2f, 0.2f));
                return;
            }
            RefreshMoney();
            StartCoroutine(Reveal(result));
        }

        // ─────────────────────── 연출 시퀀스 ───────────────────────

        IEnumerator Reveal(GachaResult result)
        {
            drawing = true;
            drawBtn.interactable = false;
            foreach (Transform c in resultRoot) Destroy(c.gameObject);

            int tier = Mathf.Clamp(result.unit.tier, 1, 5);
            Color tierCol = TierColors[tier];
            var chestRt = (RectTransform)chest.transform;
            chest.sprite = SpriteBank.GetEnv("gacha_chest_0"); // 닫힌 상자로 리셋
            chest.color = Color.white;                          // (지난 연출에서 사라졌다면 복귀)

            // 0) 암전 — 무대에 집중
            yield return Fade(dimmer, 0f, 0.6f, 0.18f);

            // 1) 어두운 분위기 속 상자만 — 금열쇠가 날아와 꽂히고, 두 번 '철컥' 돌린다
            yield return Fade(dimmer, 0.6f, 0.78f, 0.25f); // 더 깊은 암전, 상자만 남는다
            yield return new WaitForSeconds(0.2f);
            yield return KeyTurn();

            // 2) 열쇠가 돌아가자 뚜껑이 조금씩... 점점 빨리 열린다
            chest.sprite = SpriteBank.GetEnv("gacha_chest_1");
            yield return new WaitForSeconds(0.4f);
            chest.sprite = SpriteBank.GetEnv("gacha_chest_2");
            // 심장박동 — 암전이 꿀렁이며 조여온다
            yield return Fade(dimmer, 0.78f, 0.88f, 0.12f);
            yield return Fade(dimmer, 0.88f, 0.78f, 0.12f);
            chest.sprite = SpriteBank.GetEnv("gacha_chest_3");
            yield return new WaitForSeconds(0.22f);

            // 2) 개봉 — 뚜껑이 벌컥 열리는 프레임 애니메이션 + 섬광 + 폭발 + 파동 링 + 광선 (전부 금빛)
            var gold = new Color(1f, 0.9f, 0.55f);
            StartCoroutine(FlashOnce(0.95f, 0.06f, 0.35f));
            StartCoroutine(ChestOpenAnim());
            SpawnBurst(new Vector2(0, -170), 460f);
            SpawnRays(root, new Vector2(0, -170), gold, 12, 560f);
            if (tier >= 4) StartCoroutine(ShakeRoot(0.4f, tier == 5 ? 24f : 13f));
            yield return new WaitForSeconds(0.2f);

            // 3) 상자는 역할 끝 — 사라지고, 스포트라이트가 정중앙 무대를 비춘다
            StartCoroutine(ChestFadeOut());
            Spotlight();

            // 4) 유닛 등장 — 정면 프레임이 있으면 "화면 앞으로 달려오는" 등장,
            //    없으면 실루엣 서스펜스 → 컬러 공개
            var front = SpriteBank.GetFrames(result.unit.SpriteName, "front");
            var move = SpriteBank.GetFrames(result.unit.SpriteName, "move");
            bool hasFront = false; // 등장은 실루엣 → 공개로 통일 (front 프레임은 다른 연출용으로 보존)

            var unitImg = Ui.Image(resultRoot, null, "Unit");
            unitImg.raycastTarget = false;
            unitImg.preserveAspect = true;
            var unitRt = (RectTransform)unitImg.transform;
            Ui.Place(unitRt, new Vector2(0.5f, 0.5f), new Vector2(0, -180), new Vector2(380, 380)); // 발이 바닥 하이라이트에 닿게
            float pt = 0f;

            if (hasFront)
            {
                // 달려온다: 멀리(작게)에서 스포트라이트 안으로 뛰어 들어오며 커진다
                unitImg.sprite = front[0];
                unitImg.color = Color.white;
                const float runDur = 0.75f;
                int fi = 0;
                float ft = 0f;
                while (pt < runDur)
                {
                    pt += Time.deltaTime;
                    ft += Time.deltaTime;
                    if (ft >= 0.09f) { ft = 0f; fi = (fi + 1) % front.Length; unitImg.sprite = front[fi]; }
                    float k = Mathf.Clamp01(pt / runDur);
                    float e = 1f - (1f - k) * (1f - k);
                    float s = Mathf.Lerp(0.22f, 1.12f, e);
                    unitRt.localScale = new Vector3(s, s, 1f);
                    unitRt.anchoredPosition = new Vector2(0, Mathf.Lerp(-320, -120, e) + Mathf.Abs(Mathf.Sin(pt * 16f)) * 14f * (1f - k));
                    yield return null;
                }
                unitImg.sprite = front[0];
                Confetti(26 + tier * 6, tier);
                pt = 0f;
                while (pt < 0.16f) // 도착 펀치
                {
                    pt += Time.deltaTime;
                    float k = Mathf.Clamp01(pt / 0.16f);
                    float s = 1.12f - 0.12f * k + 0.1f * Mathf.Sin(k * Mathf.PI);
                    unitRt.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
                unitRt.localScale = Vector3.one;
            }
            else
            {
                unitImg.sprite = move.Length > 0 ? move[0] : null;
                unitImg.color = new Color(0.05f, 0.05f, 0.1f, 1f); // 검은 실루엣
                while (pt < 0.3f) // 실루엣 상승
                {
                    pt += Time.deltaTime;
                    float k = Mathf.Clamp01(pt / 0.3f);
                    float s = Mathf.Lerp(0.25f, 1f, 1f - (1f - k) * (1f - k));
                    unitRt.localScale = new Vector3(s, s, 1f);
                    unitRt.anchoredPosition = new Vector2(0, Mathf.Lerp(-300, -180, k));
                    yield return null;
                }
                yield return new WaitForSeconds(0.22f); // "누구지?" 한 박자

                StartCoroutine(FlashOnce(0.55f, 0.05f, 0.22f)); // 재섬광과 함께 정체 공개
                unitImg.color = Color.white;
                Confetti(26 + tier * 6, tier);
                pt = 0f;
                while (pt < 0.18f) // 펀치 스케일
                {
                    pt += Time.deltaTime;
                    float k = Mathf.Clamp01(pt / 0.18f);
                    float s = 1f + 0.28f * Mathf.Sin(k * Mathf.PI);
                    unitRt.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
                unitRt.localScale = Vector3.one;
            }

            // 5) ★ 하나씩 + 이름 펀치 인 + NEW/+N 배지
            var starText = Ui.OutlinedLabel(resultRoot, "", 36, tierCol, "Stars");
            Ui.Place((RectTransform)starText.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(600, 46));
            for (int i = 0; i < tier; i++)
            {
                starText.text += "★";
                yield return new WaitForSeconds(0.09f);
            }
            starText.text += " " + TierNames[tier];

            var plate = Ui.Image(resultRoot, SpriteBank.GetEnv("panel_parchment_m"), "NamePlate");
            plate.raycastTarget = false;
            var nameRt = (RectTransform)plate.transform;
            Ui.Place(nameRt, new Vector2(0.5f, 0.5f), new Vector2(0, 235), new Vector2(560, 88));
            var nameLabel = Ui.OutlinedLabel(nameRt, result.unit.DisplayName, 48, Color.white, "Name");
            Ui.Place((RectTransform)nameLabel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(540, 62));
            pt = 0f;
            while (pt < 0.16f)
            {
                pt += Time.deltaTime;
                float k = Mathf.Clamp01(pt / 0.16f);
                nameRt.localScale = Vector3.one * (1.5f - 0.5f * k);
                yield return null;
            }
            nameRt.localScale = Vector3.one;

            var badge = Ui.RoundedPanel(resultRoot, result.isNew
                ? new Color(0.2f, 0.7f, 0.3f, 0.95f)
                : new Color(0.92f, 0.76f, 0.15f, 0.95f), "Badge");
            var badgeRt = (RectTransform)badge.transform;
            Ui.Place(badgeRt, new Vector2(0.5f, 0.5f), new Vector2(0, 92), new Vector2(230, 54));
            var badgeText = Ui.Label(badge.transform, result.isNew ? "NEW!" : "+" + result.dupes + " 강화!",
                30, result.isNew ? Color.white : new Color(0.25f, 0.15f, 0f), "BadgeText");
            badgeText.alignment = TextAnchor.MiddleCenter;
            Ui.Stretch(badgeText.rectTransform);
            pt = 0f;
            while (pt < 0.14f)
            {
                pt += Time.deltaTime;
                badgeRt.localScale = Vector3.one * (0.3f + 0.7f * Mathf.Clamp01(pt / 0.14f));
                yield return null;
            }
            badgeRt.localScale = Vector3.one;

            // 6) 전설 피날레: 2차 금빛 광선 + 콘페티 세례
            if (tier == 5)
            {
                SpawnRays(resultRoot, new Vector2(0, -180), new Color(1f, 0.85f, 0.35f), 14, 700f);
                Confetti(30, 5);
                StartCoroutine(ShakeRoot(0.3f, 10f));
            }

            yield return Fade(dimmer, 0.6f, 0.35f, 0.3f); // 결과 감상 중에도 살짝 어둡게 유지
            drawBtn.interactable = true;
            drawing = false;
        }

        // ─────────────────────── 이펙트 조각들 ───────────────────────

        IEnumerator Fade(Image img, float from, float to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
                img.color = new Color(0, 0, 0, a);
                yield return null;
            }
        }

        IEnumerator FlashOnce(float peak, float inDur, float outDur)
        {
            float t = 0f;
            while (t < inDur)
            {
                t += Time.deltaTime;
                whiteFlash.color = new Color(1f, 1f, 1f, peak * Mathf.Clamp01(t / inDur));
                yield return null;
            }
            t = 0f;
            while (t < outDur)
            {
                t += Time.deltaTime;
                whiteFlash.color = new Color(1f, 1f, 1f, peak * (1f - Mathf.Clamp01(t / outDur)));
                yield return null;
            }
            whiteFlash.color = new Color(1f, 1f, 1f, 0f);
        }

        /// <summary>확장되며 사라지는 파동 링.</summary>
        void PopRing(Vector2 pos, float size, Color c, float alpha, RectTransform parent = null)
        {
            var ring = Ui.Image(parent != null ? (Transform)parent : root, SpriteBank.Circle, "Ring");
            ring.raycastTarget = false;
            ring.color = new Color(c.r, c.g, c.b, alpha);
            var rt = (RectTransform)ring.transform;
            Ui.Place(rt, new Vector2(0.5f, 0.5f), pos, new Vector2(size * 0.25f, size * 0.25f));
            StartCoroutine(RingOut(rt, ring, size));
        }

        IEnumerator RingOut(RectTransform rt, Image img, float size)
        {
            float t = 0f;
            const float dur = 0.4f;
            Color c0 = img.color;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (img == null) yield break;
                float k = Mathf.Clamp01(t / dur);
                rt.sizeDelta = Vector2.one * Mathf.Lerp(size * 0.25f, size, 1f - (1f - k) * (1f - k));
                img.color = new Color(c0.r, c0.g, c0.b, c0.a * (1f - k));
                yield return null;
            }
            if (img != null) Destroy(img.gameObject);
        }

        /// <summary>반짝 별 — 작게 나타나 커졌다 사라진다.</summary>
        void Sparkle(Vector2 pos, Color c, RectTransform parent = null)
        {
            var s = Ui.Image(parent != null ? (Transform)parent : root, SpriteBank.Circle, "Sparkle");
            s.raycastTarget = false;
            s.color = new Color(c.r, c.g, c.b, 0.95f);
            var rt = (RectTransform)s.transform;
            Ui.Place(rt, new Vector2(0.5f, 0.5f), pos, new Vector2(8, 8));
            StartCoroutine(SparkleAnim(rt, s));
        }

        IEnumerator SparkleAnim(RectTransform rt, Image img)
        {
            float t = 0f;
            float dur = Random.Range(0.25f, 0.45f);
            float size = Random.Range(14f, 30f);
            Color c0 = img.color;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (img == null) yield break;
                float k = Mathf.Clamp01(t / dur);
                float s = Mathf.Sin(k * Mathf.PI);
                rt.sizeDelta = new Vector2(size * s, size * s * 2.6f); // 세로로 긴 십자 반짝임 느낌
                img.color = new Color(c0.r, c0.g, c0.b, c0.a * s);
                yield return null;
            }
            if (img != null) Destroy(img.gameObject);
        }

        /// <summary>스포트라이트 — 위에서 아래로 넓어지는 원뿔 조명 + 바닥 풀.
        /// 세로로 쌓은 타원들로 원뿔을 근사한다.</summary>
        void Spotlight()
        {
            var spot = Ui.Panel(resultRoot, new Color(0, 0, 0, 0), "Spotlight");
            Ui.Place(spot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
            spot.SetAsFirstSibling(); // 유닛 뒤
            for (int i = 0; i < 9; i++)
            {
                float k = i / 8f;
                var seg = Ui.Image(spot, SpriteBank.Circle, "Seg");
                seg.raycastTarget = false;
                seg.color = new Color(1f, 0.97f, 0.85f, Mathf.Lerp(0.3f, 0.06f, k));
                var rt = (RectTransform)seg.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, Mathf.Lerp(220f, -520f, k)); // 제목 아래에서 시작
                rt.sizeDelta = new Vector2(Mathf.Lerp(130f, 1240f, k), Mathf.Lerp(200f, 300f, k)); // 아래로 갈수록 계속 확장
            }
        }

        /// <summary>콘페티 — 색색 조각이 위에서 쏟아지며 회전 낙하.</summary>
        void Confetti(int n, int tier)
        {
            for (int i = 0; i < n; i++)
            {
                var c = tier == 5 && i % 2 == 0
                    ? new Color(1f, 0.84f, 0.3f)
                    : Color.HSVToRGB(Random.value, 0.75f, 1f);
                var piece = Ui.Image(resultRoot, null, "Confetti");
                piece.raycastTarget = false;
                piece.color = c;
                var rt = (RectTransform)piece.transform;
                Ui.Place(rt, new Vector2(0.5f, 0.5f),
                    new Vector2(Random.Range(-320f, 320f), Random.Range(180f, 330f)),
                    new Vector2(Random.Range(9f, 16f), Random.Range(14f, 22f)));
                rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                StartCoroutine(ConfettiFall(rt, piece));
            }
        }

        IEnumerator ConfettiFall(RectTransform rt, Image img)
        {
            float vy = Random.Range(60f, 140f);
            float vx = Random.Range(-70f, 70f);
            float spin = Random.Range(-540f, 540f);
            float t = 0f;
            float life = Random.Range(0.9f, 1.4f);
            Color c0 = img.color;
            while (t < life)
            {
                t += Time.deltaTime;
                if (img == null) yield break;
                vy += 620f * Time.deltaTime;
                rt.anchoredPosition += new Vector2(vx * Time.deltaTime, -vy * Time.deltaTime);
                rt.Rotate(0, 0, spin * Time.deltaTime);
                if (t > life - 0.3f)
                    img.color = new Color(c0.r, c0.g, c0.b, (life - t) / 0.3f);
                yield return null;
            }
            if (img != null) Destroy(img.gameObject);
        }

        void SpawnBurst(Vector2 pos, float size)
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
            var b = Ui.Image(root, burstFrames[0], "Burst");
            b.raycastTarget = false;
            b.preserveAspect = true;
            b.color = new Color(1f, 0.85f, 0.45f);
            Ui.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            StartCoroutine(PlayOnce(b, burstFrames, 18f));
        }

        IEnumerator PlayOnce(Image img, Sprite[] fr, float fps)
        {
            foreach (var s in fr)
            {
                if (img == null) yield break;
                img.sprite = s;
                yield return new WaitForSeconds(1f / fps);
            }
            if (img != null) Destroy(img.gameObject);
        }

        /// <summary>광선 방사 — 길쭉한 빛줄기가 사방으로 뻗으며 사라진다.</summary>
        void SpawnRays(Transform parent, Vector2 pos, Color c, int n, float len)
        {
            for (int i = 0; i < n; i++)
            {
                var ray = Ui.Image(parent, SpriteBank.Circle, "Ray");
                ray.raycastTarget = false;
                ray.color = new Color(c.r, c.g, c.b, 0.85f);
                var rt = (RectTransform)ray.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(len * 0.2f, 24f);
                rt.localRotation = Quaternion.Euler(0, 0, i * (360f / n) + Random.Range(-6f, 6f));
                StartCoroutine(RayOut(rt, ray, len));
            }
        }

        IEnumerator RayOut(RectTransform rt, Image img, float len)
        {
            float t = 0f;
            const float dur = 0.45f;
            Color c0 = img.color;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (img == null) yield break;
                float k = Mathf.Clamp01(t / dur);
                rt.sizeDelta = new Vector2(Mathf.Lerp(len * 0.2f, len, 1f - (1f - k) * (1f - k)), Mathf.Lerp(24f, 7f, k));
                img.color = new Color(c0.r, c0.g, c0.b, c0.a * (1f - k * k));
                yield return null;
            }
            if (img != null) Destroy(img.gameObject);
        }

        /// <summary>금열쇠 — 왼쪽/오른쪽 걸쇠(자물쇠)를 하나씩: 구멍에 꽂아 '철컥' 돌리면
        /// 걸쇠가 툭 튕겨 떨어진다. 회전축은 열쇠 꽂힌 지점(pivot).</summary>
        IEnumerator KeyTurn()
        {
            var key = Ui.Image(root, SpriteBank.GetEnv("gacha_key"), "Key");
            key.preserveAspect = true;
            key.raycastTarget = false;
            var keyRt = (RectTransform)key.transform;
            keyRt.anchorMin = keyRt.anchorMax = new Vector2(0.5f, 0.5f);
            keyRt.pivot = new Vector2(0.9f, 0.5f); // 회전축 = 키 끝(오른쪽) — 구멍에 꽂히는 지점.
                                                   // 스프라이트: 왼쪽 = 동그란 손잡이, 오른쪽 = 키 날
            keyRt.sizeDelta = new Vector2(96, 64);
            keyRt.anchoredPosition = new Vector2(240, -150);
            key.color = new Color(1f, 1f, 1f, 0f);
            var chestRt2 = (RectTransform)chest.transform;

            // 왼쪽 걸쇠 → 오른쪽 걸쇠
            float[] holes = { -98f, 50f }; // 상자 스프라이트 실측 걸쇠 위치 (표시 320px 기준)
            for (int h = 0; h < 2; h++)
            {
                Vector2 from = keyRt.anchoredPosition;
                Vector2 to = new Vector2(holes[h], -226f); // 걸쇠(열쇠 구멍) 위치
                float t = 0f;
                const float slide = 0.26f;
                while (t < slide)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t / slide);
                    float e = 1f - (1f - k) * (1f - k);
                    keyRt.anchoredPosition = Vector2.Lerp(from, to, e);
                    if (h == 0) key.color = new Color(1f, 1f, 1f, k);
                    yield return null;
                }
                yield return new WaitForSeconds(0.18f);

                t = 0f; // 철컥 — 구멍 축으로 좌→우 1회전
                while (t < 0.14f)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t / 0.14f);
                    keyRt.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0f, -90f, k));
                    float p = 1f + 0.05f * Mathf.Sin(k * Mathf.PI); // 상자 움찔
                    chestRt2.localScale = new Vector3(p, p, 1f);
                    yield return null;
                }
                chestRt2.localScale = Vector3.one;
                StartCoroutine(LatchPop(holes[h])); // 걸쇠가 툭 — 자물쇠 해제 효과
                yield return new WaitForSeconds(0.28f);
                keyRt.localRotation = Quaternion.identity; // 다음 구멍으로 빼며 원위치
            }

            float ft = 0f; // 열쇠 퇴장
            while (ft < 0.18f)
            {
                ft += Time.deltaTime;
                key.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01(ft / 0.18f));
                yield return null;
            }
            Destroy(key.gameObject);
        }

        /// <summary>걸쇠가 바깥으로 젖혀지며 떨어져 사라진다 — 자물쇠 열림 효과.</summary>
        IEnumerator LatchPop(float x)
        {
            var latch = Ui.Image(root, null, "Latch");
            latch.raycastTarget = false;
            latch.color = new Color(0.5f, 0.34f, 0.12f, 1f); // 걸쇠 쇠붙이 톤
            var rt = (RectTransform)latch.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1f); // 위쪽 경첩 기준으로 젖혀진다
            rt.sizeDelta = new Vector2(24, 22);
            rt.anchoredPosition = new Vector2(x, -222f);
            float sign = x < 0 ? 1f : -1f; // 바깥쪽으로
            float t = 0f;
            const float dur = 0.34f;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (latch == null) yield break;
                float k = Mathf.Clamp01(t / dur);
                rt.localRotation = Quaternion.Euler(0, 0, 115f * sign * k);
                rt.anchoredPosition = new Vector2(x + 14f * sign * k, -222f - 34f * k * k);
                latch.color = new Color(0.5f, 0.34f, 0.12f, 1f - k * k);
                yield return null;
            }
            if (latch != null) Destroy(latch.gameObject);
        }

        /// <summary>'똭' — 마지막 프레임, 완전 개방.</summary>
        IEnumerator ChestOpenAnim()
        {
            chest.sprite = SpriteBank.GetEnv("gacha_chest_4");
            yield break;
        }

        /// <summary>캐릭터 등장 직전, 상자가 스르르 사라진다 — 무대는 캐릭터의 것.</summary>
        IEnumerator ChestFadeOut()
        {
            float t = 0f;
            const float dur = 0.25f;
            while (t < dur)
            {
                t += Time.deltaTime;
                chest.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01(t / dur));
                yield return null;
            }
        }

        /// <summary>상시 배경 — 금가루가 바닥에서 피어올라 사라진다.</summary>
        IEnumerator GoldDust()
        {
            while (true)
            {
                var dust = Ui.Image(root, SpriteBank.Circle, "Dust");
                dust.raycastTarget = false;
                dust.color = new Color(1f, 0.88f, 0.55f, Random.Range(0.25f, 0.5f));
                var rt = (RectTransform)dust.transform;
                float size = Random.Range(6f, 13f);
                Ui.Place(rt, new Vector2(0.5f, 0.5f),
                    new Vector2(Random.Range(-700f, 700f), Random.Range(-480f, -260f)),
                    new Vector2(size, size));
                rt.SetSiblingIndex(1); // 무대 요소 위, 상자/UI 아래
                StartCoroutine(DustRise(rt, dust));
                yield return new WaitForSeconds(Random.Range(0.12f, 0.28f));
            }
        }

        IEnumerator DustRise(RectTransform rt, Image img)
        {
            float t = 0f;
            float life = Random.Range(1.6f, 2.6f);
            float vy = Random.Range(45f, 95f);
            float sway = Random.Range(15f, 40f);
            float phase = Random.Range(0f, 6.28f);
            Color c0 = img.color;
            while (t < life)
            {
                t += Time.deltaTime;
                if (img == null) yield break;
                rt.anchoredPosition += new Vector2(Mathf.Sin(t * 2.2f + phase) * sway * Time.deltaTime, vy * Time.deltaTime);
                float k = t / life;
                img.color = new Color(c0.r, c0.g, c0.b, c0.a * (k < 0.2f ? k / 0.2f : 1f - (k - 0.2f) / 0.8f));
                yield return null;
            }
            if (img != null) Destroy(img.gameObject);
        }

        IEnumerator ShakeRoot(float dur, float amp)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = 1f - t / dur;
                root.anchoredPosition = new Vector2(Random.Range(-amp, amp) * k, Random.Range(-amp, amp) * k);
                yield return null;
            }
            root.anchoredPosition = Vector2.zero;
        }
    }
}
