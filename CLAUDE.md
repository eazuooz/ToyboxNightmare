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

프레임워크는 **임베디드 UPM 패키지** `Packages/com.yamyamcoding.gameframework/` 로 분리돼 있다(판매·배포 단위). 게임 코드와 어셈블리가 다르다.

| 층 | 경로 | 어셈블리 | 파일 수 | 수정 권한 |
|---|---|---|---|---|
| 1 | `Packages/com.yamyamcoding.gameframework/Runtime/Core/` (GameFramework 코어) | `GameFramework` | 329 | **준금지** — 먼저 사용자에게 확인 |
| 2 | `Packages/com.yamyamcoding.gameframework/Runtime/UnityGameFramework/` (Unity 래퍼) | `UnityGameFramework.Runtime` | 190 | **준금지** — `Resource/` 외에는 사실상 순정 |
| 3 | `Assets/GameMain/` (게임 로직) | `Assembly-CSharp` | — | **자유** — 여기가 우리 코드다 |
| 4 | `Assets/Sample/` (Zombie Toys 원본) | `Assembly-CSharp` | 34 | 정리 대상 |

- **코어는 이제 소스로 컴파일된다.** 예전의 `Assets/Plugins/GameFramework.dll` 은 제거했다. `Runtime/Core` 의 `.cs` 를 고치면 바로 반영된다.
- `External/GameFramework/` 는 패키지로 복사하기 전의 **옛 원본**이다(git 미추적). 더 이상 빌드에 쓰이지 않으므로 **여기를 고치지 말 것** — 정본은 패키지 쪽이다.
- 코어 어셈블리는 `noEngineReferences` 다. `UnityEngine` 을 참조하면 컴파일이 깨진다.
- upstream 히스토리가 소실되어 **원본 머지가 불가능하다.** 코어를 고치면 "이게 GF 사양인가 우리 버그인가"를 판정할 방법이 사라진다.
- 패키지는 팔 물건이다. **게임 전용 코드(`ToyBoxNightmare` 네임스페이스)나 유료·서드파티 에셋을 패키지 안에 넣지 말 것.** 패키지가 `Assets/` 를 참조해도 안 된다.
- MIT 고지(`LICENSE.md`, `Third Party Notices.md`, 각 파일 헤더의 원저작자 표기)를 지우지 말 것. 재배포 조건이다.

## 스토어 묶음 (판매 단위)

파는 것은 셋이다 — 코어, Unity 래퍼, 예제(이 게임). `Tools > Store Bundle > Build` (`Assets/Editor/StoreBundleBuilder.cs`) 가 `StoreBundle/` 에 제출용 프로젝트를 만든다(git 미추적, 매번 통째로 재생성).

- **개발 프로젝트 구조는 그대로 두고 복사본만 재배치한다.** 묶음 안에서는 `Assets/GameFramework/Framework/` (프레임워크) 와 `Assets/GameFramework/Example/` (예제) 로 들어간다. `.meta` 를 같이 복사해 GUID 가 유지된다.
- 예제에 들어갈 파일은 손으로 고르지 않는다. **빌드 씬 + Addressables 엔트리 + `Assets/GameMain`** 을 뿌리로 한 의존성 폐포다. 게임이 참조하지 않는 에셋은 따라오지 않는다.
- `Epic Toon FX` / `JMO Assets` / `118 sprite effects bundle` 은 재배포 불가다. 폐포에 섞이면 빌더가 **중단한다**(`sForbiddenFolders`). 예제 프리팹에서 이 폴더의 에셋을 참조하지 말 것.
- 폐포는 **에셋 참조만** 따라간다. 코드로만 참조하는 스크립트나 ProjectSettings 가 가리키는 에셋은 안 잡히므로 `sExtraFolders` 에 적는다. 새로 생기면 묶음이 컴파일되지 않는다.
- 묶음을 바꿨으면 깨끗한 Unity 로 열어 컴파일·누락 스크립트를 확인할 것. 개발 프로젝트에서 도는 것은 증거가 못 된다(여기엔 폐포 밖 파일도 다 있다).

## 실행 흐름

```
BaseComponent (Assets/Prefabs/GameFramework.prefab, MainScene에 배치)
  └ ProcedureComponent → "ToyBoxNightmare.ProcedureMain"
      └ ProcedureMain.OnEnter                    Assets/GameMain/Procdure/ProcedureMain.cs:22
          └ new SurvivalGame().Initialize()      Assets/GameMain/Game/SurvivalGame.cs:25
              ├ EventComponent 구독 3종           :31-34
              └ SpawnSelectCharacter("Girl"/"Boy") :37-38
                  └ [클릭] OnCharacterSelected → SpawnPlayer(characterKey)  :104
```

엔티티 프리팹 로드는 `EntityComponent.ShowEntity` → 코어 `EntityManager` → `Packages/com.yamyamcoding.gameframework/Runtime/UnityGameFramework/Resource/ResourceManager.cs:161` → **`Addressables.LoadAssetAsync`** 로 간다. `Resources.Load`나 AssetBundle 경로는 살아있는 체인 어디에도 없다.

게임 코드가 실제로 쓰는 프레임워크 컴포넌트는 **`EntityComponent`(7곳) + `EventComponent`(4곳) 단 2종**이다. 나머지 19개는 프리팹에 붙어 Awake만 돈다.

## 실행 전 필수 설정 (안 하면 검은 화면 + 로그 0줄)

1. **`ENABLE_LOG` 계열 define 켜기** — Project Settings > Player > Scripting Define Symbols에 `ENABLE_LOG;ENABLE_DEBUG_AND_ABOVE_LOG`.
   `Packages/com.yamyamcoding.gameframework/Runtime/UnityGameFramework/Utility/Log.cs`의 public 메서드 20개 **전부**가 `[Conditional]`이고 `ProjectSettings.asset:823`이 비어 있어 **현재 모든 로그가 컴파일 제거된다.** 플랫폼별 딕셔너리라 타겟마다 반복해야 한다.
2. **Addressables Play Mode Script = `Use Asset Database (fastest)`** — 이 설정은 `Library/`에 있어 git 공유가 안 된다. 안 하면 캐릭터가 하나도 안 뜬다.
3. **`Assets/Scenes/MainScene.unity`를 직접 연다** — 빌드 씬 목록에 등록되어 있지 않다.

## 코드 규약 (어기면 터진다)

| 규약 | 어기면 |
|---|---|
| `EntityLogic`/`UIFormLogic` 오버라이드는 **`protected override`** (프레임워크가 다른 어셈블리라서. `protected internal override` 는 CS0507) | 컴파일 에러 |
| **엔티티 프리팹에 `EntityLogic`/`Entity`를 붙이지 않는다** | `Entity.cs:98`이 런타임에 AddComponent → 컴포넌트 중복·이벤트 다중 발행·NRE |
| **UI 폼 프리팹에는 `UIFormLogic`을 반드시 붙인다** (엔티티와 **정반대**) | `UIForm.cs:94-98`이 `GetComponent`라서 콜백이 하나도 안 돈다 |
| 같은 엔티티에 `HideEntity`는 **정확히 1회** | 코어에서 `GameFrameworkException` |
| 엔티티 제거는 항상 `HideEntity`. `Destroy`/`SetActive` 금지 | 풀 관리가 깨져 인스턴스 누수 |
| `EntityLogic`/`UIFormLogic`에서 Unity `Awake`/`Start` 사용 금지 | 풀링 재사용이라 두 번째 스폰부터 초기화 누락. `OnInit`/`OnShow`/`OnHide`를 쓸 것 |
| `Acquire<T>()`한 객체는 정확히 한 번 `Release` | 이중 Release는 즉시 예외. `Clear()`에 상태 초기화를 빠뜨리면 이전 데이터를 물고 나온다 |
| 같은 (id, handler)를 두 번 `Subscribe`하지 않는다 / 미등록 핸들러를 `Unsubscribe`하지 않는다 | 즉시 예외 |
| 새 `~Component`는 `GameFrameworkComponent` 상속 + `protected override void Awake() { base.Awake(); ... }` | `base.Awake()`를 빼면 `GetComponent<T>()`가 영원히 null |
| 컴포넌트 참조는 `Awake`가 아니라 **`Start`**에서 잡는다 | Awake 순서 미보장 |
| `[SerializeField]` 필드명을 바꾸지 않는다 | `[FormerlySerializedAs]`가 0건이라 프리팹 인스펙터 값이 **조용히 전부 초기화**된다 |
| 새 프리팹은 반드시 Addressables **Groups 창에서** 등록(YAML 직접 편집 금지). Address는 `ShowEntity`에 넘길 문자열과 정확히 동일한 짧은 키 | 등록 누락 시 `ShowEntity`가 조용히 실패 |

**새 GameFramework 모듈은 만들 수 없다.** `GameFrameworkModule`이 `internal abstract`다. 새 전역 시스템이 필요하면 `GameFrameworkComponent`를 상속한 MonoBehaviour로 만들어 `Assets/Prefabs/GameFramework.prefab`에 자식으로 추가하고 `GameEntry.GetComponent<T>()`로 접근한다 — 이것이 유일한 확장 경로다.

프레임워크 동작을 바꾸고 싶으면 코드를 고치지 말고 **Helper 교체**를 쓴다(`EntityHelperBase`, `UIFormHelperBase`, `SoundHelperBase`, `ILogHelper`, `ITextHelper` 등 → 인스펙터에 타입명 지정). `Helper.CreateHelper`(`Packages/com.yamyamcoding.gameframework/Runtime/UnityGameFramework/Utility/Helper.cs:47`)는 순정이라 안심하고 써도 된다.

## 알려진 위험

전체 10건과 근거는 `ARCHITECTURE.md` §5.1. 작업 순서와 분담은 `WORKPLAN.md`. 코드를 건드리기 전에 반드시 읽을 것.

**해결됨**
- ✅ **Addressables 핸들 누수** (배치 C, `32b8c36`) — `ResourceManager`가 `assetName` 키 + 핸들 목록으로 취득/해제 1:1. 실패 경로도 Release.
- ✅ **`HideEntity` 이중 호출** (배치 B, `75870a8`) — `EntityLogicBase`의 `mHidden` 가드 + `SafeHide()`. 단 `SurvivalGame.cs:94,100`은 아직 직접 호출한다(배치 D 예정).
- ✅ **로그 컴파일 제거** — `ProjectSettings.asset`에 `ENABLE_LOG;ENABLE_DEBUG_AND_ABOVE_LOG` (Standalone/Android).
- ✅ **빌드 씬** — `MainScene`이 0번, `SampleScene`은 비활성.
- ✅ **레이어 정의** — 8=Floor, 9=Shootable, 10=Blocking, 13=FrostFX, 14=Environment. 원본 마스크값(17920/512/256)을 그대로 쓸 수 있다.

- ✅ **Girl/Boy baked 로직** — `Player`/`PlayerSelectLogic` + Sample 공격 5종 제거. `Antenna`(발사점 `(0.123, 0.948, 1.019)`)·`FrostCone`·LineRenderer·파티클은 **보존**되어 M3~M4에서 재사용한다.
- ✅ **`ProcedureComponent` 중복** — 루트 쪽 제거. 자식 `Procedure` GO의 `ToyBoxNightmare.ProcedureMain` 1개만 남았고 씬의 고아 오버라이드도 정리됨.
- ✅ **`Camera.main` null** — MainCamera 태그 지정. 마우스 조준 회전 동작.
- ✅ **적 프리팹 5종** — Sample 스크립트 3종 제거, NavMeshAgent 비활성(씬 미베이크 대응), 전 종 레이어 9(Shootable). 콜라이더·HitParticles 보존. Addressables는 이미 등록돼 있어 클론 불필요.
- ✅ **Missing Script 2건** — `Player.prefab`, `UpgradeForm.prefab` 삭제.

> 전체 프리팹/씬 1204개 스캔 결과 dangling 참조 0건, missing script 0건.

**미해결**
1. **Addressables Play Mode Script** — `Library/`에 저장되어 **git 공유가 안 된다.** 클론한 사람이 각자 Groups 창에서 `Use Asset Database (fastest)`로 설정해야 한다.
2. ~~NavMesh 미베이크~~ — **해결됨.** `Assets/Scenes/MainScene/NavMesh.asset` 로 베이크 완료(agentRadius 0.5 / height 1.2 / slope 45). 적이 NavMeshAgent 로 추적한다.
3. **카메라 추종 없음** — MainCamera에 Transform/Camera/AudioListener/URP데이터 4개뿐. 걸어가면 화면 밖으로 나간다. `PlayerCameraFollow` 신규 작성 필요(ARCHITECTURE §4-9).
4. **카메라가 선택 앵글에 고정** — pos `(0,4,6)` / rot ≈`(30,180,0)`, orthographic size 4.5. 원본은 게임 앵글 `(0,15,-22)`로 1초 전환한다(M6).
5. **`Assets/_Recovery/0.unity`** — 크래시 복구 산출물. 빌드/로드 경로 밖이라 무해하지만 삭제 권장.
6. **씬 핸들 덮어쓰기** — `ResourceManager`의 `mSceneHandles[...] = op`. 코어가 중복 로드를 선차단하고 `LoadScene` 호출이 0건이라 현재 도달 불가.

## 자잘한 함정

- 폴더명 오타: `Assets/GameMain/Procdure/` (Procedure 아님)
- 네임스페이스는 `ToyBoxNightmare` (폴더명 ToyboxNightmare와 B 대소문자가 다름)
- **인코딩 깨짐**: `EntityData.cs:28,39,50,65`, `TargetableObject.cs:45-46,75,81`. 편집 시 인코딩을 건드리면 diff가 폭발한다
- 삭제된 시스템을 부르는 주석 코드가 남아 있다 — `Enemy.cs:64,71`, `ExpGem.cs:44`, `TargetableObject.cs:82`. **주석만 풀면 컴파일이 깨진다**
- Missing Script 2건: `Assets/Prefabs/Player.prefab:44`, `UpgradeForm.prefab:48`
- 패키지 `Editor/` 에 커스텀 인스펙터와 `Game Framework` 메뉴가 있다(원본 `Scripts/Editor` 의 Inspector·Misc 이식 + `Editor/Addressables` 의 Addressables 도구). 원본의 AssetBundle 도구(Resource Editor/Builder/Analyzer/Pack Builder/Sync Tools)는 **일부러 옮기지 않았다** — 런타임이 Addressables 라 산출물을 읽지 못한다
- `EntitySerialId`에 리셋 API가 없다. `TypeId`는 전부 하드코딩 `1`

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

> 이 경로가 생기면서 프리팹 작업은 **YAML 직접 편집 대신 Unity API**로 할 수 있게 됐다. 아래 "작업 시" 규약보다 이쪽이 우선이다.

## 작업 시

- 게임 로직은 `Assets/GameMain/` 안에서 한다.
- 프레임워크 계층(`Packages/com.yamyamcoding.gameframework/`)을 고쳐야 한다는 결론이 나오면 **먼저 사용자에게 확인**한다. 예외는 `Packages/com.yamyamcoding.gameframework/Runtime/UnityGameFramework/Resource/ResourceManager.cs` — 100% 자작 코드라 upstream 부채가 없다.
- **프리팹/씬(`.prefab`, `.unity`) YAML을 직접 편집하지 않는다.** Unity CLI의 `remove_component`/`set_layer`/`save_prefab_contents` 등 **Unity API를 쓴다.** 에디터가 꺼져 있으면 켜 달라고 요청한다.
  (YAML 직접 편집은 실제로 사고를 낸 적이 있다 — 적 프리팹이 레거시 직렬화 포맷(`- 114: {fileID}`)이라 최신 포맷(`- component:`) 패턴이 안 맞아 dangling 참조가 생겼다.)
- **컴파일 확인은 직접 한다** — `unity command recompile_status` 와 `console --level error`. 사용자에게 물어보지 않는다.
