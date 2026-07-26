using GameMaker.Battle;
using GameMaker.Core;
using GameMaker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameMaker.Screens
{
    /// <summary>로비 — 텍스트 없이 아이콘 버튼 4개 (맵 / 업그레이드 / 뽑기(잠김) / 배치(잠김)).</summary>
    public class MainScreen : MonoBehaviour
    {
        Canvas canvas;

        void Start()
        {
            canvas = Ui.CreateCanvas(transform, "MainCanvas");

            // 배경: 스테이지1 하늘 (밝고 귀여운 톤)
            var bgSprite = SpriteBank.GetEnv("stage1bg");
            if (bgSprite != null)
            {
                var bg = Ui.Image(canvas.transform, bgSprite, "Bg");
                Ui.Stretch((RectTransform)bg.transform);
            }

            // 타이틀: 아군 성 + 유닛 행진 이미지 연출
            var castle = Ui.Image(canvas.transform, SpriteBank.GetFrames("ourcastle", "move")[0], "TitleCastle");
            Ui.Place((RectTransform)castle.transform, new Vector2(0.5f, 1f), new Vector2(0, -160), new Vector2(260, 260));
            castle.preserveAspect = true;

            var hero = Ui.Image(canvas.transform, SpriteBank.GetFrames("ourbasic", "move")[0], "TitleHero");
            Ui.Place((RectTransform)hero.transform, new Vector2(0.5f, 1f), new Vector2(-190, -220), new Vector2(140, 140));
            hero.preserveAspect = true;

            // 아이콘 버튼 4개
            MakeMenuButton("icon_map",     new Vector2(-285, -60), false, () => ScreenRouter.I.Show(ScreenId.Map));
            MakeMenuButton("icon_upgrade", new Vector2(285, -60),  false, () => ScreenRouter.I.Show(ScreenId.Upgrade));
            MakeMenuButton("icon_chest",   new Vector2(-285, -260), true, null);
            MakeMenuButton("icon_shield",  new Vector2(285, -260),  true, null);
        }

        void MakeMenuButton(string iconName, Vector2 pos, bool locked, System.Action onClick)
        {
            var panel = Ui.Panel(canvas.transform, new Color(0.15f, 0.13f, 0.1f, 0.85f), "Btn_" + iconName);
            Ui.Place(panel, new Vector2(0.5f, 0.5f), pos, new Vector2(380, 160));

            var iconSprite = SpriteBank.GetEnv(iconName);
            Image icon;
            Button btn;

            if (locked)
            {
                icon = Ui.Image(panel, iconSprite, "Icon");
                icon.color = new Color(0.5f, 0.5f, 0.5f);
                var lockImg = Ui.Image(panel, SpriteBank.GetEnv("icon_lock"), "Lock");
                Ui.Place((RectTransform)lockImg.transform, new Vector2(1f, 1f), new Vector2(-10, -10), new Vector2(56, 56));
                lockImg.preserveAspect = true;
                btn = panel.gameObject.AddComponent<Button>();
                var panelImg = panel.GetComponent<Image>();
                btn.onClick.AddListener(() => Ui.Flash(this, panelImg, new Color(0.6f, 0.15f, 0.15f)));
            }
            else
            {
                icon = Ui.Image(panel, iconSprite, "Icon");
                btn = panel.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => onClick?.Invoke());
            }

            Ui.Place((RectTransform)icon.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 110));
            icon.preserveAspect = true;
        }
    }
}
