using System;
using System.Collections.Generic;

namespace GameMaker.Data
{
    /// <summary>
    /// 유닛 스펙. 전투 규칙이 단순한 만큼 필드도 5개가 전부다:
    /// 전진(moveSpeed) → 사정거리(range) 안에 적이 오면 → attackInterval 마다 attack 만큼 때린다.
    /// </summary>
    [Serializable]
    public class MonsterData
    {
        public string name;
        public string team;          // "ally" 또는 "enemy"
        public int hp;               // 최대 체력
        public int attack;           // 1회 공격 데미지
        public int range;            // 사정거리(px) — 이 거리 안에 적이 있으면 멈추고 공격
        public float moveSpeed;      // 이동속도(px/초)
        public float attackInterval; // 공격 주기(초)
        public int cost;             // 소환 비용
        public int height;           // 화면 표시 높이(px)

        public bool IsCastle => name.Contains("castle");
        public bool IsOur => team != "enemy";

        public MonsterData Clone() => (MonsterData)MemberwiseClone();
    }

    /// <summary>스테이지 테마: 배경/지면/보스 (Resources/GameData/stages.json)</summary>
    [Serializable]
    public class StageData
    {
        public int mapNumber;
        public string bg;      // Resources/Sprites/env/{bg}
        public string ground;  // Resources/Sprites/env/{ground} (타일)
        public string boss;    // 남은시간 0초에 등장할 보스 이름 (빈 문자열 = 없음)
    }

    [Serializable]
    public class StageList { public List<StageData> items; }

    [Serializable]
    public class MonsterList { public List<MonsterData> items; }

    /// <summary>레거시 Room 엔티티 Enemy 에 대응 — 스테이지별 적 출현 타임라인.</summary>
    [Serializable]
    public class EnemySpawn
    {
        public int mapNumber;
        public int time;        // 남은 시간(초)이 이 값일 때 출현
        public string name;
    }

    [Serializable]
    public class EnemySpawnList { public List<EnemySpawn> items; }

    /// <summary>레거시 Room 엔티티 Player 에 대응. mapClear[map] = 클리어 횟수 (index 1~9 사용).</summary>
    [Serializable]
    public class PlayerData
    {
        public string name = "A";
        public int money = 0;
        public int[] mapClear = new int[10];
    }

    /// <summary>유닛별 업그레이드 상태(레거시 UpgradeActivity 기능 복원).</summary>
    [Serializable]
    public class UpgradeState
    {
        public List<string> names = new List<string>();
        public List<int> counts = new List<int>();

        public int Get(string name)
        {
            int i = names.IndexOf(name);
            return i < 0 ? 0 : counts[i];
        }

        public void Set(string name, int count)
        {
            int i = names.IndexOf(name);
            if (i < 0) { names.Add(name); counts.Add(count); }
            else counts[i] = count;
        }
    }
}
