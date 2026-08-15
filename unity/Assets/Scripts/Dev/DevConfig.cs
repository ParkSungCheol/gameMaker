using UnityEngine;

namespace GameMaker.Dev
{
    /// <summary>
    /// 개발 편의 설정(런타임에 PlayerPrefs로 토글 가능)
    /// - Dev_TestMode (int:0/1) 이 1이면 메인 메뉴에 "테스트" 버튼이 추가됩니다.
    ///
    /// NOTE: For quick developer convenience this default is set to `true` so the
    /// Test Mode button appears in the main menu during development. If you prefer
    /// to control this via PlayerPrefs or only enable it in the Editor, change the
    /// default back to `false` or edit the expression to include `Application.isEditor`.
    /// </summary>
    public static class DevConfig
    {
        const string Key = "Dev_TestMode";

        // Default intentionally 'true' per developer request (can be toggled at runtime
        // with DevConfig.SetTestMode or by changing this default back to 'false').
        public static bool TestModeEnabled => PlayerPrefs.GetInt(Key, 1) == 1;

        public static void SetTestMode(bool on)
        {
            PlayerPrefs.SetInt(Key, on ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
