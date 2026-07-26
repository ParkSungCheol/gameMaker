using GameMaker.Battle;
using GameMaker.Core;
using GameMaker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameMaker.Screens
{
    /// <summary>시작 화면 — 텍스트 없이 걷는 유닛 + 깜빡이는 시작 화살표.</summary>
    public class WaitingScreen : MonoBehaviour
    {
        void Start()
        {
            var canvas = Ui.CreateCanvas(transform, "WaitingCanvas");

            // 배경: 스테이지1 하늘
            var bgSprite = SpriteBank.GetEnv("stage1bg");
            if (bgSprite != null)
            {
                var bg = Ui.Image(canvas.transform, bgSprite, "Bg");
                Ui.Stretch((RectTransform)bg.transform);
            }
            else
            {
                var bg = Ui.Panel(canvas.transform, new Color(0.07f, 0.08f, 0.12f), "Bg");
                Ui.Stretch(bg);
            }

            // 가운데: 아군 성 + 걷는 유닛 애니메이션
            var castle = Ui.Image(canvas.transform, SpriteBank.GetFrames("ourcastle", "move")[0], "Castle");
            Ui.Place((RectTransform)castle.transform, new Vector2(0.5f, 0.5f), new Vector2(-160, 60), new Vector2(300, 300));
            castle.preserveAspect = true;

            var frames = SpriteBank.GetFrames("ourbasic", "move");
            var unitImg = Ui.Image(canvas.transform, frames[0], "UnitPreview");
            Ui.Place((RectTransform)unitImg.transform, new Vector2(0.5f, 0.5f), new Vector2(120, 20), new Vector2(180, 180));
            unitImg.preserveAspect = true;
            if (frames.Length > 1) StartCoroutine(CycleUiFrames(unitImg, frames));

            // 게임 타이틀 (ThaleahFat 대형 픽셀 폰트)
            var title = Ui.OutlinedLabel(canvas.transform, "GAME MAKER", 150, new Color(1f, 0.9f, 0.35f), "Title");
            title.font = Ui.TitleFont;
            Ui.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -170));

            // 시작 안내: 화살표 + 문구 깜빡임
            var arrow = Ui.Image(canvas.transform, SpriteBank.GetEnv("icon_return"), "StartArrow");
            Ui.Place((RectTransform)arrow.transform, new Vector2(0.5f, 0.5f), new Vector2(-190, -220), new Vector2(90, 72));
            arrow.preserveAspect = true;
            arrow.transform.localScale = new Vector3(-1f, 1f, 1f); // 좌우 반전 → 오른쪽 화살표
            StartCoroutine(Blink(arrow));

            var hint = Ui.OutlinedLabel(canvas.transform, "화면을 터치하세요", 52, Color.white, "Hint");
            Ui.Place((RectTransform)hint.transform, new Vector2(0.5f, 0.5f), new Vector2(40, -220));
            StartCoroutine(BlinkText(hint));

            // 화면 전체 클릭 → 메인
            var clickCatcher = Ui.TextButton(canvas.transform, "", 1, Vector2.zero,
                () => ScreenRouter.I.Show(ScreenId.Main), new Color(0, 0, 0, 0), "ClickCatcher");
            Ui.Stretch((RectTransform)clickCatcher.transform);
            clickCatcher.transform.SetAsLastSibling();
        }

        System.Collections.IEnumerator CycleUiFrames(Image img, Sprite[] frames)
        {
            int i = 0;
            while (img != null)
            {
                img.sprite = frames[i % frames.Length];
                i++;
                yield return new WaitForSeconds(0.18f);
            }
        }

        System.Collections.IEnumerator Blink(Image img)
        {
            while (img != null)
            {
                var c = img.color;
                c.a = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.time * 3f));
                img.color = c;
                yield return null;
            }
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
