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
    /// 유닛 업그레이드 — 야외 훈련소 느낌: 유닛들이 풀밭 위에 서 있고
    /// 위로 [나무 버튼: 코인+가격(가운데 정렬)] / [초록 뱃지: ↑레벨] / 이름 순으로 쌓인다.
    /// 성공 = 초록 플래시 / 돈 부족 = 빨간 플래시.
    /// </summary>
    public class UpgradeScreen : MonoBehaviour
    {
        static readonly string[] OurUnits = { "ourcastle", "ourbasic", "ourtank", "ourbattle", "ourmass" };
        static readonly string[] UnitNames = { "성", "검병", "궁수", "방패병", "마법사" };

        Canvas canvas;
        Text moneyText;
        readonly Dictionary<string, Text> levelTexts = new Dictionary<string, Text>();
        readonly Dictionary<string, Text> priceTexts = new Dictionary<string, Text>();
        readonly Dictionary<string, Image> btnImages = new Dictionary<string, Image>();
        readonly Dictionary<string, CanvasGroup> btnGroups = new Dictionary<string, CanvasGroup>();
        readonly Dictionary<string, float> flashUntil = new Dictionary<string, float>();

        void Start()
        {
            canvas = Ui.CreateCanvas(transform, "UpgradeCanvas");

            MenuBackdrop.Build(this, canvas);

            // 타이틀: 전투 화면과 같은 외곽선 흰 글씨 (양피지는 코인 패널에만)
            var title = Ui.OutlinedLabel(canvas.transform, "업그레이드", 52, Color.white, "Title");
            Ui.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -55));

            // 보유 코인: 양피지 패널 (전투 HUD와 같은 스타일)
            var moneyPanel = Ui.Image(canvas.transform, SpriteBank.GetEnv("panel_parchment"), "MoneyPanel");
            Ui.Place((RectTransform)moneyPanel.transform, new Vector2(0f, 1f), new Vector2(160, -55), new Vector2(280, 82));
            moneyText = Ui.CenteredIconValue(moneyPanel.transform, SpriteBank.GetEnv("icon_coin"),
                "0", 42, Color.white, "Money");
            Ui.Place((RectTransform)moneyText.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0, 2));
            RefreshMoney();

            // 되돌아가기 — 전투 화면과 동일한 우측 하단 원형 버튼
            var back = Ui.CircleIconButton(canvas.transform, "icon_return", 92,
                () => ScreenRouter.I.Show(ScreenId.Main), "BackButton");
            Ui.Place((RectTransform)back.transform, new Vector2(1f, 0f), new Vector2(-58, 50));

            float startX = -680, gap = 340;
            for (int i = 0; i < OurUnits.Length; i++)
            {
                string unitName = OurUnits[i];
                float x = startX + i * gap;

                // 유닛: 풀밭 위에 서서 걷는 애니메이션
                var frames = SpriteBank.GetFrames(unitName, "move");
                var img = Ui.Image(canvas.transform, frames[0], "Img_" + unitName);
                var imgRt = (RectTransform)img.transform;
                imgRt.anchorMin = imgRt.anchorMax = new Vector2(0.5f, 0f);
                imgRt.pivot = new Vector2(0.5f, 0f);
                imgRt.anchoredPosition = new Vector2(x, MenuBackdrop.GroundTop - 18f);
                imgRt.sizeDelta = new Vector2(230, 230);
                img.preserveAspect = true;
                if (frames.Length > 1) StartCoroutine(MenuBackdrop.CycleFrames(img, frames, 0.16f));

                var nameLabel = Ui.OutlinedLabel(canvas.transform, UnitNames[i], 40, Color.white, "Name_" + unitName);
                Ui.Place((RectTransform)nameLabel.transform, new Vector2(0.5f, 0.5f), new Vector2(x, -85));

                // 레벨 뱃지: 초록 알약 + [↑ 레벨] 가운데 정렬
                var lvBadge = Ui.RoundedPanel(canvas.transform, new Color(0.15f, 0.55f, 0.28f, 0.95f), "LvBadge_" + unitName);
                Ui.Place((RectTransform)lvBadge.transform, new Vector2(0.5f, 0.5f), new Vector2(x, -5), new Vector2(120, 52));
                levelTexts[unitName] = Ui.CenteredIconValue(lvBadge.transform, SpriteBank.GetEnv("icon_arrowup"),
                    DataHub.I.GetUpgradeCount(unitName).ToString(), 32, Color.white, "Lv_" + unitName);
                Ui.Place((RectTransform)levelTexts[unitName].transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0, 1));

                // 가격 버튼: 나무 버튼 + [코인 + 가격] 가운데 정렬
                var btn = Ui.ImageButton(canvas.transform, SpriteBank.GetEnv("btn_wood"), new Vector2(230, 104),
                    () => TryUpgrade(unitName), "Btn_" + unitName);
                Ui.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), new Vector2(x, 110));
                Ui.PressedSwap(btn, SpriteBank.GetEnv("btn_wood_pressed"));
                btnImages[unitName] = btn.GetComponent<Image>();
                btnGroups[unitName] = btn.gameObject.AddComponent<CanvasGroup>();
                flashUntil[unitName] = 0f;

                priceTexts[unitName] = Ui.CenteredIconValue(btn.transform, SpriteBank.GetEnv("icon_coin"),
                    PriceOf(unitName).ToString(), 36, new Color(1f, 0.9f, 0.3f), "Price_" + unitName);
                Ui.Place((RectTransform)priceTexts[unitName].transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0, 4));
            }
        }

        /// <summary>전투의 소환 버튼처럼 — 살 수 있으면 밝게, 돈이 모자라면 어둡고 흐리게.</summary>
        void Update()
        {
            int money = DataHub.I.GetPlayer().money;
            foreach (var kv in btnImages)
            {
                if (Time.time < flashUntil[kv.Key]) continue; // 플래시 연출 중엔 건드리지 않음
                bool affordable = money >= PriceOf(kv.Key);
                // 전투 소환 버튼과 동일한 표현: 가능 = 원색 / 불가 = 회색(0.4) + 반투명
                kv.Value.color = affordable ? Color.white : new Color(0.4f, 0.4f, 0.4f);
                btnGroups[kv.Key].alpha = affordable ? 1f : 0.55f;
            }
        }

        int PriceOf(string unitName)
        {
            var m = DataHub.I.FindMonster(unitName);
            return (m.IsCastle ? 50 : m.cost) * (DataHub.I.GetUpgradeCount(unitName) + 1);
        }

        void TryUpgrade(string unitName)
        {
            try
            {
                DataHub.I.Upgrade(unitName);
                RefreshMoney();
                levelTexts[unitName].text = DataHub.I.GetUpgradeCount(unitName).ToString();
                priceTexts[unitName].text = PriceOf(unitName).ToString();
                flashUntil[unitName] = Time.time + 0.25f;
                Ui.Flash(this, btnImages[unitName], new Color(0.2f, 0.8f, 0.3f));
            }
            catch (GameException)
            {
                flashUntil[unitName] = Time.time + 0.25f;
                Ui.Flash(this, btnImages[unitName], new Color(0.9f, 0.2f, 0.2f));
            }
        }

        void RefreshMoney() =>
            moneyText.text = DataHub.I.GetPlayer().money.ToString();
    }
}
