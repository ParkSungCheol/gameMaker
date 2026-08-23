namespace GameMaker.Core
{
    /// <summary>
    /// 개발/테스트 플래그 모음. 출시 전 전부 false 로 바꿀 것.
    /// </summary>
    public static class Dev
    {
        /// <summary>true 면 맵의 모든 테마/서브스테이지 잠금 해제 (전체 테스트용).</summary>
        public const bool UnlockAllStages = true;

        /// <summary>true 면 아군 성이 데미지를 받지 않음 (적군 출현 관찰용 — 패배 방지).</summary>
        public const bool InvincibleOurCastle = true;

        /// <summary>true 면 전투 배속에 x10/x20/x30 추가 (빠른 전체 테스트용).</summary>
        public const bool TestSpeeds = true;

        /// <summary>true 면 메인 메뉴에 "유닛 뷰어" 버튼 노출 — 전 유닛 모션(걷기/공격/사망) 열람.</summary>
        public const bool UnitViewer = true;
    }
}
