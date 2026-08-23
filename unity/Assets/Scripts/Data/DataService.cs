using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameMaker.Data
{
    /// <summary>
    /// 데이터 접근 추상화.
    /// 지금은 LocalDataService(JSON 시드 + PlayerPrefs)를 쓰고,
    /// 나중에 Spring 백엔드가 준비되면 RestDataService 로 교체한다.
    /// </summary>
    public interface IDataService
    {
        List<MonsterData> GetMonsters();
        MonsterData FindMonster(string name);                // 레거시 MonsterDao.findMonsterByName
        List<EnemySpawn> GetEnemiesByMap(int mapNumber);     // 레거시 EnemyDao.findEnemyByMapNumber
        StageData GetStage(int mapNumber);                   // 스테이지 테마/보스 설정
        PlayerData GetPlayer();                              // 레거시 PlayerDao.getPlayer("A")
        void SavePlayer(PlayerData player);
        int Clear(int mapNumber);                            // 레거시 PlayerRepository.clear — 클리어 보상(획득액 반환)
        int GetUpgradeCount(string monsterName);
        void Upgrade(string monsterName);                    // 레거시 UpgradeActivity 기능 복원(돈 차감 + 레벨업)

        // ── 뽑기/배치 ──
        bool OwnsUnit(string monsterName);                   // 기본 4종은 항상 true
        int GetDupeCount(string monsterName);                // 중복 뽑기 강화 수 (+N)
        GachaResult DrawGacha(int cost);                     // 뽑기 1회 (돈 차감, 중복이면 +1)
        List<string> GetLoadout();                           // 출전 유닛 (기본: 기본 4종)
        void SetLoadout(List<string> names);
    }

    public static class DataHub
    {
        // TODO(Spring 연동 시): new RestDataService("https://<server>/api") 로 교체
        public static readonly IDataService I = new LocalDataService();
    }

    /// <summary>
    /// 로컬 구현: 시드는 Resources/GameData/*.json (레거시 Room DB 시드 그대로),
    /// 플레이어/업그레이드 상태는 PlayerPrefs 에 JSON 으로 저장.
    /// </summary>
    public class LocalDataService : IDataService
    {
        const string PlayerKey = "gm_player_json";
        const string UpgradeKey = "gm_upgrades_json";

        List<MonsterData> monsters;
        List<EnemySpawn> enemies;
        List<StageData> stages;
        PlayerData player;
        UpgradeState upgrades;

        public LocalDataService()
        {
            monsters = JsonUtility.FromJson<MonsterList>(
                Resources.Load<TextAsset>("GameData/monsters").text).items;
            enemies = JsonUtility.FromJson<EnemySpawnList>(
                Resources.Load<TextAsset>("GameData/enemies").text).items;
            stages = JsonUtility.FromJson<StageList>(
                Resources.Load<TextAsset>("GameData/stages").text).items;

            player = PlayerPrefs.HasKey(PlayerKey)
                ? JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PlayerKey))
                : new PlayerData();
            MigrateProgress();
            upgrades = PlayerPrefs.HasKey(UpgradeKey)
                ? JsonUtility.FromJson<UpgradeState>(PlayerPrefs.GetString(UpgradeKey))
                : new UpgradeState();
        }

        public List<MonsterData> GetMonsters() => monsters;

        public MonsterData FindMonster(string name) => monsters.FirstOrDefault(m => m.name == name);

        public List<EnemySpawn> GetEnemiesByMap(int mapNumber) =>
            enemies.Where(e => e.mapNumber == mapNumber).ToList();

        public StageData GetStage(int mapNumber) =>
            stages.FirstOrDefault(s => s.mapNumber == mapNumber) ?? stages[0];

        public PlayerData GetPlayer() => player;

        public void SavePlayer(PlayerData p)
        {
            player = p;
            PlayerPrefs.SetString(PlayerKey, JsonUtility.ToJson(player));
            PlayerPrefs.Save();
        }

        /// <summary>구버전 저장(테마 1~9 인덱스, 배열 10칸)을 stageId 체계(테마*10+서브)로 이관.</summary>
        void MigrateProgress()
        {
            if (player.mapClear != null && player.mapClear.Length >= 130) return;
            var old = player.mapClear ?? new int[0];
            player.mapClear = new int[130];
            for (int t = 1; t <= 9 && t < old.Length; t++)
                if (old[t] > 0) player.mapClear[t * 10 + 1] = old[t]; // 테마 클리어 → 첫 서브 클리어로 인정
        }

        /// <summary>보상 = 난이도기준 * (11 - 클리어횟수), 최소 1. 획득액 반환.
        /// 난이도기준: stageId(테마*10+서브) 기준 테마*3+서브 (레거시 1~9 는 그대로).</summary>
        public int Clear(int mapNumber)
        {
            player.mapClear[mapNumber]++;
            int clearTime = player.mapClear[mapNumber];
            int baseVal = mapNumber >= 10 ? (mapNumber / 10) * 3 + (mapNumber % 10) : mapNumber;
            int money = baseVal * (11 - clearTime) <= 0 ? 1 : baseVal * (11 - clearTime);
            player.money += money;
            SavePlayer(player);
            return money;
        }

        public int GetUpgradeCount(string monsterName) => upgrades.Get(monsterName);

        /// <summary>비용 = (성이면 50, 아니면 cost) * (현재레벨 + 1). 부족하면 예외 대신 GameException.</summary>
        public const int MaxGoldLevel = 10; // 골드 강화 상한 — 이후는 중복 뽑기(+N)로만

        public void Upgrade(string monsterName)
        {
            var m = FindMonster(monsterName);
            if (m == null) throw new GameException("존재하지 않는 유닛입니다.");

            int level = upgrades.Get(monsterName);
            if (level >= MaxGoldLevel) throw new GameException("최대 강화입니다. 중복 뽑기로만 강화할 수 있습니다.");
            int cost = (m.IsCastle ? 50 : m.cost) * (level + 1);
            if (player.money < cost) throw new GameException("돈이 부족합니다. [ " + cost + " ] 필요");

            player.money -= cost;
            upgrades.Set(monsterName, level + 1);
            PlayerPrefs.SetString(UpgradeKey, JsonUtility.ToJson(upgrades));
            SavePlayer(player);
        }

        // ─────────────────── 뽑기 / 배치 ───────────────────

        static readonly string[] BasicUnits = { "ourbasic", "ourtank", "ourbattle", "ourmass" };
        /// <summary>등급별 확률(%) — 계획서: 일반 40 / 고급 30 / 희귀 18 / 영웅 9 / 전설 3.</summary>
        static readonly int[] TierWeights = { 0, 40, 30, 18, 9, 3 };
        public const int LoadoutMax = 5; // 출전 슬롯 수 (냥코식 5칸 배치)

        public bool OwnsUnit(string monsterName) =>
            System.Array.IndexOf(BasicUnits, monsterName) >= 0 || player.gachaNames.Contains(monsterName);

        public int GetDupeCount(string monsterName)
        {
            int i = player.gachaNames.IndexOf(monsterName);
            return i < 0 ? 0 : player.gachaDupes[i];
        }

        public GachaResult DrawGacha(int cost)
        {
            if (player.money < cost) throw new GameException("돈이 부족합니다. [ " + cost + " ] 필요");

            // 등급 추첨 → 해당 등급에서 균등 추첨
            int roll = Random.Range(0, 100), tier = 1, acc = 0;
            for (int t = 1; t <= 5; t++)
            {
                acc += TierWeights[t];
                if (roll < acc) { tier = t; break; }
            }
            var pool = monsters.FindAll(m => m.IsOur && !m.IsCastle && m.tier == tier);
            if (pool.Count == 0) throw new GameException("뽑기 풀이 비어 있습니다.");
            var unit = pool[Random.Range(0, pool.Count)];

            player.money -= cost;
            var result = new GachaResult { unit = unit };
            int i = player.gachaNames.IndexOf(unit.name);
            if (i < 0)
            {
                player.gachaNames.Add(unit.name);
                player.gachaDupes.Add(0);
                result.isNew = true;
            }
            else
            {
                player.gachaDupes[i]++;      // 중복 = +1 강화 (골드 1강화와 동급, 무제한)
                result.dupes = player.gachaDupes[i];
            }
            SavePlayer(player);
            return result;
        }

        public List<string> GetLoadout()
        {
            // 저장본에서 유효한(보유 중인) 유닛만 — 비어있으면 기본 4종
            var list = new List<string>();
            foreach (var n in player.loadout)
                if (OwnsUnit(n) && FindMonster(n) != null && !list.Contains(n)) list.Add(n);
            if (list.Count == 0) list.AddRange(BasicUnits);
            if (list.Count > LoadoutMax) list.RemoveRange(LoadoutMax, list.Count - LoadoutMax);
            return list;
        }

        public void SetLoadout(List<string> names)
        {
            player.loadout = new List<string>(names);
            SavePlayer(player);
        }
    }

    /// <summary>레거시 customedException 대응.</summary>
    public class GameException : System.Exception
    {
        public GameException(string message) : base(message) { }
    }

    /*
     * ── Spring 백엔드 연동 스텁 ─────────────────────────────────────────────
     * 서버가 준비되면 아래를 구현하고 DataHub.I 를 교체한다.
     * UnityWebRequest 로 REST 호출 (GET /monsters, GET /maps/{n}/enemies,
     * GET/PUT /players/{name}, POST /players/{name}/clear, POST /upgrades ...)
     *
     * public class RestDataService : IDataService { ... }
     * ──────────────────────────────────────────────────────────────────────
     */
}
