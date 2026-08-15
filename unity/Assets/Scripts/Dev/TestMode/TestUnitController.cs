using UnityEngine;

[RequireComponent(typeof(Animator))]
public class TestUnitController : MonoBehaviour, ITestableUnit
{
    [Header("Identification")]
    public string unitName;

    [Header("Movement")]
    public float baseMoveSpeed = 2f;

    Animator animator;
    float currentMultiplier = 1f;
    Coroutine moveLoop;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        if (TestModeManager.Instance != null)
            TestModeManager.Instance.OnSpeedMultiplierChanged += HandleMultiplierChanged;

        // 적용 초기화
        currentMultiplier = (TestModeManager.Instance != null && TestModeManager.Instance.IsTestMode)
            ? TestModeManager.Instance.SpeedMultiplier
            : 1f;
        ApplyAnimatorSpeed();
    }

    void OnDisable()
    {
        if (TestModeManager.Instance != null)
            TestModeManager.Instance.OnSpeedMultiplierChanged -= HandleMultiplierChanged;
        if (moveLoop != null) StopCoroutine(moveLoop);
    }

    void HandleMultiplierChanged(float m)
    {
        currentMultiplier = m;
        ApplyAnimatorSpeed();
    }

    void ApplyAnimatorSpeed()
    {
        // Animator 전체 속도를 배율로 곱함. (애니메이션 재생 속도 빨라짐)
        if (animator != null)
            animator.speed = currentMultiplier;
    }

    // --- ITestableUnit 구현 ---
    public string GetUnitName() => string.IsNullOrEmpty(unitName) ? gameObject.name : unitName;
    public Transform GetTransform() => transform;

    public void PlayWalk()
    {
        // Animator에서 "Walk" 트리거(또는 bool) 사용하도록 가정
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Die");
        animator.SetTrigger("Walk");
        // 이동을 시각적으로 보여주고 싶으면 코루틴으로 위치 이동 시키기(테스트 전용)
        if (TestModeManager.Instance != null && TestModeManager.Instance.IsTestMode)
        {
            if (moveLoop != null) StopCoroutine(moveLoop);
            moveLoop = StartCoroutine(TestMoveLoop());
        }
    }

    System.Collections.IEnumerator TestMoveLoop()
    {
        float moveSpeed = baseMoveSpeed * Mathf.Max(1f, currentMultiplier);
        float t = 0f;
        Vector3 dir = transform.forward;
        while (TestModeManager.Instance != null && TestModeManager.Instance.IsTestMode)
        {
            transform.position += dir * moveSpeed * Time.deltaTime;
            t += Time.deltaTime;
            // 간단히 일정 시간 후 반대 방향으로 바꿈 — 한눈에 움직임 확인용
            if (t > 1.5f)
            {
                dir = -dir;
                t = 0f;
            }
            yield return null;
        }
    }

    public void PlayAttack()
    {
        if (moveLoop != null) { StopCoroutine(moveLoop); moveLoop = null; }
        animator.ResetTrigger("Walk");
        animator.ResetTrigger("Die");
        animator.SetTrigger("Attack");
    }

    public void PlayDie()
    {
        if (moveLoop != null) { StopCoroutine(moveLoop); moveLoop = null; }
        animator.ResetTrigger("Walk");
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Die");
    }

    public void SetTestSpeedMultiplier(float multiplier)
    {
        currentMultiplier = multiplier;
        ApplyAnimatorSpeed();
    }

    // === 통합 팁 ===
    // 실제 기존 Movement 스크립트가 있다면 이동 속도 계산시 baseMoveSpeed * (TestModeManager.Instance?.SpeedMultiplier ?? 1f)
    // 또는 TestUnitController.SetTestSpeedMultiplier를 호출해서 속도를 동기화하도록 하자.
}
