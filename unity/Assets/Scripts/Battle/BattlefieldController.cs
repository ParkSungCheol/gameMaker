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
        Text speedText;
        int gameSpeed = 1;
        const string SpeedPrefKey = "gameSpeed"; // 배속은 스테이지가 바뀌어도 유지
        Image walletImg;
        readonly List<Image> spawnBtnImages = new List<Image>();
        readonly List<int> spawnBtnCosts = new List<int>();
        readonly List<Image> spawnCdOverlays = new List<Image>();  // 쿨타임 차오름 표시
        readonly List<float> spawnCooldowns = new List<float>();
        readonly List<float> spawnTimers = new List<float>();      // 남은 쿨타임
        readonly List<Image> partyPips = new List<Image>();        // 부대 정원 게이지 (10칸)

        static readonly string[] SpawnableUnits = { "ourbasic", "ourtank", "ourbattle", "ourmass" };

        void Start()
        {
            stage = DataHub.I.GetStage(mapNumber);
            SetupCamera();
            SetupBackground();
            SetupGround();
            SetupAmbient();
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

        /// <summary>스테이지 배경 — 하늘 장면(하늘+태양+소품) 또는 통짜 그림.</summary>
        void SetupBackground()
        {
            if (!string.IsNullOrEmpty(stage.sky))
            {
                SetupSkyScene();
                return;
            }

            var bgSprite = Resources.Load<Sprite>("Sprites/env/" + stage.bg);
            if (bgSprite == null) return;

            var bg = new GameObject("StageBg");
            bg.transform.SetParent(transform, false);
            var sr = bg.AddComponent<SpriteRenderer>();
            sr.sprite = bgSprite;
            sr.sortingOrder = -20;

            // 전장(2800px + 여유)을 덮도록 균등 스케일.
            // 배경 그림의 자체 지면 실루엣(하단 ~8%)이 우리 지면선(y=0)에 걸치도록 배치
            float scale = (WorldWidth + 900f) / bgSprite.bounds.size.x;
            bg.transform.localScale = new Vector3(scale, scale, 1f);
            float h = bgSprite.bounds.size.y * scale;
            bg.transform.position = new Vector3(WorldWidth * 0.5f, h * 0.5f - h * 0.08f, 0f);

            // 배경 하단 색으로 카메라 배경도 맞춤 (배경 밖 영역 위화감 제거)
            var tex = bgSprite.texture;
            if (tex != null && tex.isReadable)
                Camera.main.backgroundColor = tex.GetPixel(tex.width / 2, 5);
        }

        /// <summary>카툰 하늘 장면: 하늘 그라데이션 + 태양 + 스테이지 소품들.</summary>
        void SetupSkyScene()
        {
            // 스테이지 시간대/분위기 색조 (아침/정글 안개/노을/설원 냉기 등)
            var tint = Color.white;
            if (!string.IsNullOrEmpty(stage.skyTint))
                ColorUtility.TryParseHtmlString(stage.skyTint, out tint);

            var skySprite = Resources.Load<Sprite>("Sprites/env/" + stage.sky);
            if (skySprite != null)
            {
                var sky = new GameObject("Sky");
                sky.transform.SetParent(transform, false);
                var sr = sky.AddComponent<SpriteRenderer>();
                sr.sprite = skySprite;
                sr.color = tint;
                sr.sortingOrder = -20;
                sky.transform.localScale = new Vector3(
                    (WorldWidth + 900f) / skySprite.bounds.size.x,
                    2200f / skySprite.bounds.size.y, 1f);
                sky.transform.position = new Vector3(WorldWidth * 0.5f, 800f, 0f);
                Camera.main.backgroundColor = new Color(0.55f, 0.86f, 0.95f) * tint;
            }

            // 떠다니는 구름들 — 화면 전체에 고르게 시작, 왼쪽 밖으로 나가면 오른쪽에서 새 모습으로 재등장
            for (int i = 0; i < (stage.noClouds ? 0 : 6); i++)
            {
                var cl = new GameObject("Cloud" + i);
                cl.transform.SetParent(transform, false);
                var csr = cl.AddComponent<SpriteRenderer>();
                csr.sprite = SpriteBank.Cloud;
                csr.color = new Color(1f, 1f, 1f, 0.92f);
                csr.sortingOrder = -18;
                float s = 1.5f + (i % 3) * 0.6f;
                cl.transform.localScale = new Vector3(s, s, 1f);
                float y = 800f + (i % 3) * 160f + (i % 2) * 70f;
                cl.transform.position = new Vector3(150f + i * 560f, y, 0f);
                var drift = cl.AddComponent<Drift>();
                drift.speed = Random.Range(24f, 55f); // 완전 랜덤 — 서로 다른 속도로 흩어짐
                drift.baseY = y;
            }

            var sunSprite = stage.noSun ? null : Resources.Load<Sprite>("Sprites/env/sun");
            if (sunSprite != null)
            {
                var sun = new GameObject("Sun");
                sun.transform.SetParent(transform, false);
                var sr = sun.AddComponent<SpriteRenderer>();
                sr.sprite = sunSprite;
                sr.sortingOrder = -19;
                float s = 260f / sunSprite.bounds.size.y;
                sun.transform.localScale = new Vector3(s, s, 1f);
                sun.transform.position = new Vector3(WorldWidth - 480f, 980f, 0f);
            }

            if (stage.props == null) return;
            foreach (var p in stage.props)
            {
                var sp = Resources.Load<Sprite>("Sprites/env/" + p.img);
                if (sp == null) continue;

                // 밑동 루트 — 지면에 깊이 파묻혀 립이 자연스럽게 덮음.
                // 원경 실루엣(반투명 거대 오브젝트)은 더 깊이 심어 지평선 언덕처럼
                bool isBackdrop = p.alpha > 0f && p.alpha < 1f;
                var root = new GameObject("Prop_" + p.img);
                root.transform.SetParent(transform, false);
                root.transform.position = new Vector3(p.x, isBackdrop ? -110f : -30f, 0f);

                var body = new GameObject("Sprite");
                body.transform.SetParent(root.transform, false);
                var sr = body.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = isBackdrop ? -16 : -12; // 원경 실루엣은 더 뒤에
                if (isBackdrop) sr.color = new Color(1f, 1f, 1f, p.alpha);
                float s = p.h / sp.bounds.size.y;
                body.transform.localScale = new Vector3(s, s, 1f);
                body.transform.localPosition = new Vector3(0, p.h * 0.5f, 0);

                // 나무/덤불만 바람 흔들림 (바위/건물 제외)
                if (p.img.Contains("pine") || p.img.Contains("bush") || p.img.Contains("tree"))
                {
                    var sway = root.AddComponent<Sway>();
                    sway.amplitude = p.h > 300 ? 1.6f : 2.6f;
                }

            }
        }

        void SetupGround()
        {
            // 구름 지면 (운해 스테이지): 흰 구름층 + 몽글몽글 구름 립
            if (stage.groundCol == "clouds")
            {
                var fillGo = new GameObject("CloudFill");
                fillGo.transform.SetParent(transform, false);
                var fillSr = fillGo.AddComponent<SpriteRenderer>();
                fillSr.sprite = SpriteBank.White;
                fillSr.color = new Color(0.93f, 0.96f, 1f);
                fillSr.sortingOrder = -10;
                fillGo.transform.localScale = new Vector3(WorldWidth + 900f, 800f, 1f);
                fillGo.transform.position = new Vector3(WorldWidth * 0.5f, -395f, 0f);

                for (int i = 0; i < 16; i++)
                {
                    var puff = new GameObject("Puff" + i);
                    puff.transform.SetParent(transform, false);
                    var psr = puff.AddComponent<SpriteRenderer>();
                    psr.sprite = SpriteBank.Cloud;
                    psr.color = i % 2 == 0 ? Color.white : new Color(0.88f, 0.93f, 1f);
                    psr.sortingOrder = -9;
                    float s = 1.1f + (i % 3) * 0.5f;
                    puff.transform.localScale = new Vector3(s, s, 1f);
                    puff.transform.position = new Vector3(i * 235f + (i % 2) * 80f, 8f + (i % 3) * 8f, 0f);
                    var bob = puff.AddComponent<Bob>();
                    bob.amplitude = 5f + (i % 3) * 3f;
                    bob.speed = 0.6f + (i % 4) * 0.18f;
                }
                return;
            }

            // 카툰 지면 기둥 (윗면 잔디/모래/파도 립 포함) — 가로로 타일링
            if (!string.IsNullOrEmpty(stage.groundCol))
            {
                var col = Resources.Load<Sprite>("Sprites/env/" + stage.groundCol);
                if (col != null)
                {
                    var g = new GameObject("GroundCol");
                    g.transform.SetParent(transform, false);
                    var gsr = g.AddComponent<SpriteRenderer>();
                    gsr.sprite = col;
                    gsr.drawMode = SpriteDrawMode.Tiled;
                    float ch = col.bounds.size.y;
                    gsr.size = new Vector2(WorldWidth + 900f, ch);
                    gsr.sortingOrder = -10;
                    g.transform.position = new Vector3(WorldWidth * 0.5f, -ch * 0.5f + 20f, 0f);

                    // 바다는 수면 전체가 천천히 오르내림
                    if (stage.groundCol.Contains("ocean"))
                    {
                        var bob = g.AddComponent<Bob>();
                        bob.amplitude = 9f;
                        bob.speed = 0.8f;
                    }

                    // 지면 립 물결: 립 스트립 두 겹을 좌우로 일렁이게 → 바닥 자체가 움직이는 느낌
                    // (바다는 크게 출렁, 잔디/숲/모래/눈은 잔잔하게)
                    var lip = Resources.Load<Sprite>("Sprites/env/" + stage.groundCol.Replace("col_", "lip_"));
                    if (lip != null)
                    {
                        bool isSea = stage.groundCol.Contains("ocean");
                        float lipH = lip.bounds.size.y;
                        for (int layer = 0; layer < 2; layer++)
                        {
                            var lo = new GameObject("LipWave" + layer);
                            lo.transform.SetParent(transform, false);
                            var lsr = lo.AddComponent<SpriteRenderer>();
                            lsr.sprite = lip;
                            lsr.drawMode = SpriteDrawMode.Tiled;
                            lsr.size = new Vector2(WorldWidth + 1000f, lipH);
                            lsr.sortingOrder = -9;
                            lsr.color = layer == 0 ? Color.white : new Color(1f, 1f, 1f, 0.85f);
                            // 지면 윗선(y=20)에 정확히 겹침
                            lo.transform.position = new Vector3(WorldWidth * 0.5f + layer * 90f, 20f - lipH * 0.5f, 0f);
                            var wave = lo.AddComponent<LipWave>();
                            wave.amplitude = (layer == 0 ? 7f : 10f) * (isSea ? 2.2f : 1f);
                            wave.vertAmp = isSea ? 8f : 1.5f; // 바다 파도는 상하로도 출렁
                            wave.speed = (layer == 0 ? 1.2f : 0.9f) * (isSea ? 1.5f : 1f);
                            wave.phase = layer * 2.4f;
                        }
                    }
                    return;
                }
            }

            var tile = string.IsNullOrEmpty(stage.ground) ? null
                     : Resources.Load<Sprite>("Sprites/env/" + stage.ground);
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

                // 타일 아래 빈 공간을 어두운 동굴색으로 채움 (파란 카메라색 노출 방지)
                var under = CaveUnderColor();
                var fill = new GameObject("GroundUnder");
                fill.transform.SetParent(transform, false);
                var fsr = fill.AddComponent<SpriteRenderer>();
                fsr.sprite = SpriteBank.White;
                fsr.color = under;
                fsr.sortingOrder = -11;
                fill.transform.localScale = new Vector3(WorldWidth + 900f, 900f, 1f);
                fill.transform.position = new Vector3(WorldWidth * 0.5f, -450f, 0f);
                Camera.main.backgroundColor = under;
            }
            else
            {
                // 배경 그림 하단 색을 샘플링해서 지면을 같은 팔레트로 (조화 보장)
                sr.sprite = SpriteBank.White;
                sr.color = SampleBgBottomColor() * 0.8f;
                ground.transform.localScale = new Vector3(WorldWidth + 900f, 800f, 1f);
                ground.transform.position = new Vector3(WorldWidth * 0.5f, -400f, 0f);
            }
        }

        /// <summary>동굴 스테이지별 하부/카메라 색 — 배경 그림과 같은 계열의 어두운 톤.</summary>
        Color CaveUnderColor()
        {
            switch (stage.bg)
            {
                case "stage5bg": return new Color(0.16f, 0.11f, 0.09f); // 갈색 동굴
                case "stage6bg": return new Color(0.10f, 0.09f, 0.18f); // 수정굴 남보라
                case "stage9bg": return new Color(0.13f, 0.07f, 0.18f); // 심연 보라
                default: return new Color(0.1f, 0.1f, 0.12f);
            }
        }

        /// <summary>배경 텍스처 맨 아래 줄 평균색 (읽기 불가면 무난한 어두운 녹색).</summary>
        Color SampleBgBottomColor()
        {
            var bgSprite = Resources.Load<Sprite>("Sprites/env/" + stage.bg);
            var tex = bgSprite != null ? bgSprite.texture : null;
            if (tex == null || !tex.isReadable) return new Color(0.22f, 0.3f, 0.18f);

            Color sum = Color.black;
            int n = 0;
            for (int x = 0; x < tex.width; x += 40)
            {
                sum += tex.GetPixel(x, 4);
                n++;
            }
            var c = sum / Mathf.Max(n, 1);
            c.a = 1f;
            return c;
        }

        /// <summary>스테이지 분위기 파티클 (눈/낙엽/모래바람/반딧불/반짝임/심연/물보라).</summary>
        void SetupAmbient()
        {
            if (string.IsNullOrEmpty(stage.ambient)) return;
            var go = new GameObject("Ambient_" + stage.ambient);
            go.transform.SetParent(transform, false);
            go.AddComponent<Ambient>().Init(stage.ambient, WorldWidth);
        }

        void SetupHud()
        {
            hud = Ui.CreateCanvas(transform, "BattleHud");

            // 여행지 이름: 상단 중앙 — 기존 스타일 (외곽선 흰 글씨)
            var placeText = Ui.OutlinedLabel(hud.transform, stage.label, 52, Color.white, "Place");
            Ui.Place((RectTransform)placeText.transform, new Vector2(0.5f, 1f), new Vector2(0, -55));

            // 남은 시간: 양피지 배지 + 짙은 갈색 숫자
            var timeBadge = Ui.Image(hud.transform, SpriteBank.GetEnv("panel_parchment"), "TimeBadge");
            Ui.Place((RectTransform)timeBadge.transform, new Vector2(1f, 1f), new Vector2(-115, -55), new Vector2(200, 78));
            timeText = Ui.Label(timeBadge.transform, "3:00", 46, new Color(0.35f, 0.22f, 0.08f), "Time");
            Ui.Place((RectTransform)timeText.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 2));

            // 현재 코스트: 양피지 + [코인 + "현재 / 최대"] — 다른 HUD 글씨와 같은 외곽선 흰 글씨
            var costPanel = Ui.Image(hud.transform, SpriteBank.GetEnv("panel_parchment"), "CostPanel");
            Ui.Place((RectTransform)costPanel.transform, new Vector2(0f, 1f), new Vector2(160, -55), new Vector2(280, 82));
            costText = Ui.IconValue(costPanel.transform, SpriteBank.GetEnv("icon_coin"), "0 / 100", 42,
                Color.white, "Cost");
            Ui.Place((RectTransform)costText.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(-70, 1));

            // 지갑 업그레이드: 소환 버튼 열과 같은 높이·비슷한 크기의 원형 버튼 (좌측)
            var walletBtn = Ui.CircleIconButton(hud.transform, "icon_coins", 148, UpgradeWallet, "WalletUpgrade");
            Ui.Place((RectTransform)walletBtn.transform, new Vector2(0f, 0f), new Vector2(110, 85)); // 소환 열(y=85) 정렬
            walletImg = walletBtn.GetComponent<Image>();

            var lvBadge = Ui.RoundedPanel(walletBtn.transform, new Color(0.15f, 0.6f, 0.3f, 0.95f), "LvBadge");
            Ui.Place((RectTransform)lvBadge.transform, new Vector2(0.5f, 1f), new Vector2(38, 0), new Vector2(90, 44));
            walletLevelText = Ui.OutlinedLabel(lvBadge.transform, "Lv.1", 30, Color.white, "WalletLevel");
            Ui.Place((RectTransform)walletLevelText.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 1));

            walletPriceText = Ui.IconValue(walletBtn.transform, SpriteBank.GetEnv("icon_coin"), "50", 30,
                new Color(1f, 0.9f, 0.3f), "WalletPrice");
            Ui.Place((RectTransform)walletPriceText.transform.parent, new Vector2(0.5f, 0f), new Vector2(-28, -20));

            // 유닛 소환 버튼: 나무 버튼 프레임 + 초상화 + 가격
            for (int i = 0; i < SpawnableUnits.Length; i++)
            {
                string unitName = SpawnableUnits[i];
                var m = DataHub.I.FindMonster(unitName);
                var portrait = SpriteBank.GetEnv(unitName.Replace("our", "portrait_"));

                var btn = Ui.ImageButton(hud.transform, SpriteBank.GetEnv("btn_wood"), new Vector2(150, 140),
                    () => TrySpawnOur(unitName), "Spawn_" + unitName);
                Ui.Place((RectTransform)btn.transform, new Vector2(0.5f, 0f),
                    new Vector2((i - (SpawnableUnits.Length - 1) * 0.5f) * 175f, 85));

                var face = Ui.Image(btn.transform, portrait, "Portrait");
                Ui.Place((RectTransform)face.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(100, 100));
                face.preserveAspect = true;

                var price = Ui.IconValue(btn.transform, SpriteBank.GetEnv("icon_coin"),
                    m.cost.ToString(), 30, new Color(1f, 0.9f, 0.3f), "Price");
                Ui.Place((RectTransform)price.transform.parent, new Vector2(0.5f, 0f), new Vector2(-30, -20));

                // 쿨타임 오버레이 — 위에서부터 걷히며 차오르는 표현 (냥코 스타일)
                var cd = Ui.Image(btn.transform, SpriteBank.White, "CdOverlay");
                Ui.Stretch((RectTransform)cd.transform);
                cd.color = new Color(0f, 0f, 0f, 0.62f);
                cd.type = Image.Type.Filled;
                cd.fillMethod = Image.FillMethod.Vertical;
                cd.fillOrigin = (int)Image.OriginVertical.Top;
                cd.fillAmount = 0f;
                cd.raycastTarget = false;

                spawnBtnImages.Add(btn.GetComponent<Image>());
                spawnBtnCosts.Add(m.cost);
                spawnCdOverlays.Add(cd);
                spawnCooldowns.Add(m.cooldown > 0 ? m.cooldown : 1f);
                spawnTimers.Add(0f);
            }

            // 부대 정원 게이지 — 소환 버튼 아래 화면 바닥에 10칸 점: 찰수록 금색, 가득 차면 빨갛게 고동
            for (int i = 0; i < 10; i++)
            {
                var pip = Ui.Image(hud.transform, SpriteBank.Circle, "Pip" + i);
                Ui.Place((RectTransform)pip.transform, new Vector2(0.5f, 0f),
                    new Vector2((i - 4.5f) * 26f, 8f), new Vector2(14, 14));
                pip.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);
                partyPips.Add(pip);
            }

            // 포기(귀환) 버튼 — 우측 하단 원형 버튼
            var giveUp = Ui.CircleIconButton(hud.transform, "icon_return", 92,
                () => ScreenRouter.I.Show(ScreenId.Map), "GiveUp");
            Ui.Place((RectTransform)giveUp.transform, new Vector2(1f, 0f), new Vector2(-58, 50));

            // 배속 버튼 — 타이머 아래, x1 → x2 → x3 순환
            var speedBg = Ui.Image(hud.transform, SpriteBank.Circle, "SpeedButton");
            Ui.Place((RectTransform)speedBg.transform, new Vector2(1f, 1f), new Vector2(-115, -135), new Vector2(88, 88));
            speedBg.color = new Color(0.1f, 0.11f, 0.16f, 0.82f);
            speedText = Ui.OutlinedLabel(speedBg.transform, "x1", 38, Color.white, "SpeedLabel");
            speedText.font = Ui.TitleFont;
            Ui.Place((RectTransform)speedText.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 2));
            speedBg.gameObject.AddComponent<Button>().onClick.AddListener(CycleSpeed);

            // 저장된 배속 복원 (스테이지를 옮겨도 유지)
            gameSpeed = Mathf.Clamp(PlayerPrefs.GetInt(SpeedPrefKey, 1), 1, 3);
            ApplySpeed();
        }

        /// <summary>배속 순환 x1→x2→x3. 노랑(x2)/빨강(x3)으로 상태 표시.</summary>
        void CycleSpeed()
        {
            gameSpeed = gameSpeed >= 3 ? 1 : gameSpeed + 1;
            PlayerPrefs.SetInt(SpeedPrefKey, gameSpeed);
            ApplySpeed();
        }

        void ApplySpeed()
        {
            Time.timeScale = gameSpeed;
            speedText.text = "x" + gameSpeed;
            speedText.color = gameSpeed == 1 ? Color.white
                            : gameSpeed == 2 ? new Color(1f, 0.85f, 0.3f)
                                             : new Color(1f, 0.45f, 0.35f);
        }

        void OnDestroy() => Time.timeScale = 1f; // 화면 이탈 시 배속 복구

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

                // 보스는 종료 45초 전 등장 — 시간 내 처치/성 파괴 압박
                if (!bossSpawned && enemyCount == 45 && !string.IsNullOrEmpty(stage.boss))
                {
                    bossSpawned = true;
                    SpawnUnit(stage.boss);
                }

                timeText.text = enemyCount / 60 + ":" + (enemyCount % 60).ToString("00");
                if (enemyCount <= 10) timeText.color = new Color(0.75f, 0.15f, 0.1f); // 임박 경고(양피지 위 빨강)

                // 제한시간 종료 — 적 성을 부수지 못했으면 패배
                if (enemyCount <= 0)
                {
                    EndBattle(false);
                    yield break;
                }

                enemyCount = Mathf.Max(0, enemyCount - 1);
                yield return new WaitForSeconds(1f);
            }
        }

        void RefreshCostHud()
        {
            costText.text = cost + " / " + costMax; // 지갑 용량이 보이도록
            walletLevelText.text = "Lv." + WalletLevel;
            walletPriceText.text = (50 * WalletLevel).ToString();
        }

        /// <summary>매 프레임 소환 버튼 3단계 상태:
        /// 쿨타임 중 = 어둡게 + 오버레이 차오름 / 쿨타임 완료·돈 부족 = 회색 / 준비 = 밝게.</summary>
        void Update()
        {
            if (BattleOver || hud == null) return;

            var dim = new Color(0.4f, 0.4f, 0.4f);
            var cdDim = new Color(0.55f, 0.55f, 0.55f);
            bool partyFull = ourParty.Count - 1 >= 10;
            // 정원 가득참: 버튼 전체가 빨갛게 고동 — 못 뽑는 상태를 무조건 인지
            float fullPulse = 0.75f + 0.25f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
            var fullRed = new Color(1f * fullPulse, 0.3f * fullPulse, 0.25f * fullPulse);

            for (int i = 0; i < spawnBtnImages.Count; i++)
            {
                if (spawnTimers[i] > 0f)
                {
                    spawnTimers[i] -= Time.deltaTime;
                    spawnCdOverlays[i].fillAmount = Mathf.Clamp01(spawnTimers[i] / spawnCooldowns[i]);
                    spawnBtnImages[i].color = partyFull ? fullRed : cdDim;
                }
                else
                {
                    spawnCdOverlays[i].fillAmount = 0f;
                    spawnBtnImages[i].color = partyFull ? fullRed
                        : cost >= spawnBtnCosts[i] ? Color.white : dim;
                }
            }

            bool canUpgrade = costSpeedMs - 10 >= 100 && cost >= 50 * WalletLevel;
            walletImg.color = canUpgrade ? Color.white : dim;

            // 부대 정원 게이지 갱신 — 가득 차면 크게 고동치는 빨간 점
            int count = ourParty.Count - 1; // 성 제외
            for (int i = 0; i < partyPips.Count; i++)
            {
                if (partyFull)
                {
                    partyPips[i].color = new Color(1f, 0.25f, 0.2f);
                    float s = 1f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
                    partyPips[i].transform.localScale = new Vector3(s, s, 1f);
                }
                else
                {
                    partyPips[i].transform.localScale = Vector3.one;
                    partyPips[i].color = i < count
                        ? new Color(1f, 0.85f, 0.25f)                 // 채워진 칸 (금색)
                        : new Color(0.15f, 0.15f, 0.2f, 0.8f);        // 빈 칸
                }
            }
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

            int idx = System.Array.IndexOf(SpawnableUnits, name);
            var m = DataHub.I.FindMonster(name);

            // 쿨타임 미완 / 인원 초과 / 코스트 부족 → 해당 버튼 빨간 플래시
            if ((idx >= 0 && spawnTimers[idx] > 0f) || ourParty.Count - 1 >= 10 || cost < m.cost)
            {
                if (idx >= 0) Ui.Flash(this, spawnBtnImages[idx], new Color(1f, 0.25f, 0.25f));
                return;
            }

            cost -= m.cost;
            if (idx >= 0) spawnTimers[idx] = spawnCooldowns[idx]; // 쿨타임 시작
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

        /// <summary>이 유닛과 가장 가까운 (살아있는) 적 유닛.
        /// attackableOnly=true 면 공격 가능한 적만 — 근접 유닛(사거리 200 미만)에게
        /// 공중 적은 공격 불가. (전진 차단 판정은 attackableOnly=false 로 모든 적 포함)</summary>
        public Unit FindNearestEnemy(Unit unit, bool attackableOnly = true)
        {
            bool isMelee = unit.data.range < 200;
            var enemies = unit.IsOur ? yourParty : ourParty;
            Unit nearest = null;
            float best = float.MaxValue;
            foreach (var e in enemies)
            {
                if (e == null || e.Dead) continue;
                if (attackableOnly && isMelee && e.data.fly > 0f) continue; // 근접은 공중 타격 불가
                float d = Mathf.Abs(e.X - unit.X);
                if (d < best) { best = d; nearest = e; }
            }
            return nearest;
        }

        /// <summary>범위 피해 — 중심 X 반경 내 모든 적에게 데미지 (마법사 스플래시).</summary>
        public void DamageArea(Unit attacker, float centerX, float radius, int damage)
        {
            var enemies = attacker.IsOur ? yourParty : ourParty;
            // TakeDamage 중 사망으로 리스트가 변할 수 있어 복사본 순회
            foreach (var e in new List<Unit>(enemies))
            {
                if (e == null || e.Dead) continue;
                if (Mathf.Abs(e.X - centerX) <= radius) e.TakeDamage(damage);
            }
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
            Time.timeScale = 1f; // 결과창은 항상 정상 속도

            // 승리 = 황금 보물상자 / 패배 = 무너진 아군 성. 승리 시 보상 획득 내역 표시
            var image = win
                ? SpriteBank.GetEnv("icon_win")
                : SpriteBank.GetFrames("ourcastle", "defeat")[0];
            int before = DataHub.I.GetPlayer().money;
            int reward = win ? DataHub.I.Clear(mapNumber) : 0;

            Ui.ResultDialog(hud.transform, win, image, reward, before,
                onRetry: () => ScreenRouter.I.Show(ScreenId.Battlefield, mapNumber),
                onHome: () => ScreenRouter.I.Show(ScreenId.Map));
        }
    }
}
