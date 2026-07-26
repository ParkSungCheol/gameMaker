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
        public static Font DefaultFont =>
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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

        /// <summary>승패 결과 모달 — 텍스트 없이 이미지(보물상자/무너진 성)와 체크 버튼만.</summary>
        public static void ResultDialog(Transform canvas, Sprite resultImage, Action onOk)
        {
            var overlay = Panel(canvas, new Color(0, 0, 0, 0.6f), "DialogOverlay");
            Stretch(overlay);
            overlay.SetAsLastSibling();

            var box = Panel(overlay, new Color(0.12f, 0.13f, 0.2f, 0.98f), "DialogBox");
            Place(box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 420));

            if (resultImage != null)
            {
                var img = Image(box, resultImage, "ResultImage");
                Place((RectTransform)img.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(240, 240));
                img.preserveAspect = true;
            }

            var okIcon = Resources.Load<Sprite>("Sprites/env/icon_check");
            var ok = ImageButton(box, okIcon, new Vector2(110, 90),
                () => { UnityEngine.Object.Destroy(overlay.gameObject); onOk?.Invoke(); }, "OkButton");
            Place((RectTransform)ok.transform, new Vector2(0.5f, 0f), new Vector2(0, 25));
            ok.GetComponent<Image>().preserveAspect = true;
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
