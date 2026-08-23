using GameMaker.Battle;
using GameMaker.Core;
using GameMaker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameMaker.Screens
{
    /// <summary>
    /// 로비 — 타이틀 화면과 같은 야외 배경/행진 연출을 공유하고,
    /// 메뉴는 전투 소환 버튼과 같은 나무 프레임 한 줄(게임 속 아트 아이콘 + 라벨)로 구성한다.
    /// </summary>
    public class MainScreen : MonoBehaviour
    {
        Canvas canvas;

        void Start()
        {
            canvas = Ui.CreateCanvas(transform, "MainCanvas");

            MenuBackdrop.Build(this, canvas);
            MenuBackdrop.BuildParade(this, canvas);

            // 타이틀 화면과 같은 게임명 (조금 작게, 상단)
            MenuBackdrop.TitleLabel(canvas, 92, new Vector2(0.5f, 1f), new Vector2(0, -105));

            // 메뉴 4개 — 전투 소환 버튼과 같은 나무 프레임, 2x2 큰 버튼. 아이콘은 게임 속 아트 재사용
            MakeMenuButton(SpriteBank.GetEnv("stage1thumb"), new Vector2(180, 96),
                "맵", new Vector2(-504, -150), false, () => ScreenRouter.I.Show(ScreenId.Map));
            MakeMenuButton(SpriteBank.GetEnv("icon_arrowup"), new Vector2(118, 118),
                "업그레이드", new Vector2(-168, -150), false, () => ScreenRouter.I.Show(ScreenId.Upgrade));
            MakeMenuButton(SpriteBank.GetEnv("icon_chest_closed"), new Vector2(150, 94),
                "뽑기", new Vector2(168, -150), false, () => ScreenRouter.I.Show(ScreenId.Gacha));
            MakeMenuButton(SpriteBank.GetEnv("portrait_tank"), new Vector2(126, 126),
                "배치", new Vector2(504, -150), false, () => ScreenRouter.I.Show(ScreenId.Deploy));

            // [DEV] 유닛 뷰어 — 메뉴 그리드를 건드리지 않도록 우상단 구석의 작은 버튼 (Core/Dev.cs 로 토글)
            if (Core.Dev.UnitViewer)
            {
                var devBtn = Ui.TextButton(canvas.transform, "유닛 뷰어", 26, new Vector2(190, 58),
                    () => ScreenRouter.I.Show(ScreenId.UnitTest),
                    new Color(0.3f, 0.25f, 0.15f, 0.9f), "UnitViewerBtn");
                Ui.Place((RectTransform)devBtn.transform, new Vector2(1f, 1f), new Vector2(-125, -55));
            }
        }

        void MakeMenuButton(Sprite iconSprite, Vector2 iconSize, string label, Vector2 pos, bool locked, System.Action onClick)
        {
            // 나무 프레임 원본 비율(150:140) 유지 — 옆으로 늘리면 무늬가 왜곡되어 조잡해진다
            var btn = Ui.ImageButton(canvas.transform, SpriteBank.GetEnv("btn_wood"), new Vector2(300, 280),
                null, "Btn_" + label);
            Ui.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), pos);
            Ui.PressedSwap(btn, SpriteBank.GetEnv("btn_wood_pressed"));
            var btnImg = btn.GetComponent<Image>();

            var icon = Ui.Image(btn.transform, iconSprite, "Icon");
            Ui.Place((RectTransform)icon.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 42), iconSize);
            icon.preserveAspect = true;

            var txt = Ui.OutlinedLabel(btn.transform, label, 40, Color.white, "Label");
            Ui.Place((RectTransform)txt.transform, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(270, 44));

            if (locked)
            {
                // 시스템 공통 '불가' 표현: 회색 + 반투명 + 자물쇠
                btnImg.color = new Color(0.4f, 0.4f, 0.4f);
                btn.gameObject.AddComponent<CanvasGroup>().alpha = 0.55f;
                var lockImg = Ui.Image(btn.transform, SpriteBank.GetEnv("icon_lock"), "Lock");
                Ui.Place((RectTransform)lockImg.transform, new Vector2(1f, 1f), new Vector2(-16, -16), new Vector2(54, 54));
                lockImg.preserveAspect = true;
                btn.onClick.AddListener(() => Ui.Flash(this, btnImg, new Color(0.75f, 0.3f, 0.28f)));
            }
            else
            {
                btn.onClick.AddListener(() => onClick?.Invoke());
            }
        }
    }
}
