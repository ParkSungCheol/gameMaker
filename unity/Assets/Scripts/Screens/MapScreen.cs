using GameMaker.Battle;
using GameMaker.Core;
using GameMaker.Data;
using GameMaker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameMaker.Screens
{
    /// <summary>
    /// 보물찾기 여정 지도 — 테마 12개(4x3) 카드, 테마 안 서브스테이지는 ◀ ▶ 화살표로 선택.
    /// stageId = 테마*10 + 서브. 순차 해금: 이전 서브(테마 첫 판이면 이전 테마 마지막 판) 클리어 필요.
    /// </summary>
    public class MapScreen : MonoBehaviour
    {
        PlayerData player;

        // 신규 테마(9~11)는 전용 썸네일이 없어 분위기가 비슷한 기존 썸네일을 빌려 쓴다.
        static readonly System.Collections.Generic.Dictionary<int, int> ThumbAlias =
            new System.Collections.Generic.Dictionary<int, int> { { 9, 1 }, { 10, 1 }, { 11, 7 }, { 12, 9 } };

        bool Cleared(int stageId) => stageId >= 0 && stageId < player.mapClear.Length && player.mapClear[stageId] > 0;

        ToastBar toast;

        bool Unlocked(int theme, int sub)
        {
            if (Core.Dev.UnlockAllStages) return true; // [DEV] 전체 해금 — Core/Dev.cs 에서 끄면 아래 순차 해금 복원
            if (theme == 1 && sub == 1) return true;
            if (sub > 1) return Cleared(theme * 10 + sub - 1);
            var prev = DataHub.I.GetStage(theme - 1);
            return Cleared((theme - 1) * 10 + Mathf.Max(1, prev.subCount));
        }

        void Start()
        {
            var canvas = Ui.CreateCanvas(transform, "MapCanvas");
            MenuBackdrop.Build(this, canvas, dim: 0.5f, withGround: false);
            toast = Ui.CreateToast(canvas.transform, 60);

            var title = Ui.OutlinedLabel(canvas.transform, "보물찾기 여정", 52, Color.white, "Title");
            Ui.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -55));

            player = DataHub.I.GetPlayer();
            var moneyPanel = Ui.Image(canvas.transform, SpriteBank.GetEnv("panel_parchment"), "MoneyPanel");
            Ui.Place((RectTransform)moneyPanel.transform, new Vector2(0f, 1f), new Vector2(160, -55), new Vector2(280, 82));
            var money = Ui.CenteredIconValue(moneyPanel.transform, SpriteBank.GetEnv("icon_coin"),
                player.money.ToString(), 42, Color.white, "Money");
            Ui.Place((RectTransform)money.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0, 2));

            var back = Ui.CircleIconButton(canvas.transform, "icon_return", 92,
                () => ScreenRouter.I.Show(ScreenId.Main), "BackButton");
            Ui.Place((RectTransform)back.transform, new Vector2(1f, 0f), new Vector2(-58, 50));

            // 테마 카드 4x3
            for (int t = 1; t <= 12; t++)
                BuildThemeCard(canvas, t);
        }

        void BuildThemeCard(Canvas canvas, int theme)
        {
            var stageData = DataHub.I.GetStage(theme);
            int subCount = Mathf.Max(1, stageData.subCount);

            int row = (theme - 1) / 4;
            int col = (theme - 1) % 4;
            var pos = new Vector2((col - 1.5f) * 335, 135 - row * 245);

            // 기본 선택: 아직 못 깬 첫 판 (전부 깼으면 마지막 판)
            int selected = subCount;
            for (int s = 1; s <= subCount; s++)
                if (!Cleared(theme * 10 + s)) { selected = s; break; }

            bool themeOpen = Unlocked(theme, 1);

            var frame = Ui.RoundedPanel(canvas.transform, themeOpen
                ? new Color(0.96f, 0.92f, 0.78f)
                : new Color(0.4f, 0.4f, 0.45f), "Frame" + theme);
            Ui.Place((RectTransform)frame.transform, new Vector2(0.5f, 0.5f), pos, new Vector2(320, 228));

            int thumbTheme = ThumbAlias.TryGetValue(theme, out var alias) ? alias : theme;
            var thumb = SpriteBank.GetEnv("stage" + thumbTheme + "thumb") ?? SpriteBank.GetEnv(stageData.bg);

            var btn = Ui.ImageButton(canvas.transform, thumb, new Vector2(304, 158), null, "Theme" + theme);
            var btnRt = (RectTransform)btn.transform;
            Ui.Place(btnRt, new Vector2(0.5f, 0.5f), pos + new Vector2(0, 27));
            var btnImg = btn.GetComponent<Image>();

            // 지역명 띠
            var band = Ui.RoundedPanel(btn.transform, new Color(0f, 0f, 0f, 0.55f), "NameBand");
            Ui.Place((RectTransform)band.transform, new Vector2(0.5f, 0f), new Vector2(0, 6), new Vector2(250, 42));
            band.raycastTarget = false;
            var place = Ui.OutlinedLabel(band.transform, stageData.label, 28, Color.white, "Place");
            Ui.Stretch((RectTransform)place.transform);

            if (!themeOpen)
            {
                btnImg.color = new Color(0.3f, 0.3f, 0.35f);
                place.color = new Color(0.82f, 0.82f, 0.82f);
                var lockImg = Ui.Image(btn.transform, SpriteBank.GetEnv("icon_lock"), "Lock");
                Ui.Place((RectTransform)lockImg.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 16), new Vector2(56, 56));
                lockImg.preserveAspect = true;
                btn.onClick.AddListener(() =>
                {
                    Ui.Flash(this, btnImg, new Color(0.55f, 0.15f, 0.15f));
                    toast.Show("이전 여행지를 먼저 클리어해야 합니다.");
                });
                // 잠긴 테마도 서브 표시줄 자리는 유지 (레이아웃 정렬)
                var lockedLbl = Ui.OutlinedLabel(canvas.transform, "???", 28, new Color(0.85f, 0.85f, 0.85f), "Sub" + theme);
                Ui.Place((RectTransform)lockedLbl.transform, new Vector2(0.5f, 0.5f), pos + new Vector2(0, -85));
                return;
            }

            // ── 서브스테이지 선택줄: ◀ t-s ▶  (+ 보상 미리보기) ──
            var subLabel = Ui.OutlinedLabel(canvas.transform, "", 30, Color.white, "Sub" + theme);
            Ui.Place((RectTransform)subLabel.transform, new Vector2(0.5f, 0.5f), pos + new Vector2(-30, -85), new Vector2(120, 40));

            var rewardLbl = Ui.OutlinedLabel(canvas.transform, "", 28, new Color(1f, 0.88f, 0.3f), "Reward" + theme);
            rewardLbl.alignment = TextAnchor.MiddleLeft;
            Ui.Place((RectTransform)rewardLbl.transform, new Vector2(0.5f, 0.5f), pos + new Vector2(75, -85), new Vector2(110, 36));

            System.Action refresh = () =>
            {
                bool open = Unlocked(theme, selected);
                subLabel.text = open ? theme + " - " + selected : theme + " - " + selected + " (잠김)";
                subLabel.color = open ? Color.white : new Color(0.65f, 0.65f, 0.65f);
                btnImg.color = open ? Color.white : new Color(0.45f, 0.45f, 0.5f);
                if (open)
                {
                    int stageId = theme * 10 + selected;
                    int clears = player.mapClear[stageId];
                    int reward = LocalDataService.ClearReward(stageId, clears + 1); // 다음 클리어 보상 (10단위)
                    rewardLbl.text = "+" + reward;
                    rewardLbl.color = clears >= 10 ? new Color(0.85f, 0.85f, 0.85f) : new Color(1f, 0.88f, 0.3f);
                }
                else rewardLbl.text = "";
            };

            // 화살표 버튼 (원형)
            var left = Ui.CircleIconButton(canvas.transform, "icon_return", 52, () =>
            {
                selected = selected == 1 ? subCount : selected - 1;
                refresh();
            }, "Prev" + theme);
            Ui.Place((RectTransform)left.transform, new Vector2(0.5f, 0.5f), pos + new Vector2(-115, -85));

            var right = Ui.CircleIconButton(canvas.transform, "icon_return", 52, () =>
            {
                selected = selected == subCount ? 1 : selected + 1;
                refresh();
            }, "Next" + theme);
            ((RectTransform)right.transform).localScale = new Vector3(-1f, 1f, 1f); // 아이콘 좌우 반전 → 오른쪽 화살표
            Ui.Place((RectTransform)right.transform, new Vector2(0.5f, 0.5f), pos + new Vector2(115, -85));

            btn.onClick.AddListener(() =>
            {
                if (Unlocked(theme, selected))
                    ScreenRouter.I.Show(ScreenId.Battlefield, theme * 10 + selected);
                else
                {
                    Ui.Flash(this, btnImg, new Color(0.55f, 0.15f, 0.15f));
                    toast.Show("이전 스테이지를 먼저 클리어해야 합니다.");
                }
            });

            refresh();
        }
    }
}
