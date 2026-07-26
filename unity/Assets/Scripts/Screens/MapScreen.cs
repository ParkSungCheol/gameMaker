using GameMaker.Battle;
using GameMaker.Core;
using GameMaker.Data;
using GameMaker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameMaker.Screens
{
    /// <summary>스테이지 선택 — 각 스테이지의 실제 배경 썸네일 버튼 + 클리어 표시(코인).</summary>
    public class MapScreen : MonoBehaviour
    {
        void Start()
        {
            var canvas = Ui.CreateCanvas(transform, "MapCanvas");

            var bg = Ui.Panel(canvas.transform, new Color(0.09f, 0.11f, 0.16f), "Bg");
            Ui.Stretch(bg);

            // 머니: [코인 + 숫자]
            var player = DataHub.I.GetPlayer();
            var money = Ui.IconValue(canvas.transform, SpriteBank.GetEnv("icon_coin"),
                player.money.ToString(), 48, new Color(1f, 0.85f, 0.2f), "Money");
            Ui.Place((RectTransform)money.transform.parent, new Vector2(0f, 1f), new Vector2(60, -80));

            // 홈 버튼
            var home = Ui.ImageButton(canvas.transform, SpriteBank.GetEnv("homebutton"), new Vector2(90, 90),
                () => ScreenRouter.I.Show(ScreenId.Main), "HomeButton");
            Ui.Place((RectTransform)home.transform, new Vector2(1f, 1f), new Vector2(-40, -40));

            // 스테이지 버튼 3x3: 테마 배경 썸네일 + 스테이지 숫자 + 클리어 코인
            for (int s = 1; s <= 9; s++)
            {
                int stageNum = s;
                int row = (s - 1) / 3;
                int col = (s - 1) % 3;
                var pos = new Vector2((col - 1) * 420, 130 - row * 230);

                var thumb = SpriteBank.GetEnv("stage" + s + "bg");
                var btn = Ui.ImageButton(canvas.transform, thumb, new Vector2(380, 190),
                    () => ScreenRouter.I.Show(ScreenId.Battlefield, stageNum), "Stage" + s);
                Ui.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), pos);

                // 스테이지 숫자 (외곽선)
                var num = Ui.OutlinedLabel(btn.transform, s.ToString(), 52, Color.white, "Num");
                Ui.Place((RectTransform)num.transform, new Vector2(0f, 1f), new Vector2(34, -34));

                // 클리어 횟수 → 코인 아이콘 나열 (최대 5개)
                int cleared = Mathf.Min(player.mapClear[s], 5);
                for (int c = 0; c < cleared; c++)
                {
                    var coin = Ui.Image(btn.transform, SpriteBank.GetEnv("icon_coin"), "Clear" + c);
                    Ui.Place((RectTransform)coin.transform, new Vector2(1f, 0f), new Vector2(-24 - c * 36, 24), new Vector2(32, 32));
                    coin.preserveAspect = true;
                }
            }
        }
    }
}
