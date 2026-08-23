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
            // 세로 기준 스케일 — 와이드 화면에서 가로가 남는 건 괜찮지만,
            // 가로 기준이면 세로가 1080 미만으로 줄어 상하 레이아웃이 겹친다
            scaler.matchWidthOrHeight = 1f;
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
            o.effectColor = new Color(0f, 0f, 0f, 1f);
            o.effectDistance = new Vector2(2.5f, -2.5f);
            // 반대 방향 외곽선 한 겹 더 — 네 방향 모두 테두리가 생겨 어떤 배경에서도 또렷
            var o2 = t.gameObject.AddComponent<Outline>();
            o2.effectColor = new Color(0f, 0f, 0f, 1f);
            o2.effectDistance = new Vector2(-2f, 2f);
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

        /// <summary>[아이콘 + 숫자]를 통째로 가운데 정렬하는 수치 표시 — 자릿수가 바뀌어도 중앙 유지.</summary>
        public static Text CenteredIconValue(Transform parent, Sprite icon, string initial, int fontSize, Color color, string name = "IconValue")
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var lay = root.AddComponent<HorizontalLayoutGroup>();
            lay.childAlignment = TextAnchor.MiddleCenter;
            lay.spacing = fontSize * 0.25f;
            lay.childForceExpandWidth = false;
            lay.childForceExpandHeight = false;
            var fit = root.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var iconImg = Image(root.transform, icon, "Icon");
            iconImg.preserveAspect = true;
            var le = iconImg.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = fontSize * 1.1f;
            le.preferredHeight = fontSize * 1.1f;

            var txt = OutlinedLabel(root.transform, initial, fontSize, color, "Value");
            txt.alignment = TextAnchor.MiddleCenter;
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

        /// <summary>화면 공용 알림 — 항상 '아래쪽'에, 주황 외곽선 글씨로 강조해서 1초 떴다 사라진다 (배경판 없음).
        /// (돈 부족 / 최대 강화 / 최소 배치 / 잠긴 스테이지 등). bottomY = 화면 바닥에서의 중심 높이.</summary>
        public static ToastBar CreateToast(Transform canvas, float bottomY)
        {
            // 배경판·테두리 없이 글씨만 (기본 배경이 비치게) — 외곽선 글씨라 어디서나 읽힌다
            var bg = Image(canvas, null, "Toast");
            bg.color = new Color(0, 0, 0, 0);
            bg.raycastTarget = false;
            Place(bg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, bottomY), new Vector2(10, 56));
            bg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var text = OutlinedLabel(bg.transform, "", 30, new Color(1f, 0.78f, 0.35f), "Text");
            text.raycastTarget = false;
            Stretch(text.rectTransform);
            var toast = bg.gameObject.AddComponent<ToastBar>();
            toast.Init(bg, text);
            bg.gameObject.SetActive(false);
            return toast;
        }

        /// <summary>레거시 alert() 대응 — 나타났다 사라지는 안내문구 (하단).</summary>
        public static void Alert(MonoBehaviour host, Transform canvas, string message)
        {
            var text = OutlinedLabel(canvas, message, 44, new Color(1f, 0.95f, 0.4f), "FadeAlert");
            var rt = (RectTransform)text.transform;
            Place(rt, new Vector2(0.5f, 0f), new Vector2(0, 120));
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

            var title = OutlinedLabel(board.transform, win ? "SUCCESS" : "FAIL", 84,
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

            MakeTextButton(board.transform, "재시도", new Color(1f, 0.82f, 0.25f), new Vector2(-130, 80),
                () => { UnityEngine.Object.Destroy(overlay.gameObject); onRetry?.Invoke(); }, koreanFont: true);
            MakeTextButton(board.transform, "돌아가기", Color.white, new Vector2(130, 80),
                () => { UnityEngine.Object.Destroy(overlay.gameObject); onHome?.Invoke(); }, koreanFont: true);
        }

        static void MakeTextButton(Transform parent, string label, Color color, Vector2 pos, Action onClick, bool koreanFont = false)
        {
            // 매끈한 타원 버튼 (원형 스프라이트를 늘려서 — 지갑/뒤로가기와 같은 계열)
            var bg = Image(parent, Battle.SpriteBank.Circle, "Btn_" + label);
            Place((RectTransform)bg.transform, new Vector2(0.5f, 0f), pos, new Vector2(240, 96));
            bg.color = new Color(0.1f, 0.11f, 0.16f, 0.88f);
            bg.gameObject.AddComponent<Button>().onClick.AddListener(() => onClick?.Invoke());

            var txt = OutlinedLabel(bg.transform, label, koreanFont ? 36 : 42, color, "Label");
            if (!koreanFont) txt.font = TitleFont;
            Place((RectTransform)txt.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 2));
        }

        /// <summary>범용 팝업 보드 — 어두운 반투명 판 + 가는 금테. 제목만 두고 [닫기] 버튼은 없다:
        /// 바깥(어두운 영역) 클릭으로 닫히며, 보드 아래에 깜빡이는 안내 문구가 그걸 알려준다.
        /// 내용을 붙일 보드 RectTransform 을 돌려준다 (제목 아래 약 -110 부터 자유 영역).</summary>
        public static RectTransform Popup(Transform canvas, string title, Vector2 size, Action onClose = null)
        {
            var overlay = Panel(canvas, new Color(0, 0, 0, 0.8f), "PopupOverlay");
            Stretch(overlay);
            overlay.SetAsLastSibling();
            overlay.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            { UnityEngine.Object.Destroy(overlay.gameObject); onClose?.Invoke(); });

            var board = Panel(overlay.transform, new Color(0.03f, 0.03f, 0.05f, 0.55f), "Board");
            Place(board, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            board.gameObject.AddComponent<Button>(); // 보드 클릭은 닫히지 않게 (레이캐스트 흡수)
            Frame(board, new Color(1f, 0.82f, 0.35f, 0.9f), 3f);

            var titleTxt = OutlinedLabel(board, title, 48, new Color(1f, 0.82f, 0.3f), "Title");
            Place((RectTransform)titleTxt.transform, new Vector2(0.5f, 1f), new Vector2(0, -44));
            var hint = OutlinedLabel(board, "바깥쪽을 누르면 닫힙니다", 26, Color.white, "CloseHint");
            hint.raycastTarget = false;
            Place((RectTransform)hint.transform, new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(600, 36));
            hint.gameObject.AddComponent<Blink>();
            return board;
        }

        /// <summary>가는 외곽 테두리 — 네 변에 얇은 판을 붙인다 (또렷한 선, 블러 없음).</summary>
        public static void Frame(RectTransform rt, Color color, float thickness)
        {
            for (int i = 0; i < 4; i++)
            {
                var edge = Panel(rt, color, "Edge" + i);
                edge.GetComponent<Image>().raycastTarget = false;
                bool horiz = i < 2;
                edge.anchorMin = new Vector2(horiz ? 0f : (i == 2 ? 0f : 1f), horiz ? (i == 0 ? 1f : 0f) : 0f);
                edge.anchorMax = new Vector2(horiz ? 1f : (i == 2 ? 0f : 1f), horiz ? (i == 0 ? 1f : 0f) : 1f);
                edge.pivot = new Vector2(horiz ? 0.5f : (i == 2 ? 0f : 1f), horiz ? (i == 0 ? 1f : 0f) : 0.5f);
                edge.anchoredPosition = Vector2.zero;
                edge.sizeDelta = horiz ? new Vector2(0, thickness) : new Vector2(thickness, 0);
            }
        }

        /// <summary>깔끔한 스크롤바 — 각진 트랙 + 금색 손잡이, 항상 표시(페이드 없음).
        /// horizontal 이면 size.x 가 길이, 아니면 size.y 가 길이.</summary>
        public static Scrollbar CleanScrollbar(Transform parent, bool horizontal, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var bg = Panel(parent, new Color(1f, 1f, 1f, 0.18f), horizontal ? "HScrollbar" : "VScrollbar");
            Place(bg, anchor, pos, size);
            Frame(bg, new Color(1f, 1f, 1f, 0.35f), 1f);
            var sb = bg.gameObject.AddComponent<Scrollbar>();
            var handle = Panel(bg, new Color(1f, 0.84f, 0.4f, 1f), "Handle");
            Stretch(handle);
            handle.offsetMin = new Vector2(2, 2);
            handle.offsetMax = new Vector2(-2, -2);
            sb.handleRect = handle;
            sb.targetGraphic = handle.GetComponent<Image>();
            sb.direction = horizontal ? Scrollbar.Direction.LeftToRight : Scrollbar.Direction.BottomToTop;
            return sb;
        }

        /// <summary>눈에 띄는 도움말 버튼 — 금색 원 + 진한 ? (단순 글자 버튼보다 존재감 있게).</summary>
        public static Button HelpButton(Transform parent, float size, Action onClick, string name = "HelpButton")
        {
            // 금색 테두리 원 위에 어두운 원 — 되돌아가기(CircleIconButton)와 같은 톤, 장식 없이 단정하게
            var btn = ImageButton(parent, Battle.SpriteBank.Circle, new Vector2(size, size), onClick, name);
            btn.GetComponent<Image>().color = new Color(1f, 0.84f, 0.3f);
            var core = Image(btn.transform, Battle.SpriteBank.Circle, "Core");
            core.color = new Color(0.12f, 0.09f, 0.05f, 0.95f);
            core.raycastTarget = false;
            Place(core.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size - 6, size - 6));
            // 글자는 금색 — 글리프 위치 편차를 없애려고 박스를 원 전체로 잡고 정중앙 정렬 (오프셋 없음)
            var q = Label(btn.transform, "?", Mathf.RoundToInt(size * 0.6f), new Color(1f, 0.84f, 0.3f), "Q");
            q.alignment = TextAnchor.MiddleCenter;
            q.raycastTarget = false;
            Stretch(q.rectTransform);
            return btn;
        }

        /// <summary>확인 팝업 — 결과 모달과 같은 카툰 프레임 재활용. 그림 + 메시지 + [계속]/[확인] 두 버튼.</summary>
        public static void ConfirmDialog(Transform canvas, string title, string message,
            string stayLabel, string leaveLabel, Action onStay, Action onLeave, Sprite image = null)
        {
            var overlay = Panel(canvas, new Color(0, 0, 0, 0.65f), "ConfirmOverlay");
            Stretch(overlay);
            overlay.SetAsLastSibling();

            var board = Image(overlay.transform, Resources.Load<Sprite>("Sprites/env/popup_frame"), "Board");
            Place((RectTransform)board.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 640));

            var titleTxt = OutlinedLabel(board.transform, title, 70, new Color(1f, 0.72f, 0.1f), "Title");
            Place((RectTransform)titleTxt.transform, new Vector2(0.5f, 1f), new Vector2(0, -115));

            if (image != null)
            {
                var img = Image(board.transform, image, "ConfirmImage");
                Place((RectTransform)img.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 45), new Vector2(150, 150));
                img.preserveAspect = true;
            }

            var msg = OutlinedLabel(board.transform, message, 38, Color.white, "Message");
            Place((RectTransform)msg.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -75));

            MakeTextButton(board.transform, stayLabel, new Color(1f, 0.82f, 0.25f), new Vector2(-130, 80),
                () => { UnityEngine.Object.Destroy(overlay.gameObject); onStay?.Invoke(); }, koreanFont: true);
            MakeTextButton(board.transform, leaveLabel, Color.white, new Vector2(130, 80),
                () => { UnityEngine.Object.Destroy(overlay.gameObject); onLeave?.Invoke(); }, koreanFont: true);
        }

        /// <summary>나무 버튼 눌림 피드백 — 누르는 동안만 눌린 스프라이트로 교체 (투명도 변화 없음).</summary>
        public static void PressedSwap(Button btn, Sprite pressed)
        {
            if (pressed == null) return;
            btn.targetGraphic = btn.GetComponent<Image>();
            btn.transition = Selectable.Transition.SpriteSwap;
            var ss = btn.spriteState;
            ss.pressedSprite = pressed;
            btn.spriteState = ss;
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

    /// <summary>하단 알림 — Show(msg) 로 띄우면 시간이 지나면 사라진다.
    /// 같은 화면에서 여러 번 불러도 마지막 메시지로 갱신되며 타이머가 연장된다.</summary>
    public class ToastBar : MonoBehaviour
    {
        Image bg;
        Text text;
        float until;

        public void Init(Image bg, Text text) { this.bg = bg; this.text = text; }

        public void Show(string msg, float seconds = 1f)
        {
            gameObject.SetActive(true);
            text.text = msg;
            bg.rectTransform.sizeDelta = new Vector2(text.preferredWidth + 64f, 56f);
            until = Time.unscaledTime + seconds;
            transform.SetAsLastSibling();
        }

        void Update()
        {
            if (Time.unscaledTime > until) gameObject.SetActive(false);
        }
    }

    /// <summary>텍스트/이미지 깜빡임 — 안내 문구용 (알파 0.35~1 사인파).</summary>
    public class Blink : MonoBehaviour
    {
        public float speed = 3.2f;
        Graphic g;
        void Awake() => g = GetComponent<Graphic>();
        void Update()
        {
            if (g == null) return;
            var c = g.color;
            c.a = 0.675f + 0.325f * Mathf.Sin(Time.unscaledTime * speed);
            g.color = c;
        }
    }
}
