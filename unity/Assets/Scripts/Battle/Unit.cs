using System.Collections;
using GameMaker.Data;
using UnityEngine;

namespace GameMaker.Battle
{
    /// <summary>
    /// ★ 유닛 전투 로직 — 이 게임의 핵심 서비스 로직 ★
    ///
    /// 규칙은 단 세 줄이다 (Update 참고):
    ///   1. 사정거리(range) 안에 적이 있으면 → 멈추고 attackInterval 마다 때린다 (모션과 데미지 동시)
    ///   2. 없으면 → 앞으로 걷는다 (아군은 오른쪽, 적군은 왼쪽. 성은 안 걷는다)
    ///   3. 체력이 0이 되면 → 죽는다 (성이 죽으면 승패 — BattlefieldController.OnUnitDead)
    ///
    /// 구조: 루트(위치/체력바) ─ Body 자식(스프라이트/걷기 흔들림 회전)
    /// </summary>
    public class Unit : MonoBehaviour
    {
        /// <summary>유닛 발밑 기준선 — 지면 립(윗면 띠) 안쪽을 밟도록 0보다 아래.</summary>
        public const float GroundY = -30f;

        public MonsterData data;
        public int hp;
        public bool IsOur => data.IsOur;
        public bool IsCastle => data.IsCastle;
        public bool Dead { get; private set; }

        public float X => transform.position.x;

        BattlefieldController ctrl;
        Transform body;             // 스프라이트가 붙는 자식 (흔들림 회전 전용)
        SpriteRenderer sr;
        SimpleSpriteAnimator anim;
        Transform hpFill;
        Transform hpBarRoot;
        SpriteRenderer hpFillSr;
        float barWidth;
        float barHeight;
        float displayedRatio = 1f;
        float targetHeight;
        float bodyScale = 1f;
        float bodyBaseY;
        float walkPhase;
        Color baseColor = Color.white; // 틴트 변형 몬스터(물/얼음/수정)의 본색
        float attackTimer;
        bool striking;              // 공격 모션 재생 중
        bool wasInRange;
        string currentAction = "";

        // ─────────────────────── 핵심 로직 ───────────────────────

        void Update()
        {
            if (Dead || ctrl == null || ctrl.BattleOver) return;

            // 체력바 부드러운 감소
            float actual = Mathf.Clamp01((float)hp / data.hp);
            if (displayedRatio > actual)
            {
                displayedRatio = Mathf.MoveTowards(displayedRatio, actual, Time.deltaTime * 1.6f);
                SetBarFill(displayedRatio);
            }

            // 휘두르는 모션이 끝날 때까지는 다른 행동으로 전환하지 않는다
            // (타깃이 이 공격으로 죽어도 모션이 끊기지 않도록)
            if (striking)
            {
                body.localRotation = Quaternion.identity;
                return;
            }

            // 차단 판정: 공중 적 포함 — 적이 앞에 있으면 전진 불가 (근접은 못 때려도 막힘)
            Unit blocker = ctrl.FindNearestEnemy(this, false);
            bool blocked = blocker != null && Mathf.Abs(blocker.X - X) <= data.range;

            if (blocked)
            {
                // 공격 판정: 실제로 때릴 수 있는 적이 사거리 안일 때만 공격, 아니면 대기(맞고 버팀)
                Unit target = ctrl.FindNearestEnemy(this, true);
                bool canHit = target != null && Mathf.Abs(target.X - X) <= data.range;
                if (canHit)
                {
                    if (!wasInRange) attackTimer = data.attackInterval; // 진입 즉시 첫 타
                    AttackTick(target);
                }
                else
                {
                    // 공중 적에게 가로막힘 — 제자리 대기
                    body.localRotation = Quaternion.identity;
                    body.localScale = new Vector3(bodyScale, bodyScale, 1f);
                    body.localPosition = new Vector3(body.localPosition.x, bodyBaseY, 0);
                    SetAction("idle");
                    wasInRange = false;
                }
                if (canHit) wasInRange = true;
            }
            else if (!IsCastle)
            {
                MoveForward();
                wasInRange = false;
            }
        }

        /// <summary>규칙 1: interval 이 찰 때만 휘두른다 — 모션과 데미지가 같은 순간에 발생.</summary>
        void AttackTick(Unit target)
        {
            body.localRotation = Quaternion.identity; // 걷기 흔들림 원위치
            body.localScale = new Vector3(bodyScale, bodyScale, 1f);
            body.localPosition = new Vector3(body.localPosition.x, bodyBaseY, 0);
            attackTimer += Time.deltaTime;

            if (data.attackInterval > 0 && attackTimer >= data.attackInterval)
            {
                attackTimer = 0f;
                Strike(target);
            }
            else if (!striking)
            {
                SetAction("idle"); // 다음 공격 대기 자세
            }
        }

        int strikeCount; // 보스 범위공격 주기 카운터

        void Strike(Unit target)
        {
            striking = true;
            currentAction = "attack";
            anim.Play(SpriteBank.GetFrames(data.SpriteName, "attack"), false,
                () => { striking = false; currentAction = ""; });
            strikeCount++;
            if (data.name == "ourmass")
            {
                // 마법사: 하늘에서 화염이 내리꽂히고, "떨어진 순간" 광역 데미지
                StartCoroutine(MeteorStrike(target.X));
            }
            else if (!IsCastle && data.aoe > 0 && strikeCount % 3 == 0
                     && data.range < 200 && string.IsNullOrEmpty(data.projectile))
            {
                // 근접 보스: 3회마다 점프 내리찍기 — 착지 순간 반경 내 광역 데미지
                // (원거리 + aoe = 마법사 스플래시 — FireProjectile 에서 처리)
                StartCoroutine(BossSlam(target));
            }
            else if (!IsCastle && (data.range >= 200 || !string.IsNullOrEmpty(data.projectile)))
            {
                // 원거리: 발사체가 "닿는 순간" 데미지 (FireProjectile 안에서 처리)
                StartCoroutine(FireProjectile(target));
            }
            else if (!IsCastle && data.melee == "pounce")
            {
                StartCoroutine(PounceAttack(target)); // 점프해서 덮치기 — 착지 순간 데미지
            }
            else if (!IsCastle && data.melee == "ram")
            {
                StartCoroutine(RamAttack(target));    // 빠르게 들이받기 — 최대 전진 순간 데미지
            }
            else if (!IsCastle && data.melee == "stomp")
            {
                StartCoroutine(StompAttack(target));  // 묵직한 내려찍기 — 착지 순간 데미지 + 흙먼지
            }
            else
            {
                if (!IsCastle) StartCoroutine(StyleMotion());
                target.TakeDamage(data.attack); // 근접: 휘두르는 순간 데미지
            }
        }

        // ─────────── 공격 이펙트 프리미티브 (Circle 스프라이트 합성) ───────────

        SpriteRenderer AtkFx(float fx, float fy, Vector2 size, Color c, float ang = 0f)
        {
            float dir = IsOur ? 1f : -1f;
            var go = new GameObject("AtkFx");
            go.transform.position = transform.position +
                new Vector3(fx * targetHeight * dir, bodyBaseY + fy * targetHeight, 0);
            go.transform.rotation = Quaternion.Euler(0, 0, ang * -dir);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = SpriteBank.Circle;
            r.color = c;
            r.sortingOrder = 31;
            go.transform.localScale = new Vector3(size.x * targetHeight / 64f, size.y * targetHeight / 64f, 1f);
            Destroy(go, 1f); // 안전장치
            return r;
        }

        IEnumerator FxFade(SpriteRenderer r, float dur, float growTo, float rotSpeed)
        {
            float dir = IsOur ? 1f : -1f;
            if (r == null) yield break;
            Vector3 s0 = r.transform.localScale;
            Color c0 = r.color;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (r == null) yield break;
                float k = Mathf.Clamp01(t / dur);
                r.transform.localScale = s0 * (1f + (growTo - 1f) * k);
                r.transform.Rotate(0, 0, rotSpeed * -dir * Time.deltaTime);
                r.color = new Color(c0.r, c0.g, c0.b, c0.a * (1f - k));
                yield return null;
            }
            if (r != null) Destroy(r.gameObject);
        }

        /// <summary>참격 궤적 — 길쭉한 타원이 살짝 돌며 사라진다.</summary>
        void FxSlash(float fx, float fy, float ang, float len, Color c, float rotSpeed = 220f) =>
            StartCoroutine(FxFade(AtkFx(fx, fy, new Vector2(len, len * 0.2f), c, ang), 0.18f, 1.3f, rotSpeed));

        /// <summary>임팩트 팝 — 원이 확 커지며 사라진다.</summary>
        void FxPop(float fx, float fy, float d, Color c) =>
            StartCoroutine(FxFade(AtkFx(fx, fy, new Vector2(d, d), c), 0.16f, 2.1f, 0f));

        /// <summary>흙먼지 — 작은 원 여러 개가 부챗살로 퍼진다.</summary>
        void FxPuff(float fx, float fy, Color c, int n = 3)
        {
            for (int i = 0; i < n; i++)
            {
                float spread = (i - (n - 1) * 0.5f) * 0.09f;
                StartCoroutine(FxFade(AtkFx(fx + spread, fy + Mathf.Abs(spread) * 0.4f,
                    new Vector2(0.11f, 0.09f), c), 0.26f, 2.4f, 0f));
            }
        }

        /// <summary>스타일 링 — 몸 주위로 퍼지는 원형 파동.</summary>
        void FxRing(float fx, float fy, float d, Color c, bool flat = false) =>
            StartCoroutine(FxFade(AtkFx(fx, fy, new Vector2(d, flat ? d * 0.35f : d), c), 0.28f, 1.9f, 0f));

        /// <summary>공격 스타일별 몸 움직임 — 스프라이트 프레임과 짝을 이루는 시그니처 모션.
        /// 모든 변위는 몸집(targetHeight)의 45% 이내라 다른 유닛 영역을 침범하지 않는다.
        /// 알 수 없는/빈 스타일은 기본 런지.</summary>
        IEnumerator StyleMotion()
        {
            float dir = IsOur ? 1f : -1f;
            float th = targetHeight;
            float fw = -dir; // 진행 방향으로 기울이는 회전 부호

            // 페이즈: dur 동안 f(0→1) 적용
            IEnumerator Phase(float dur, System.Action<float> f)
            {
                float t = 0f;
                while (t < dur)
                {
                    t += Time.deltaTime;
                    f(Mathf.Clamp01(t / dur));
                    yield return null;
                }
            }
            void Pose(float x, float y, float rot, float sx, float sy)
            {
                body.localPosition = new Vector3(x * th * dir, bodyBaseY + y * th, 0);
                body.localRotation = Quaternion.Euler(0, 0, rot * fw);
                body.localScale = new Vector3(bodyScale * sx, bodyScale * sy, 1f);
            }

            switch (data.atkStyle)
            {
                case "spin": // 제자리에서 좌우(세로축)로 한 바퀴 — 가로 폭을 cos 으로 눌러
                             // 앞→옆→뒤→옆→앞 으로 서서 도는 느낌 (공중제비 아님)
                    FxRing(0f, 0.05f, 1.1f, new Color(1f, 1f, 1f, 0.35f));
                    FxSlash(0.3f, 0.05f, 0f, 0.7f, new Color(1f, 1f, 0.9f, 0.7f), 320f);
                    FxSlash(-0.3f, 0.05f, 180f, 0.7f, new Color(1f, 1f, 0.9f, 0.7f), 320f);
                    yield return Phase(0.36f, k => Pose(0.08f * Mathf.Sin(k * Mathf.PI), 0, 0, Mathf.Cos(k * Mathf.PI * 2f), 1));
                    break;
                case "flurry": // 3연속 잽 — 잽마다 임팩트 팝
                    StartCoroutine(FlurryPops());
                    yield return Phase(0.42f, k => Pose(0.16f * Mathf.Abs(Mathf.Sin(k * Mathf.PI * 3f)), 0, 5f * Mathf.Sin(k * Mathf.PI * 6f), 1, 1));
                    break;
                case "bite": // 웅크렸다 콱 물기 — 위아래 이빨 궤적 + 팝
                    yield return Phase(0.1f, k => Pose(-0.05f * k, 0, -6f * k, 1f, 1f - 0.1f * k));
                    FxSlash(0.3f, 0.14f, -35f, 0.42f, new Color(1f, 1f, 1f, 0.85f), 140f);
                    FxSlash(0.3f, -0.08f, 35f, 0.42f, new Color(1f, 1f, 1f, 0.85f), -140f);
                    FxPop(0.32f, 0.03f, 0.24f, new Color(1f, 0.85f, 0.8f, 0.8f));
                    yield return Phase(0.09f, k => Pose(-0.05f + 0.27f * k, 0, -6f + 20f * k, 1f + 0.06f * k, 0.9f + 0.1f * k));
                    yield return Phase(0.14f, k => Pose(0.22f * (1f - k), 0, 14f * (1f - k), 1, 1));
                    break;
                case "peck": // 고개 콱 찍기 — 부리 끝 스파크
                    yield return Phase(0.08f, k => Pose(-0.04f * k, 0.03f * k, -12f * k, 1, 1));
                    FxPop(0.24f, -0.04f, 0.16f, new Color(1f, 0.92f, 0.45f, 0.9f));
                    yield return Phase(0.07f, k => Pose(-0.04f + 0.18f * k, 0.03f - 0.06f * k, -12f + 34f * k, 1, 1));
                    yield return Phase(0.12f, k => Pose(0.14f * (1f - k), -0.03f * (1f - k), 22f * (1f - k), 1, 1));
                    break;
                case "horn": // 숙였다 퍼올리는 박치기 — 위로 쓸리는 궤적 + 흙먼지
                    yield return Phase(0.12f, k => Pose(-0.08f * k, -0.04f * k, 10f * k, 1, 1));
                    FxSlash(0.26f, 0.02f, 60f, 0.6f, new Color(1f, 0.8f, 0.45f, 0.8f), -300f);
                    FxPuff(0.18f, -0.42f, new Color(0.75f, 0.68f, 0.55f, 0.7f));
                    yield return Phase(0.09f, k => Pose(-0.08f + 0.26f * k, -0.04f + 0.1f * k, 10f - 26f * k, 1, 1));
                    yield return Phase(0.14f, k => Pose(0.18f * (1f - k), 0.06f * (1f - k), -16f * (1f - k), 1, 1));
                    break;
                case "buck": // 앞으로 숙였다 뒤로 차기 — 뒤쪽 흙먼지
                    yield return Phase(0.1f, k => Pose(0.05f * k, 0, 14f * k, 1, 1));
                    FxPuff(-0.24f, -0.35f, new Color(0.72f, 0.62f, 0.48f, 0.8f), 4);
                    yield return Phase(0.08f, k => Pose(0.05f - 0.2f * k, 0.04f * k, 14f - 34f * k, 1, 1));
                    yield return Phase(0.14f, k => Pose(-0.15f * (1f - k), 0.04f * (1f - k), -20f * (1f - k), 1, 1));
                    break;
                case "trample": // 곧추섰다 내리누르기 — 납작 링 + 먼지
                    yield return Phase(0.13f, k => Pose(0, 0.2f * k, -4f * k, 0.96f, 1f + 0.08f * k));
                    yield return Phase(0.07f, k => Pose(0.04f * k, 0.2f * (1f - k), -4f + 8f * k, 0.96f + 0.1f * k, 1.08f - 0.24f * k));
                    FxRing(0.04f, -0.42f, 0.8f, new Color(0.8f, 0.73f, 0.6f, 0.6f), true);
                    FxPuff(0.04f, -0.4f, new Color(0.78f, 0.7f, 0.58f, 0.65f));
                    yield return Phase(0.13f, k => Pose(0.04f * (1f - k), 0, 4f * (1f - k), Mathf.Lerp(1.06f, 1f, k), Mathf.Lerp(0.84f, 1f, k)));
                    break;
                case "squash": // 눌렀다 튕기는 슬라임 — 양옆 점액 튐
                    yield return Phase(0.12f, k => Pose(0, -0.05f * k, 0, 1f + 0.16f * k, 1f - 0.22f * k));
                    FxPuff(0.2f, -0.38f, new Color(0.75f, 0.9f, 1f, 0.7f), 2);
                    FxPuff(-0.16f, -0.38f, new Color(0.75f, 0.9f, 1f, 0.7f), 2);
                    yield return Phase(0.09f, k => Pose(0.14f * k, -0.05f + 0.09f * k, 5f * k, 1.16f - 0.22f * k, 0.78f + 0.32f * k));
                    yield return Phase(0.13f, k => Pose(0.14f * (1f - k), 0.04f * (1f - k), 5f * (1f - k), Mathf.Lerp(0.94f, 1f, k), Mathf.Lerp(1.1f, 1f, k)));
                    break;
                case "flap": // 솟았다 앞으로 덮치기 — 아래로 쓸리는 돌풍 줄기
                    yield return Phase(0.14f, k => Pose(-0.03f * k, 0.24f * k, -8f * k, 1, 1));
                    FxSlash(0.16f, 0.1f, -60f, 0.55f, new Color(0.85f, 0.97f, 1f, 0.7f), 90f);
                    FxSlash(0.06f, 0.24f, -60f, 0.45f, new Color(0.85f, 0.97f, 1f, 0.5f), 90f);
                    yield return Phase(0.1f, k => Pose(-0.03f + 0.23f * k, 0.24f * (1f - k), -8f + 20f * k, 1, 1));
                    yield return Phase(0.13f, k => Pose(0.2f * (1f - k), 0, 12f * (1f - k), 1, 1));
                    break;
                case "cast": // 모았다가 방출 — 마법 파동 링 + 글로우 팝
                    yield return Phase(0.16f, k => Pose(0, 0.06f * k, -4f * k, 1f - 0.06f * k, 1f + 0.04f * k));
                    FxRing(0f, 0.1f, 0.95f, new Color(0.75f, 0.5f, 1f, 0.55f));
                    FxPop(0f, 0.16f, 0.4f, new Color(0.85f, 0.65f, 1f, 0.7f));
                    yield return Phase(0.09f, k => Pose(0, 0.06f * (1f - k), -4f + 6f * k, Mathf.Lerp(0.94f, 1.12f, k), Mathf.Lerp(1.04f, 1.08f, k)));
                    yield return Phase(0.13f, k => Pose(0, 0, 2f * (1f - k), Mathf.Lerp(1.12f, 1f, k), Mathf.Lerp(1.08f, 1f, k)));
                    break;
                case "swing": // 감았다 크게 휘두르기 — 참격 궤적
                    yield return Phase(0.12f, k => Pose(-0.06f * k, 0, -16f * k, 1, 1));
                    FxSlash(0.28f, 0.08f, -28f, 0.85f, new Color(1f, 1f, 0.88f, 0.85f), 300f);
                    yield return Phase(0.09f, k => Pose(-0.06f + 0.26f * k, 0, -16f + 42f * k, 1, 1));
                    yield return Phase(0.15f, k => Pose(0.2f * (1f - k), 0, 26f * (1f - k), 1, 1));
                    break;
                // ── 아군 전용 스타일 ──
                case "dash": // 잔상 돌진 베기 (닌자)
                    yield return Phase(0.08f, k => Pose(-0.06f * k, 0, -8f * k, 1, 1));
                    FxSlash(0.2f, 0.05f, -8f, 0.8f, new Color(0.7f, 0.95f, 1f, 0.8f), 60f);
                    FxSlash(0.05f, 0.02f, -8f, 0.6f, new Color(0.7f, 0.95f, 1f, 0.45f), 60f);
                    yield return Phase(0.07f, k => Pose(-0.06f + 0.46f * k, 0, -8f + 12f * k, 1, 1));
                    yield return Phase(0.12f, k => Pose(0.4f * (1f - k), 0, 4f * (1f - k), 1, 1));
                    break;
                case "combo": // 좌우 2연속 교차 베기 (검사)
                    yield return Phase(0.09f, k => Pose(-0.05f * k, 0, -14f * k, 1, 1));
                    FxSlash(0.26f, 0.1f, -30f, 0.7f, new Color(1f, 0.95f, 0.7f, 0.85f), 260f);
                    yield return Phase(0.08f, k => Pose(-0.05f + 0.24f * k, 0, -14f + 34f * k, 1, 1));
                    FxSlash(0.26f, 0f, 30f, 0.7f, new Color(1f, 0.95f, 0.7f, 0.85f), -260f);
                    yield return Phase(0.08f, k => Pose(0.19f - 0.06f * k, 0, 20f - 34f * k, 1, 1));
                    yield return Phase(0.12f, k => Pose(0.13f * (1f - k), 0, -14f * (1f - k), 1, 1));
                    break;
                case "uppercut": // 웅크렸다 위로 쳐올리기 (격투가)
                    yield return Phase(0.1f, k => Pose(-0.03f * k, -0.06f * k, 8f * k, 1, 1f - 0.08f * k));
                    FxSlash(0.2f, 0.15f, 80f, 0.6f, new Color(1f, 0.9f, 0.6f, 0.85f), -200f);
                    yield return Phase(0.09f, k => Pose(-0.03f + 0.15f * k, -0.06f + 0.24f * k, 8f - 26f * k, 1, 0.92f + 0.12f * k));
                    yield return Phase(0.13f, k => Pose(0.12f * (1f - k), 0.18f * (1f - k), -18f * (1f - k), 1, 1));
                    break;
                case "chi": // 장풍 손바닥 파동 (몽크)
                    yield return Phase(0.12f, k => Pose(-0.04f * k, 0, -6f * k, 1f - 0.05f * k, 1));
                    FxRing(0.3f, 0.05f, 0.7f, new Color(0.55f, 0.8f, 1f, 0.6f));
                    FxPop(0.3f, 0.05f, 0.3f, new Color(0.7f, 0.9f, 1f, 0.7f));
                    yield return Phase(0.08f, k => Pose(-0.04f + 0.22f * k, 0, -6f + 10f * k, Mathf.Lerp(0.95f, 1.04f, k), 1));
                    yield return Phase(0.12f, k => Pose(0.18f * (1f - k), 0, 4f * (1f - k), Mathf.Lerp(1.04f, 1f, k), 1));
                    break;
                case "rocket": // 피스톤 펀치 (로봇)
                    yield return Phase(0.1f, k => Pose(-0.08f * k, 0, 0, 1f + 0.04f * k, 1));
                    FxPop(0.38f, 0.04f, 0.34f, new Color(1f, 0.75f, 0.4f, 0.85f));
                    FxSlash(0.3f, 0.04f, 0f, 0.5f, new Color(1f, 0.85f, 0.5f, 0.6f), 0f);
                    yield return Phase(0.06f, k => Pose(-0.08f + 0.34f * k, 0, 0, 1.04f - 0.04f * k, 1));
                    yield return Phase(0.14f, k => Pose(0.26f * (1f - k), 0, 0, 1, 1));
                    break;
                case "smash": // 머리 위로 들었다 내려찍기 (곤봉/석상)
                    yield return Phase(0.14f, k => Pose(-0.05f * k, 0.08f * k, -22f * k, 1, 1));
                    FxPop(0.28f, -0.3f, 0.3f, new Color(1f, 0.9f, 0.7f, 0.8f));
                    FxRing(0.28f, -0.42f, 0.6f, new Color(0.8f, 0.72f, 0.58f, 0.6f), true);
                    yield return Phase(0.08f, k => Pose(-0.05f + 0.25f * k, 0.08f - 0.1f * k, -22f + 50f * k, 1, 1));
                    yield return Phase(0.14f, k => Pose(0.2f * (1f - k), -0.02f * (1f - k), 28f * (1f - k), 1, 1));
                    break;
                case "punch": // 원투 펀치 (마을사람)
                    yield return Phase(0.09f, k => Pose(0.12f * k, 0, 3f * k, 1, 1));
                    FxPop(0.24f, 0.04f, 0.15f, new Color(1f, 1f, 0.8f, 0.75f));
                    yield return Phase(0.09f, k => Pose(0.12f - 0.04f * k, 0, 3f - 5f * k, 1, 1));
                    FxPop(0.26f, -0.02f, 0.17f, new Color(1f, 1f, 0.8f, 0.85f));
                    yield return Phase(0.08f, k => Pose(0.08f + 0.08f * k, 0, -2f + 4f * k, 1, 1));
                    yield return Phase(0.1f, k => Pose(0.16f * (1f - k), 0, 2f * (1f - k), 1, 1));
                    break;
                case "bash": // 방패 밀치기 (방패병)
                    yield return Phase(0.1f, k => Pose(-0.07f * k, 0, -5f * k, 1, 1));
                    FxRing(0.3f, 0.02f, 0.55f, new Color(0.8f, 0.9f, 1f, 0.6f), true);
                    FxPop(0.3f, 0.02f, 0.24f, new Color(0.9f, 0.95f, 1f, 0.7f));
                    yield return Phase(0.07f, k => Pose(-0.07f + 0.3f * k, 0, -5f + 5f * k, 1.02f, 0.98f));
                    yield return Phase(0.13f, k => Pose(0.23f * (1f - k), 0, 0, 1, 1));
                    break;
                default: // 기본 런지 — 접촉 지점 소형 팝
                    FxPop(0.24f, 0.03f, 0.18f, new Color(1f, 1f, 1f, 0.55f));
                    yield return Lunge();
                    yield break;
            }
            Pose(0, 0, 0, 1, 1);

            IEnumerator FlurryPops()
            {
                for (int j = 0; j < 3; j++)
                {
                    yield return new WaitForSeconds(j == 0 ? 0.06f : 0.14f);
                    FxPop(0.28f, 0.02f + 0.04f * (j - 1), 0.17f, new Color(1f, 0.95f, 0.6f, 0.85f));
                }
            }
        }

        /// <summary>점프 덮치기 — 포물선으로 뛰어올라 적 위로 떨어지며 타격 (개·고양이·개구리류).</summary>
        IEnumerator PounceAttack(Unit target)
        {
            float dir = IsOur ? 1f : -1f;
            float dist = targetHeight * 0.45f;
            float rise = targetHeight * 0.4f;
            float dur = 0.28f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                var p = body.localPosition;
                p.x = dir * dist * k;
                p.y = bodyBaseY + rise * 4f * k * (1f - k); // 포물선 점프
                body.localPosition = p;
                yield return null;
            }
            FxPuff(0.38f, -0.42f, new Color(0.78f, 0.72f, 0.6f, 0.7f)); // 착지 흙먼지
            if (target != null && !target.Dead) target.TakeDamage(data.attack);
            // 복귀
            t = 0f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                var p = body.localPosition;
                p.x = dir * dist * (1f - Mathf.Clamp01(t / 0.12f));
                p.y = bodyBaseY;
                body.localPosition = p;
                yield return null;
            }
            var end = body.localPosition;
            end.x = 0f;
            end.y = bodyBaseY;
            body.localPosition = end;
        }

        /// <summary>들이받기 — 잠깐 움츠렸다가 빠르고 길게 돌진 (돼지·소·멧돼지·숫양류).</summary>
        IEnumerator RamAttack(Unit target)
        {
            float dir = IsOur ? 1f : -1f;
            float back = targetHeight * 0.12f;
            float dist = targetHeight * 0.6f;
            float t = 0f;
            while (t < 0.12f) // 움츠리기
            {
                t += Time.deltaTime;
                var p = body.localPosition;
                p.x = -dir * back * Mathf.Clamp01(t / 0.12f);
                body.localPosition = p;
                yield return null;
            }
            t = 0f;
            while (t < 0.08f) // 폭발적 돌진
            {
                t += Time.deltaTime;
                var p = body.localPosition;
                p.x = Mathf.Lerp(-dir * back, dir * dist, Mathf.Clamp01(t / 0.08f));
                body.localPosition = p;
                yield return null;
            }
            FxPop(0.5f, 0.02f, 0.26f, new Color(1f, 0.9f, 0.7f, 0.8f)); // 들이받는 순간 임팩트
            if (target != null && !target.Dead) target.TakeDamage(data.attack);
            t = 0f;
            while (t < 0.16f) // 복귀
            {
                t += Time.deltaTime;
                var p = body.localPosition;
                p.x = dir * dist * (1f - Mathf.Clamp01(t / 0.16f));
                body.localPosition = p;
                yield return null;
            }
            var end = body.localPosition;
            end.x = 0f;
            body.localPosition = end;
        }

        /// <summary>내려찍기 — 몸을 들어올렸다 쿵 찍기, 착지 흙먼지 (곰·판다·거인류).</summary>
        IEnumerator StompAttack(Unit target)
        {
            float rise = targetHeight * 0.28f;
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                var p = body.localPosition;
                p.y = bodyBaseY + rise * Mathf.Clamp01(t / 0.18f);
                body.localPosition = p;
                yield return null;
            }
            t = 0f;
            while (t < 0.07f)
            {
                t += Time.deltaTime;
                var p = body.localPosition;
                p.y = bodyBaseY + rise * (1f - Mathf.Clamp01(t / 0.07f));
                body.localPosition = p;
                yield return null;
            }
            var end = body.localPosition;
            end.y = bodyBaseY;
            body.localPosition = end;
            // 착지 흙먼지 링 (작게) + 데미지
            StartCoroutine(ShockRing(new Vector3(X + (IsOur ? 60f : -60f), GroundY + 22f, 0), targetHeight * 0.5f));
            if (target != null && !target.Dead) target.TakeDamage(data.attack);
        }

        /// <summary>보스 내리찍기 — 몸이 떠올랐다 쾅 떨어지며 충격파 링 + 범위 데미지.</summary>
        IEnumerator BossSlam(Unit target)
        {
            float centerX = target != null ? target.X : X + (IsOur ? 200f : -200f);
            float rise = targetHeight * 0.45f;
            float t = 0f;
            // 떠오르기 (0.22초)
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                var p = body.localPosition;
                p.y = bodyBaseY + rise * Mathf.Sin(Mathf.Clamp01(t / 0.22f) * Mathf.PI * 0.5f);
                body.localPosition = p;
                yield return null;
            }
            // 내리찍기 (0.09초)
            t = 0f;
            while (t < 0.09f)
            {
                t += Time.deltaTime;
                var p = body.localPosition;
                p.y = bodyBaseY + rise * (1f - Mathf.Clamp01(t / 0.09f));
                body.localPosition = p;
                yield return null;
            }
            var end = body.localPosition;
            end.y = bodyBaseY;
            body.localPosition = end;

            // 착지: 충격파 링 + 광역 데미지 (연출-피해 동기화)
            StartCoroutine(ShockRing(new Vector3(centerX, GroundY + 30f, 0), data.aoe));
            if (ctrl != null && !ctrl.BattleOver && !Dead)
                ctrl.DamageArea(this, centerX, data.aoe, Mathf.RoundToInt(data.attack * 1.5f));
        }

        /// <summary>바닥에서 퍼지는 충격파 링 — 범위공격 반경 시각화.</summary>
        IEnumerator ShockRing(Vector3 center, float radius)
        {
            var go = new GameObject("ShockRing");
            go.transform.position = center;
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite = SpriteBank.Circle;
            sr2.sortingOrder = 28;
            float dur = 0.32f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (go == null) yield break;
                float k = Mathf.Clamp01(t / dur);
                float d = radius * 2f * (0.25f + 0.75f * k) / 64f; // Circle 스프라이트 64px 기준
                go.transform.localScale = new Vector3(d, d * 0.38f, 1f); // 납작한 지면 링
                sr2.color = new Color(1f, 0.75f, 0.35f, 0.55f * (1f - k));
                yield return null;
            }
            Destroy(go);
        }

        /// <summary>공격 순간 몸을 적 쪽으로 내질렀다 돌아오는 연출 — 칼이 닿는 타격감.</summary>
        IEnumerator Lunge()
        {
            float dir = IsOur ? 1f : -1f;
            float baseX = 0f;
            float dist = targetHeight * 0.22f; // 몸집에 비례한 전진량
            float t = 0f;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                // 앞으로 확 나갔다가(0.08초) 천천히 복귀
                float k = t < 0.08f ? t / 0.08f : 1f - (t - 0.08f) / 0.14f;
                var p = body.localPosition;
                p.x = baseX + dir * dist * Mathf.Clamp01(k);
                body.localPosition = p;
                yield return null;
            }
            var end = body.localPosition;
            end.x = baseX;
            body.localPosition = end;
        }

        /// <summary>원거리 공격 발사체 — 종류별 모양/속도/궤적: arrow(직선 화살), bullet(빠른 탄환),
        /// rock(포물선 돌덩이), orb(마법 구슬+착탄 burst).</summary>
        IEnumerator FireProjectile(Unit target)
        {
            string kind = string.IsNullOrEmpty(data.projectile) ? "arrow" : data.projectile;

            var go = new GameObject("Projectile");
            var psr = go.AddComponent<SpriteRenderer>();
            psr.sortingOrder = 30;

            float dur = 0.16f;
            float arc = 0f; // 포물선 최고 높이
            switch (kind)
            {
                case "bullet":
                    psr.sprite = SpriteBank.Circle;
                    psr.color = new Color(1f, 0.9f, 0.4f);
                    go.transform.localScale = new Vector3(0.28f, 0.16f, 1f); // 길쭉한 탄환
                    dur = 0.07f;
                    break;
                case "rock":
                    psr.sprite = SpriteBank.Circle;
                    psr.color = new Color(0.55f, 0.52f, 0.48f);
                    go.transform.localScale = new Vector3(0.55f, 0.5f, 1f);
                    dur = 0.34f;
                    arc = 140f;
                    break;
                case "orb":
                    psr.sprite = SpriteBank.Circle;
                    psr.color = new Color(0.75f, 0.45f, 1f);
                    go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                    var glow = new GameObject("Glow");
                    glow.transform.SetParent(go.transform, false);
                    var gsr = glow.AddComponent<SpriteRenderer>();
                    gsr.sprite = SpriteBank.Circle;
                    gsr.color = new Color(0.6f, 0.3f, 1f, 0.4f);
                    gsr.sortingOrder = 29;
                    glow.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
                    dur = 0.24f;
                    arc = 60f;
                    break;
                default: // arrow
                    psr.sprite = SpriteBank.Arrow;
                    go.transform.localScale = new Vector3(3.2f, 3.2f, 1f);
                    break;
            }

            // 발사/명중 지점 모두 몸 중심 기준 (비행 고도 반영)
            Vector3 from = transform.position + new Vector3(0, bodyBaseY + targetHeight * 0.1f, 0);
            Vector3 to = target != null
                ? target.transform.position + new Vector3(0, target.bodyBaseY, 0)
                : from + new Vector3(IsOur ? 300f : -300f, 0, 0);

            // 화살은 진행 방향으로 회전
            var d = to - from;
            if (kind == "arrow" || kind == "bullet")
                go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (go == null) yield break;
                float k = Mathf.Clamp01(t / dur);
                var pos = Vector3.Lerp(from, to, k);
                pos.y += arc * 4f * k * (1f - k); // 포물선 궤적 (arc=0 이면 직선)
                go.transform.position = pos;
                if (kind == "rock") go.transform.Rotate(0, 0, 620f * Time.deltaTime); // 돌덩이 회전
                yield return null;
            }
            Destroy(go);

            // 발사체가 닿는 순간 데미지 (날아가는 동안 죽었으면 무효)
            if (target != null && !target.Dead)
            {
                if (kind == "orb") SpawnBurst(to); // 마법구: 착탄 이펙트
                if (data.aoe > 0 && ctrl != null && !ctrl.BattleOver)
                {
                    // 마법사 포지션: 착탄 지점 스플래시 — 반경 내 광역 데미지 (범위 공격)
                    StartCoroutine(ShockRing(new Vector3(to.x, GroundY + 20f, 0), data.aoe));
                    ctrl.DamageArea(this, to.x, data.aoe, data.attack);
                }
                else target.TakeDamage(data.attack);
            }
        }

        /// <summary>마법사 화염 강타 — 하늘에서 불덩이가 내리꽂히고 착탄 순간 광역 데미지.</summary>
        IEnumerator MeteorStrike(float targetX)
        {
            var go = new GameObject("Meteor");
            var psr = go.AddComponent<SpriteRenderer>();
            psr.sprite = SpriteBank.Circle;
            psr.color = new Color(1f, 0.55f, 0.15f);
            psr.sortingOrder = 30;
            go.transform.localScale = new Vector3(1.5f, 2.4f, 1f); // 세로로 긴 불덩이

            var glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            var gsr = glow.AddComponent<SpriteRenderer>();
            gsr.sprite = SpriteBank.Circle;
            gsr.color = new Color(1f, 0.35f, 0.08f, 0.4f);
            gsr.sortingOrder = 29;
            glow.transform.localScale = new Vector3(1.5f, 1.4f, 1f);

            Vector3 from = new Vector3(targetX + 70f, GroundY + 780f, 0);
            Vector3 to = new Vector3(targetX, GroundY + 40f, 0);
            var dir = to - from;
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f);

            float dur = 0.3f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (go == null) yield break;
                float k = t / dur;
                go.transform.position = Vector3.Lerp(from, to, k * k); // 가속 낙하
                yield return null;
            }
            Destroy(go);

            // 착탄 순간: 폭발 + 링 + 광역 데미지 (연출-피해 동기화)
            SpawnBurst(to);
            if (ctrl != null && !ctrl.BattleOver && !Dead)
                ctrl.DamageArea(this, targetX, 60f, data.attack);
        }

        static Sprite[] burstFrames;

        /// <summary>착탄 지점에 빛 폭발 애니메이션 (FX 팩) 1회 재생.</summary>
        void SpawnBurst(Vector3 pos)
        {
            if (burstFrames == null)
            {
                var list = new System.Collections.Generic.List<Sprite>();
                for (int i = 0; i < 8; i++)
                {
                    var s = Resources.Load<Sprite>("Sprites/fx/magicburst_" + i);
                    if (s == null) break;
                    list.Add(s);
                }
                burstFrames = list.ToArray();
            }
            if (burstFrames.Length == 0) return;

            var go = new GameObject("MagicBurst");
            go.transform.position = pos;
            var bsr = go.AddComponent<SpriteRenderer>();
            bsr.sortingOrder = 31;
            bsr.color = new Color(1f, 0.6f, 0.2f); // 화염색
            // 광역(반경 60) 임팩트가 느껴지도록 폭발 크게
            float scale = 240f / Mathf.Max(burstFrames[0].bounds.size.y, 1f);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            // 바닥 충격 링 — 범위를 시각적으로 표시
            var ring = new GameObject("ImpactRing");
            ring.transform.position = new Vector3(pos.x, GroundY + 14f, 0);
            var rsr = ring.AddComponent<SpriteRenderer>();
            rsr.sprite = SpriteBank.Circle;
            rsr.color = new Color(1f, 0.55f, 0.15f, 0.45f);
            rsr.sortingOrder = 8;
            ring.transform.localScale = new Vector3(120f / 64f, 34f / 64f, 1f);
            StartCoroutine(ExpandFade(ring, rsr));

            var banim = go.AddComponent<SimpleSpriteAnimator>();
            banim.fps = 18f;
            banim.Play(burstFrames, false, () => Destroy(go));
            Destroy(go, 1f); // 안전장치
        }

        /// <summary>규칙 2: 전진 — 지상: 절제된 밥(bob)+기울임 / 비행: 부드러운 부유.</summary>
        void MoveForward()
        {
            SetAction("move");
            if (data.fly > 0f)
            {
                // 비행: 천천히 위아래로 떠다니며 활공
                float ft = Time.time * 2.6f + walkPhase;
                body.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(ft * 0.7f) * 3f);
                body.localScale = new Vector3(bodyScale, bodyScale, 1f);
                body.localPosition = new Vector3(body.localPosition.x,
                    bodyBaseY + Mathf.Sin(ft) * 14f, 0);
            }
            else
            {
                // 지상 유닛은 발이 땅에 붙은 채로 걷는다 — 절차적 밥/스쿼시 없이 프레임에 맡기고,
                // 프레임 높이가 달라도(슬라임 꿀렁 등) 발바닥이 항상 지면에 닿도록 프레임별 정렬
                body.localRotation = Quaternion.identity;
                body.localScale = new Vector3(bodyScale, bodyScale, 1f);
                float half = (sr.sprite != null ? sr.sprite.bounds.size.y : targetHeight / bodyScale) * bodyScale * 0.5f;
                body.localPosition = new Vector3(body.localPosition.x, half - data.sink, 0);
            }

            float dir = IsOur ? 1f : -1f;
            float nx = X + dir * data.moveSpeed * Time.deltaTime;
            nx = Mathf.Clamp(nx, BattlefieldController.OurBaseX, BattlefieldController.WorldWidth);
            transform.position = new Vector3(nx, GroundY, 0f);
        }

        /// <summary>규칙 3: 체력 0 → 사망. 죽음 처리(승패 판정 포함)는 컨트롤러에 위임.</summary>
        public void TakeDamage(int damage)
        {
            if (Dead) return;
            // [DEV] 테스트 모드: 아군 성 무적 — 적군 관찰 중 패배 방지 (출시 전 Dev.InvincibleOurCastle=false)
            if (Core.Dev.InvincibleOurCastle && data.IsCastle && data.IsOur) return;
            hp -= damage;
            UpdateHpBar();
            StartCoroutine(HitFlash());

            if (hp <= 0)
            {
                Dead = true;
                ctrl.OnUnitDead(this);
            }
        }

        // ─────────────────────── 생성/연출 (전투 규칙 아님) ───────────────────────

        public void Init(BattlefieldController controller, MonsterData monsterData, float startX)
        {
            ctrl = controller;
            data = monsterData;
            hp = data.hp;

            // Body: 스프라이트 + 애니메이션
            var bodyGo = new GameObject("Body");
            body = bodyGo.transform;
            body.SetParent(transform, false);
            sr = bodyGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = IsCastle ? -11 : 10; // 성은 지면 잔디(-10)가 밑동을 덮도록
            anim = bodyGo.AddComponent<SimpleSpriteAnimator>();

            // 진행 방향 바라보기: 아군→오른쪽, 적군→왼쪽 (원본이 보는 방향 기준으로 반전)
            bool sourceFacesLeft = data.facing == "left";
            if (!IsCastle) sr.flipX = sourceFacesLeft ? IsOur : !IsOur;

            // 틴트 변형 (같은 스프라이트에 색만 입힌 물/얼음/수정 몬스터)
            if (!string.IsNullOrEmpty(data.tint) && ColorUtility.TryParseHtmlString(data.tint, out var tc))
                baseColor = tc;
            sr.color = baseColor;

            SetAction("move");

            // 목표 높이에 맞춰 Body 만 스케일 (루트/체력바는 월드 크기 유지)
            targetHeight = data.height > 0 ? data.height : (IsCastle ? 350f : 180f);
            float h = sr.sprite != null ? sr.sprite.bounds.size.y : 128f;
            bodyScale = targetHeight / Mathf.Max(h, 0.01f);
            body.localScale = new Vector3(bodyScale, bodyScale, 1f);
            // 발밑 = 루트(y0). 성은 잔디에 살짝 파묻힘, 비행 유닛은 공중에 뜸,
            // 스프라이트에 내장 그림자/여백이 있는 유닛은 sink 만큼 내려앉힘
            bodyBaseY = targetHeight * 0.5f + data.fly - data.sink - (IsCastle ? 14f : 0f);
            body.localPosition = new Vector3(0, bodyBaseY, 0);

            // 성은 원래 위치(자체 -14 보정만), 일반 유닛만 지면선(-30)으로
            transform.position = new Vector3(startX, IsCastle ? 0f : GroundY, 0f);
            walkPhase = startX * 0.037f; // 유닛마다 걸음 위상 다르게

            // 발밑 그림자 — 캐릭터가 지면에 붙어 보이게 (비행 유닛은 작고 옅은 그림자)
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(transform, false);
            var shSr = shadow.AddComponent<SpriteRenderer>();
            shSr.sprite = SpriteBank.Circle;
            bool flying = data.fly > 0f;
            shSr.color = new Color(0, 0, 0, flying ? 0.12f : 0.22f);
            shSr.sortingOrder = IsCastle ? -12 : 6; // 몸보다 뒤, (성은 지면보다도 뒤)
            shadow.transform.localPosition = new Vector3(0, 6f, 0);
            float shScale = flying ? 0.32f : 0.5f;
            shadow.transform.localScale = new Vector3(targetHeight * shScale / 64f, targetHeight * shScale * 0.26f / 64f, 1f);

            // 체력바: 테두리 + 배경 + 채움 3겹, 풀피면 숨김 (비행 유닛은 고도만큼 위로)
            barWidth = IsCastle ? 170f : 96f;
            float barY = targetHeight + data.fly + 20f;
            float barH = IsCastle ? 16f : 10f;

            hpBarRoot = new GameObject("HpBar").transform;
            hpBarRoot.SetParent(transform, false);
            hpBarRoot.localPosition = new Vector3(0, barY, 0);

            var border = NewRoundedBar(hpBarRoot, "Border", new Color(0.08f, 0.07f, 0.06f, 0.75f), 48,
                barWidth + 8f, barH + 8f);
            var back = NewRoundedBar(hpBarRoot, "Back", new Color(0.4f, 0.14f, 0.12f, 0.9f), 49,
                barWidth, barH);
            var fill = NewRoundedBar(hpBarRoot, "Fill", new Color(0.3f, 0.9f, 0.35f), 50,
                barWidth, barH);
            hpFill = fill.transform;
            hpFillSr = fill.GetComponent<SpriteRenderer>();
            barHeight = barH;
            displayedRatio = 1f;
            UpdateHpBar();
            hpBarRoot.gameObject.SetActive(false); // 풀피 = 숨김 (성 포함)
        }

        GameObject NewRoundedBar(Transform parent, string name, Color color, int order, float w, float h)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var barSr = go.AddComponent<SpriteRenderer>();
            barSr.sprite = SpriteBank.Rounded;
            barSr.drawMode = SpriteDrawMode.Sliced;
            barSr.size = new Vector2(w, h);
            barSr.color = color;
            barSr.sortingOrder = order;
            return go;
        }

        void UpdateHpBar()
        {
            if (hpFill == null) return;
            float ratio = Mathf.Clamp01((float)hp / data.hp);

            if (hpBarRoot != null)
                hpBarRoot.gameObject.SetActive(ratio < 1f); // 다치면 표시

            SetBarFill(displayedRatio);
        }

        /// <summary>표시 비율로 채움 막대 갱신 — 왼쪽 고정, 색은 체력에 따라 초록→노랑→빨강.</summary>
        void SetBarFill(float ratio)
        {
            float w = Mathf.Max(barWidth * Mathf.Clamp01(ratio), barHeight); // 최소 = 둥근 캡 크기
            hpFillSr.size = new Vector2(w, barHeight);
            hpFill.localPosition = new Vector3(-barWidth * 0.5f + w * 0.5f, 0, 0);

            hpFillSr.color = ratio > 0.5f
                ? Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(0.3f, 0.9f, 0.35f), (ratio - 0.5f) * 2f)
                : Color.Lerp(new Color(0.95f, 0.25f, 0.2f), new Color(1f, 0.85f, 0.2f), ratio * 2f);
        }

        /// <summary>죽는 애니메이션 1회 재생 후 오브젝트 제거. 영혼이 위로 빠져나가는 연출 동반.</summary>
        public void PlayDeathAndDestroy()
        {
            body.localRotation = Quaternion.identity;
            foreach (Transform child in transform)
                if (child != body) child.gameObject.SetActive(false); // 체력바 숨김
            SpawnSoul();
            anim.Play(SpriteBank.GetFrames(data.SpriteName, "defeat"), false, () => Destroy(gameObject));
            Destroy(gameObject, 1.5f); // 안전장치
        }

        /// <summary>같은 모습의 반투명 "영혼"이 몸에서 떠올라 사라지는 연출 —
        /// 사망 포즈가 유닛마다 달라도 죽음이 직관적으로 읽힌다.</summary>
        void SpawnSoul()
        {
            var mv = SpriteBank.GetFrames(data.SpriteName, "move");
            if (mv.Length == 0) return;
            var go = new GameObject("Soul");
            go.transform.position = body.position;
            var ssr = go.AddComponent<SpriteRenderer>();
            ssr.sprite = mv[0];
            ssr.flipX = sr.flipX;
            ssr.sortingOrder = 32;
            ssr.color = new Color(0.85f, 0.95f, 1f, 0.55f); // 푸르스름한 반투명
            float s = bodyScale * 0.55f;
            go.transform.localScale = new Vector3(s, s, 1f);
            go.AddComponent<SoulRise>(); // 유닛이 파괴돼도 혼자 떠올라 사라진다
        }

        /// <summary>충격 링이 퍼지며 사라지는 연출.</summary>
        IEnumerator ExpandFade(GameObject go, SpriteRenderer sr2)
        {
            float t = 0f;
            var baseScale = go.transform.localScale;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                if (go == null) yield break;
                float k = t / 0.35f;
                go.transform.localScale = baseScale * (1f + k * 0.9f);
                var c = sr2.color;
                c.a = 0.4f * (1f - k);
                sr2.color = c;
                yield return null;
            }
            Destroy(go);
        }

        void SetAction(string action)
        {
            if (currentAction == action) return;
            currentAction = action;
            if (action == "idle")
            {
                var f = SpriteBank.GetFrames(data.SpriteName, "move");
                anim.Play(new[] { f[0] }, true); // 대기 = 정지 자세
            }
            else
            {
                anim.Play(SpriteBank.GetFrames(data.SpriteName, action), action != "defeat");
            }
        }

        IEnumerator HitFlash()
        {
            sr.color = new Color(1f, 0.45f, 0.45f);
            yield return new WaitForSeconds(0.12f);
            if (sr != null) sr.color = baseColor;
        }
    }

    /// <summary>사망 영혼 연출 — 좌우로 하늘거리며 떠올라 서서히 사라진다. 스스로 소멸.</summary>
    public class SoulRise : MonoBehaviour
    {
        const float Life = 0.9f;
        float t;
        SpriteRenderer sr;

        void Awake() { sr = GetComponent<SpriteRenderer>(); }

        void Update()
        {
            t += Time.deltaTime;
            float k = t / Life;
            transform.position += new Vector3(Mathf.Sin(t * 7f) * 16f * Time.deltaTime, 170f * Time.deltaTime, 0);
            if (sr != null) sr.color = new Color(0.85f, 0.95f, 1f, 0.55f * (1f - k));
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
