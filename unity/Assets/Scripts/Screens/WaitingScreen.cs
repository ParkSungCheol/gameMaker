using GameMaker.Battle;
using GameMaker.Core;
using GameMaker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameMaker.Screens
{
    /// <summary>타이틀 화면 — 야외 배경 + 원정대 행진 + 한글 게임명 + 깜빡이는 터치 안내.</summary>
    public class WaitingScreen : MonoBehaviour
    {
        void Start()
        {
            var canvas = Ui.CreateCanvas(transform, "WaitingCanvas");

            MenuBackdrop.Build(this, canvas);
            MenuBackdrop.BuildParade(this, canvas);

            // 게임 타이틀 (한글, 금색 + 두꺼운 외곽선)
            MenuBackdrop.TitleLabel(canvas, 140, new Vector2(0.5f, 0.5f), new Vector2(0, 210));

            // 시작 안내 문구 깜빡임 (화살표 없이 문구만 중앙 정렬)
            var hint = Ui.OutlinedLabel(canvas.transform, "화면을 터치하세요", 50, Color.white, "Hint");
            Ui.Place((RectTransform)hint.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -20));
            StartCoroutine(BlinkText(hint));

            // 화면 전체 클릭 → 메인
            var clickCatcher = Ui.TextButton(canvas.transform, "", 1, Vector2.zero,
                () => ScreenRouter.I.Show(ScreenId.Main), new Color(0, 0, 0, 0), "ClickCatcher");
            Ui.Stretch((RectTransform)clickCatcher.transform);
            clickCatcher.transform.SetAsLastSibling();
        }

        System.Collections.IEnumerator BlinkText(Text txt)
        {
            while (txt != null)
            {
                var c = txt.color;
                c.a = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.time * 3f));
                txt.color = c;
                yield return null;
            }
        }
    }
}
