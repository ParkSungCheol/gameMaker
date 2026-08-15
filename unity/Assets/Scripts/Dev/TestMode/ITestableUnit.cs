using UnityEngine;

public interface ITestableUnit
{
    string GetUnitName();            // UI에 표시할 이름
    Transform GetTransform();        // 리스트에서 카메라 포커스 등용
    void PlayWalk();                 // 걷기 애니메이션 / 모션
    void PlayAttack();               // 공격 애니메이션 / 모션
    void PlayDie();                  // 죽는 애니메이션 / 모션
    void SetTestSpeedMultiplier(float multiplier); // 테스트 모드 배율 적용 (이동/애니메이터 속도 등)
}
