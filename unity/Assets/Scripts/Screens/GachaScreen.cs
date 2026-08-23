using System.Collections;
using System.Collections.Generic;
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

            root = Ui.Panel(canvas.transform, new Color(0, 0, 0, 0), "Root");
            Ui.Place(root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080));

            // 연출용 암전막 (결과/상자 뒤, UI 앞)
            dimmer = Ui.Image(root, null, "Dimmer");
            dimmer.color = new Color(0, 0, 0, 0);
            dimmer.raycastTarget = false;
            Ui.Stretch((RectTransform)dimmer.transform);

            var title = Ui.OutlinedLabel(root, "뽑기", 52, Color.white, "Title");
            Ui.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -55));

            var moneyPanel = Ui.Image(root, SpriteBank.GetEnv("panel_parchment"), "MoneyPanel");
            Ui.Place((RectTransform)moneyPanel.transform, new Vector2(0f, 1f), new Vector2(160, -55), new Vector2(280, 82));
            moneyText = Ui.CenteredIconValue(moneyPanel.transform, SpriteBank.GetEnv("icon_coin"),
                "0", 42, Color.white, "Money");
            Ui.Place((RectTransform)moneyText.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0, 2));

            var back = Ui.CircleIconButton(root, "icon_return", 92,
                () => { if (!drawing) ScreenRouter.I.Show(ScreenId.Main); }, "BackButton");
            Ui.Place((RectTransform)back.transform, new Vector2(1f, 0f), new Vector2(-58, 50));

            resultRoot = Ui.Panel(root, new Color(0, 0, 0, 0), "Result");
            Ui.Place(resultRoot, new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(1100, 640));

            // 보물상자 — 가만히 놓여 있다 (요동은 뽑기 연출에서만)
            var idleGlow = Ui.Image(root, SpriteBank.Circle, "IdleGlow");
            idleGlow.color = new Color(1f, 0.85f, 0.4f, 0.16f);
            idleGlow.raycastTarget = false;
            Ui.Place((RectTransform)idleGlow.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -215), new Vector2(430, 160));

            chest = Ui.Image(root, SpriteBank.GetEnv("icon_chest_closed"), "Chest");
            chest.preserveAspect = true;
            chest.raycastTarget = false;
            Ui.Place((RectTransform)chest.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -190), new Vector2(300, 220));

            drawBtn = Ui.ImageButton(root, SpriteBank.GetEnv("btn_wood"), new Vector2(320, 130), TryDraw, "DrawBtn");
            Ui.Place((RectTransform)drawBtn.transform, new Vector2(0.5f, 0f), new Vector2(0, 110));
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
            chest.sprite = SpriteBank.GetEnv("icon_chest_closed");

            // 0) 암전 — 무대에 집중
            yield return Fade(dimmer, 0f, 0.6f, 0.18f);

            // 1) 3단 요동 — 단계마다 더 격해지고, 링이 터지며, 마지막엔 등급 힌트 색으로 물든다
            var charge = Ui.Image(root, SpriteBank.Circle, "Charge");
            charge.raycastTarget = false;
            charge.transform.SetSiblingIndex(chest.transform.GetSiblingIndex());
            Ui.Place((RectTransform)charge.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -190), new Vector2(80, 80));
            for (int pulse = 0; pulse < 3; pulse++)
            {
                float amp = 5f + pulse * 6f;
                Color glowCol = pulse == 2 && tier >= 3 ? tierCol : new Color(1f, 0.85f, 0.4f);
                float t = 0f;
                const float dur = 0.3f;
                while (t < dur)
                {
                    t += Time.deltaTime;
                    float k = t / dur;
                    chestRt.anchoredPosition = new Vector2(Mathf.Sin(t * 60f) * amp * k, -190 + Mathf.Abs(Mathf.Sin(t * 34f)) * amp * k);
                    chestRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 52f) * (4f + pulse * 4f) * k);
                    ((RectTransform)charge.transform).sizeDelta = Vector2.one * (80f + 130f * pulse + 130f * k);
                    charge.color = new Color(glowCol.r, glowCol.g, glowCol.b, 0.18f + 0.1f * pulse);
                    yield return null;
                }
                // 펄스 마침표: 링 팝 + 반짝이 별
                PopRing(new Vector2(0, -190), 220f + pulse * 90f, glowCol, 0.5f);
                for (int s = 0; s < 3 + pulse * 2; s++)
                    Sparkle(new Vector2(Random.Range(-170f, 170f), -190 + Random.Range(-90f, 120f)),
                        pulse == 2 ? tierCol : new Color(1f, 0.95f, 0.6f));
                yield return new WaitForSeconds(0.1f);
            }
            chestRt.localRotation = Quaternion.identity;
            chestRt.anchoredPosition = new Vector2(0, -190);
            Destroy(charge.gameObject);

            // 2) 개봉 — 섬광 + 폭발 + 이중 파동 링 + 광선 방사
            StartCoroutine(FlashOnce(0.95f, 0.06f, 0.35f));
            chest.sprite = SpriteBank.GetEnv("icon_chest");
            SpawnBurst(new Vector2(0, -170), 460f);
            PopRing(new Vector2(0, -170), 520f, Color.white, 0.8f);
            PopRing(new Vector2(0, -170), 760f, tierCol, 0.6f);
            SpawnRays(root, new Vector2(0, -170), tierCol, 12, 560f);
            if (tier >= 4) StartCoroutine(ShakeRoot(0.4f, tier == 5 ? 24f : 13f));
            yield return new WaitForSeconds(0.2f);

            // 3) 회전 선버스트 무대 (결과가 살아있는 동안 계속 돈다)
            Sunburst(tierCol, 0.16f, 12, 40f);
            Sunburst(tierCol, 0.09f, 8, -24f);

            var glow = Ui.Image(resultRoot, SpriteBank.Circle, "Glow");
            glow.raycastTarget = false;
            glow.color = new Color(tierCol.r, tierCol.g, tierCol.b, 0.3f);
            Ui.Place((RectTransform)glow.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(450, 450));
            StartCoroutine(GlowPulse(glow));

            // 4) 실루엣 서스펜스 → 컬러 공개
            var frames = SpriteBank.GetFrames(result.unit.SpriteName, "move");
            var unitImg = Ui.Image(resultRoot, frames.Length > 0 ? frames[0] : null, "Unit");
            unitImg.raycastTarget = false;
            unitImg.preserveAspect = true;
            unitImg.color = new Color(0.05f, 0.05f, 0.1f, 1f); // 검은 실루엣
            var unitRt = (RectTransform)unitImg.transform;
            Ui.Place(unitRt, new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(340, 340));

            float pt = 0f;
            while (pt < 0.3f) // 실루엣 상승
            {
                pt += Time.deltaTime;
                float k = Mathf.Clamp01(pt / 0.3f);
                float s = Mathf.Lerp(0.25f, 1f, 1f - (1f - k) * (1f - k));
                unitRt.localScale = new Vector3(s, s, 1f);
                unitRt.anchoredPosition = new Vector2(0, Mathf.Lerp(-170, -30, k));
                yield return null;
            }
            yield return new WaitForSeconds(0.22f); // "누구지?" 한 박자

            StartCoroutine(FlashOnce(0.55f, 0.05f, 0.22f)); // 재섬광과 함께 정체 공개
            unitImg.color = Color.white;
            PopRing(new Vector2(0, -30), 560f, tierCol, 0.55f, resultRoot);
            Confetti(26 + tier * 6, tier);
            StartCoroutine(TwinkleAround(unitRt, tierCol));
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

            // 5) ★ 하나씩 + 이름 펀치 인 + NEW/+N 배지
            var starText = Ui.OutlinedLabel(resultRoot, "", 36, tierCol, "Stars");
            Ui.Place((RectTransform)starText.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 202), new Vector2(600, 46));
            for (int i = 0; i < tier; i++)
            {
                starText.text += "★";
                Sparkle(new Vector2((i - (tier - 1) * 0.5f) * 40f, 202f + 130f), tierCol, resultRoot);
                yield return new WaitForSeconds(0.09f);
            }
            starText.text += " " + TierNames[tier];

            var nameLabel = Ui.OutlinedLabel(resultRoot, result.unit.DisplayName, 54, Color.white, "Name");
            var nameRt = (RectTransform)nameLabel.transform;
            Ui.Place(nameRt, new Vector2(0.5f, 0.5f), new Vector2(0, 256), new Vector2(900, 66));
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
            Ui.Place(badgeRt, new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(230, 54));
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

            // 6) 전설 피날레: 2차 광선 + 금빛 세례
            if (tier == 5)
            {
                SpawnRays(resultRoot, new Vector2(0, -30), tierCol, 14, 700f);
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

        /// <summary>결과가 살아있는 동안 유닛 주변에 반짝임을 계속 뿌린다.</summary>
        IEnumerator TwinkleAround(RectTransform target, Color c)
        {
            while (target != null)
            {
                Sparkle((Vector2)target.anchoredPosition +
                    new Vector2(Random.Range(-190f, 190f), Random.Range(-150f, 190f)), c, resultRoot);
                yield return new WaitForSeconds(Random.Range(0.08f, 0.18f));
            }
        }

        /// <summary>회전 선버스트 — 결과 뒤에서 계속 도는 방사 광선판.</summary>
        void Sunburst(Color c, float alpha, int blades, float degPerSec)
        {
            var pivot = Ui.Panel(resultRoot, new Color(0, 0, 0, 0), "Sunburst");
            Ui.Place(pivot, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(10, 10));
            pivot.SetAsFirstSibling(); // 유닛/글로우 뒤
            for (int i = 0; i < blades; i++)
            {
                var blade = Ui.Image(pivot, SpriteBank.Circle, "Blade");
                blade.raycastTarget = false;
                blade.color = new Color(c.r, c.g, c.b, alpha);
                var rt = (RectTransform)blade.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f); // 중심에서 바깥으로 뻗는 날개
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(560f, 54f);
                rt.localRotation = Quaternion.Euler(0, 0, i * (360f / blades));
            }
            StartCoroutine(Rotate(pivot, degPerSec));
        }

        IEnumerator Rotate(RectTransform rt, float degPerSec)
        {
            while (rt != null)
            {
                rt.Rotate(0, 0, degPerSec * Time.deltaTime);
                yield return null;
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

        IEnumerator GlowPulse(Image glow)
        {
            var rt = (RectTransform)glow.transform;
            Color c0 = glow.color;
            while (glow != null)
            {
                float p = 1f + 0.08f * Mathf.Sin(Time.time * 4f);
                rt.localScale = new Vector3(p, p, 1f);
                glow.color = new Color(c0.r, c0.g, c0.b, c0.a * (0.85f + 0.15f * Mathf.Sin(Time.time * 3f)));
                yield return null;
            }
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
