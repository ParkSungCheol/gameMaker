using GameMaker.Battle;
using GameMaker.Core;
using GameMaker.Data;
using GameMaker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameMaker.Screens
{
    /// <summary>
    /// 보물찾기 여정 지도 — 순차 해금 구조.
    /// 하늘 배경 위에 어두운 오버레이를 깔아 여행지 카드가 또렷하게 떠 보이도록 하고,
    /// 카드는 크림색 액자 + 이름띠로 정돈했다.
    /// </summary>
    public class MapScreen : MonoBehaviour
    {
        void Start()
        {
            var canvas = Ui.CreateCanvas(transform, "MapCanvas");

            // 하늘/구름 배경 + 어두운 오버레이 (카드 가독성)
            MenuBackdrop.Build(this, canvas, dim: 0.5f, withGround: false);

            // 타이틀: 전투 화면과 같은 외곽선 흰 글씨 (양피지는 코인 패널에만)
            var title = Ui.OutlinedLabel(canvas.transform, "보물찾기 여정", 52, Color.white, "Title");
            Ui.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -55));

            // 보유 코인: 양피지 패널
            var player = DataHub.I.GetPlayer();
            var moneyPanel = Ui.Image(canvas.transform, SpriteBank.GetEnv("panel_parchment"), "MoneyPanel");
            Ui.Place((RectTransform)moneyPanel.transform, new Vector2(0f, 1f), new Vector2(160, -55), new Vector2(280, 82));
            var money = Ui.CenteredIconValue(moneyPanel.transform, SpriteBank.GetEnv("icon_coin"),
                player.money.ToString(), 42, Color.white, "Money");
            Ui.Place((RectTransform)money.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0, 2));

            // 되돌아가기 — 전투 화면과 동일한 우측 하단 원형 버튼
            var back = Ui.CircleIconButton(canvas.transform, "icon_return", 92,
                () => ScreenRouter.I.Show(ScreenId.Main), "BackButton");
            Ui.Place((RectTransform)back.transform, new Vector2(1f, 0f), new Vector2(-58, 50));

            // 여행지 카드 3x3 — 크림색 액자 + 지역명 띠 (숫자 없음)
            for (int s = 1; s <= 9; s++)
            {
                int stageNum = s;
                int row = (s - 1) / 3;
                int col = (s - 1) % 3;
                var pos = new Vector2((col - 1) * 420, 110 - row * 230);

                var stageData = DataHub.I.GetStage(stageNum);
                var thumb = SpriteBank.GetEnv("stage" + s + "thumb") ?? SpriteBank.GetEnv(stageData.bg);
                bool locked = s > 1 && player.mapClear[s - 1] == 0; // 이전 여행지 클리어 필요

                var frame = Ui.RoundedPanel(canvas.transform, locked
                    ? new Color(0.4f, 0.4f, 0.45f)
                    : new Color(0.96f, 0.92f, 0.78f), "Frame" + s);
                Ui.Place((RectTransform)frame.transform, new Vector2(0.5f, 0.5f), pos, new Vector2(398, 208));

                var btn = Ui.ImageButton(canvas.transform, thumb, new Vector2(380, 190), null, "Stage" + s);
                Ui.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), pos);
                var btnImg = btn.GetComponent<Image>();

                // 지역명 띠: 반투명 어두운 배경 위 흰 글씨 — 어떤 썸네일 위에서도 또렷
                var band = Ui.RoundedPanel(btn.transform, new Color(0f, 0f, 0f, 0.55f), "NameBand");
                Ui.Place((RectTransform)band.transform, new Vector2(0.5f, 0f), new Vector2(0, 8), new Vector2(310, 50));
                band.raycastTarget = false;

                var place = Ui.OutlinedLabel(band.transform, stageData.label, 34, Color.white, "Place");
                Ui.Stretch((RectTransform)place.transform);

                if (locked)
                {
                    btnImg.color = new Color(0.3f, 0.3f, 0.35f); // 어둡게
                    place.color = new Color(0.65f, 0.65f, 0.65f);
                    var lockImg = Ui.Image(btn.transform, SpriteBank.GetEnv("icon_lock"), "Lock");
                    Ui.Place((RectTransform)lockImg.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(64, 64));
                    lockImg.preserveAspect = true;
                    btn.onClick.AddListener(() => Ui.Flash(this, btnImg, new Color(0.55f, 0.15f, 0.15f)));
                }
                else
                {
                    btn.onClick.AddListener(() => ScreenRouter.I.Show(ScreenId.Battlefield, stageNum));

                    // 다음 클리어 보상 미리보기 — 우측 상단 코너에 [코인 +금액]
                    // (보상 = 맵번호 x (11 - 클리어횟수), 최소 1 / 고갈되면 회색)
                    int reward = Mathf.Max(1, stageNum * (11 - player.mapClear[stageNum]));
                    bool drained = reward <= stageNum;
                    var color = drained ? new Color(0.6f, 0.6f, 0.6f) : new Color(1f, 0.88f, 0.3f);

                    // 코너 딱 붙는 뱃지: [ 🪙 +11 ] — 우상단 pivot 고정, padding 10px
                    var coin = Ui.Image(btn.transform, SpriteBank.GetEnv("icon_coin"), "RewardCoin");
                    coin.preserveAspect = true;
                    var coinRt = (RectTransform)coin.transform;
                    coinRt.anchorMin = coinRt.anchorMax = new Vector2(1f, 1f);
                    coinRt.pivot = new Vector2(1f, 1f);
                    coinRt.anchoredPosition = new Vector2(-68f, -10f);
                    coinRt.sizeDelta = new Vector2(26f, 26f);

                    var amt = Ui.OutlinedLabel(btn.transform, "+" + reward, 28, color, "RewardAmt");
                    amt.alignment = TextAnchor.MiddleLeft;
                    var amtRt = amt.rectTransform;
                    amtRt.anchorMin = amtRt.anchorMax = new Vector2(1f, 1f);
                    amtRt.pivot = new Vector2(0f, 1f);
                    amtRt.anchoredPosition = new Vector2(-64f, -8f);
                    amtRt.sizeDelta = new Vector2(60f, 30f);
                }
            }
        }
    }
}
