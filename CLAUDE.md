# CLAUDE.md

이 파일은 Claude Code가 이 저장소에서 작업할 때 참조하는 지침이다.

> **상세 문서**: [`ARCHITECTURE.md`](ARCHITECTURE.md) (1344줄). 이 파일은 그 요약이며, 세부가 필요하면 항상 그쪽을 볼 것.
> `GameCoreLoop.md`는 2026-03-09 시점 설계 기록으로 **현재 코드와 절반 이상 불일치**한다. 현재 상태는 `ARCHITECTURE.md`를 신뢰할 것.

## 프로젝트

GameFramework(EllanJiang) 기반 뱀서라이크 프로토타입. Unity 공식 샘플 **Zombie Toys**를 껍데기로 삼아 GameFramework 파이프라인으로 다시 짜는 **리팩터링 중단 상태**다.

현재 실제로 동작하는 루프는 **캐릭터 선택(Girl/Boy) → Player 엔티티 재스폰 → WASD 이동**이 전부다. 적 스폰·전투·경험치·레벨업·업그레이드·게임오버는 커밋 `88408fd`에서 삭제되었거나 주석 처리되어 있다.

| 항목 | 값 |
|---|---|
| Unity | 6000.6.0f1 / URP 17.6.0 |
| 에셋 로딩 | Addressables 2.9.0 (**GameFramework 리소스 시스템을 완전 대체**) |
| 입력 | Input System 1.18.0, `activeInputHandler: 2` (Both — 레거시 입력도 살아 있음) |
| 카메라 | Cinemachine **없음** |
| 어셈블리 | asmdef **0개** → 전부 `Assembly-CSharp` |

## 코드 레이어 (수정 권한이 층마다 다르다)

세 층이다. **GameFramework(코어)는 Unity 밖의 별도 C# DLL 프로젝트**이고, UnityGameFramework 가 그 DLL 을 써서 Unity 에 붙이며, 게임은 그 둘을 쓴다.

| 층 | 경로 | 어셈블리 | 파일 수 | 수정 권한 |
|---|---|---|---|---|
| 1 | `External/GameFramework/` (GameFramework 코어, netstandard2.1 프로젝트) → 빌드하면 `Assets/Plugins/GameFramework.dll` | `GameFramework` | 329 | **준금지** — 먼저 사용자에게 확인 |
| 2 | `Assets/Scripts/` (UnityGameFramework — Unity 래퍼), `Assets/Scripts/Editor/` (커스텀 인스펙터·메뉴) | `Assembly-CSharp` / `Assembly-CSharp-Editor` | 190 / 33 | **준금지** — `Resource/` 외에는 사실상 순정 |
| 3 | `Assets/GameMain/` (게임 로직) | `Assembly-CSharp` | — | **자유** — 여기가 우리 코드다 |
| 4 | `Assets/Sample/` (Zombie Toys 원본) | `Assembly-CSharp` | 34 | 정리 대상 |

> **이 폴더 구조는 사용자가 정한 것이다. 요청 없이 바꾸지 말 것.** 폴더를 옮기거나 새 최상위 폴더·패키지·asmdef 를 만드는 일은 먼저 물어본다.
> (2026-10-08 에 프레임워크를 `Packages/com.yamyamcoding.gameframework` UPM 패키지로 옮기고 코어를 소스로 합쳤다가, 의도와 달라 2026-10-11 에 전부 되돌렸다.)

- **코어의 정본은 `External/GameFramework/` 다.** 이 저장소에서는 git 미추적(`.gitignore`)이고, 그 폴더 자체가 별도 저장소 `https://github.com/eazuooz/GameFramework` 의 클론이다 — **코어를 고쳤으면 그 저장소에 커밋·푸시한다.** 코어를 고치면 그 프로젝트를 빌드해야 반영된다. `GameFramework.csproj` 의 빌드 후 이벤트(XCOPY)가 `GameFramework.dll` / `.pdb` / `.deps.json` 을 프로젝트 파일 기준 상대 경로(`..\..\..\Assets\Plugins\`)로 복사한다. IDE 에서 솔루션을 빌드하거나 `dotnet build External/GameFramework/GameFramework/GameFramework.csproj -c Debug` 를 쓴다.
- git 에 올라가는 것은 빌드 결과인 `Assets/Plugins/GameFramework.dll` 이다. **External 을 고쳤으면 DLL 을 다시 빌드해 같이 커밋할 것** — 소스가 git 밖에 있어서 DLL 만이 공유되는 코어다.
- **코어 소스를 Unity 프로젝트(`Assets/`·`Packages/`) 안에 두지 말 것.** DLL 과 같은 이름의 어셈블리가 둘이 되어 `CS1704` 로 전체 컴파일이 깨진다.
- 프로젝트에 asmdef 는 없다. 래퍼와 게임 코드가 같은 `Assembly-CSharp` 이다.
- upstream 히스토리가 소실되어 **원본 머지가 불가능하다.** 프레임워크를 고치면 "이게 GF 사양인가 우리 버그인가"를 판정할 방법이 사라진다.
- 프레임워크 폴더(`Assets/Scripts`)는 배포할 물건이다. **게임 전용 코드(`ToyBoxNightmare` 네임스페이스)나 유료·서드파티 에셋을 넣지 말 것.**
- MIT 고지(`Docs/GameFramework/LICENSE.md`, `Third Party Notices.md`, 각 파일 헤더의 원저작자 표기)를 지우지 말 것. 재배포 조건이다.

## 패키지 내보내기 (배포 단위)

`Tools > Export Packages` (`Assets/Editor/StorePackageExporter.cs`) 가 `Builds/StorePackages/` 에 `.unitypackage` 세 개를 쓴다(git 미추적).

| 파일 | 내용 | 푸는 곳 |
|---|---|---|
| `GameFramework-Core-<버전>.unitypackage` | `Assets/Plugins/GameFramework.dll` + 라이선스 문서(`Assets/Plugins/GameFramework-*.md`) | 아무 프로젝트 |
| `GameFramework-Unity-<버전>.unitypackage` | 코어 DLL + `Assets/Scripts/**`(시작 예제 `Starter/` 포함) + `Assets/Prefabs/GameFramework.prefab` | 아무 프로젝트 |
| `GameFramework-Example-<버전>.unitypackage` | 위 + 예제 게임이 쓰는 에셋 + Addressables 설정 + **ProjectSettings** | **빈 프로젝트** (설정을 덮어쓴다) |

- **경로를 바꾸지 않는다.** 패키지를 열면 이 프로젝트와 똑같은 위치(`Assets/…`)에 풀린다. `.meta` 를 함께 담아 GUID 가 유지된다.
- 코어는 **DLL 로** 들어간다. 내보내기 전에 External 을 빌드해 둘 것 — DLL 이 없으면 중단한다.
- 예제에 들어갈 파일은 손으로 고르지 않는다. **빌드 씬 + Addressables 엔트리 + `Assets/GameMain`** 을 뿌리로 한 의존성 폐포다. 게임이 참조하지 않는 에셋은 따라오지 않는다.
- `Epic Toon FX` / `JMO Assets` / `118 sprite effects bundle` 은 재배포 불가다. 폐포에 섞이면 **중단한다**(`sForbiddenFolders`). 예제 프리팹에서 이 폴더의 에셋을 참조하지 말 것.
- 폐포는 **에셋 참조만** 따라간다. 코드로만 참조하는 스크립트나 ProjectSettings 가 가리키는 에셋은 안 잡히므로 `sExtraFolders` 에 적는다. 새로 생기면 예제 패키지가 컴파일되지 않는다.
- UPM 의존성은 패키지 안의 `packagemanagermanifest` 항목으로 전달된다(프레임워크: Addressables, SharpZipLib).
- 받는 쪽은 메뉴 `Game Framework > Download Core Source` (`Assets/Scripts/Editor/Misc/CoreSourceDownloader.cs`)로 코어 소스를 `External/GameFramework` 에 받을 수 있다(git 클론, git 이 없으면 GitHub ZIP). 이 프로젝트에는 이미 있어서 "already downloaded" 만 뜬다. **확인 창에서 동의했을 때만** 받는다 — 임포트만으로 자동 실행되게 바꾸지 말 것(스토어 가이드라인 2.5.1.d).
- 문서(`Docs/GameFramework/` 의 `LICENSE.md`, `Third Party Notices.md`, `README.md`)는 Assets 밖에 있어 내보낼 때 `Assets/Scripts/` 아래로 넣어 준다. 예제 패키지에는 Luckiest Guy·Zombie Toys 고지가 덧붙는다.
- 버전은 Player Settings 의 Version(`bundleVersion`)이다.
- `GameFramework.prefab` 의 Procedure 는 **시작 절차 `UnityGameFramework.Runtime.ProcedureStarter`** 다(2026-10-18). 이 게임의 절차(`ToyBoxNightmare.ProcedureMain` / `ProcedureGameOver`)는 `MainScene` 의 프리팹 인스턴스 **오버라이드**로 들어 있다. 그래서 유니티 패키지만 받은 프로젝트에서도 프리팹을 놓고 Play 하면 돈다(시작 씬: `Assets/Scripts/Starter/GameFrameworkStarter.unity`).
- 패키지를 바꿨으면 깨끗한 프로젝트에 `Unity.exe -batchmode -importPackage` 로 임포트해 컴파일·누락 스크립트를 확인할 것. 개발 프로젝트에서 도는 것은 증거가 못 된다. 배치 모드에서는 `WaitForEndOfFrame` 이 재개되지 않아 `ProcedureComponent.Start` 가 진입 절차를 시작하지 못한다 — 플레이까지 보려면 그 한 줄을 검증 쪽에서 대신 불러야 한다.

## 실행 흐름

```
BaseComponent (Assets/Prefabs/GameFramework.prefab, MainScene에 배치)
  └ ProcedureComponent → "ToyBoxNightmare.ProcedureMain"   (MainScene 의 오버라이드. 프리팹 기본값은 ProcedureStarter)
      └ ProcedureMain.OnEnter                    Assets/GameMain/Procdure/ProcedureMain.cs:22
          └ new SurvivalGame().Initialize()      Assets/GameMain/Game/SurvivalGame.cs:25
              ├ EventComponent 구독 3종           :31-34
              └ SpawnSelectCharacter("Girl"/"Boy") :37-38
                  └ [클릭] OnCharacterSelected → SpawnPlayer(characterKey)  :104
```

엔티티 프리팹 로드는 `EntityComponent.ShowEntity` → 코어 `EntityManager` → `Assets/Scripts/Resource/ResourceManager.cs:161` → **`Addressables.LoadAssetAsync`** 로 간다. `Resources.Load`나 AssetBundle 경로는 살아있는 체인 어디에도 없다.

게임 코드는 프레임워크의 **Procedure / Fsm / Entity / Event / UI / Sound / ReferencePool** 을 쓴다. 게임이 하는 일은 전부 이 경로를 탄다 — 아래 "게임 코드는 프레임워크 안에서만 돈다" 를 볼 것.

## 실행 전 필수 설정 (안 하면 검은 화면 + 로그 0줄)

1. **`ENABLE_LOG` 계열 define 켜기** — Project Settings > Player > Scripting Define Symbols에 `ENABLE_LOG;ENABLE_DEBUG_AND_ABOVE_LOG`.
   `Assets/Scripts/Utility/Log.cs`의 public 메서드 20개 **전부**가 `[Conditional]`이고 `ProjectSettings.asset:823`이 비어 있어 **현재 모든 로그가 컴파일 제거된다.** 플랫폼별 딕셔너리라 타겟마다 반복해야 한다.
2. **Addressables Play Mode Script = `Use Asset Database (fastest)`** — 이 설정은 `Library/`에 있어 git 공유가 안 된다. 안 하면 캐릭터가 하나도 안 뜬다.
3. **`Assets/Scenes/MainScene.unity`를 직접 연다** — 빌드 씬 목록에 등록되어 있지 않다.

## 코드 규약 (어기면 터진다)

| 규약 | 어기면 |
|---|---|
| `EntityLogic`/`UIFormLogic` 오버라이드는 **`protected internal override`** (프레임워크와 같은 어셈블리라서. `protected override` 는 CS0507) | 컴파일 에러 |
| 엔티티는 **`XxxLogic` + `XxxData` 짝**이다. 능력치·튜닝값(체력·사거리·쿨다운·지연 시간 등)은 데이터에 두고, **스폰하는 쪽**이 표(`EnemyTable`/`WeaponTable`/`AllyTable`)에서 채운다. 로직은 표를 직접 읽지 않고 받은 데이터만 쓴다. 로직에 남기는 것은 실행 상태(타이머·FSM·캐시한 컴포넌트)와 프리팹 계약(자식 경로) 뿐이다 | 같은 값이 두 곳에 생겨 어긋난다. 데이터는 `EntityLogicBase.OnHide` 에서 `ReferencePool.Release` 되므로 로직이 들고 있던 참조는 `OnHide` 에서 null 로 놓을 것 — 안 그러면 다음 스폰의 값을 읽는다 |
| **엔티티 프리팹에 `EntityLogic`/`Entity`를 붙이지 않는다** | `Entity.cs:98`이 런타임에 AddComponent → 컴포넌트 중복·이벤트 다중 발행·NRE |
| **UI 폼 프리팹에는 `UIFormLogic`을 반드시 붙인다** (엔티티와 **정반대**) | `UIForm.cs:94-98`이 `GetComponent`라서 콜백이 하나도 안 돈다 |
| 같은 엔티티에 `HideEntity`는 **정확히 1회** | 코어에서 `GameFrameworkException` |
| 엔티티 제거는 항상 `HideEntity`. `Destroy`/`SetActive` 금지 | 풀 관리가 깨져 인스턴스 누수 |
| `EntityLogic`/`UIFormLogic`에서 Unity `Awake`/`Start` 사용 금지 | 풀링 재사용이라 두 번째 스폰부터 초기화 누락. `OnInit`/`OnShow`/`OnHide`를 쓸 것 |
| `Acquire<T>()`한 객체는 정확히 한 번 `Release` | 이중 Release는 즉시 예외. `Clear()`에 상태 초기화를 빠뜨리면 이전 데이터를 물고 나온다 |
| 같은 (id, handler)를 두 번 `Subscribe`하지 않는다 / 미등록 핸들러를 `Unsubscribe`하지 않는다 | 즉시 예외 |
| 새 `~Component`는 `GameFrameworkComponent` 상속 + `protected override void Awake() { base.Awake(); ... }` | `base.Awake()`를 빼면 `GetComponent<T>()`가 영원히 null |
| 컴포넌트 참조는 `Awake`가 아니라 **`Start`**에서 잡는다 | Awake 순서 미보장 |
| 게임의 절차 설정(MainScene 의 `GameFramework → Procedure` 오버라이드)을 **프리팹에 Apply 하지 않는다** | 프리팹이 다시 게임 전용이 되어 유니티 패키지를 받은 프로젝트에서 "Can not find procedure type" 으로 아무것도 안 돈다 |
| `[SerializeField]` 필드명을 바꾸지 않는다 | `[FormerlySerializedAs]`가 0건이라 프리팹 인스펙터 값이 **조용히 전부 초기화**된다 |
| 새 프리팹은 반드시 Addressables **Groups 창에서** 등록(YAML 직접 편집 금지). Address는 `ShowEntity`에 넘길 문자열과 정확히 동일한 짧은 키 | 등록 누락 시 `ShowEntity`가 조용히 실패 |

### 게임 코드는 프레임워크 안에서만 돈다 (2026-10-12 전수 점검)

`Assets/GameMain` 의 코드가 무언가를 **띄우고, 소리 내고, 서로 찾고, 매 프레임 도는** 방법은 아래로 고정한다. 새 코드를 쓸 때 다른 길을 만들지 말 것.

| 하려는 일 | 이렇게 한다 | 이렇게 하지 않는다 |
|---|---|---|
| 화면에 무언가를 띄운다(캐릭터·무기·투사체·이펙트·선택 조명) | 엔티티 — `ShowEntity` + `EntityData` | 씬에 미리 놓기, `Instantiate`, 프리팹에 로직 붙여 두기 |
| 소리를 낸다(효과음·반복음·배경음악) | `GameSound.PlaySfx` / `PlayLoop` / `PlayMusic` → `SoundComponent` (클립은 Addressables, 주소는 `SoundTable`) | 프리팹·씬의 `AudioSource` 를 직접 `Play()` — 사운드 그룹·믹서·볼륨 설정을 전부 비켜 간다 |
| 다른 엔티티를 찾는다(적이 플레이어를, 카메라가 플레이어를) | `EntityFinder.FindPlayer()` / `FindAlly()` 또는 엔티티 Id → `EntityComponent.GetEntity` | 정적 싱글턴(`X.Instance`) — 풀 재사용과 어긋나면 회수된 인스턴스를 가리킨다 |
| 매 프레임 돈다 | `EntityLogic.OnUpdate` / `UIFormLogic.OnUpdate` / `FsmState.OnUpdate` / `ProcedureBase.OnUpdate`. 그 안에서 굴리는 순수 객체(`WeaponLoadout`, `EnemyDebuffs`, `LightningBoltVfx`, `EnemySpawner`, `AllySquad`)는 소유자가 직접 부른다 | 게임 로직에 `MonoBehaviour.Update` / 코루틴을 따로 달기 |
| 소식을 알린다(점수·체력·사망·쿨다운) | `EventComponent.Fire` + `GameEventArgs` | C# `static event`, 직접 참조해서 메서드 호출 |
| 화면 UI | `UIComponent.OpenUIForm` + `UIFormLogic`. 폼 안의 연출(피격 섬광, 쿨다운 게이지)도 폼의 `OnUpdate` 가 굴린다 | 폼 프리팹에 자체 Update/코루틴을 도는 스크립트 달기 |
| 판과 무관하게 살아 있는 전역 시스템 | `GameFrameworkComponent` 를 상속해 등록(`PlayerCameraFollow`) | 씬의 일반 `MonoBehaviour` |
| 게임을 끝낸다 | `GameEntry.Shutdown(ShutdownType.Quit)` | `Application.Quit()` 직접 호출 |

**남겨 둔 예외 (이유가 있다)**

- `PlayerLogic.FixedUpdate`, `TargetableObjectLogic.OnTriggerEnter` — 물리 콜백이다. 프레임워크에 고정 타임스텝 훅이 없다. 엔티티가 회수되면 GameObject 가 꺼지므로 돌지 않고, 데이터가 없으면 바로 빠진다.
- `Camera.main`, `Mouse.current`/`Keyboard.current` — 프레임워크에 카메라·입력 모듈이 없다.
- `PlayerCameraFollow` 의 `LateUpdate` — 프레임워크 컴포넌트는 원래 Unity 갱신으로 돈다(`BaseComponent.Update` 가 프레임워크 전체를 굴리는 것과 같다).
- `EnemyTable` / `WeaponTable` / `AllyTable` / `SoundTable` / `UITable` — 수치와 주소를 **코드 상수**로 들고 있다. 동작이 아니라 데이터라 그대로 뒀다. `DataTableComponent` 로 옮기는 것은 튜토리얼 10장의 과제다.
- `PauseMenuForm` 의 `AudioMixerSnapshot.TransitionTo` — `SoundComponent` 에 스냅샷 API 가 없다. 같은 믹서(`MasterMixer`)를 쓰므로 프레임워크로 낸 소리에 그대로 먹는다.
- 씬에 직접 놓인 것은 `MainCamera`, `Lighting`, `Environment`(배경 프리팹), `Floor Collider`, `EventSystem`, `GameFramework` 뿐이다. 전부 정적인 무대다.

**새 GameFramework 모듈은 만들 수 없다.** `GameFrameworkModule`이 `internal abstract`다. 새 전역 시스템이 필요하면 `GameFrameworkComponent`를 상속한 MonoBehaviour로 만들어 `Assets/Prefabs/GameFramework.prefab`에 자식으로 추가하고 `GameEntry.GetComponent<T>()`로 접근한다 — 이것이 유일한 확장 경로다.

프레임워크 동작을 바꾸고 싶으면 코드를 고치지 말고 **Helper 교체**를 쓴다(`EntityHelperBase`, `UIFormHelperBase`, `SoundHelperBase`, `ILogHelper`, `ITextHelper` 등 → 인스펙터에 타입명 지정). `Helper.CreateHelper`(`Assets/Scripts/Utility/Helper.cs:47`)는 순정이라 안심하고 써도 된다.

## 알려진 위험

전체 10건과 근거는 `ARCHITECTURE.md` §5.1. 작업 순서와 분담은 `WORKPLAN.md`. 코드를 건드리기 전에 반드시 읽을 것.

**해결됨**
- ✅ **Addressables 핸들 누수** (배치 C, `32b8c36`) — `ResourceManager`가 `assetName` 키 + 핸들 목록으로 취득/해제 1:1. 실패 경로도 Release.
- ✅ **`HideEntity` 이중 호출** (배치 B, `75870a8`) — `EntityLogicBase`의 `mHidden` 가드 + `SafeHide()`. 단 `SurvivalGame.cs:94,100`은 아직 직접 호출한다(배치 D 예정).
- ✅ **로그 컴파일 제거** — `ProjectSettings.asset`에 `ENABLE_LOG;ENABLE_DEBUG_AND_ABOVE_LOG` (Standalone/Android).
- ✅ **빌드 씬** — `MainScene`이 0번, `SampleScene`은 비활성.
- ✅ **레이어 정의** — 8=Floor, 9=Shootable, 10=Blocking, 13=FrostFX, 14=Environment. 원본 마스크값(17920/512/256)을 그대로 쓸 수 있다.

- ✅ **Girl/Boy baked 로직** — `Player`/`PlayerSelectLogic` + Sample 공격 5종 제거. 캐릭터 프리팹에는 빈 `Antenna`(발사점 `(0.123, 0.948, 1.019)`)만 남아 있다.
- ✅ **무기는 엔티티다** (2026-10-11) — `WeaponLogicBase : EntityLogicBase` + 무기별 데이터 `{Lightning,Frost,Stink,Slime}WeaponData : WeaponData : EntityData`(능력치는 `WeaponTable` 에서 채워 오고, 투사체·착탄 이펙트 데이터로 이어서 넘긴다). 외형은 `Assets/Art/Prefabs/Attacks/{Lightning,Frost,Stink,Slime}Attack.prefab`(Addressables 주소 = 프리팹 이름, 엔티티 그룹 `Weapon`)이고, `WeaponLoadout.Equip` 이 `ShowEntity` 로 띄우면 무기가 `OnShow` 에서 `AttachEntity(…, "Antenna")` 로 플레이어에 붙는다. 플레이어는 `OnAttached`/`OnDetached` 로 로드아웃에 알린다. **무기를 따로 `HideEntity` 하지 말 것** — 플레이어가 회수될 때 코어가 붙어 있는 무기를 먼저 회수한다. 발사는 무기의 `OnUpdate` 가 아니라 `PlayerLogic.TickAliveInput → WeaponLoadout.OnUpdate` 가 호출한다(조준점 계산 뒤에 쏘기 위해서). 그 결과 선택 화면의 캐릭터에는 안테나 스파클이 없다(전에는 캐릭터 프리팹에 박혀 있어서 보였다).
- ✅ **`ProcedureComponent` 중복** — 루트 쪽 제거. 자식 `Procedure` GO의 `ToyBoxNightmare.ProcedureMain` 1개만 남았고 씬의 고아 오버라이드도 정리됨.
- ✅ **`Camera.main` null** — MainCamera 태그 지정. 마우스 조준 회전 동작.
- ✅ **적 프리팹 5종** — Sample 스크립트 3종 제거, NavMeshAgent 비활성(씬 미베이크 대응), 전 종 레이어 9(Shootable). 콜라이더·HitParticles 보존. Addressables는 이미 등록돼 있어 클론 불필요.
- ✅ **Missing Script 2건** — `Player.prefab`, `UpgradeForm.prefab` 삭제.

> 전체 프리팹/씬 1204개 스캔 결과 dangling 참조 0건, missing script 0건.

**미해결**
1. **Addressables Play Mode Script** — `Library/`에 저장되어 **git 공유가 안 된다.** 클론한 사람이 각자 Groups 창에서 `Use Asset Database (fastest)`로 설정해야 한다.
2. ~~NavMesh 미베이크~~ — **해결됨.** `Assets/Scenes/MainScene/NavMesh.asset` 로 베이크 완료(agentRadius 0.5 / height 1.2 / slope 45). 적이 NavMeshAgent 로 추적한다.
3. ~~카메라 추종 없음~~ — **해결됨.** `PlayerCameraFollow`(프레임워크 컴포넌트, MainCamera 에 붙어 있다)가 플레이어를 따라가고, 플레이어가 없으면 씬에 놓인 선택 앵글 `(0,4,6)` 로 돌아간다.
4. ~~카메라가 선택 앵글에 고정~~ — **해결됨.** 위와 같다. 게임 앵글은 플레이어 기준 offset `(0,15,-22)`.
5. **`Assets/_Recovery/0.unity`** — 크래시 복구 산출물. 빌드/로드 경로 밖이라 무해하지만 삭제 권장.
6. **씬 핸들 덮어쓰기** — `ResourceManager`의 `mSceneHandles[...] = op`. 코어가 중복 로드를 선차단하고 `LoadScene` 호출이 0건이라 현재 도달 불가.

## 자잘한 함정

- 폴더명 오타: `Assets/GameMain/Procdure/` (Procedure 아님)
- 네임스페이스는 `ToyBoxNightmare` (폴더명 ToyboxNightmare와 B 대소문자가 다름)
- Missing Script 2건: `Assets/Prefabs/Player.prefab:44`, `UpgradeForm.prefab:48`
- `Assets/Scripts/Editor/` 에 커스텀 인스펙터와 `Game Framework` 메뉴가 있다(원본 `Scripts/Editor` 의 Inspector·Misc 이식 + `Editor/Addressables` 의 Addressables 도구). 원본의 AssetBundle 도구(Resource Editor/Builder/Analyzer/Pack Builder/Sync Tools)는 **일부러 옮기지 않았다** — 런타임이 Addressables 라 산출물을 읽지 못한다
- `EntitySerialId`에 리셋 API가 없다. `TypeId`는 전부 하드코딩 `1`
- `Assets/Sample/`(Zombie Toys 원본 스크립트)과 `Assets/AssetManager/` 는 이제 게임이 **하나도 참조하지 않는다**(HUD 의 `Countdown`/`FlashFade`, Environment 의 `AddressablesController` 를 걷어냈다). 내보내기 폐포에도 들어가지 않는다. 지워도 게임은 돈다 — 지울지는 사용자가 정한다
- Zombie Toys 프리팹에 박혀 있던 `AudioSource` 는 전부 제거했다. 그것들은 원본 믹서(`SoundEffects.mixer`)로 나가서 이 게임의 볼륨·일시정지와 무관하게 울렸다

## Unity CLI (에디터 직접 제어)

`com.unity.pipeline` 이 설치되어 있어 **Unity 에디터가 켜져 있으면** 터미널에서 직접 조회·조작할 수 있다.
바이너리: `%LOCALAPPDATA%\Unity\bin\unity.exe` (새 셸에서는 `unity` 로 실행 가능)

```bash
unity status                       # 연결 확인 (포트/프로젝트/PID)
unity command                      # 노출된 명령 150여 개 목록
unity command <name> --json        # 실행
```

**가장 자주 쓸 것**

| 목적 | 명령 |
|---|---|
| 컴파일 에러 확인 | `unity command recompile_status` / `eval` 로 `EditorUtility.scriptCompilationFailed` |
| 콘솔 로그 읽기 | `unity command console --tail 30 --level error` |
| 씬/프리팹 조회 | `get_scene_hierarchy`, `find_gameobjects`, `find_assets`, `get_serialized_fields` |
| 프리팹 안전 편집 | `remove_component`, `add_component`, `set_layer`, `set_tag`, `save_prefab_contents` |
| 플레이 모드 | `editor_play`, `editor_pause`, `editor_stop` |
| NavMesh 베이크 | `bake_navmesh` → `navmesh_bake_status` 폴링 |
| 화면 캡처 | `capture_game_view`, `capture_scene_view` |

**함정 (실제로 겪은 것)**

- 결과는 JSON의 **`.data.result`** 에 들어 있다.
- **`--quiet` 를 쓰지 말 것** — 결과 출력까지 숨긴다.
- `eval` 은 **메서드 본문으로 컴파일된다.** `using` 지시문을 쓸 수 없고 `System.Linq` 확장 메서드도 못 쓴다. **전부 정규화된 이름**을 쓸 것 (`UnityEditor.AssetDatabase...`).
- PowerShell에서 `--code` 로 넘기면 **큰따옴표가 벗겨진다.** 문자열 리터럴이 있는 코드는 반드시 파일에 쓴 뒤 **`eval_file --file <path>`** 로 실행할 것.
- 파괴적 명령(`delete_asset`, `package_remove`, `set_*_settings` 등)은 `--confirm true` 를 요구하고 `--dry_run` 을 지원한다. **먼저 dry_run 으로 확인할 것.**
- CLI 와 프로젝트의 `com.unity.pipeline` 패키지 버전이 맞아야 한다. CLI 가 자동 갱신되면 "패키지가 너무 오래되어…" 라며 모든 `command` 를 거부한다 → `unity pipeline upgrade --project-path <경로>` 로 패키지를 올린다(2026-10-11 에 0.4.0 → 0.8.0-exp.1).
- **플레이 모드에 들어가는 중에는 스크립트를 고치거나 `recompile` 을 보내지 말 것.** 2026-10-11 에 그 타이밍이 겹쳐 에디터가 멈췄다(강제 종료 필요). 보내기 전에 `eval` 로 `EditorApplication.isPlaying` 을 확인한다.
- pipeline 0.8 부터 메인 스레드 명령은 **5초**에서 CLI 쪽이 타임아웃 난다("Main thread operation timed out"). 에디터 쪽 작업은 계속 돌므로, 오래 걸리는 작업(패키지 내보내기 등)은 에러가 떠도 결과 파일·로그로 완료를 확인한다.
- 에디터가 포커스를 잃은 상태에서는 스크립트 컴파일이 **요청만 되고 시작되지 않을 수 있다**(`isCompiling` 이 계속 true, `Library/ScriptAssemblies/*.dll` 시각이 안 바뀜). 사용자가 Unity 창을 한 번 클릭하면 진행된다. 창 포커스를 코드로 뺏지 말 것.

> 이 경로가 생기면서 프리팹 작업은 **YAML 직접 편집 대신 Unity API**로 할 수 있게 됐다. 아래 "작업 시" 규약보다 이쪽이 우선이다.

## 작업 시

- 게임 로직은 `Assets/GameMain/` 안에서 한다.
- 프레임워크 계층(`Assets/Scripts/`, `External/GameFramework/`)을 고쳐야 한다는 결론이 나오면 **먼저 사용자에게 확인**한다. **폴더 구조를 바꾸는 일도 마찬가지다.** 예외는 `Assets/Scripts/Resource/ResourceManager.cs` — 100% 자작 코드라 upstream 부채가 없다.
- **프리팹/씬(`.prefab`, `.unity`) YAML을 직접 편집하지 않는다.** Unity CLI의 `remove_component`/`set_layer`/`save_prefab_contents` 등 **Unity API를 쓴다.** 에디터가 꺼져 있으면 켜 달라고 요청한다.
  (YAML 직접 편집은 실제로 사고를 낸 적이 있다 — 적 프리팹이 레거시 직렬화 포맷(`- 114: {fileID}`)이라 최신 포맷(`- component:`) 패턴이 안 맞아 dangling 참조가 생겼다.)
- **컴파일 확인은 직접 한다** — `unity command recompile_status` 와 `console --level error`. 사용자에게 물어보지 않는다.
