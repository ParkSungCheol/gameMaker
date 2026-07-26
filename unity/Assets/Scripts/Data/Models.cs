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
        public float cooldown;       // 소환 쿨타임(초) — 아군 전용
        public int height;           // 화면 표시 높이(px)
        public string facing;        // 원본 스프라이트가 보는 방향: "left" 또는 ""(오른쪽)

        public bool IsCastle => name.Contains("castle");
        public bool IsOur => team != "enemy";

        public MonsterData Clone() => (MonsterData)MemberwiseClone();
    }

    /// <summary>스테이지 테마: 배경/지면/보스 (Resources/GameData/stages.json)</summary>
    /// <summary>스테이지 배경 소품 (하늘 장면용): env 스프라이트를 x 위치에 h 높이로.</summary>
    [Serializable]
    public class PropSpec
    {
        public string img;
        public float x;
        public float h;
    }

    [Serializable]
    public class StageData
    {
        public int mapNumber;
        public string label;     // 여행지 이름 (보물찾기 여정 컨셉)
        public string bg;        // 통짜 배경 그림 (동굴 등) — sky 가 비어있을 때 사용
        public string sky;       // 하늘 장면: env 하늘 스프라이트 (지정 시 sun+props 조합 장면)
        public string groundCol; // 카툰 지면 기둥 (가로 타일링)
        public string ground;    // (구) 지면 타일 — groundCol 이 비었을 때 사용
        public string boss;      // 남은시간 0초에 등장할 보스 이름 (빈 문자열 = 없음)
        public List<PropSpec> props;
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
