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

        void Strike(Unit target)
        {
            striking = true;
            currentAction = "attack";
            anim.Play(SpriteBank.GetFrames(data.SpriteName, "attack"), false,
                () => { striking = false; currentAction = ""; });
            if (data.name == "ourmass")
            {
                // 마법사: 하늘에서 화염이 내리꽂히고, "떨어진 순간" 광역 데미지
                StartCoroutine(MeteorStrike(target.X));
            }
            else if (!IsCastle && data.range >= 200)
            {
                // 원거리: 화살이 "닿는 순간" 데미지 (FireProjectile 안에서 처리)
                StartCoroutine(FireProjectile(target));
            }
            else
            {
                if (!IsCastle) StartCoroutine(Lunge());
                target.TakeDamage(data.attack); // 근접: 휘두르는 순간 데미지
            }
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

        /// <summary>원거리 공격 발사체 — 화살이 목표까지 날아간다.</summary>
        IEnumerator FireProjectile(Unit target)
        {
            var go = new GameObject("Projectile");
            var psr = go.AddComponent<SpriteRenderer>();
            psr.sortingOrder = 30;
            psr.sprite = SpriteBank.Arrow;
            go.transform.localScale = new Vector3(3.2f, 3.2f, 1f);

            // 발사/명중 지점 모두 몸 중심 기준 (비행 고도 반영)
            Vector3 from = transform.position + new Vector3(0, bodyBaseY + targetHeight * 0.1f, 0);
            Vector3 to = target != null
                ? target.transform.position + new Vector3(0, target.bodyBaseY, 0)
                : from + new Vector3(IsOur ? 300f : -300f, 0, 0);

            // 화살은 진행 방향으로 회전
            var d = to - from;
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);

            float dur = 0.16f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (go == null) yield break;
                go.transform.position = Vector3.Lerp(from, to, t / dur);
                yield return null;
            }
            Destroy(go);

            // 화살이 닿는 순간 데미지 (날아가는 동안 죽었으면 무효)
            if (target != null && !target.Dead)
                target.TakeDamage(data.attack);
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
                float t = Time.time * 9f + walkPhase;
                body.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t) * 1.6f);
                float squash = 1f + 0.022f * Mathf.Sin(t * 2f);
                body.localScale = new Vector3(bodyScale, bodyScale * squash, 1f);
                body.localPosition = new Vector3(body.localPosition.x,
                    bodyBaseY + Mathf.Abs(Mathf.Sin(t)) * targetHeight * 0.025f, 0);
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

        /// <summary>죽는 애니메이션 1회 재생 후 오브젝트 제거.</summary>
        public void PlayDeathAndDestroy()
        {
            body.localRotation = Quaternion.identity;
            foreach (Transform child in transform)
                if (child != body) child.gameObject.SetActive(false); // 체력바 숨김
            anim.Play(SpriteBank.GetFrames(data.SpriteName, "defeat"), false, () => Destroy(gameObject));
            Destroy(gameObject, 1.5f); // 안전장치
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
}
