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
        float barWidth;
        float targetHeight;
        float attackTimer;
        bool striking;              // 공격 모션 재생 중
        bool wasInRange;
        string currentAction = "";

        // ─────────────────────── 핵심 로직 ───────────────────────

        void Update()
        {
            if (Dead || ctrl == null || ctrl.BattleOver) return;

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
            target.TakeDamage(data.attack); // 휘두르는 순간 데미지
        }

        /// <summary>규칙 2: 전진. 흔들림은 이미지 회전이 아니라 Body 회전으로 (픽셀 선명 유지).</summary>
        void MoveForward()
        {
            SetAction("move");
            body.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 12f) * 6f);

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
            sr.sortingOrder = IsCastle ? 0 : 10;
            anim = bodyGo.AddComponent<SimpleSpriteAnimator>();

            // 스프라이트 원본이 오른쪽을 보므로 적군은 왼쪽으로 반전 (성은 제외)
            if (!IsOur && !IsCastle) sr.flipX = true;

            SetAction("move");

            // 목표 높이에 맞춰 Body 만 스케일 (루트/체력바는 월드 크기 유지)
            targetHeight = data.height > 0 ? data.height : (IsCastle ? 350f : 180f);
            float h = sr.sprite != null ? sr.sprite.bounds.size.y : 128f;
            float s = targetHeight / Mathf.Max(h, 0.01f);
            body.localScale = new Vector3(s, s, 1f);
            body.localPosition = new Vector3(0, targetHeight * 0.5f, 0); // 발밑 = 루트(y0)

            transform.position = new Vector3(startX, 0f, 0f);

            // 체력바 (배경 + 채움)
            barWidth = IsCastle ? 170f : 110f;
            float barY = targetHeight + 22f;

            var bg = NewBarSprite("HpBarBg", new Color(0.08f, 0.08f, 0.08f, 0.85f), 49);
            bg.transform.localPosition = new Vector3(0, barY, 0);
            bg.transform.localScale = new Vector3(barWidth + 8f, 22f, 1f);

            var fill = NewBarSprite("HpBarFill",
                IsOur ? new Color(0.25f, 0.9f, 0.3f) : new Color(0.95f, 0.3f, 0.25f), 50);
            hpFill = fill.transform;
            UpdateHpBar();
        }

        GameObject NewBarSprite(string name, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var barSr = go.AddComponent<SpriteRenderer>();
            barSr.sprite = SpriteBank.White;
            barSr.color = color;
            barSr.sortingOrder = order;
            return go;
        }

        void UpdateHpBar()
        {
            if (hpFill == null) return;
            float ratio = Mathf.Clamp01((float)hp / data.hp);
            float w = barWidth * ratio;
            hpFill.localScale = new Vector3(w, 16f, 1f);
            // 왼쪽 기준으로 줄어들도록 위치 보정
            hpFill.localPosition = new Vector3(-barWidth * 0.5f + w * 0.5f, targetHeight + 22f, 0);
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
