using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameMaker.Core;
using GameMaker.Data;
using GameMaker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameMaker.Battle
{
    /// <summary>
    /// 레거시 BattlefieldActivity 포팅 — 전투 화면 전체.
    /// 코스트 수급/지갑 업그레이드, 180초 타이머, 적 출현 타임라인,
    /// 유닛 소환, 전투 판정(상성·공격스타일·넉백), 승패 처리.
    /// </summary>
    public class BattlefieldController : MonoBehaviour
    {
        public const float WorldWidth = 2800f;   // 레거시 width = 2800 (absolute)
        public const float OurBaseX = 25f;       // 레거시 translationX 25

        public int mapNumber = 1;

        // ── 전투 상태 ──
        readonly List<Unit> ourParty = new List<Unit>();
        readonly List<Unit> yourParty = new List<Unit>();
        public bool BattleOver { get; private set; }

        // ── 코스트 (레거시 ObjectTimer) ──
        int cost = 0;
        int costMax = 100;
        int costSpeedMs = 200;   // -10/업그레이드, 최소 100
        int WalletLevel => (210 - costSpeedMs) / 10;

        // ── 적 출현 타이머 ──
        int enemyCount = 180;
        bool bossSpawned;
        Dictionary<int, List<string>> timeline;
        StageData stage;

        // ── HUD ──
        Canvas hud;
        Text costText, timeText;
        Text walletLevelText, walletPriceText;
        Image walletImg;
        readonly List<Image> spawnBtnImages = new List<Image>();
        readonly List<int> spawnBtnCosts = new List<int>();

        static readonly string[] SpawnableUnits = { "ourbasic", "ourtank", "ourbattle", "ourmass" };

        void Start()
        {
            stage = DataHub.I.GetStage(mapNumber);
            SetupCamera();
            SetupBackground();
            SetupGround();
            SetupHud();

            // 성 배치 (레거시 runnableByName("ourcastle"/"yourcastle"))
            SpawnUnit("ourcastle");
            SpawnUnit("yourcastle");

            // 적 출현 타임라인 로드 (레거시 getEnemiesByMapNumber)
            timeline = DataHub.I.GetEnemiesByMap(mapNumber)
                .GroupBy(e => e.time)
                .ToDictionary(g => g.Key, g => g.Select(e => e.name).ToList());

            StartCoroutine(CostLoop());
            StartCoroutine(EnemyTimerLoop());
        }

        // ────────────────────────── 셋업 ──────────────────────────

        void SetupCamera()
        {
            var cam = Camera.main;
            cam.orthographic = true;
            float aspect = (float)Screen.width / Screen.height;
            cam.orthographicSize = (WorldWidth * 0.5f + 150f) / aspect;
            cam.transform.position = new Vector3(WorldWidth * 0.5f, cam.orthographicSize * 0.45f, -100f);
            cam.backgroundColor = new Color(0.45f, 0.65f, 0.85f); // 하늘
        }

        /// <summary>스테이지 테마 배경 이미지를 전장 전체에 깔아준다.</summary>
        void SetupBackground()
        {
            var bgSprite = Resources.Load<Sprite>("Sprites/env/" + stage.bg);
            if (bgSprite == null) return;

            var bg = new GameObject("StageBg");
            bg.transform.SetParent(transform, false);
            var sr = bg.AddComponent<SpriteRenderer>();
            sr.sprite = bgSprite;
            sr.sortingOrder = -20;

            // 전장(2800px + 여유)을 덮도록 균등 스케일, 지면(y=0) 기준 배치
            float scale = (WorldWidth + 900f) / bgSprite.bounds.size.x;
            bg.transform.localScale = new Vector3(scale, scale, 1f);
            float h = bgSprite.bounds.size.y * scale;
            bg.transform.position = new Vector3(WorldWidth * 0.5f, h * 0.5f - 120f, 0f);

            // 배경 하단 색으로 카메라 배경도 맞춤 (배경 밖 영역 위화감 제거)
            var tex = bgSprite.texture;
            if (tex != null && tex.isReadable)
                Camera.main.backgroundColor = tex.GetPixel(tex.width / 2, 5);
        }

        void SetupGround()
        {
            var tile = Resources.Load<Sprite>("Sprites/env/" + stage.ground);
            var ground = new GameObject("Ground");
            ground.transform.SetParent(transform, false);
            var sr = ground.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -10;

            if (tile != null)
            {
                sr.sprite = tile;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(WorldWidth + 900f, tile.bounds.size.y);
                ground.transform.position = new Vector3(WorldWidth * 0.5f, -tile.bounds.size.y * 0.5f + 8f, 0f);
            }
            else
            {
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                sr.color = new Color(0.25f, 0.4f, 0.2f);
                ground.transform.localScale = new Vector3(WorldWidth + 800f, 800f, 1f);
                ground.transform.position = new Vector3(WorldWidth * 0.5f, -400f, 0f);
            }
        }

        void SetupHud()
        {
            hud = Ui.CreateCanvas(transform, "BattleHud");

            // 스테이지 표기: 방패 아이콘 + 숫자 (텍스트 없음)
            var stageNo = Ui.IconValue(hud.transform, SpriteBank.GetEnv("icon_shield"),
                mapNumber.ToString(), 46, Color.white, "StageNo");
            Ui.Place((RectTransform)stageNo.transform.parent, new Vector2(0.5f, 1f), new Vector2(-40, -60));

            // 남은 시간 (레거시 timeText)
            timeText = Ui.Label(hud.transform, "3:0", 52, Color.white, "Time");
            Ui.Place((RectTransform)timeText.transform, new Vector2(1f, 1f), new Vector2(-80, -50));

            // 현재 코스트: [코인 아이콘 + 숫자]
            costText = Ui.IconValue(hud.transform, SpriteBank.GetEnv("icon_coin"), "0", 54,
                new Color(1f, 0.9f, 0.3f), "Cost");
            Ui.Place((RectTransform)costText.transform.parent, new Vector2(0f, 1f), new Vector2(70, -70));

            // 지갑 업그레이드 버튼: 업그레이드 아이콘 + 위에 Lv 뱃지 + 아래에 [코인+가격]
            var walletBtn = Ui.ImageButton(hud.transform, SpriteBank.GetEnv("icon_upgrade"),
                new Vector2(100, 85), UpgradeWallet, "WalletUpgrade");
            Ui.Place((RectTransform)walletBtn.transform, new Vector2(0f, 1f), new Vector2(90, -160));
            walletImg = walletBtn.GetComponent<Image>();
            walletImg.preserveAspect = true;

            walletLevelText = Ui.OutlinedLabel(walletBtn.transform, "Lv.1", 30, Color.white, "WalletLevel");
            Ui.Place((RectTransform)walletLevelText.transform, new Vector2(0.5f, 1f), new Vector2(0, 34));

            walletPriceText = Ui.IconValue(walletBtn.transform, SpriteBank.GetEnv("icon_coin"), "50", 30,
                new Color(1f, 0.9f, 0.3f), "WalletPrice");
            Ui.Place((RectTransform)walletPriceText.transform.parent, new Vector2(0.5f, 0f), new Vector2(-28, -26));

            // 유닛 소환 버튼: 에셋의 초상화(Portrait) 사용, 가격은 버튼 아래 [코인+숫자]로 (이미지 가림 없음)
            for (int i = 0; i < SpawnableUnits.Length; i++)
            {
                string unitName = SpawnableUnits[i];
                var m = DataHub.I.FindMonster(unitName);
                var portrait = SpriteBank.GetEnv(unitName.Replace("our", "portrait_"));

                var btn = Ui.ImageButton(hud.transform, portrait, new Vector2(130, 130),
                    () => TrySpawnOur(unitName), "Spawn_" + unitName);
                Ui.Place((RectTransform)btn.transform, new Vector2(0.5f, 0f),
                    new Vector2((i - (SpawnableUnits.Length - 1) * 0.5f) * 170f, 80));

                var price = Ui.IconValue(btn.transform, SpriteBank.GetEnv("icon_coin"),
                    m.cost.ToString(), 30, new Color(1f, 0.9f, 0.3f), "Price");
                Ui.Place((RectTransform)price.transform.parent, new Vector2(0.5f, 0f), new Vector2(-30, -28));

                spawnBtnImages.Add(btn.GetComponent<Image>());
                spawnBtnCosts.Add(m.cost);
            }

            // 포기(귀환) 버튼 — 화살표 아이콘
            var giveUp = Ui.ImageButton(hud.transform, SpriteBank.GetEnv("icon_return"),
                new Vector2(90, 72), () => ScreenRouter.I.Show(ScreenId.Map), "GiveUp");
            Ui.Place((RectTransform)giveUp.transform, new Vector2(1f, 0f), new Vector2(-40, 40));
            giveUp.GetComponent<Image>().preserveAspect = true;
        }

        // ────────────────────────── 루프 ──────────────────────────

        /// <summary>레거시 Timer/TimerTask — costSpeedMs 마다 코스트 +1.</summary>
        IEnumerator CostLoop()
        {
            while (!BattleOver)
            {
                yield return new WaitForSeconds(costSpeedMs / 1000f);
                cost = Mathf.Min(cost + 1, costMax);
                RefreshCostHud();
            }
        }

        /// <summary>레거시 enemyTimer — 1초마다 타임라인 체크, 0초에 보스.</summary>
        IEnumerator EnemyTimerLoop()
        {
            while (!BattleOver)
            {
                if (timeline.TryGetValue(enemyCount, out var names))
                {
                    timeline.Remove(enemyCount);
                    foreach (var n in names) SpawnUnit(n);
                }

                if (!bossSpawned && enemyCount == 0 && !string.IsNullOrEmpty(stage.boss))
                {
                    bossSpawned = true;
                    SpawnUnit(stage.boss);
                }

                timeText.text = enemyCount / 60 + ":" + (enemyCount % 60).ToString("00");
                if (enemyCount <= 10) timeText.color = new Color(0.9f, 0.4f, 1f); // 레거시 purple_200

                enemyCount = Mathf.Max(0, enemyCount - 1);
                yield return new WaitForSeconds(1f);
            }
        }

        void RefreshCostHud()
        {
            costText.text = cost.ToString();
            walletLevelText.text = "Lv." + WalletLevel;
            walletPriceText.text = (50 * WalletLevel).ToString();
        }

        /// <summary>매 프레임: 살 수 없는 버튼은 어둡게 표시 (텍스트 안내 대신 시각적 피드백).</summary>
        void Update()
        {
            if (BattleOver || hud == null) return;

            var dim = new Color(0.4f, 0.4f, 0.4f);
            for (int i = 0; i < spawnBtnImages.Count; i++)
                spawnBtnImages[i].color = cost >= spawnBtnCosts[i] ? Color.white : dim;

            bool canUpgrade = costSpeedMs - 10 >= 100 && cost >= 50 * WalletLevel;
            walletImg.color = canUpgrade ? Color.white : dim;
        }

        /// <summary>레거시 costUpgrade 클릭 — 속도 -10ms(최소 100), max +20, 비용 50*level.
        /// 조건 미달이면 조용히 무시 (버튼이 어둡게 표시되므로 별도 안내 없음).</summary>
        void UpgradeWallet()
        {
            int newSpeed = costSpeedMs - 10;
            int require = 50 * WalletLevel;

            if (newSpeed < 100 || cost < require) return;

            cost -= require;
            costSpeedMs = newSpeed;
            costMax += 20;
            RefreshCostHud();
        }

        // ────────────────────────── 소환 ──────────────────────────

        /// <summary>아군 소환 버튼 — 인원 초과/코스트 부족은 버튼 빨간 플래시로만 알림 (텍스트 없음).</summary>
        void TrySpawnOur(string name)
        {
            if (BattleOver) return;

            var m = DataHub.I.FindMonster(name);
            if (ourParty.Count >= 10 || cost < m.cost)
            {
                foreach (var img in spawnBtnImages) Ui.Flash(this, img, new Color(1f, 0.25f, 0.25f));
                return;
            }

            cost -= m.cost;
            RefreshCostHud();
            SpawnUnit(name);
        }

        Unit SpawnUnit(string name)
        {
            var data = DataHub.I.FindMonster(name)?.Clone();
            if (data == null) return null;

            // 업그레이드 적용: 레벨당 HP/공격 +20%
            if (data.IsOur)
            {
                int level = DataHub.I.GetUpgradeCount(name);
                if (level > 0)
                {
                    data.hp = Mathf.RoundToInt(data.hp * (1f + 0.2f * level));
                    data.attack = Mathf.RoundToInt(data.attack * (1f + 0.2f * level));
                }
            }

            var go = new GameObject("Unit_" + name);
            go.transform.SetParent(transform, false);
            var unit = go.AddComponent<Unit>();
            unit.Init(this, data, data.IsOur ? OurBaseX : WorldWidth);

            (data.IsOur ? ourParty : yourParty).Add(unit);
            return unit;
        }

        // ────────────────────────── 전투 판정 ──────────────────────────
        // 공격 규칙 자체는 Unit.cs 에 있다. 컨트롤러는 두 가지만 담당:
        //   ① 타깃 찾기(FindNearestEnemy)  ② 죽음 처리 및 승패(OnUnitDead)

        /// <summary>이 유닛과 가장 가까운 (살아있는) 적 유닛.</summary>
        public Unit FindNearestEnemy(Unit unit)
        {
            var enemies = unit.IsOur ? yourParty : ourParty;
            Unit nearest = null;
            float best = float.MaxValue;
            foreach (var e in enemies)
            {
                if (e == null || e.Dead) continue;
                float d = Mathf.Abs(e.X - unit.X);
                if (d < best) { best = d; nearest = e; }
            }
            return nearest;
        }

        /// <summary>유닛 사망 처리. 성이 죽으면 그 자리에서 승패 결정.</summary>
        public void OnUnitDead(Unit unit)
        {
            (unit.IsOur ? ourParty : yourParty).Remove(unit);
            unit.PlayDeathAndDestroy();

            if (unit.IsCastle)
                EndBattle(win: !unit.IsOur); // 적 성이 죽었으면 승리
        }

        void EndBattle(bool win)
        {
            if (BattleOver) return;
            BattleOver = true;

            // 승리 = 황금 보물상자 / 패배 = 무너진 아군 성 (텍스트 없음)
            var image = win
                ? SpriteBank.GetEnv("icon_win")
                : SpriteBank.GetFrames("ourcastle", "defeat")[0];
            Ui.ResultDialog(hud.transform, image, () =>
            {
                if (win) DataHub.I.Clear(mapNumber);
                ScreenRouter.I.Show(ScreenId.Map);
            });
        }
    }
}
