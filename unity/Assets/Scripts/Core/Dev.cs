using UnityEngine;

namespace GameMaker.Core
{
    /// <summary>
    /// 개발/테스트 플래그 모음.
    /// 마스터 스위치(Enabled)가 에디터/개발 빌드에서만 켜지므로,
    /// 릴리즈 빌드에서는 아래 플래그가 전부 자동으로 꺼진다 — 출시 전에 손댈 필요 없음.
    /// 개발 중 개별 기능을 끄고 싶으면 아래 const 값만 false 로.
    /// </summary>
    public static class Dev
    {
        /// <summary>마스터 스위치 — 에디터 또는 Development Build 에서만 true.</summary>
        public static bool Enabled => Application.isEditor || Debug.isDebugBuild;

        // ── 개별 토글 (개발 중 조절) ──
        const bool unlockAllStages = true;     // 맵 전체 잠금 해제
        const bool invincibleOurCastle = true; // 아군 성 무적 (적군 관찰용)
        const bool testSpeeds = true;          // 전투 배속 x10/x20/x30 추가
        const bool unitViewer = true;          // 메인 메뉴 "유닛 뷰어" 버튼
        const bool freeGacha = true;           // 뽑기 무료 (무한 뽑기 테스트)

        public static bool UnlockAllStages => Enabled && unlockAllStages;
        public static bool InvincibleOurCastle => Enabled && invincibleOurCastle;
        public static bool TestSpeeds => Enabled && testSpeeds;
        public static bool UnitViewer => Enabled && unitViewer;
        public static bool FreeGacha => Enabled && freeGacha;
    }
}
