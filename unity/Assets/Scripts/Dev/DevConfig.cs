using UnityEngine;

namespace GameMaker.Dev
{
    /// <summary>
    /// 개발 편의 설정(런타임에 PlayerPrefs로 토글 가능)
    /// - Dev_TestMode (int:0/1) 이 1이면 메인 메뉴에 "테스트" 버튼이 추가됩니다.
    /// </summary>
    public static class DevConfig
    {
        const string Key = "Dev_TestMode";

        public static bool TestModeEnabled => PlayerPrefs.GetInt(Key, 0) == 1;

        public static void SetTestMode(bool on)
        {
            PlayerPrefs.SetInt(Key, on ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
