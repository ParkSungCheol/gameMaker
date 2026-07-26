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
    /// 이전 여행지를 클리어해야 다음이 열리고, 클리어한 곳엔 황금 보물상자가 표시된다.
    /// </summary>
    public class MapScreen : MonoBehaviour
    {
        void Start()
        {
            var canvas = Ui.CreateCanvas(transform, "MapCanvas");

            var bg = Ui.Panel(canvas.transform, new Color(0.09f, 0.11f, 0.16f), "Bg");
            Ui.Stretch(bg);

            var title = Ui.OutlinedLabel(canvas.transform, "보물찾기 여정", 58, Color.white, "Title");
            Ui.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -70));

            // 머니: [코인 + 숫자]
            var player = DataHub.I.GetPlayer();
            var money = Ui.IconValue(canvas.transform, SpriteBank.GetEnv("icon_coin"),
                player.money.ToString(), 48, new Color(1f, 0.85f, 0.2f), "Money");
            Ui.Place((RectTransform)money.transform.parent, new Vector2(0f, 1f), new Vector2(60, -80));

            // 홈 버튼
            var home = Ui.ImageButton(canvas.transform, SpriteBank.GetEnv("homebutton"), new Vector2(90, 90),
                () => ScreenRouter.I.Show(ScreenId.Main), "HomeButton");
            Ui.Place((RectTransform)home.transform, new Vector2(1f, 1f), new Vector2(-40, -40));

            // 여행지 버튼 3x3 — 지역명만 표시 (숫자 없음)
            for (int s = 1; s <= 9; s++)
            {
                int stageNum = s;
                int row = (s - 1) / 3;
                int col = (s - 1) % 3;
                var pos = new Vector2((col - 1) * 420, 130 - row * 230);

                var stageData = DataHub.I.GetStage(stageNum);
                var thumb = SpriteBank.GetEnv("stage" + s + "thumb") ?? SpriteBank.GetEnv(stageData.bg);
                bool locked = s > 1 && player.mapClear[s - 1] == 0; // 이전 여행지 클리어 필요

                var btn = Ui.ImageButton(canvas.transform, thumb, new Vector2(380, 190), null, "Stage" + s);
                Ui.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), pos);
                var btnImg = btn.GetComponent<Image>();

                var place = Ui.OutlinedLabel(btn.transform, stageData.label, 34, Color.white, "Place");
                Ui.Place((RectTransform)place.transform, new Vector2(0.5f, 0f), new Vector2(0, 30));

                if (locked)
                {
                    btnImg.color = new Color(0.3f, 0.3f, 0.35f); // 어둡게
                    place.color = new Color(0.65f, 0.65f, 0.65f);
                    var lockImg = Ui.Image(btn.transform, SpriteBank.GetEnv("icon_lock"), "Lock");
                    Ui.Place((RectTransform)lockImg.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(64, 64));
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
