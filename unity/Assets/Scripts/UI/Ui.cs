using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameMaker.UI
{
    /// <summary>
    /// 프로그래밍 방식 uGUI 생성 헬퍼.
    /// 레거시 XML 레이아웃(main.xml, map.xml, battlefield.xml ...) 대신 코드로 UI 를 만든다.
    /// 기준 해상도 1920x1080 (가로모드 — 레거시 landscape 고정에 대응).
    /// </summary>
    public static class Ui
    {
        static Font pixelFont;
        static Font titleFont;

        /// <summary>한글 지원 픽셀 폰트(neodgm). 없으면 내장 폰트로 폴백.</summary>
        public static Font DefaultFont
        {
            get
            {
                if (pixelFont == null)
                    pixelFont = Resources.Load<Font>("Fonts/neodgm");
                return pixelFont != null
                    ? pixelFont
                    : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        /// <summary>타이틀용 대형 픽셀 폰트(ThaleahFat, 영문 전용).</summary>
        public static Font TitleFont
        {
            get
            {
                if (titleFont == null)
                    titleFont = Resources.Load<Font>("Fonts/ThaleahFat");
                return titleFont != null ? titleFont : DefaultFont;
            }
        }

        public static Canvas CreateCanvas(Transform parent, string name = "Canvas")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Panel(Transform parent, Color color, string name = "Panel")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return (RectTransform)go.transform;
        }

        public static Image Image(Transform parent, Sprite sprite, string name = "Image")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = false;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, string name = "Text")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button TextButton(Transform parent, string label, int fontSize,
            Vector2 size, Action onClick, Color? bg = null, string name = "Button")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bg ?? new Color(0.15f, 0.15f, 0.2f, 0.95f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;

            var text = Label(go.transform, label, fontSize, Color.white);
            Stretch((RectTransform)text.transform);
            return btn;
        }

        public static Button ImageButton(Transform parent, Sprite sprite, Vector2 size,
            Action onClick, string name = "ImageButton")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            else img.color = new Color(0.4f, 0.4f, 0.5f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            ((RectTransform)go.transform).sizeDelta = size;
            return btn;
        }

        /// <summary>검은 외곽선이 있는 게임풍 라벨.</summary>
        public static Text OutlinedLabel(Transform parent, string text, int size, Color color, string name = "Text")
        {
            var t = Label(parent, text, size, color, name);
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.9f);
            o.effectDistance = new Vector2(2.5f, -2.5f);
            return t;
        }

        /// <summary>[아이콘 + 숫자] 형태의 게임풍 수치 표시. 반환된 Text 로 값을 갱신한다.</summary>
        public static Text IconValue(Transform parent, Sprite icon, string initial, int fontSize, Color color, string name = "IconValue")
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var iconImg = Image(root.transform, icon, "Icon");
            Place((RectTransform)iconImg.transform, new Vector2(0f, 0.5f), Vector2.zero,
                new Vector2(fontSize * 1.1f, fontSize * 1.1f));
            iconImg.preserveAspect = true;

            var txt = OutlinedLabel(root.transform, initial, fontSize, color, "Value");
            Place((RectTransform)txt.transform, new Vector2(0f, 0.5f), new Vector2(fontSize * 1.35f, 0));
            txt.alignment = TextAnchor.MiddleLeft;
            return txt;
        }

        // ── RectTransform 배치 헬퍼 ──────────────────────────────

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>anchor 지점 기준 배치. anchor: (0,0)=좌하 ~ (1,1)=우상</summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 anchoredPos, Vector2? size = null)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            if (size.HasValue) rt.sizeDelta = size.Value;
        }

        /// <summary>레거시 alert() 대응 — 나타났다 사라지는 안내문구.</summary>
        public static void Alert(MonoBehaviour host, Transform canvas, string message)
        {
            var text = Label(canvas, message, 44, new Color(1f, 0.95f, 0.4f), "FadeAlert");
            var rt = (RectTransform)text.transform;
            Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0, 200));
            text.transform.SetAsLastSibling();
            host.StartCoroutine(FadeAndDie(text));
        }

        static System.Collections.IEnumerator FadeAndDie(Text text)
        {
            float dur = 1.6f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                if (text == null) yield break;
                var c = text.color;
                c.a = 1f - (t / dur);
                text.color = c;
                yield return null;
            }
            if (text != null) UnityEngine.Object.Destroy(text.gameObject);
        }

        /// <summary>모서리 둥근 반투명 패널 — 깔끔한 HUD 배경용.</summary>
        public static Image RoundedPanel(Transform parent, Color color, string name = "RoundedPanel")
        {
            var img = Image(parent, Battle.SpriteBank.Rounded, name);
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.55f; // 모서리 반경 키움
            img.color = color;
            return img;
        }

        /// <summary>원형 아이콘 버튼 — 반투명 다크 원 + 아이콘.</summary>
        public static Button CircleIconButton(Transform parent, string iconName, float size,
            Action onClick, string name = "CircleButton")
        {
            var bg = Image(parent, Battle.SpriteBank.Circle, name);
            bg.color = new Color(0.1f, 0.11f, 0.16f, 0.82f);
            ((RectTransform)bg.transform).sizeDelta = new Vector2(size, size);
            var btn = bg.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var icon = Image(bg.transform, Resources.Load<Sprite>("Sprites/env/" + iconName), "Icon");
            Place((RectTransform)icon.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(size * 0.55f, size * 0.55f));
            icon.preserveAspect = true;
            return btn;
        }

        /// <summary>승패 결과 모달 — 카툰 프레임 + SUCCESS/FAIL + 보상 내역 + [RETRY]/[HOME].</summary>
        public static void ResultDialog(Transform canvas, bool win, Sprite resultImage,
            int reward, int moneyBefore, Action onRetry, Action onHome)
        {
            var overlay = Panel(canvas, new Color(0, 0, 0, 0.65f), "DialogOverlay");
            Stretch(overlay);
            overlay.SetAsLastSibling();

            // 에셋 팝업 프레임 (Layer Lab 금테 보드 — 몬스터와 같은 화풍)
            var board = Image(overlay.transform, Resources.Load<Sprite>("Sprites/env/popup_frame"), "Board");
            Place((RectTransform)board.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 640));

            var title = OutlinedLabel(board.transform, win ? "SUCCESS!" : "FAIL...", 84,
                win ? new Color(1f, 0.72f, 0.1f) : new Color(0.88f, 0.3f, 0.25f), "Title");
            title.font = TitleFont;
            Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -110));

            if (resultImage != null)
            {
                var img = Image(board.transform, resultImage, "ResultImage");
                Place((RectTransform)img.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(150, 150));
                img.preserveAspect = true;
            }

            // 승리 보상 내역: [코인 +20]  (40 → 60) — 외곽선 흰/골드 글씨 (반투명 배경에서도 선명)
            if (win && reward > 0)
            {
                var gain = IconValue(board.transform, Resources.Load<Sprite>("Sprites/env/icon_coin"),
                    "+" + reward, 46, new Color(1f, 0.85f, 0.25f), "RewardGain");
                Place((RectTransform)gain.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(-105, -80));

                var flow = OutlinedLabel(board.transform,
                    moneyBefore + " → " + (moneyBefore + reward), 36, Color.white, "RewardFlow");
                Place((RectTransform)flow.transform, new Vector2(0.5f, 0.5f), new Vector2(90, -80));
            }

            MakeTextButton(board.transform, "RETRY", new Color(1f, 0.82f, 0.25f), new Vector2(-130, 80),
                () => { UnityEngine.Object.Destroy(overlay.gameObject); onRetry?.Invoke(); });
            MakeTextButton(board.transform, "HOME", Color.white, new Vector2(130, 80),
                () => { UnityEngine.Object.Destroy(overlay.gameObject); onHome?.Invoke(); });
        }

        static void MakeTextButton(Transform parent, string label, Color color, Vector2 pos, Action onClick)
        {
            // 매끈한 타원 버튼 (원형 스프라이트를 늘려서 — 지갑/뒤로가기와 같은 계열)
            var bg = Image(parent, Battle.SpriteBank.Circle, "Btn_" + label);
            Place((RectTransform)bg.transform, new Vector2(0.5f, 0f), pos, new Vector2(240, 96));
            bg.color = new Color(0.1f, 0.11f, 0.16f, 0.88f);
            bg.gameObject.AddComponent<Button>().onClick.AddListener(() => onClick?.Invoke());

            var txt = OutlinedLabel(bg.transform, label, 42, color, "Label");
            txt.font = TitleFont;
            Place((RectTransform)txt.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 2));
        }

        /// <summary>실패/성공 피드백 — 텍스트 대신 이미지 색 플래시.</summary>
        public static void Flash(MonoBehaviour host, Graphic target, Color color)
        {
            host.StartCoroutine(FlashRoutine(target, color));
        }

        static System.Collections.IEnumerator FlashRoutine(Graphic g, Color color)
        {
            if (g == null) yield break;
            var orig = g.color;
            g.color = color;
            yield return new WaitForSeconds(0.18f);
            if (g != null) g.color = orig;
        }
    }
}
