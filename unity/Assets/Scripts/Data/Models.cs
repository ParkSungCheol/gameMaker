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
        public string label;         // 한글 표시명 (빈 값이면 name 사용)
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
        public string sprite;        // 프레임 공유: 다른 유닛의 스프라이트 이름 (빈 값 = name 사용)
        public string tint;          // 색조 "#RRGGBB" — 색만 바꾼 변형 몬스터 (물/얼음/수정 등)
        public float fly;            // 비행 고도(px) — 0이면 지상 유닛
        public float sink;           // 발밑 보정(px) — 스프라이트에 그림자/여백이 있어 떠 보일 때 내려앉힘
        public string projectile;    // 발사체 종류: "arrow"/"bullet"/"rock"/"orb" (빈 값 = 근접 타격)
        public float aoe;            // 범위공격 반경(px) — 보스 전용, 3회 공격마다 내리찍기
        public string melee;         // 근접 타입: "pounce"(점프덮치기)/"ram"(들이받기)/"stomp"(내려찍기)/빈 값(기본 런지)
        public int tier;             // 아군 뽑기 등급: 0 기본 / 1 일반 ~ 5 전설 (적군은 0)
        public int stage;            // 적군 소속 스테이지 ID (테마*10+서브, 아군은 0)
        public string atkStyle;      // 공격 모션 스타일 — 스프라이트(베이크)와 몸 움직임(전투/뷰어) 공용:
                                     // swing/spin/flurry/bite/peck/horn/buck/trample/squash/flap/cast/빈 값(기본 런지)
        public string desc;          // 도감 설명 (병맛 한 줄)
        public string role;          // 아군 포지션: 탱커/전사/암살자/궁수/마법사 (적군은 빈 값)
                                     // 궁수 = 원거리 단일, 마법사 = 원거리 범위(aoe 스플래시), 나머지 근접

        public string SpriteName => string.IsNullOrEmpty(sprite) ? name : sprite;
        public string DisplayName => string.IsNullOrEmpty(label) ? name : label;

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
        public float alpha;  // 0 또는 1 = 불투명(전경), 0<alpha<1 = 반투명 원경 실루엣
    }

    [Serializable]
    public class StageData
    {
        public int mapNumber;    // 테마 번호 (1~12)
        public string label;     // 여행지 이름 (보물찾기 여정 컨셉)
        public string bg;        // 통짜 배경 그림 (동굴 등) — sky 가 비어있을 때 사용
        public string sky;       // 하늘 장면: env 하늘 스프라이트 (지정 시 sun+props 조합 장면)
        public string groundCol; // 카툰 지면 기둥 (가로 타일링)
        public string ground;    // (구) 지면 타일 — groundCol 이 비었을 때 사용
        public string boss;      // (구) 테마 단일 보스 — subBosses 로 대체, 폴백용
        public string ambient;   // 분위기 파티클: snow/leaves/petals/sand/fireflies/sparkle/abyss/sea/rain/sprinkles
        public bool noSun;       // true 면 하늘 장면에서 태양 생략 (흐림/설원 등)
        public bool noClouds;    // true 면 떠다니는 구름 생략
        public string skyTint;   // 하늘 색조 "#RRGGBB" — 스테이지별 시간대/분위기 (빈 값 = 원본)
        public int subCount = 1;          // 테마 내 서브스테이지 수 (1-1, 1-2 ...)
        public List<string> subBosses;    // 서브스테이지별 보스 (index = sub-1, 빈 문자열 = 없음)
        public List<PropSpec> props;

        /// <summary>서브스테이지 보스 이름 (sub 는 1부터).</summary>
        public string BossOf(int sub)
        {
            if (subBosses != null && sub >= 1 && sub <= subBosses.Count) return subBosses[sub - 1];
            return boss;
        }
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

    /// <summary>레거시 Room 엔티티 Player 에 대응.
    /// mapClear[stageId] = 클리어 횟수. stageId = 테마*10+서브 (11~123). 구버전 1~9 인덱스는 로드시 이관.</summary>
    [Serializable]
    public class PlayerData
    {
        public string name = "A";
        public int money = 0;
        public int[] mapClear = new int[130];
        // 뽑기 보유: 뽑은 유닛 이름 + 중복 횟수(+N 강화). 기본 4종은 항상 보유로 취급.
        public List<string> gachaNames = new List<string>();
        public List<int> gachaDupes = new List<int>();
        // 배치(출전 슬롯) — 전투 소환 바에 나갈 유닛들. 비어있으면 기본 4종.
        public List<string> loadout = new List<string>();
        // 한 번이라도 시작해 본 스테이지 ID — 도감 적군 공개 기준 (클리어 여부와 무관)
        public List<int> triedStages = new List<int>();
    }

    /// <summary>뽑기 1회 결과.</summary>
    public class GachaResult
    {
        public MonsterData unit;
        public bool isNew;    // 처음 뽑음
        public int dupes;     // 뽑은 뒤의 중복 강화 수 (+N)
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
