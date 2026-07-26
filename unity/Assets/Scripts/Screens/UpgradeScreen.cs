using GameMaker.Battle;
using GameMaker.Core;
using GameMaker.Data;
using GameMaker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameMaker.Screens
{
    /// <summary>
    /// 유닛 업그레이드 — 텍스트 없이: 유닛 이미지 + 레벨 숫자 뱃지 + [코인+가격] 버튼.
    /// 성공 = 초록 플래시 / 돈 부족 = 빨간 플래시.
    /// </summary>
    public class UpgradeScreen : MonoBehaviour
    {
        static readonly string[] OurUnits = { "ourcastle", "ourbasic", "ourtank", "ourbattle", "ourmass" };

        Canvas canvas;
        Text moneyText;

        void Start()
        {
            canvas = Ui.CreateCanvas(transform, "UpgradeCanvas");

            var bg = Ui.Panel(canvas.transform, new Color(0.1f, 0.1f, 0.16f), "Bg");
            Ui.Stretch(bg);

            // 업그레이드 화면 상징: 업그레이드 아이콘
            var title = Ui.Image(canvas.transform, SpriteBank.GetEnv("icon_upgrade"), "TitleIcon");
            Ui.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(90, 90));
            title.preserveAspect = true;

            moneyText = Ui.IconValue(canvas.transform, SpriteBank.GetEnv("icon_coin"),
                "0", 48, new Color(1f, 0.85f, 0.2f), "Money");
            Ui.Place((RectTransform)moneyText.transform.parent, new Vector2(0f, 1f), new Vector2(60, -80));
            RefreshMoney();

            var home = Ui.ImageButton(canvas.transform, SpriteBank.GetEnv("homebutton"), new Vector2(90, 90),
                () => ScreenRouter.I.Show(ScreenId.Main), "HomeButton");
            Ui.Place((RectTransform)home.transform, new Vector2(1f, 1f), new Vector2(-40, -40));

            float startX = -680, gap = 340;
            for (int i = 0; i < OurUnits.Length; i++)
            {
                string unitName = OurUnits[i];
                float x = startX + i * gap;

                var frames = SpriteBank.GetFrames(unitName, "move");
                var img = Ui.Image(canvas.transform, frames[0], "Img_" + unitName);
                Ui.Place((RectTransform)img.transform, new Vector2(0.5f, 0.5f), new Vector2(x, 60), new Vector2(220, 220));
                img.preserveAspect = true;

                // 레벨 뱃지 (방패 + 숫자)
                var level = Ui.IconValue(canvas.transform, SpriteBank.GetEnv("icon_shield"),
                    DataHub.I.GetUpgradeCount(unitName).ToString(), 30, Color.white, "Lv_" + unitName);
                Ui.Place((RectTransform)level.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(x - 40, 190));

                // 가격 버튼: [코인 + 가격]
                var btn = Ui.TextButton(canvas.transform, "", 1, new Vector2(240, 100),
                    () => TryUpgrade(unitName), new Color(0.14f, 0.14f, 0.2f, 0.95f), "Btn_" + unitName);
                Ui.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), new Vector2(x, -140));

                var price = Ui.IconValue(btn.transform, SpriteBank.GetEnv("icon_coin"),
                    PriceOf(unitName).ToString(), 36, new Color(1f, 0.9f, 0.3f), "Price_" + unitName);
                Ui.Place((RectTransform)price.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(-55, 0));
            }
        }

        int PriceOf(string unitName)
        {
            var m = DataHub.I.FindMonster(unitName);
            return (m.IsCastle ? 50 : m.cost) * (DataHub.I.GetUpgradeCount(unitName) + 1);
        }

        void TryUpgrade(string unitName)
        {
            var btn = canvas.transform.Find("Btn_" + unitName);
            var btnImg = btn != null ? btn.GetComponent<Image>() : null;
            try
            {
                DataHub.I.Upgrade(unitName);
                RefreshMoney();

                // 레벨/가격 갱신
                var lv = canvas.transform.Find("Lv_" + unitName);
                if (lv != null) lv.GetComponentInChildren<Text>().text = DataHub.I.GetUpgradeCount(unitName).ToString();
                var price = btn.Find("Price_" + unitName);
                if (price != null) price.GetComponentInChildren<Text>().text = PriceOf(unitName).ToString();

                if (btnImg != null) Ui.Flash(this, btnImg, new Color(0.2f, 0.8f, 0.3f));
            }
            catch (GameException)
            {
                if (btnImg != null) Ui.Flash(this, btnImg, new Color(0.9f, 0.2f, 0.2f));
            }
        }

        void RefreshMoney() =>
            moneyText.text = DataHub.I.GetPlayer().money.ToString();
    }
}
