# CLAUDE.md

이 파일은 Claude Code가 이 저장소에서 작업할 때 참조하는 지침이다.

> **상세 문서**: [`ARCHITECTURE.md`](ARCHITECTURE.md) (1344줄). 이 파일은 그 요약이며, 세부가 필요하면 항상 그쪽을 볼 것.
> `GameCoreLoop.md`는 2026-03-09 시점 설계 기록으로 **현재 코드와 절반 이상 불일치**한다. 현재 상태는 `ARCHITECTURE.md`를 신뢰할 것.

## 프로젝트

GameFramework(EllanJiang) 기반 뱀서라이크 프로토타입. Unity 공식 샘플 **Zombie Toys**를 껍데기로 삼아 GameFramework 파이프라인으로 다시 짜는 **리팩터링 중단 상태**다.

현재 실제로 동작하는 루프는 **캐릭터 선택(Girl/Boy) → Player 엔티티 재스폰 → WASD 이동**이 전부다. 적 스폰·전투·경험치·레벨업·업그레이드·게임오버는 커밋 `88408fd`에서 삭제되었거나 주석 처리되어 있다.

| 항목 | 값 |
|---|---|
| Unity | 6000.3.10f1 / URP 17.3.0 |
| 에셋 로딩 | Addressables 2.9.0 (**GameFramework 리소스 시스템을 완전 대체**) |
| 입력 | Input System 1.18.0, `activeInputHandler: 2` (Both — 레거시 입력도 살아 있음) |
| 카메라 | Cinemachine **없음** |
| 어셈블리 | asmdef **0개** → 전부 `Assembly-CSharp` |

## 코드 레이어 (수정 권한이 층마다 다르다)

| 층 | 경로 | 파일 수 | 수정 권한 |
|---|---|---|---|
| 1 | `External/GameFramework/` | 333 | **금지** — git 미추적, Unity 미컴파일 |
| 2 | `Assets/Scripts/` (UnityGameFramework 래퍼) | 190 | **준금지** — Resource 폴더 외에는 사실상 순정 |
| 3 | `Assets/GameMain/` (게임 로직) | 22 | **자유** — 여기가 우리 코드다 |
| 4 | `Assets/Sample/` (Zombie Toys 원본) | 34 | 정리 대상 |

**1층이 컴파일되지 않는다는 것이 가장 중요한 전제다.** `External/`는 `Assets/` 바깥이라 Unity가 컴파일하지 않는다. 별도 netstandard2.1 프로젝트로 빌드된 뒤 PostBuild XCOPY로 `Assets/Plugins/GameFramework.dll`에 복사된다.

- **`External`의 `.cs`를 고쳐도 런타임 동작은 바뀌지 않는다.** DLL을 재빌드해야 한다.
- PostBuild가 `GameFramework.csproj:13`에서 **절대경로 하드코딩**(`D:\Github\ToyboxNightmare\Assets\Plugins\`)이라 다른 머신에서는 조용히 실패한다.
- upstream 히스토리가 소실되어(`External`은 커밋 3개짜리 개인 포크) **원본 머지가 불가능하다.** 코어를 고치면 "이게 GF 사양인가 우리 버그인가"를 판정할 방법이 사라진다.
- `Assets/Plugins/GameFramework.pdb`는 로컬 빌드 산출물이다 — **커밋하지 말 것.**

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

엔티티 프리팹 로드는 `EntityComponent.ShowEntity` → 코어 `EntityManager` → `Assets/Scripts/Resource/ResourceManager.cs:161` → **`Addressables.LoadAssetAsync`** 로 간다. `Resources.Load`나 AssetBundle 경로는 살아있는 체인 어디에도 없다.

게임 코드가 실제로 쓰는 프레임워크 컴포넌트는 **`EntityComponent`(7곳) + `EventComponent`(4곳) 단 2종**이다. 나머지 19개는 프리팹에 붙어 Awake만 돈다.

## 실행 전 필수 설정 (안 하면 검은 화면 + 로그 0줄)

1. **`ENABLE_LOG` 계열 define 켜기** — Project Settings > Player > Scripting Define Symbols에 `ENABLE_LOG;ENABLE_DEBUG_AND_ABOVE_LOG`.
   `Assets/Scripts/Utility/Log.cs`의 public 메서드 20개 **전부**가 `[Conditional]`이고 `ProjectSettings.asset:823`이 비어 있어 **현재 모든 로그가 컴파일 제거된다.** 플랫폼별 딕셔너리라 타겟마다 반복해야 한다.
2. **Addressables Play Mode Script = `Use Asset Database (fastest)`** — 이 설정은 `Library/`에 있어 git 공유가 안 된다. 안 하면 캐릭터가 하나도 안 뜬다.
3. **`Assets/Scenes/MainScene.unity`를 직접 연다** — 빌드 씬 목록에 등록되어 있지 않다.

## 코드 규약 (어기면 터진다)

| 규약 | 어기면 |
|---|---|
| `EntityLogic` 오버라이드는 **`protected internal override`** | 컴파일 에러 |
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

프레임워크 동작을 바꾸고 싶으면 코드를 고치지 말고 **Helper 교체**를 쓴다(`EntityHelperBase`, `UIFormHelperBase`, `SoundHelperBase`, `ILogHelper`, `ITextHelper` 등 → 인스펙터에 타입명 지정). `Helper.CreateHelper`(`Assets/Scripts/Utility/Helper.cs:47`)는 순정이라 안심하고 써도 된다.

## 알려진 위험

전체 10건과 근거는 `ARCHITECTURE.md` §5.1. 작업 순서와 분담은 `WORKPLAN.md`. 코드를 건드리기 전에 반드시 읽을 것.

**해결됨**
- ✅ **Addressables 핸들 누수** (배치 C) — `ResourceManager`가 `assetName` 키 + 핸들 목록으로 취득/해제 1:1. 실패 경로도 Release.
- ✅ **`HideEntity` 이중 호출** (배치 B) — `EntityLogicBase`의 `mHidden` 가드 + `SafeHide()`. 단 `SurvivalGame.cs:94,100`은 아직 직접 호출한다(배치 D 예정).

**미해결**
1. **Girl/Boy 프리팹에 `Player`/`PlayerSelectLogic`이 baked** — 클릭 1회에 이벤트 2번 Fire. `EventPoolMode.AllowNoHandler` 덕에 안 터지고 있을 뿐이다. `[에디터]`
2. **모든 로그가 컴파일 제거** — 위 "실행 전 필수 설정" 1번. `[에디터]`
3. **`ProcedureComponent`가 프리팹에 2개** — 루트 쪽은 네임스페이스 빠진 `ProcedureMain`(`GameFramework.prefab:782-784`)이고 등록 순서가 비결정적이다. `[에디터]`
4. **빌드 씬이 빈 `SampleScene` 하나뿐** — 지금 빌드하면 프레임워크가 기동하지 않는다. `Restart`는 복구 불가 종료다. `[에디터]`
5. **`Camera.main`이 null** — `MainScene.unity:296` 카메라가 `Untagged`라 마우스 조준이 죽어 있다. "회전만 안 됨"으로 보인다. `[에디터]`

## 자잘한 함정

- 폴더명 오타: `Assets/GameMain/Procdure/` (Procedure 아님)
- 네임스페이스는 `ToyBoxNightmare` (폴더명 ToyboxNightmare와 B 대소문자가 다름)
- **인코딩 깨짐**: `EntityData.cs:28,39,50,65`, `TargetableObject.cs:45-46,75,81`. 편집 시 인코딩을 건드리면 diff가 폭발한다
- 삭제된 시스템을 부르는 주석 코드가 남아 있다 — `Enemy.cs:64,71`, `ExpGem.cs:44`, `TargetableObject.cs:82`. **주석만 풀면 컴파일이 깨진다**
- Missing Script 2건: `Assets/Prefabs/Player.prefab:44`, `UpgradeForm.prefab:48`
- `Assets/Scripts` 하위에 Editor 폴더가 없어 프리팹 인스펙터에 raw 필드명이 그대로 노출된다
- `EntitySerialId`에 리셋 API가 없다. `TypeId`는 전부 하드코딩 `1`

## 작업 시

- 게임 로직은 `Assets/GameMain/` 안에서 한다.
- 프레임워크 계층(`External/`, `Assets/Scripts/`)을 고쳐야 한다는 결론이 나오면 **먼저 사용자에게 확인**한다. 예외는 `Assets/Scripts/Resource/ResourceManager.cs` — 100% 자작 코드라 upstream 부채가 없다.
- 프리팹/씬(`.prefab`, `.unity`) YAML을 직접 편집하지 않는다. 필요하면 사용자에게 에디터 조작을 요청한다.
- 코드 변경 후 컴파일 확인이 필요하면 사용자에게 Unity 에디터에서 확인해 달라고 요청한다 (CLI 빌드 경로가 설정되어 있지 않다).
