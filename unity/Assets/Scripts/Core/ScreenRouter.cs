using GameMaker.Screens;
using UnityEngine;

namespace GameMaker.Core
{
    /// <summary>레거시 Activity 5개에 대응하는 화면 전환기.</summary>
    public enum ScreenId
    {
        Waiting,      // WaitingActivity
        Main,         // MainActivity
        Map,          // MapActivity
        Battlefield,  // BattlefieldActivity
        Upgrade,      // UpgradeActivity
        UnitTest      // [DEV] 유닛 모션 뷰어 (Core.Dev.UnitViewer)
    }

    public class ScreenRouter : MonoBehaviour
    {
        public static ScreenRouter I { get; private set; }

        GameObject current;

        void Awake() => I = this;

        /// <param name="arg">Battlefield 로 갈 때 mapNumber (레거시 Intent extra "key")</param>
        public void Show(ScreenId id, int arg = 0)
        {
            if (current != null) Destroy(current);

            current = new GameObject("Screen_" + id);
            switch (id)
            {
                case ScreenId.Waiting:     current.AddComponent<WaitingScreen>(); break;
                case ScreenId.Main:        current.AddComponent<MainScreen>(); break;
                case ScreenId.Map:         current.AddComponent<MapScreen>(); break;
                case ScreenId.Upgrade:     current.AddComponent<UpgradeScreen>(); break;
                case ScreenId.Battlefield: current.AddComponent<Battle.BattlefieldController>().mapNumber = arg; break;
                case ScreenId.UnitTest:    current.AddComponent<UnitTestScreen>(); break;
            }
        }
    }
}
