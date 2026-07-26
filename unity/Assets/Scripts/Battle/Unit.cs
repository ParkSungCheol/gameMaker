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

            Unit target = ctrl.FindNearestEnemy(this);
            bool inRange = target != null && Mathf.Abs(target.X - X) <= data.range;

            if (inRange)
            {
                if (!wasInRange) attackTimer = data.attackInterval; // 진입 즉시 첫 타
                AttackTick(target);
            }
            else if (!IsCastle)
            {
                MoveForward();
            }
            wasInRange = inRange;
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
            anim.Play(SpriteBank.GetFrames(data.name, "attack"), false,
                () => { striking = false; currentAction = ""; });
            if (!IsCastle)
            {
                // 원거리 유닛은 발사체 연출, 근접 유닛은 몸 내지르기
                if (data.range >= 200) StartCoroutine(FireProjectile(target));
                else StartCoroutine(Lunge());
            }
            target.TakeDamage(data.attack); // 휘두르는 순간 데미지
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

        /// <summary>원거리 공격 발사체 — 마법사는 구슬, 나머지는 화살이 목표까지 날아간다.</summary>
        IEnumerator FireProjectile(Unit target)
        {
            bool magic = data.name == "ourmass";
            var go = new GameObject("Projectile");
            var psr = go.AddComponent<SpriteRenderer>();
            psr.sortingOrder = 30;

            if (magic)
            {
                psr.sprite = SpriteBank.Rounded;
                psr.color = new Color(0.8f, 0.45f, 1f);
                go.transform.localScale = new Vector3(1.6f, 1.6f, 1f);

                // 은은한 겉광(글로우) 한 겹
                var glow = new GameObject("Glow");
                glow.transform.SetParent(go.transform, false);
                var gsr = glow.AddComponent<SpriteRenderer>();
                gsr.sprite = SpriteBank.Rounded;
                gsr.color = new Color(0.8f, 0.5f, 1f, 0.35f);
                gsr.sortingOrder = 29;
                glow.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
            }
            else
            {
                psr.sprite = SpriteBank.Arrow;
                go.transform.localScale = new Vector3(3.2f, 3.2f, 1f);
            }

            Vector3 from = transform.position + new Vector3(0, targetHeight * 0.62f, 0);
            Vector3 to = target != null
                ? target.transform.position + new Vector3(0, target.targetHeight * 0.5f, 0)
                : from + new Vector3(IsOur ? 300f : -300f, 0, 0);

            // 화살은 진행 방향으로 회전
            var d = to - from;
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);

            float dur = magic ? 0.22f : 0.16f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (go == null) yield break;
                go.transform.position = Vector3.Lerp(from, to, t / dur);
                if (magic) // 마법구슬은 맥동
                {
                    float p = 1.6f + 0.35f * Mathf.Sin(t * 40f);
                    go.transform.localScale = new Vector3(p, p, 1f);
                }
                yield return null;
            }
            Destroy(go);

            // 마법 착탄 폭발 이펙트
            if (magic) SpawnBurst(to);
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
            bsr.color = new Color(0.85f, 0.55f, 1f);
            float scale = 160f / Mathf.Max(burstFrames[0].bounds.size.y, 1f);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var banim = go.AddComponent<SimpleSpriteAnimator>();
            banim.fps = 18f;
            banim.Play(burstFrames, false, () => Destroy(go));
            Destroy(go, 1f); // 안전장치
        }

        /// <summary>규칙 2: 전진 — 절제된 상하 밥(bob) + 미세한 기울임 (세련된 걸음).</summary>
        void MoveForward()
        {
            SetAction("move");
            float t = Time.time * 9f + walkPhase;
            body.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t) * 1.6f);
            float squash = 1f + 0.022f * Mathf.Sin(t * 2f);
            body.localScale = new Vector3(bodyScale, bodyScale * squash, 1f);
            body.localPosition = new Vector3(body.localPosition.x,
                bodyBaseY + Mathf.Abs(Mathf.Sin(t)) * targetHeight * 0.025f, 0);

            float dir = IsOur ? 1f : -1f;
            float nx = X + dir * data.moveSpeed * Time.deltaTime;
            nx = Mathf.Clamp(nx, BattlefieldController.OurBaseX, BattlefieldController.WorldWidth);
            transform.position = new Vector3(nx, 0f, 0f);
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

            SetAction("move");

            // 목표 높이에 맞춰 Body 만 스케일 (루트/체력바는 월드 크기 유지)
            targetHeight = data.height > 0 ? data.height : (IsCastle ? 350f : 180f);
            float h = sr.sprite != null ? sr.sprite.bounds.size.y : 128f;
            bodyScale = targetHeight / Mathf.Max(h, 0.01f);
            body.localScale = new Vector3(bodyScale, bodyScale, 1f);
            // 발밑 = 루트(y0). 성은 잔디에 살짝 파묻힘
            bodyBaseY = targetHeight * 0.5f - (IsCastle ? 14f : 0f);
            body.localPosition = new Vector3(0, bodyBaseY, 0);

            transform.position = new Vector3(startX, 0f, 0f);
            walkPhase = startX * 0.037f; // 유닛마다 걸음 위상 다르게

            // 발밑 그림자 — 캐릭터가 지면에 붙어 보이게
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(transform, false);
            var shSr = shadow.AddComponent<SpriteRenderer>();
            shSr.sprite = SpriteBank.Circle;
            shSr.color = new Color(0, 0, 0, 0.22f);
            shSr.sortingOrder = IsCastle ? -12 : 6; // 몸보다 뒤, (성은 지면보다도 뒤)
            shadow.transform.localPosition = new Vector3(0, 6f, 0);
            shadow.transform.localScale = new Vector3(targetHeight * 0.5f / 64f, targetHeight * 0.13f / 64f, 1f);

            // 체력바: 테두리 + 배경 + 채움 3겹, 풀피면 숨김 (성은 항상 표시)
            barWidth = IsCastle ? 170f : 96f;
            float barY = targetHeight + 20f;
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
            anim.Play(SpriteBank.GetFrames(data.name, "defeat"), false, () => Destroy(gameObject));
            Destroy(gameObject, 1.5f); // 안전장치
        }

        void SetAction(string action)
        {
            if (currentAction == action) return;
            currentAction = action;
            if (action == "idle")
            {
                var f = SpriteBank.GetFrames(data.name, "move");
                anim.Play(new[] { f[0] }, true); // 대기 = 정지 자세
            }
            else
            {
                anim.Play(SpriteBank.GetFrames(data.name, action), action != "defeat");
            }
        }

        IEnumerator HitFlash()
        {
            sr.color = new Color(1f, 0.45f, 0.45f);
            yield return new WaitForSeconds(0.12f);
            if (sr != null) sr.color = Color.white;
        }
    }
}
