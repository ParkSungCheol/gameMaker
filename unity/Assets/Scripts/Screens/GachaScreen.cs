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
    /// 뽑기 — 보물상자 연출:
    /// [뽑기] → 상자가 점점 격하게 흔들리고 → 섬광 + 폭발 + 금빛 광선 방사 →
    /// 등급색 글로우를 두르고 유닛이 떠오른다. NEW! 또는 중복 +N 강화! 배지.
    /// 영웅/영웅 이상은 화면 흔들림 + 추가 광선 웨이브로 더 화려하게.
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
        RectTransform root;       // 화면 흔들림용 컨테이너
        Text moneyText;
        Image chest, whiteFlash;
        Button drawBtn;
        Image drawBtnImg;
        RectTransform resultRoot; // 결과 연출 (매 뽑기마다 갈아엎음)
        bool drawing;
        static Sprite[] burstFrames;

        void Start()
        {
            canvas = Ui.CreateCanvas(transform, "GachaCanvas");
            MenuBackdrop.Build(this, canvas, dim: 0.7f, withGround: false);

            root = Ui.Panel(canvas.transform, new Color(0, 0, 0, 0), "Root");
            Ui.Place(root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080));

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

            // 결과 표시 영역 (상자 위쪽)
            resultRoot = Ui.Panel(root, new Color(0, 0, 0, 0), "Result");
            Ui.Place(resultRoot, new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(1000, 620));

            // 보물상자 — 은은한 금빛 링 위에서 둥실거린다
            var idleGlow = Ui.Image(root, SpriteBank.Circle, "IdleGlow");
            idleGlow.color = new Color(1f, 0.85f, 0.4f, 0.16f);
            idleGlow.raycastTarget = false;
            Ui.Place((RectTransform)idleGlow.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -215), new Vector2(430, 160));

            chest = Ui.Image(root, SpriteBank.GetEnv("icon_chest_closed"), "Chest");
            chest.preserveAspect = true;
            chest.raycastTarget = false;
            Ui.Place((RectTransform)chest.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -190), new Vector2(300, 220));
            StartCoroutine(ChestIdle());

            // 뽑기 버튼
            drawBtn = Ui.ImageButton(root, SpriteBank.GetEnv("btn_wood"), new Vector2(320, 130), TryDraw, "DrawBtn");
            Ui.Place((RectTransform)drawBtn.transform, new Vector2(0.5f, 0f), new Vector2(0, 110));
            Ui.PressedSwap(drawBtn, SpriteBank.GetEnv("btn_wood_pressed"));
            drawBtnImg = drawBtn.GetComponent<Image>();
            var priceText = Ui.CenteredIconValue(drawBtn.transform, SpriteBank.GetEnv("icon_coin"),
                Cost + "  뽑기", 38, new Color(1f, 0.9f, 0.3f), "Price");
            Ui.Place((RectTransform)priceText.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0, 4));

            // 섬광 오버레이 (맨 위)
            whiteFlash = Ui.Image(canvas.transform, null, "Flash");
            whiteFlash.color = new Color(1f, 1f, 1f, 0f);
            whiteFlash.raycastTarget = false;
            Ui.Stretch((RectTransform)whiteFlash.transform);

            RefreshMoney();
        }

        void RefreshMoney() => moneyText.text = DataHub.I.GetPlayer().money.ToString();

        IEnumerator ChestIdle()
        {
            var rt = (RectTransform)chest.transform;
            while (true)
            {
                if (!drawing)
                    rt.anchoredPosition = new Vector2(0, -190 + Mathf.Sin(Time.time * 2.2f) * 7f);
                yield return null;
            }
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

        IEnumerator Reveal(GachaResult result)
        {
            drawing = true;
            drawBtn.interactable = false;
            foreach (Transform c in resultRoot) Destroy(c.gameObject);

            var chestRt = (RectTransform)chest.transform;
            chest.sprite = SpriteBank.GetEnv("icon_chest_closed");

            // 1) 상자 흔들림 — 점점 격해지며 뒤에서 금빛 글로우가 차오른다
            var charge = Ui.Image(root, SpriteBank.Circle, "Charge");
            charge.raycastTarget = false;
            charge.transform.SetSiblingIndex(chest.transform.GetSiblingIndex());
            Ui.Place((RectTransform)charge.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -190), new Vector2(60, 60));
            float t = 0f;
            const float shakeDur = 0.85f;
            while (t < shakeDur)
            {
                t += Time.deltaTime;
                float k = t / shakeDur;
                chestRt.anchoredPosition = new Vector2(
                    Mathf.Sin(t * 55f) * 10f * k, -190 + Mathf.Abs(Mathf.Sin(t * 30f)) * 12f * k);
                chestRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 48f) * 9f * k);
                ((RectTransform)charge.transform).sizeDelta = Vector2.one * (60f + 360f * k);
                charge.color = new Color(1f, 0.85f, 0.4f, 0.35f * k);
                yield return null;
            }
            chestRt.localRotation = Quaternion.identity;
            chestRt.anchoredPosition = new Vector2(0, -190);
            Destroy(charge.gameObject);

            // 2) 섬광 + 상자 개봉 + 폭발 + 광선 방사
            Color tierCol = TierColors[Mathf.Clamp(result.unit.tier, 1, 5)];
            StartCoroutine(FlashOnce(0.9f, 0.08f, 0.3f));
            chest.sprite = SpriteBank.GetEnv("icon_chest");
            SpawnBurst(new Vector2(0, -170), 430f);
            SpawnRays(new Vector2(0, -170), tierCol, 10, 520f);
            if (result.unit.tier >= 4) StartCoroutine(ShakeRoot(0.35f, result.unit.tier == 5 ? 22f : 12f));
            yield return new WaitForSeconds(0.22f);

            // 3) 유닛 등장 — 등급색 글로우 + 오버슈트 스케일
            var glow = Ui.Image(resultRoot, SpriteBank.Circle, "Glow");
            glow.raycastTarget = false;
            glow.color = new Color(tierCol.r, tierCol.g, tierCol.b, 0.32f);
            Ui.Place((RectTransform)glow.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(430, 430));
            StartCoroutine(GlowPulse(glow));

            var frames = SpriteBank.GetFrames(result.unit.SpriteName, "move");
            var unitImg = Ui.Image(resultRoot, frames.Length > 0 ? frames[0] : null, "Unit");
            unitImg.raycastTarget = false;
            unitImg.preserveAspect = true;
            var unitRt = (RectTransform)unitImg.transform;
            Ui.Place(unitRt, new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(320, 320));

            t = 0f;
            const float popDur = 0.32f;
            while (t < popDur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / popDur);
                float s = k < 0.7f ? Mathf.Lerp(0.2f, 1.18f, k / 0.7f) : Mathf.Lerp(1.18f, 1f, (k - 0.7f) / 0.3f);
                unitRt.localScale = new Vector3(s, s, 1f);
                unitRt.anchoredPosition = new Vector2(0, Mathf.Lerp(-160, -30, k));
                yield return null;
            }
            unitRt.localScale = Vector3.one;

            // 이름 + 등급 + NEW!/+N 배지
            int tier = Mathf.Clamp(result.unit.tier, 1, 5);
            var tierLabel = Ui.OutlinedLabel(resultRoot, new string('★', tier) + " " + TierNames[tier], 34, tierCol, "Tier");
            Ui.Place((RectTransform)tierLabel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(600, 44));
            var nameLabel = Ui.OutlinedLabel(resultRoot, result.unit.DisplayName, 52, Color.white, "Name");
            Ui.Place((RectTransform)nameLabel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(800, 64));

            var badge = Ui.RoundedPanel(resultRoot, result.isNew
                ? new Color(0.2f, 0.7f, 0.3f, 0.95f)
                : new Color(0.92f, 0.76f, 0.15f, 0.95f), "Badge");
            Ui.Place((RectTransform)badge.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 148), new Vector2(220, 52));
            var badgeText = Ui.Label(badge.transform, result.isNew ? "NEW!" : "+" + result.dupes + " 강화!",
                30, result.isNew ? Color.white : new Color(0.25f, 0.15f, 0f), "BadgeText");
            badgeText.alignment = TextAnchor.MiddleCenter;
            Ui.Stretch(badgeText.rectTransform);

            // 전설: 광선 한 번 더
            if (tier == 5)
            {
                yield return new WaitForSeconds(0.15f);
                SpawnRays(new Vector2(0, 100), tierCol, 12, 640f);
            }

            drawBtn.interactable = true;
            drawing = false;
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

        /// <summary>금빛 광선 방사 — 길쭉한 타원들이 사방으로 뻗으며 사라진다.</summary>
        void SpawnRays(Vector2 pos, Color c, int n, float len)
        {
            for (int i = 0; i < n; i++)
            {
                var ray = Ui.Image(root, SpriteBank.Circle, "Ray");
                ray.raycastTarget = false;
                ray.color = new Color(c.r, c.g, c.b, 0.8f);
                var rt = (RectTransform)ray.transform;
                Ui.Place(rt, new Vector2(0.5f, 0.5f), pos, new Vector2(len * 0.25f, 26f));
                rt.localRotation = Quaternion.Euler(0, 0, i * (360f / n) + Random.Range(-8f, 8f));
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
                rt.sizeDelta = new Vector2(Mathf.Lerp(len * 0.25f, len, k), Mathf.Lerp(26f, 8f, k));
                // 광선 바깥쪽으로 밀려나가기
                float push = Mathf.Lerp(0f, len * 0.55f, k);
                var dir = rt.localRotation * Vector3.right;
                rt.anchoredPosition += (Vector2)(dir * (push - rt.sizeDelta.x * 0f) * Time.deltaTime * 4f);
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
                root.anchoredPosition = new Vector2(
                    Random.Range(-amp, amp) * k, Random.Range(-amp, amp) * k);
                yield return null;
            }
            root.anchoredPosition = Vector2.zero;
        }
    }
}
