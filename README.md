# GameMaker

횡스크롤 실시간 디펜스 게임 프로젝트.

## 구조

```
├── legacy/android/   # (레거시) 기존 Android Java 앱 — 참고용, 더 이상 개발하지 않음
├── unity/            # (현재) Unity 6 프론트엔드 — 게임 클라이언트
└── (예정) server/    # Spring Boot 백엔드 — REST API (플레이어/몬스터/스테이지 데이터)
```

## Unity 클라이언트 실행 방법

1. [Unity Hub](https://unity.com/download) 설치 → 로그인(무료 개인 라이선스)
2. Unity Hub → **Installs → Install Editor → Unity 6 LTS (6000.x)** 설치
   - 버전이 `6000.0.32f1` 과 달라도 됨 — 열 때 "다른 버전으로 열기" 선택하면 자동 업그레이드됨
3. Unity Hub → **Projects → Add → `unity/` 폴더 선택** → 열기 (최초 임포트 수 분 소요)
4. 에디터 상단 **▶ Play** 버튼 → 게임 시작
   - 씬 파일 없이 어떤 씬에서든 Play 하면 게임이 부팅됨 (`GameBootstrap.cs`)
   - Game 뷰 해상도를 **16:9 (가로)** 로 설정 권장

## 게임 구성 (레거시 → Unity 대응)

| 레거시 (Android) | Unity |
|---|---|
| WaitingActivity | `Screens/WaitingScreen.cs` |
| MainActivity | `Screens/MainScreen.cs` |
| MapActivity | `Screens/MapScreen.cs` |
| BattlefieldActivity (723줄) | `Battle/BattlefieldController.cs` + `Battle/Unit.cs` |
| UpgradeActivity | `Screens/UpgradeScreen.cs` (유실됐던 ViewModel 로직 복원) |
| Room DB 4개 (시드) | `Assets/Resources/GameData/*.json` |
| Repository / ViewModel | `Data/DataService.cs` (`IDataService`) |
| Glide GIF 애니메이션 | `Battle/SpriteBank.cs` + 자동 생성 프레임 PNG |

### 게임 규칙 (레거시에서 단순화함)
- **전투 규칙 (전부다)**: 앞으로 걷는다 → 사정거리(range) 안에 적이 오면 멈추고 attackInterval(초)마다 attack만큼 때린다 → 체력 0이면 죽는다 → 성이 죽으면 승패
  - 핵심 로직 위치: `unity/Assets/Scripts/Battle/Unit.cs` (규칙), `BattlefieldController.cs` (타깃 찾기·승패·소환)
  - 레거시의 타입상성 / 공격스타일 3종 / 넉백 / 최전방(mostParty) 개념 / 코스트 환급은 **제거함**
- 코스트: 0.2초당 +1 (최대 100), 지갑 업그레이드(비용 50×레벨, 속도 -10ms, 최대치 +20)
- 전투 시간 180초, 스테이지별 적 출현 타임라인, 0초에 보스(yourboss) 등장
- 아군 최대 10마리, 클리어 보상 = 맵번호 × (11 − 클리어횟수)
- 유닛 스펙은 `unity/Assets/Resources/GameData/monsters.json` — hp / attack / range(px) / moveSpeed(px/초) / attackInterval(초) / cost 6개 필드가 전부

## Spring 백엔드 연동 (예정)

`unity/Assets/Scripts/Data/DataService.cs` 의 `IDataService` 가 데이터 접근 추상화 계층.
현재는 `LocalDataService`(JSON 시드 + PlayerPrefs)이며, 서버가 준비되면
`RestDataService` (UnityWebRequest 기반) 를 구현해 `DataHub.I` 만 교체하면 된다.

예상 API:
```
GET  /api/monsters
GET  /api/maps/{n}/enemies
GET  /api/players/{name}
POST /api/players/{name}/clear   (body: mapNumber)
POST /api/players/{name}/upgrade (body: monsterName)
```

## 플레이스홀더 아트

유닛 GIF 원본이 레거시에 없어서 도형 기반 애니메이션 프레임(이동/공격/사망 × 2프레임)을
자동 생성해 사용 중 (`unity/Assets/Resources/Sprites/units/`).
성·보스·배경은 레거시 JPG 를 그대로 사용. 추후 실제 아트로 교체하면 됨
(파일명 규칙: `{유닛명}{동작}_{프레임}.png`, 예: `ourbasicattack_0.png`).
