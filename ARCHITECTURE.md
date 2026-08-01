# ToyboxNightmare 스크립트 아키텍처

> **이 문서의 목적**: 내일부터 이 프로젝트에 기능을 추가하는 개발자가, 다른 문서를 찾지 않고 하루를 온전히 쓸 수 있게 하는 것.
>
> **읽는 순서**: 처음이라면 **§0 → §5.1 → §4**만 읽으면 작업을 시작할 수 있다. §1~§3은 참조용, §6~§8은 배경과 미결정 사항이다.
>
> **표기 규약**
> - 파일 경로는 `경로:줄번호` 형식으로 쓴다.
> - 소스만으로 단정할 수 없는 항목은 **미확인**으로 명시했다. 추측은 사실로 쓰지 않았다.
> - 같은 내용을 반복하지 않고 `(§5.1 [3] 참조)` 형태로 상호참조한다.
>
> **⚠ `GameCoreLoop.md`(546줄)는 이 문서와 다른 내용을 담고 있다.** 그 문서는 2026-03-09 커밋 84ce872 시점의 기록이며 현재 코드와 절반 이상 불일치한다(§6-1 대조표). 현재 상태는 이 문서를 신뢰할 것.

---

## 0. 개발 환경 부트스트랩 — 코드를 건드리기 전에 반드시 밟을 4단계

> 이 절이 문서 맨 앞에 있는 이유: **아래 4단계를 밟지 않으면 프로젝트를 실행해도 검은 화면이 뜨고, 그 원인을 알려주는 로그가 단 한 줄도 나오지 않는다.** 클론 직후의 기본 경험이 그렇다.

### 0-1. 로그를 먼저 되살린다 (가장 먼저, 예외 없이)

`Assets/Scripts/Utility/Log.cs`(433줄)의 public static 메서드 20개 **전부**에 아래 3개 어트리뷰트가 붙어 있다(`Assets/Scripts/Utility/Log.cs:26-28`, 파일 전체 101개):

```csharp
[Conditional("ENABLE_LOG")]
[Conditional("ENABLE_DEBUG_LOG")]
[Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
```

`Conditional`은 OR 조건이므로 **셋 중 하나만 정의되면 살아난다.** 그런데 `ProjectSettings/ProjectSettings.asset:823`이 `scriptingDefineSymbols: {}`이고 프로젝트에 `csc.rsp`도 없다 → **전부 컴파일 단계에서 호출 자체가 삭제된다.**

**절차**

1. Project Settings > Player > **현재 빌드 타겟 탭**을 연다.
2. Other Settings > Scripting Define Symbols에 `ENABLE_LOG;ENABLE_DEBUG_AND_ABOVE_LOG` 입력 → **Apply** → 리컴파일 대기.
3. ⚠ `scriptingDefineSymbols`는 **플랫폼별 딕셔너리**다. Standalone 탭에서만 켜면 Android/WebGL 타겟으로 전환하는 순간 다시 침묵한다. 사용하는 타겟마다 반복해야 한다.

**켜기 전까지의 유일한 대안**: 런타임 `DebuggerComponent` 창. 프리팹에 `m_IsActive: 1`로 살아 있고 콘솔 윈도우 설정까지 되어 있다(`Assets/Prefabs/GameFramework.prefab:351-405`). 플레이 중 화면 좌상단 디버거 버튼으로 연다. 단 이건 `GameFrameworkLog`(DLL 내부, Conditional 없음)가 찍는 것만 보여주므로 `Log.*` 호출은 여전히 안 보인다.

이 한 단계가 나머지 모든 진단의 비용을 결정한다. §5.1 [3]에 파급 범위를 정리했다.

### 0-2. Addressables를 실행 가능한 상태로 만든다

문서 어디에도 언급이 없었지만, **이 프로젝트는 Addressables 없이는 캐릭터 하나도 뜨지 않는다.** 캐릭터 선택 화면의 Girl/Boy는 100% Addressables 로드다(§2-1 10단계).

- `Assets/AddressableAssetsData/AddressableAssetSettings.asset:117` = `m_ActivePlayerDataBuilderIndex: 2`(PackedMode).
- **Play Mode Script 설정은 이 에셋에 직렬화되지 않는다.** 로컬 `Library/com.unity.addressables/` 상태에 좌우된다.
- 그 `Library/`와 `Assets/StreamingAssets/aa/`는 둘 다 `.gitignore` 대상이다(`/[Aa]ssets/[Ss]treamingAssets/aa/*`). → **새로 클론한 사람에게는 번들이 0개다.**

**절차**

1. `Assets/Scenes/MainScene.unity`를 **직접 연다.** (빌드 씬 목록에 없으므로 Play 버튼만 눌러서는 열리지 않는다 — §0-3)
2. Window > Asset Management > Addressables > Groups.
3. 좌상단 **Play Mode Script**를 **`Use Asset Database (fastest)`** 로 설정한다. ← **일상 개발 권장값**
4. 릴리스 검증이 필요할 때만 Build > New Build > Default Build Script를 돌리고 `Use Existing Build`로 바꾼다.

**실패했을 때의 증상**: `Use Existing Build` 상태로 번들이 없으면 `SurvivalGame.cs:58`/`:69`의 `ShowEntity` 2건이 전부 실패하고 화면에 아무것도 뜨지 않는다. 실패 통지는 `Assets/GameMain/Game/SurvivalGame.cs:129`의 `Log.Warning` 하나뿐이고 §0-1을 안 했으면 그마저 침묵한다.

### 0-3. MainScene을 직접 연다 (빌드 씬 목록은 아직 잘못되어 있다)

`ProjectSettings/EditorBuildSettings.asset:7-10`에 enabled로 등록된 씬은 `Assets/Scenes/SampleScene.unity` **하나뿐**이고, 그 씬에는 Main Camera / Directional Light / Global Volume 3개밖에 없다. `Assets/Scenes/MainScene.unity`는 목록에 없다.

에디터에서 MainScene을 열어두면 정상 동작하지만, **지금 빌드하면 빈 URP 템플릿 씬이 뜨고 프레임워크가 아예 기동하지 않는다.** 파급은 §5.1 [6]. 고치는 방법은 §8-1의 2번(5분 작업).

### 0-4. 확인 체크리스트

MainScene을 Play했을 때 아래가 전부 성립하면 환경이 정상이다.

| # | 확인 항목 | 실패 시 |
|---|---|---|
| 1 | 콘솔에 `Log.*` 계열 메시지가 하나라도 보인다 | §0-1 미완 |
| 2 | 화면에 Girl(-2,0,0)과 Boy(+2,0,0) 두 캐릭터가 보인다 | §0-2 미완 (또는 §5.1 [1]) |
| 3 | 캐릭터를 클릭하면 하나는 사라지고 다른 하나는 죽는 연출 후 사라진다 | `PlayerSelectLogic.OnMouseUp` 경로 문제 |
| 4 | 월드 원점(0,0,0)에 플레이어가 새로 뜬다 | `SpawnPlayer` 경로 문제 |
| 5 | WASD로 이동하고 걷기 애니메이션이 재생된다 | Animator 파라미터 계약(§4-1 5단계) |
| 6 | **마우스를 움직여도 캐릭터가 회전하지 않는다** | ← **이게 정상이다.** 알려진 버그, §5.1 [9] |

여기까지가 **현재 이 프로젝트에서 동작하는 전부**다.

---

## 1. 한눈에 보기

### 1-1. 프로젝트 성격

GameFramework(EllanJiang) 기반의 뱀서라이크(서바이버류) 프로토타입이다. 원래 Unity 공식 샘플 **Zombie Toys**를 껍데기로 삼아, 그 위에 GameFramework 파이프라인으로 게임을 다시 짜고 있는 **리팩터링 중단 상태**다.

지금 실제로 돌아가는 루프는 **"Girl/Boy 캐릭터 선택 → 선택한 캐릭터를 Player 엔티티로 재스폰 → WASD 이동"**이 전부다. 적 스폰·전투·경험치·레벨업·업그레이드·게임오버는 코드에서 삭제되었거나 주석 처리되어 있다(§6-1).

### 1-2. 기술 스택

| 항목 | 값 | 근거 |
|---|---|---|
| Unity | 6000.3.10f1 | `ProjectSettings/ProjectVersion.txt` |
| 렌더 파이프라인 | URP 17.3.0 | `Packages/manifest.json:10` |
| 에셋 로딩 | Addressables 2.9.0 | `Packages/manifest.json:3` |
| 입력 | Input System 1.18.0, `activeInputHandler: 2` (Both) | `Packages/manifest.json:8`, `ProjectSettings/ProjectSettings.asset:920` |
| 카메라 | Cinemachine **없음** | `Packages/manifest.json` 전체에 항목 없음 |
| 어셈블리 분리 | asmdef **0개** | Assets 전체 검색 0건 |

`activeInputHandler: 2`는 중요하다. `PlayerSelectLogic.OnMouseUp`(`Assets/GameMain/Entity/EntityLogic/PlayerSelectLogic.cs:46`)이 레거시 입력 백엔드에 의존하므로, 이 값이 1(New only)이었다면 캐릭터 선택 자체가 동작하지 않는다. 입력 경로 3중화 문제는 §3-6.

### 1-3. 코드 레이어 4층

| 층 | 경로 | 네임스페이스 | 파일 수 | 역할 | 컴파일 대상 |
|---|---|---|---|---|---|
| 1 | `External/GameFramework` | `GameFramework.*` | 333 .cs | 엔진 독립 순수 C# 코어 (모듈/FSM/이벤트/엔티티/오브젝트풀/ReferencePool) | **아니오** |
| 2 | `Assets/Scripts` | `UnityGameFramework.Runtime` | 190 .cs | 코어 모듈을 1:1로 감싸는 MonoBehaviour 래퍼 (`~Component`) | Assembly-CSharp |
| 3 | `Assets/GameMain` | `ToyBoxNightmare` | 22 .cs | 실제 게임 로직 (프로시저/게임모드/엔티티/무기) | Assembly-CSharp |
| 4 | `Assets/Sample` | (전역) | 34 .cs | Zombie Toys 원본 샘플 (§7-1) | Assembly-CSharp |

#### 1층이 컴파일되지 않는다는 사실이 이 프로젝트에서 가장 중요한 전제다

- `External/`는 `Assets/` 바깥이라 Unity가 컴파일하지 않는다. `Assembly-CSharp.csproj`에 `External` 문자열이 0건이다.
- 별도 netstandard2.1 .NET 프로젝트로 빌드된 뒤 PostBuild XCOPY로 `Assets/Plugins/GameFramework.dll`(프리컴파일 managed plugin)로 복사된다. `Assembly-CSharp.csproj:925`가 이 DLL을 HintPath로 참조한다.
- `.gitignore:80`의 `/External/GameFramework` 때문에 **git에 추적되지 않는다**(`git ls-files External` = 0건). 팀원은 DLL만 받는다.
- 배포된 DLL은 `External/GameFramework/GameFramework/bin/Debug/netstandard2.1/GameFramework.dll`(2026-03-28, 459776B)과 **바이트 단위 동일**하다. 즉 **Debug 빌드**가 배포되어 있고, 같은 트리의 Release 빌드(2025-11-16, 413184B)는 미사용이다.
- **External 소스가 DLL보다 최신이다** (소스 최종 수정 2026-06-14 > DLL 2026-03-28). 따라서 External 소스를 읽어 내린 코어 동작 결론은 "소스 기준 추론"이며, DLL과 정확히 일치하는지는 **미확인**이다.
- **정정** — 이전 초안은 `Assets/Plugins/`에 `GameFramework.pdb`가 함께 커밋되어 있다고 적었으나 **사실이 아니다.** `git ls-files Assets/Plugins/`로 확인한 추적 파일은 `GameFramework.dll`, `GameFramework.deps.json`과 각 `.meta` 4개뿐이다. pdb(270KB)는 각자의 로컬 빌드 산출물이며 **커밋하지 말 것**.

**결론: External의 .cs를 고쳐도 런타임 동작은 바뀌지 않는다. 코어를 고치려면 DLL을 재빌드해 Plugins에 덮어써야 한다.**

### 1-4. 코어 DLL 재빌드 절차 (그리고 그 안의 지뢰 3개)

| # | 단계 |
|---|---|
| 1 | `External/GameFramework/GameFramework.sln`을 Visual Studio / `dotnet build`로 연다 |
| 2 | 구성을 선택한다. **현재 배포본은 Debug 빌드**다. Release로 바꾸면 최적화·심볼이 달라지므로 동작 차이 가능성을 감안할 것 |
| 3 | 빌드하면 PostBuild가 자동으로 `Assets/Plugins/`에 복사한다 |
| 4 | Unity로 돌아가 해당 DLL을 **리임포트**한다 (플러그인은 자동 감지가 느릴 수 있다) |

**지뢰 1 — PostBuild가 절대경로 하드코딩이다.**
`External/GameFramework/GameFramework/GameFramework.csproj:13`:
```xml
<Exec Command="XCOPY &quot;$(TargetDir)$(TargetName).*&quot; &quot;D:\Github\ToyboxNightmare\Assets\Plugins\&quot; /Y /I" />
```
다른 팀원 머신에서 빌드하면 **조용히 엉뚱한 경로에 복사되거나 실패한다.** `$(SolutionDir)..\..\Assets\Plugins\` 형태의 상대경로로 고치는 것을 권장한다(§8-18).

**지뢰 2 — 와일드카드가 `$(TargetName).*`다.** dll/pdb/deps.json이 **전부** 덮어써진다. §1-3의 "pdb는 커밋하지 않는다" 원칙과 맞물려, 재빌드 후 `git status`를 반드시 확인할 것.

**지뢰 3 — 코어 소스 일부가 잘려 있다.** `External/GameFramework/GameFramework/Base/Log/GameFrameworkLog.cs`(107줄)가 주석 기계 번역 중 절단되어 Info/Warning/Error/Fatal 오버로드가 하나씩만 남아 있다(§7-7). **재빌드 시 잘린 오버로드를 부르는 코드가 있으면 컴파일이 깨진다.**

---

## 2. 실행 흐름 (부팅 → 플레이)

### 2-0. 전제

이 흐름은 **에디터에서 MainScene을 직접 열었을 때만** 성립한다(§0-3). 씬 루트는 6개: GameFramework 프리팹 인스턴스, Background Music(순수 AudioSource, §3-8), MainCamera, 그리고 프리팹 인스턴스 3개(Environment / Lighting / Floor Collider).

### 2-1. 부팅 체인

1. **씬 로드** — `Assets/Scenes/MainScene.unity:679-747`에 `Assets/Prefabs/GameFramework.prefab`(guid `0c57aed266d3dd04c8a7639f949371c0`) 인스턴스가 배치되어 있다.

2. **컴포넌트 등록** — 각 `~Component`의 `Awake()` → `Assets/Scripts/Base/GameFrameworkComponent.cs:16-19` → `GameEntry.RegisterComponent(this)`(`Assets/Scripts/Base/GameEntry.cs:95-118`). 동일 타입 중복은 `Log.Error` 후 거부된다(`GameEntry.cs:110`) — 이게 §5.1 [5]의 원인이다.

3. **모듈 생성** — 각 컴포넌트가 자기 Awake에서 `GameFrameworkEntry.GetModule<IXxxManager>()`를 부른다(`External/GameFramework/GameFramework/Base/GameFrameworkEntry.cs:37`). 인터페이스 강제(`:40`) + FullName이 `GameFramework.`로 시작 강제(`:45`) + `Name.Substring(1)`로 'I' 제거(`:50`) → `Type.GetType`(`:51`), 실패 시 예외(`:52-55`). 인스턴스가 없으면 `CreateModule`(`:73`)이 Priority 내림차순 링크드리스트에 삽입한다(`:84`, 등호 없음 → 동순위는 뒤에 붙음).
   - **실제 생성되는 모듈은 18개**다. 프리팹의 21개 컴포넌트 타입 중 `BaseComponent`, `ReferencePoolComponent`, `ResourceComponent`는 GetModule을 호출하지 않는다.

4. **부트스트랩 헬퍼 주입** — `Assets/Scripts/Base/BaseComponent.cs:154-192`가 Text/Version/Log/Compression/Json 헬퍼를 타입명 문자열로 리플렉션 생성한다. `:175-179`에서 `mEditorResourceMode &= Application.isEditor`.
   - Json 헬퍼는 `GameFramework.prefab:765`에 `UnityGameFramework.Runtime.DefaultJsonHelper`로 **유효하게 설정되어 있다**(확인 완료). `SettingComponent.GetObject<T>`가 동작하는 전제가 충족된다(§3-9).

5. **리소스 어댑터 생성** — `Assets/Scripts/Resource/ResourceComponent.cs:40-44`가 `new ResourceManager()`. **여기가 표준 GF와 갈라지는 지점**이다. `GetModule<IResourceManager>()` 호출은 프로젝트 전체에 0건이고, 대신 `Assets/Scripts/Resource/ResourceManager.cs:26`의 Addressables 어댑터를 직접 new 한다(§3-4).

6. **Start 페이즈 상호 주입** — `Assets/Scripts/Entity/EntityComponent.cs:114`가 `SetResourceManager(GameEntry.GetComponent<ResourceComponent>().ResourceManager)`. 같은 패턴이 `Assets/Scripts/UI/UIComponent.cs:181`, `Assets/Scripts/Sound/SoundComponent.cs:142`, `Assets/Scripts/Scene/SceneComponent.cs:97`, `Assets/Scripts/Config/ConfigComponent.cs:98`, `Assets/Scripts/DataTable/DataTableComponent.cs:87`, `Assets/Scripts/Localization/LocalizationComponent.cs:118`에 있다. **7곳 모두 null 체크가 없다**(§5-2).
   - `EntityComponent.cs:139-146`에서 인스펙터의 엔티티 그룹 4개를 등록한다.
   - `Assets/Scripts/Setting/SettingComponent.cs:64-70`도 이 페이즈에서 `mSettingManager.Load()`를 자동 호출한다(§3-9).

7. **프로시저 시작** — `Assets/Scripts/Procedure/ProcedureComponent.cs:60`(코루틴 Start)
   - `:65` `Utility.Assembly.GetType(타입명)` — `Type.GetType` 기반이라 **FQN이 아니면 절대 못 찾는다**
   - `:68-69` 실패 시 `Log.Error` 후 `yield break`
   - `:72` `Activator.CreateInstance` (파라미터 없는 기본 생성자 필요)
   - `:91` `mProcedureManager.Initialize(GetModule<IFsmManager>(), procedures)` → `FsmManager.CreateFsm` → `Fsm.Create`의 각 state `OnInit(fsm)`
   - `:93` `yield return new WaitForEndOfFrame()`
   - `:95` `StartProcedure(entranceType)` → `Fsm.Start` → `mCurrentState.OnEnter(this)`

8. **게임 생성** — `Assets/GameMain/Procdure/ProcedureMain.cs:16-24` OnEnter
   - `:22` `mGame = new SurvivalGame();` ← **하드코딩. GameMode enum을 참조하는 팩토리/딕셔너리/switch가 없다.**
   - `:23` `mGame.Initialize();`

9. **캐릭터 선택 스폰** — `Assets/GameMain/Game/SurvivalGame.cs:25-39`
   - `:27` `base.Initialize()` → `Assets/GameMain/Game/GameBase.cs:19` `GameOver = false`
   - `:29` `Instance = this`
   - `:31-34` EventComponent 구독 3종 (ShowEntitySuccess / ShowEntityFailure / CharacterSelected)
   - `:37-38` `SpawnSelectCharacter("Girl", (-2,0,0))`, `SpawnSelectCharacter("Boy", (2,0,0))`

10. **엔티티 로드** — `SurvivalGame.cs:54-64`
    - `:56` `EntitySerialId.Next()` (`Assets/GameMain/Utility/EntitySerialId.cs:11`, 리셋 없는 전역 카운터, 첫 값 1)
    - `:58-63` `EntityComponent.ShowEntity(id, typeof(PlayerSelectLogic), assetPath, "Player", new CharacterSelectData(id, 1, key){ Position = ... })`(`Assets/Scripts/Entity/EntityComponent.cs:312` 오버로드)
    - 코어 EntityManager → 인스턴스 풀 미스 → `IResourceManager.LoadAsset` → `Assets/Scripts/Resource/ResourceManager.cs:152`, `:161` `Addressables.LoadAssetAsync<object>(assetName)`
    - 주소 해석: `Assets/AddressableAssetsData/AssetGroups/Prefabs.asset:54-55`(Girl → `Assets/Art/Prefabs/Characters/Girl.prefab`), `:30-31`(Boy)
    - 로드 성공 → `Assets/Scripts/Entity/DefaultEntityHelper.cs:19-22` Instantiate → `:24-37` CreateEntity(`:36` `GetOrAddComponent<Entity>()`) → `Assets/Scripts/Entity/Entity.cs:64-113` OnInit(`:98` `gameObject.AddComponent(entityLogicType)`) → `Entity.OnShow`(`:130-141`) → `EntityLogic.OnShow`

11. **성공 통지** — `Assets/Scripts/Entity/EntityComponent.cs:689-692` → `ShowEntitySuccessEventArgs.Create(e)`(`Assets/Scripts/Entity/ShowEntitySuccessEventArgs.cs:60-70`, `:68`에서 ShowEntityInfo를 ReferencePool에 반납) → `EventComponent.Fire` → `Assets/GameMain/Game/SurvivalGame.cs:107-124`에서 `mGirlSelect`/`mBoySelect`/`mPlayer` 캐싱

12. **클릭 입력** — `Assets/GameMain/Entity/EntityLogic/PlayerSelectLogic.cs:46-54` `OnMouseUp()` → `:52-53` `EventComponent.Fire(CharacterSelectedEventArgs.Create(characterKey))`
    - ⚠ 이 콜백이 **클릭 1회에 2번** 실행된다. 원인은 §5.1 [2].

13. **선택 처리** — `Assets/GameMain/Game/SurvivalGame.cs:79-105`
    - `:84-85` 자기 자신을 즉시 Unsubscribe (1회성)
    - `:90-102` 선택된 쪽 즉시 `HideEntity`, 나머지는 `DisableAndHide()`(`PlayerSelectLogic.cs:57-68`, 콜라이더 off → Animator `"Die"` 트리거 → 1.5초 후 HideEntity)
    - `:104` `SpawnPlayer(ne.CharacterKey)`

14. **플레이어 스폰** — `SurvivalGame.cs:66-75`
    - `ShowEntity(id, typeof(Player), characterKey, "Player", new PlayerData(id, 1))` — **에셋 이름이 곧 characterKey**, 즉 선택 프리팹과 동일한 Addressable을 다시 로드한다(§5.1 [1]의 직접 원인).
    - `PlayerData`에 Position을 안 넣으므로 `Assets/GameMain/Entity/EntityData/EntityData.cs:16-17` 기본값 `Vector3.zero` → 플레이어가 항상 **월드 원점**에 뜬다.

15. **플레이** — `Assets/GameMain/Entity/EntityLogic/Player.cs:36-50` OnShow에서 `Instance = this`, `:58-65` OnUpdate에서 입력 읽기(`:90-107` `Keyboard.current`), `:69-86` FixedUpdate에서 `Rigidbody.MovePosition/MoveRotation` + `Animator.SetBool("IsWalking")`.

### 2-2. 매 프레임

```
Assets/Scripts/Base/BaseComponent.cs:198-201  Update()
  → GameFrameworkEntry.Update(Time.deltaTime, Time.unscaledDeltaTime)
     → EventManager(7)      : 큐 배출 → 핸들러 호출 → ReferencePool.Release(e)
     → ObjectPoolManager(6)
     → DownloadManager(5) / FileSystemManager(4)
     → [ResourceManager(3) 슬롯은 비어 있음 — 모듈로 생성되지 않는다]
     → SceneManager(2)
     → FsmManager(1)        → Fsm.Update → ProcedureMain.OnUpdate (ProcedureMain.cs:26-31)
                                → mGame?.Update(...) → GameBase.Update (GameBase.cs:25-28) = 빈 메서드
     → Priority 0 그룹       : EntityManager, Config, DataNode, DataTable, Localization,
                               Network, Setting, Sound, UI, WebRequest
                               → EntityManager: recycle 큐 소진 + EntityGroup.Update
                                 → Entity.OnUpdate (Entity.cs:207-217) → EntityLogic.OnUpdate
     → DebuggerManager(-1)
     → ProcedureManager(-2) : Update() 본문이 비어 있음 (ProcedureManager.cs:59-61)
```

**핵심 1: `SurvivalGame`은 `Update()`를 오버라이드하지 않는다.** `ProcedureMain.cs:30`의 `mGame?.Update(...)`는 `GameBase.cs:25-28`의 빈 메서드로 흘러간다. **프레임마다 도는 게임 루프는 0줄이다.** SurvivalGame은 100% 이벤트 구동이다. → 적 스폰이나 게임오버 판정을 붙이려면 **가장 먼저 `SurvivalGame.Update` 오버라이드를 부활**시켜야 한다(§4-3 ★).

**핵심 2: `ProcedureManager`가 Priority -2라고 해서 "프로시저가 맨 마지막에 돈다"고 착각하면 안 된다.** 프로시저 OnUpdate는 `FsmManager`(Priority 1)가 돌리므로 **EntityManager(0)보다 먼저** 돈다.

**핵심 3: 동순위(Priority 0) 10개 모듈의 상호 순서는 정해져 있지 않다.** `CreateModule`의 삽입 조건에 등호가 없어(`GameFrameworkEntry.cs:84`) 동순위는 뒤에 붙는데, 그 순서는 서로 다른 GameObject의 Awake 순서에 좌우되고 Unity는 이를 보장하지 않는다(`ProjectSettings/MonoManager.asset` 없음).

### 2-3. 종료

`BaseComponent.cs:211-213` OnDestroy → `GameFrameworkEntry.Shutdown()` → 모듈 역순 Shutdown → `ReferencePool.ClearAll()` + `Utility.Marshal.FreeCachedHGlobal()` → ProcedureManager.Shutdown → FsmManager 파괴 → `Fsm.Clear` → `ProcedureMain.OnLeave(isShutdown: true)`(`ProcedureMain.cs:33-39`).

**프로젝트 전체에 `ChangeState` 호출이 0건**이고 `StartProcedure` 호출은 `ProcedureComponent.cs:95` 단 1건이다. FSM은 상태 1개짜리로 진입 후 절대 전이하지 않으며, `OnLeave`는 앱 종료 경로로만 도달한다. 이 제약을 푸는 방법은 §4-3 ★.

---

## 3. 핵심 시스템별 정리

> 각 절은 **무엇을 한다 / 핵심 타입 / 현재 상태 / 확장 진입점** 순서로 되어 있다.
> 서브시스템별 존재 여부 요약표는 §6-0에 있다.

### 3-1. 게임 루프 / 프로시저

**무엇을 한다**
`ProcedureComponent`가 인스펙터의 문자열 타입명을 리플렉션으로 인스턴스화해 `IProcedureManager`의 FSM에 등록하고, 진입 프로시저를 시작한다. 프로시저 = `FsmState<IProcedureManager>`다. `ProcedureMain`은 `SurvivalGame`의 수명(생성/갱신/파괴)만 소유하는 얇은 래퍼다.

**핵심 타입**

| 타입 | 경로 | 비고 |
|---|---|---|
| `ProcedureComponent` | `Assets/Scripts/Procedure/ProcedureComponent.cs` | 118줄. `[DisallowMultipleComponent]`(`:19`) — 그런데도 프리팹에 2개가 붙어 있다(§5.1 [5]) |
| `ToyBoxNightmare.ProcedureBase` | `Assets/GameMain/Procdure/ProcedureBase.cs` | `abstract bool UseNativeDialog`(`:17-20`)만 추가. **이 값을 읽는 코드는 0건** |
| `ToyBoxNightmare.ProcedureMain` | `Assets/GameMain/Procdure/ProcedureMain.cs` | 48줄. 유일한 프로시저 |
| `GameBase` | `Assets/GameMain/Game/GameBase.cs` | 48줄. Initialize/Shutdown/Update virtual, abstract GameMode, `bool GameOver { get; protected set; }` |
| `GameMode` | `Assets/GameMain/Game/GameMode.cs` | byte enum, 멤버 `Survival` 하나. **읽는 코드 0건** |
| `SurvivalGame` | `Assets/GameMain/Game/SurvivalGame.cs` | 132줄. 유일한 GameBase 구현체 |

**현재 상태**: 상태 1개짜리 FSM. 전이 없음. 프레임 루프 0줄(§2-2). **확장** → §4-3.

### 3-2. 엔티티

**무엇을 한다**
`EntityData`(순수 C# 데이터 컨테이너) + `EntityLogic`(런타임 AddComponent되는 MonoBehaviour) 두 축. 스폰 시 Data 인스턴스를 userData로 넘기고 Logic이 `OnShow`에서 받아 초기화한다. 인스턴스는 파괴되지 않고 엔티티 그룹의 오브젝트 풀로 반납된다.

**핵심 타입 계층**

```
EntityData (Assets/GameMain/Entity/EntityData/EntityData.cs, 79줄)
  ├─ Id(:30) / TypeId(:41) / Position(:52) / Rotation(:67)
  ├─ TargetableObjectData (57줄) — HitPoints(:32) / abstract MaxHitPoints(:44) / HitPointRatio(:49)
  │    ├─ PlayerData   MaxHP 100, MoveSpeed 5f  (:9-10), ctor에서 HitPoints=mMaxHP (:12-15)
  │    └─ EnemyData    MaxHP 30, Speed 2f, Damage 10, Exp 5 (:9-12), ctor에서 HitPoints=mMaxHP (:16)
  ├─ ProjectileData    Damage/Speed/Lifetime/Direction (:7-10)
  ├─ ExpGemData        ExpAmount 5, MoveSpeed 4f (:7-8)
  └─ CharacterSelectData  CharacterKey (:5), ctor (int, int, string) (:7)

EntityLogic (Assets/Scripts/Entity/EntityLogic.cs, 139줄)
  ├─ TargetableObject (88줄) — IsDead(:9-15), ApplyDamage(:19-39), OnDead(:62-65)
  │    ├─ Player (130줄) — static Instance(:17), [RequireComponent(typeof(Rigidbody))](:13)
  │    └─ Enemy (75줄)
  ├─ Projectile (56줄)
  ├─ ExpGem (56줄)
  └─ PlayerSelectLogic (76줄)
```

**엔티티 그룹** (`Assets/Prefabs/GameFramework.prefab:1067-1087`)

| 그룹 | capacity | 사용처 |
|---|---|---|
| Player | 4 | `SurvivalGame.cs:62`, `:73` |
| Enemy | 64 | **미사용** |
| ExpGem | 100 | **미사용** |
| Projectile | 64 | `ProjectileWeapon.cs:36` (도달 불가) |

Player 그룹의 `InstanceAutoReleaseInterval` / `InstanceExpireTime`은 각각 60초다(`GameFramework.prefab:1069`, `:1071`).

**중요 — 인스턴스 풀 동작 (External 소스 기준 추론, DLL 일치 여부는 미확인)**
`HideEntity`는 인스턴스를 즉시 언스폰하지 않고 `mRecycleQueue`에 Enqueue만 하며, 실제 Unspawn은 다음 `EntityManager.Update`에서 일어난다. 인스턴스 풀은 single-spawn 풀이라 IsInUse 객체를 반환하지 않는다. 따라서 `SurvivalGame.OnCharacterSelected`가 `HideEntity` 직후 같은 콜스택에서 `SpawnPlayer`를 부르면 **같은 인스턴스가 재사용되지 않고** 비동기 LoadAsset → 새 Instantiate 경로로 빠진다. 결과적으로 `Entity.cs:86-96`의 "기존 로직 Destroy" 분기는 이 흐름에서 **실행되지 않는다** → §5.1 [2]의 baked 컴포넌트 문제가 그대로 발현된다.

**조회 API가 통째로 미사용이다.** `EntityComponent.GetEntity/GetEntities/GetAllLoadedEntities`(`Assets/Scripts/Entity/EntityComponent.cs:201-265`), `AttachEntity/DetachEntity`(`:449-651`)를 GameMain이 한 번도 쓰지 않는다. 현재는 `Player.Instance` static + Physics 쿼리로 때우고 있는데, **적을 여러 마리 관리하기 시작하면 이 API로 갈아타야 한다**(§4-1 8단계).

**확장** → §4-1.

### 3-3. 무기 · 전투

**설계 의도**
무기는 Entity 시스템 밖의 순수 MonoBehaviour(`WeaponBase`)로 Player GameObject에 AddComponent되어 자체 `Update` 타이머로 공격하고, 타격체(`Projectile`)만 EntityComponent로 스폰한다. 데미지는 `TargetableObject.ApplyDamage()` 단일 지점으로 수렴한다.

**핵심 타입**

| 타입 | 경로 | 상태 |
|---|---|---|
| `WeaponBase` | `Assets/GameMain/Weapon/WeaponBase.cs` (99줄) | abstract MonoBehaviour. `Initialize(Player)`(`:28-32`), private `Update()` 타이머(`:34-44`), `Attack()`(`:47`), `FindNearestEnemy(radius=20f)`(`:62-83`), `GetMouseWorldPosition()`(`:86-97`) |
| `ProjectileWeapon` | `Assets/GameMain/Weapon/ProjectileWeapon.cs` (52줄) | 유일한 구체 무기. damage 25 / speed 10f / lifetime 3f (`:14-16`), DetectRadius 20f (`:12`) |
| `Projectile` | `Assets/GameMain/Entity/EntityLogic/Projectile.cs` (56줄) | `OnTriggerEnter`(`:44-54`)가 유일한 실 데미지 경로 |
| `TargetableObject` | `Assets/GameMain/Entity/EntityLogic/TargetableObject.cs` (88줄) | 데미지 수렴점 |

**전투 파이프라인의 실제 상태 — 양쪽 끝이 모두 끊겨 있다**

```
[X] 무기 부착      : WeaponBase.Initialize(Player) 호출자 0건. Player.AttachWeapon<T>() 삭제됨.
                     Girl/Boy 프리팹에도 WeaponBase 파생 컴포넌트 없음.
                     설령 프리팹에 붙여도 Owner가 null이라 WeaponBase.cs:36에서 매 프레임 즉시 return.
[X] 투사체 에셋    : Addressable 주소 "Projectile"이 프로젝트 전체에 미등록.
[X] 적 스폰        : typeof(Enemy)로 ShowEntity하는 코드 0건. SpawnEnemy 메서드 자체가 없음.
[X] 적→플레이어    : Enemy.cs:64 //player.TakeDamage(...) 주석. Player에 TakeDamage 메서드 없음.
[X] 충돌 처리      : TargetableObject.cs:82 //AIUtility.PerformCollision 주석. AIUtility 타입 자체가 없음.
[O] 데미지 적용    : TargetableObject.ApplyDamage(:19-39) → OnDead(:62-65) → HideEntity — 실제 동작함
```

즉 `ProjectileWeapon.Attack()`은 **실행 자체가 되지 않으므로** "Projectile" 주소 로드는 시도조차 되지 않는다. `ProjectileWeapon.cs:46-49`의 try/catch는 애초에 도달하지 않는 코드다.

**충돌 판정 방식**: 조준은 `Physics.OverlapSphere` + `Vector3.Distance` 최소값(`WeaponBase.cs:62-83`), 명중은 Unity 물리 트리거(`Projectile.cs:44`). 서로 다른 메커니즘이다. **레이어마스크·태그 필터링이 전혀 없고**, `ProjectSettings/TagManager.asset:7-39`에 커스텀 레이어가 0개다(§8-13).

**확장** → §4-2. 단 **§4-2를 시작하기 전에 §5.1 [4]의 HideEntity 가드를 먼저 넣을 것.**

### 3-4. 에셋 로딩 (Addressables)

**무엇을 한다**
`Assets/Scripts/Resource/ResourceManager.cs`(295줄)가 `IResourceManager` 인터페이스 뒤에 Addressables를 숨기는 어댑터다. GF의 에셋번들 파이프라인을 통째로 대체했다(커밋 afd656b).

**실제 동작하는 것은 4개뿐**

| 메서드 | 줄 | 비고 |
|---|---|---|
| `LoadAsset` | `:152-178` | `:161` `Addressables.LoadAssetAsync<object>(assetName)` — **assetType/priority 인자를 완전히 무시** |
| `UnloadAsset` | `:180-190` | `mAssetHandles` 조회 → `Addressables.Release` + 딕셔너리 제거 |
| `LoadScene` | `:203-227` | `:212` `LoadSceneMode.Additive` **하드코딩** |
| `UnloadScene` | `:232-249` | |

나머지(프로퍼티 `:35-69`, 이벤트 `:73-83`, Set* `:87-96`, 버전/업데이트 `:110-118`, 바이너리 `:253-283`, 리소스 그룹 `:287-293`)는 전부 no-op 스텁이다. `InitResources`(`:103-106`)는 콜백을 즉시 동기 호출할 뿐이고 `Addressables.InitializeAsync()`를 명시적으로 부르지 않는다. `HasAsset`(`:127`)은 **무조건 `AssetOnDisk`를 반환**한다. 실패는 전부 `LoadResourceStatus.NotExist`로 뭉뚱그려지고(`:175`), 로딩 시간을 `Time.time`으로 재므로 timeScale 0에서는 항상 0이다.

⚠ **이 클래스에 이 프로젝트 최대의 리소스 버그가 있다 — §5.1 [1]을 반드시 읽을 것.**

**Addressables 구성**

- 주소는 정확히 10개, 전부 `Assets/AddressableAssetsData/AssetGroups/Prefabs.asset:19-73`에 있다: `Clown` `Environment` `Boy` `Zombunny` `ZomBear` `Sheep` `Girl` `ZombieDuck` `Hellephant` `Dog`
- 나머지 5개 그룹(Default Local Group / Materials / Models / Shaders / Textures)은 `m_SerializeEntries: []`로 비어 있다
- 주소는 전체 경로가 아니라 **파일명 단축형**이고, 전부 `"Prefabs"` 라벨을 단다
- 빌드: `BuildScriptPackedMode`, `m_BuildRemoteCatalog: 0`, Prefabs 그룹은 `PackTogether` + `LZ4` + Local BuildPath/LoadPath
- **씬은 Addressable에 하나도 등록되어 있지 않다** → `SceneComponent` 경유 씬 로드는 현재 무조건 실패한다

**해제 경로**: `EntityComponent.HideEntity` → 풀 만료(60초) → `Assets/Scripts/Entity/DefaultEntityHelper.cs:41` `UnloadAsset(entityAsset)` → `:42` `Destroy(instance)`. 별도로 `Assets/Scripts/Resource/ResourceComponent.cs:51-76`이 최대 300초마다 `Resources.UnloadUnusedAssets()` + 필요 시 `GC.Collect()`를 돌린다.

**중복 구현 주의**: `Assets/AssetManager/AddressablesController.cs` + `AddressablesLoader.cs`가 별개의 미완성 Addressables 진입점이다. 씬에 실제로 존재하지만 본문이 주석 처리되어 무해하다 — 상세와 판정은 §7-5.

**확장** → §4-5.

### 3-5. 이벤트

**무엇을 한다**
`EventComponent`(`Assets/Scripts/Event/EventComponent.cs`, 90줄)가 `IEventManager` 래퍼. `EventManager`는 Priority 7로 **모든 모듈 중 가장 먼저** Update된다. `Fire`는 큐잉(`:80-83`), `FireNow`는 즉시(`:85`).

**핵심 규약**

- `GameEventArgs` 상속 (3단 상속: `GameFrameworkEventArgs`(EventArgs + IReference) → `BaseEventArgs`(+abstract Id) → `GameEventArgs`)
- `public static readonly int EventId = typeof(X).GetHashCode();` + `public override int Id => EventId;`
- static `Create(...)`에서 `ReferencePool.Acquire<X>()` (public 무인자 생성자 필수)
- `Clear()`에서 모든 필드를 초기값으로 되돌린다
- 정본 템플릿: `Assets/GameMain/Game/CharacterSelectedEventArgs.cs`(25줄) — 프로젝트에 남은 **유일한 커스텀 이벤트**

**EventPool 모드**: `AllowNoHandler | AllowMultiHandler`(`External/GameFramework/GameFramework/Event/EventManager.cs:19`).
- `AllowDuplicateHandler`가 빠져 있어 **같은 (id, handler) 쌍을 두 번 Subscribe하면 예외**다.
- `Unsubscribe`도 미등록 핸들러에 대해 예외를 던진다.
- 반대로 핸들러가 **없는** 이벤트는 조용히 버려진다. 지금 §5.1 [2]의 이중 발행이 크래시하지 않는 유일한 이유가 이것이다 — 매우 얇은 우연이므로 §5.1 [7]을 함께 읽을 것.

**Fire 타이밍 (중요 — 흔한 오해)**
"Fire는 항상 1프레임 지연"이 아니다. 지연은 `EventManager.Update`(Priority 7) **이후에 실행되는 호출자**에게만 성립한다. 즉 `BaseComponent.Update` 안에서 도는 Priority<7 모듈들(FsmManager, EntityManager 등)에 한정된다.
- `PlayerSelectLogic.OnMouseUp`은 Unity가 MonoBehaviour.Update 이전 입력 단계에서 디스패치하므로, 그 Fire는 **같은 프레임**의 `BaseComponent.Update` → `EventManager.Update`에서 배출된다.
- 반면 `EntityComponent.OnShowEntitySuccess`가 발화한 이벤트는 다음 프레임에 도착한다.
- 다만 `Assets/Scripts/Base/BaseComponent.cs.meta:7`의 `executionOrder: 0`이고 `ProjectSettings/MonoManager.asset`도 없어서 **순서가 보장되지는 않는다.**

**확장** → §4-4.

### 3-6. 입력 — 현재 3중 경로가 공존한다 (결정 필요)

문서화되지 않았던 사실: **`Assets/InputSystem_Actions.inputactions`가 프로젝트 루트 Assets에 실재한다**(URP 템플릿 기본 액션맵). 그런데 코드 참조가 0건이다.

| # | 경로 | 사용처 | 백엔드 |
|---|---|---|---|
| 1 | 레거시 `OnMouseUp` | `Assets/GameMain/Entity/EntityLogic/PlayerSelectLogic.cs:46` | Legacy Input Manager |
| 2 | `Keyboard.current` / `Mouse.current` 직접 폴링 | `Assets/GameMain/Entity/EntityLogic/Player.cs:90-107`, `:109-121` | New Input System |
| 3 | `.inputactions` 에셋 | **없음 (미사용)** | New Input System |

`ProjectSettings/ProjectSettings.asset:920`의 `activeInputHandler: 2`(Both)라서 셋 다 컴파일·동작은 된다. ⚠ **이 값을 1(New only)로 바꾸면 캐릭터 선택이 즉시 죽는다.**

**임시 규약 (§8-17에서 최종 결정할 것)**
- 신규 입력은 **`Keyboard.current` / `Mouse.current` 폴링으로 통일**한다.
- `Input.GetAxis` / `Input.GetButtonDown` 계열의 **신규 사용 금지**. (`Assets/Sample/Scripts/Player/PlayerInputPC.cs:83`, `:102`가 `ProjectSettings/InputManager.asset`에 없는 축을 부르므로 되살리면 런타임 `ArgumentException`이다.)
- `OnMouseUp`은 캐릭터 선택 1건만 예외로 두되 UI 도입 시 함께 제거한다.
- `.inputactions`를 채택하게 되면 **`PlayerInput` 컴포넌트를 Entity 프리팹에 직접 붙이지 말 것** — §5.1 [2]와 완전히 같은 함정이다. `EntityLogic.OnShow`에서 AddComponent 하거나, 씬에 단일 입력 라우터를 두고 `Player.Instance`에 전달하는 방식으로 간다.

### 3-7. UI — 코드는 완비, 배선은 0

**상태: 프레임워크는 있고 실제 UI는 하나도 없다.**

| 항목 | 현재 값 | 근거 |
|---|---|---|
| UIForm 개수 | **0** | `Assets/GameMain/UI/`는 빈 폴더(88408fd에서 3파일 삭제) |
| `mUIGroups` | `[]` (빈 배열) | `Assets/Prefabs/GameFramework.prefab:155` |
| `mInstanceRoot` | `{fileID: 0}` | `Assets/Prefabs/GameFramework.prefab:150` |
| 씬의 Canvas | **없음** | `Assets/Scenes/MainScene.unity` 루트 6개에 Canvas 없음 |
| 씬의 EventSystem | **없음** | 동일 |

**막히는 지점 5개 (전부 확인 완료)**

1. `mUIGroups: []` → 그룹 없이 `OpenUIForm`을 부르면 실패한다. 최소 1개(`Default`, depth 0)를 인스펙터에서 추가해야 한다.
2. `mInstanceRoot: {fileID: 0}` → `Assets/Scripts/UI/UIComponent.cs:203-208`이 `new GameObject("UI Form Instances")`를 만든다. **이건 Canvas가 아니다.** uGUI 폼을 열어도 **렌더링되지 않는다.**
3. `Assets/Scripts/UI/DefaultUIGroupHelper.cs:14-16` `SetDepth(int depth)`가 **빈 구현**이다 → 그룹 depth 정렬이 무동작. 여러 그룹을 겹쳐 쓸 거면 커스텀 헬퍼가 필요하다.
4. MainScene에 EventSystem이 없어 버튼 클릭 자체가 안 된다. 게다가 EventSystem을 추가하는 순간 `PlayerSelectLogic.cs:49`의 `EventSystem.current != null` 가드가 **처음으로 살아나며 캐릭터 선택 클릭 동작이 바뀐다.**
5. UIForm 프리팹도 Addressable 등록이 필수다(로드 경로가 엔티티와 동일).

**⚠ 엔티티와 정반대인 규약 (확인 완료, 이전 초안에 없던 내용)**
`Assets/Scripts/UI/UIForm.cs:94-98`은 `GetComponent<UIFormLogic>()`을 하고 **null이면 `Log.Error` 후 return한다.** `Assets/Scripts/UI/DefaultUIFormHelper.cs:36`도 `GetOrAddComponent<UIForm>()`까지만 한다.

> **엔티티: 프리팹에 로직을 붙이면 안 된다(런타임 AddComponent).
> UI 폼: 프리팹에 로직을 반드시 붙여야 한다(런타임 GetComponent).**

이 비대칭을 모르면 UI 폼이 열려도 아무 콜백이 안 도는 현상을 겪는다(그리고 그 `Log.Error`도 §0-1 전에는 침묵한다).

**기존 Canvas 프리팹은 그대로 못 쓴다**: `Assets/Art/Prefabs/UI/HUDCanvas.prefab`(Countdown, FlashFade), `PauseMenuCanvas.prefab`(PauseMenu)에 Sample 스크립트가 붙어 있다(§7-1).

**확장** → §4-6.

### 3-8. 사운드 — BGM이 프레임워크 밖에 있다

**현재 상태**: `Assets/Scenes/MainScene.unity:167-295`에 "Background Music" GameObject가 **순수 `AudioSource`로 직접 배치**되어 있다(커밋 8de9a6e). GF 사운드 그룹/볼륨/뮤트 관리를 전혀 받지 못한다.

`SoundComponent`는 모듈이 생성되고 매 프레임 Update되지만 **`PlaySound` 호출이 0건**이다. 그리고 지금 상태로는 불러도 실패한다:

| 항목 | 현재 값 | 근거 |
|---|---|---|
| `mAudioMixer` | `{fileID: 0}` | `Assets/Prefabs/GameFramework.prefab:546` |
| `mSoundGroups` | `[]` | `Assets/Prefabs/GameFramework.prefab:553` |
| `mInstanceRoot` | `{fileID: 0}` | `Assets/Prefabs/GameFramework.prefab:545` |

**이 셋을 채우기 전에는 `PlaySound`가 무조건 실패한다.**

부수 사실: `Assets/Scripts/Sound/SoundComponent.cs:112`가 `GetModule<ISceneManager>()`를 호출한다. 프리팹에서 Scene GO를 제거해도 Sound가 남아 있으면 SceneManager 모듈은 계속 생성된다.

**확장** → §4-7.

### 3-9. 설정 / 세이브 — 유일하게 배선이 끝나 있는 서브시스템

**이건 오늘 바로 쓸 수 있다.** 별도 준비가 필요 없다.

| 항목 | 값 | 근거 |
|---|---|---|
| 컴포넌트 | `Assets/Scripts/Setting/SettingComponent.cs` (`GameFrameworkComponent` 파생) | |
| 헬퍼 (현재 선택값) | `UnityGameFramework.Runtime.DefaultSettingHelper` | `Assets/Prefabs/GameFramework.prefab:829` |
| 대안 헬퍼 | `Assets/Scripts/Setting/PlayerPrefsSettingHelper.cs` | 존재하나 미선택 |
| 저장 위치 | `Application.persistentDataPath` + `/GameFrameworkSetting.dat` | `Assets/Scripts/Setting/DefaultSettingHelper.cs:20`, `:225` (**확인 완료**) |
| 자동 Load | **있다.** `SettingComponent.cs:64-70`의 `Start()`가 `mSettingManager.Load()` | |
| 자동 Save | **없다.** `Save()`(`:72`)를 명시적으로 불러야 파일에 기록된다 | |

**API**: `GetBool/GetInt/GetFloat/GetString`(+ defaultValue 오버로드) / `SetBool/SetInt/SetFloat/SetString`(`:102-160`), 객체 직렬화용 `GetObject<T>/SetObject<T>`(`:162-190`), `HasSetting`(`:87`) / `RemoveSetting`(`:92`) / `Save`(`:72`).

`GetObject<T>`는 Json 헬퍼에 의존하는데, `GameFramework.prefab:765`에 `UnityGameFramework.Runtime.DefaultJsonHelper`가 유효하게 지정되어 있음을 확인했다 → **동작 전제 충족.**

**확장** → §4-8.

### 3-10. 카메라 — 시스템이라 부를 것이 없다

| 항목 | 상태 |
|---|---|
| Cinemachine | **패키지 없음** (`Packages/manifest.json`) |
| 추종 스크립트 | **씬에 없음** |
| MainCamera 태그 | **`Untagged`** → `Camera.main`이 null (`Assets/Scenes/MainScene.unity:296-310`) |
| 투영 | orthographic, size **4.5** (`Assets/Scenes/MainScene.unity:401-402`) |

`Camera.main == null`이 만드는 실제 증상은 §5.1 [9]에 있다(마우스 조준 회전 전멸). 카메라가 따라가지 않으므로 플레이어가 즉시 화면 밖으로 나간다.

⚠ **`Assets/Sample/Scripts/Helpers/CameraFollow.cs`를 쓰지 말 것.** `:15`가 `GameManager.Instance.Player.transform.position`을 참조하는데 MainScene에 `GameManager`가 없어 **FixedUpdate마다 NRE**다. 게다가 `GameManager.Player`는 Sample의 `PlayerHealth` 계열이라 GF 엔티티인 `ToyBoxNightmare.Player`와 타입도 다르다.

**확장** → §4-9 (20줄짜리를 새로 작성한다).

---

## 4. 작업 레시피

> 여기 있는 9개 레시피는 **§0을 끝냈다는 전제**로 쓰여 있다.
> 표의 ★ 표시는 "이 프로젝트에서 반드시 이렇게 하기로 정한 것"이다.

### 4-1. 새 엔티티(적/투사체/아이템) 추가하기

| # | 작업 | 파일 / 위치 |
|---|---|---|
| 1 | **Data 클래스 생성** | `Assets/GameMain/Entity/EntityData/XData.cs` |
| | `EntityData` 또는 `TargetableObjectData` 상속. 생성자 `public XData(int entityId, int typeId) : base(entityId, typeId)` | |
| | HP가 있으면 **생성자에서 `HitPoints = mMaxHP;` 필수** — `TargetableObjectData` 생성자가 0으로 두므로 빠뜨리면 스폰 즉시 `IsDead == true` | `TargetableObjectData.cs:17-22`, `PlayerData.cs:12-15`, `EnemyData.cs:14-17` |
| | 추가 payload는 생성자 인자(예: `CharacterSelectData.cs:7`) 또는 오브젝트 이니셜라이저 | |
| | ⚠ `[SerializeField]`를 붙여도 **인스펙터로 조절할 수 없다.** `UnityEngine.Object`가 아니라 코드에서 `new`로 만들어지므로 항상 고정값이다 | |
| 2 | **Logic 클래스 생성** | `Assets/GameMain/Entity/EntityLogic/X.cs` |
| | HP 필요 → `TargetableObject`, 아니면 `UnityGameFramework.Runtime.EntityLogic` 상속 | |
| | 오버라이드 접근자는 반드시 **`protected internal override`** (`protected override`면 컴파일 에러) | `Player.cs:29/36/52/58` |
| | `OnShow` 고정 패턴: `base.OnShow(userData);` → `mData = userData as XData;` → null이면 `Log.Error` 후 return → `CachedTransform.position/rotation = mData.Position/Rotation;` | `Enemy.cs:22-37`, `Projectile.cs:14-28` |
| | Unity의 Awake/Start 대신 이 훅을 쓸 것 (풀링 재사용이라 Awake는 인스턴스당 한 번뿐) | |
| 3 | ★ **HideEntity 1회 가드를 넣는다** | **선택이 아니라 필수** |
| | `private bool mHidden;` 을 두고 `OnShow`에서 false로 리셋, Hide 직전에 `if (mHidden) return; mHidden = true;` | |
| | 이유와 재현 조건은 §5.1 [4]. **가드 없이 Projectile/ExpGem을 되살리면 첫날에 예외로 터진다** | |
| | 장기적으로는 `EntityLogic` 공통 베이스에 넣는 편이 낫다 | |
| 4 | **프리팹 제작** | `Assets/Art/Prefabs/...` |
| | ★ **EntityLogic 컴포넌트를 붙이지 말 것** — `Entity.cs:98`이 런타임에 무조건 AddComponent 한다. 붙이면 §5.1 [2]를 새로 만드는 것이다 | |
| | ★ **Entity 컴포넌트도 붙이지 말 것** — `DefaultEntityHelper.cs:36`이 `GetOrAddComponent<Entity>()` | |
| | (UI 폼은 규약이 정반대다 — §3-7 참조) | |
| 5 | **Animator 파라미터 계약 확인** | `Assets/Art/Animations/BoyAnimatorController.controller` |
| | 코드가 문자열로 참조하는 파라미터는 2개다: `"IsWalking"`(`:11`, `Player.cs:85`가 `SetBool`), `"Die"`(`:17`, `PlayerSelectLogic.cs:60`이 `SetTrigger`) | |
| | Girl은 `Assets/Art/Animations/GirlAnimatorController.overrideController`로 Boy 컨트롤러를 오버라이드한다 — **파라미터 정의는 Boy 쪽에만 있다** | |
| | ⚠ 파라미터가 없는 Animator를 물리면 **경고 한 줄 없이 애니메이션만 무동작**한다(§0-1 전이면 더더욱 안 보인다) | |
| 6 | **물리 구성 결정** | |
| | 트리거 판정이 필요하면 한쪽에 `isTrigger` Collider + 한쪽에 Rigidbody | |
| | ⚠ 기존 캐릭터 프리팹을 복제해 만들 거면 Rigidbody를 확인할 것: `Assets/Art/Prefabs/Characters/Girl.prefab:44489-44490`이 `m_LinearDamping: Infinity`, `m_AngularDamping: Infinity`, `m_Constraints: 80`이다(§5.1 [10-e]) | |
| | ★ **신규 프리팹은 damping 0을 기본으로 한다.** 지면 콜라이더(`Floor Collider` 프리팹) 위에 스폰되도록 Y를 맞출 것 | |
| 7 | **Addressables 등록** | Addressables Groups 창 |
| | ★ **YAML(`Prefabs.asset`)을 직접 편집하지 말 것.** GUID/엔트리 무결성이 깨진다 | |
| | Window > Asset Management > Addressables > Groups → 프리팹을 `Prefabs` 그룹에 드래그 → **Address를 파일명 단축형으로 rename** → 라벨 `Prefabs` 부여 | |
| | ← **이 단계를 빼먹으면 ShowEntity가 조용히 실패한다.** Enemy/ExpGem/Projectile이 지금 그 상태다 | |
| 8 | **엔티티 그룹 확인/추가** | `Assets/Prefabs/GameFramework.prefab:1067-1087` |
| | 기존 4개(Player/Enemy/ExpGem/Projectile) 중 선택하거나 인스펙터에서 새 그룹 추가. **없는 이름을 넘기면 코어에서 `GameFrameworkException`** | |
| 9 | **스폰 코드 작성** | 보통 `Assets/GameMain/Game/SurvivalGame.cs` |
| | ```int id = EntitySerialId.Next();```<br>```GameEntry.GetComponent<EntityComponent>().ShowEntity(id, typeof(X), "주소", "그룹", new XData(id, 1){ Position = pos });``` | `SurvivalGame.cs:56-63` 패턴 |
| | id는 한 번만 뽑아 ShowEntity 인자와 Data 생성자에 **같은 값**을 넘긴다 | |
| 10 | ★ **여러 개를 관리한다면 id를 직접 들고 다니지 말고 `EntityComponent` 조회 API를 쓴다** | `Assets/Scripts/Entity/EntityComponent.cs:201-265` |
| | `GetEntity(int id)` / `GetEntities(string entityGroupName)` / `GetAllLoadedEntities()` / `HasEntity(int id)` / `HideEntity(int id)` | |
| | 지금 GameMain은 이 API를 **한 번도 쓰지 않는다**(`Player.Instance` static + Physics 쿼리로 대체). "적 50마리 관리"를 시작하는 순간 여기로 갈아타야 한다 | |
| | `GetEntities("Enemy")`로 살아있는 적을 순회하는 것이, 자체 `List<int>`를 유지하며 Hide 타이밍과 동기화하는 것보다 안전하다 | |
| 11 | **로직 참조가 필요하면** | `Assets/GameMain/Game/SurvivalGame.cs:107-124` |
| | `OnShowEntitySuccess`에 `ne.EntityLogicType` 분기 추가. 이벤트는 큐잉이므로 참조가 채워지는 건 ShowEntity 호출보다 **최소 한 프레임 뒤**다(§3-5) | |

### 4-2. 새 무기 추가하기

> **선행 조건 2개**: (a) 무기 시스템 전체가 고아 상태이므로 부착 경로를 먼저 만들어야 한다. (b) §4-1 3단계의 HideEntity 가드를 Projectile에 먼저 넣어야 한다(§5.1 [4]).

| # | 작업 | 파일 / 위치 |
|---|---|---|
| 0 | **부착 경로 신설(필수)** — `WeaponBase.Initialize(Player)` 호출자가 프로젝트 전체 0건이다. 원설계는 `SurvivalGame.OnShowEntitySuccess`의 Player 분기에서 부착하는 것이었다 | `Assets/GameMain/Game/SurvivalGame.cs:119-123`에 추가 |
| | 예: `var w = mPlayer.gameObject.AddComponent<XWeapon>(); w.Initialize(mPlayer);` | |
| | ⚠ 프리팹에 미리 붙여두는 방식은 쓰지 말 것 — Owner가 null이라 `WeaponBase.cs:36`에서 매 프레임 즉시 return하고, §5.1 [2]와 같은 baked 문제를 만든다 | |
| 1 | **무기 클래스 생성** | `Assets/GameMain/Weapon/XWeapon.cs` |
| | `WeaponBase` 상속. 자동 발사면 `protected override void Attack()` 오버라이드 | `WeaponBase.cs:47` |
| | 파생 클래스에서 자체 타이머를 만들지 말 것 — `WeaponBase.Update`(`:34-44`)의 `attackInterval` 타이머가 관리한다 | |
| | 수동 발사면 `OnFireStart/OnFireHeld/OnFireStop`(`:51-57`) 오버라이드 — 단 **이걸 호출해 줄 입력 라우팅 코드가 없으므로 직접 만들어야 한다.** 만들 때 §3-6의 임시 규약을 따를 것 | |
| 2 | **타겟 탐색** — 직접 Physics를 새로 짜지 말고 `FindNearestEnemy(radius)` 사용 | `WeaponBase.cs:62-83` |
| 3 | **투사체가 필요하면** §4-1 전체 수행 + Addressables에 `"Projectile"` 주소 **신규 등록**(현재 미등록) | |
| 4 | **대상 프리팹 물리 확인** | |
| | ⚠ `Assets/Prefabs/Enemy.prefab`에는 Collider가 **없다**(46줄, Transform + Enemy 스크립트뿐). 대신 이미 Addressable에 등록되고 콜라이더도 있는 `Zombunny`/`ZomBear`/`ZombieDuck`/`Hellephant`/`Clown`을 쓰는 편이 압도적으로 저비용이다(`Zombunny.prefab:245` SphereCollider, `:257` CapsuleCollider). 단 이 프리팹들에는 Sample 스크립트가 붙어 있으므로 **복제 후 스크립트를 떼고 쓸 것**(§8-10) | |
| 5 | **데미지는 반드시** `TargetableObject.ApplyDamage(Entity attacker, int damage)` 로만. HP 필드 직접 조작 금지 | `TargetableObject.cs:19` |
| | attacker에는 보통 `Owner.Entity` 또는 자신의 `Entity`를 넘긴다 | `Projectile.cs:51` |
| 6 | **레이어 설계 검토** — `TagManager.asset`에 커스텀 레이어가 0개다. `OverlapSphere`가 마스크 없이 전 레이어를 훑고 있다. 적을 되살리기 **전에** 도입하는 편이 재작업이 적다(§8-13) | |

### 4-3. 새 프로시저(게임 상태) 추가하기 · 게임오버 전이 만들기

| # | 작업 | 파일 / 위치 |
|---|---|---|
| 1 | **클래스 생성** | `Assets/GameMain/Procdure/ProcedureX.cs` (폴더명 오타 `Procdure` 주의) |
| | `ToyBoxNightmare.ProcedureBase` 상속, `public override bool UseNativeDialog => false;` **반드시 구현** | `ProcedureBase.cs:17-20` |
| | 파라미터 없는 기본 생성자 유지 (`Activator.CreateInstance` 사용) | `ProcedureComponent.cs:72` |
| 2 | **프리팹에 문자열 등록** | `Assets/Prefabs/GameFramework.prefab:679-693` |
| | ★ **자식 'Procedure' GameObject**의 ProcedureComponent → `mAvailableProcedureTypeNames`에 **네임스페이스 포함 FQN**으로 추가 (`ToyBoxNightmare.ProcedureX`) | |
| | ⚠ 루트 GameObject 쪽 ProcedureComponent(`:770-784`)는 이미 깨져 있다. **거기 넣으면 동작하지 않는다.** 씬 인스펙터에서 편집해도 마찬가지다 — 상세는 §5.1 [5] | |
| 3 | **전이 코드** — `ChangeState<ProcedureX>(procedureOwner)`. **`FsmState` 내부에서만 호출 가능**하다 | |
| 4 | **주의** — FSM 상태 집합은 `Fsm.Create` 시점에 고정된다. 런타임 상태 추가 API가 없다 | |
| 5 | **주의** — 타입은 런타임 문자열 리플렉션으로 해석된다. 클래스명/네임스페이스 리팩터링 시 프리팹 문자열을 같이 고쳐야 하며, **컴파일 에러 없이 런타임에서만 터진다**(그 에러 로그도 §0-1 전에는 침묵) | |

#### ★ 게임오버 전이 확정 패턴

문제: **게임 로직이 사는 `SurvivalGame`은 `FsmState`가 아니다.** 그래서 `ChangeState`를 직접 부를 수 없다. 게다가 `GameBase.Update`가 빈 메서드라 폴링 지점조차 없다(§2-2). 아래 4단계로 푼다.

1. **`SurvivalGame.Update` 오버라이드를 부활시킨다.** 이게 모든 프레임 단위 로직(적 스폰, 생존 타이머, 난이도 곡선)의 유일한 진입점이 된다.
   ```csharp
   public override void Update(float elapseSeconds, float realElapseSeconds)
   {
       base.Update(elapseSeconds, realElapseSeconds);
       // 스폰 / 타이머 / 종료 판정
       if (조건) GameOver = true;   // GameBase.cs:42-46, protected set
   }
   ```
2. **판정은 `SurvivalGame`이 `GameOver = true`로만 표현한다.** 여기서 씬 전환이나 UI를 직접 건드리지 않는다.
3. **전이는 `ProcedureMain.OnUpdate`가 한다**(`Assets/GameMain/Procdure/ProcedureMain.cs:26-31`). 여기는 `FsmState` 내부이므로 `ChangeState`가 합법이다.
   ```csharp
   protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float e, float r)
   {
       base.OnUpdate(procedureOwner, e, r);
       mGame?.Update(e, r);
       if (mGame != null && mGame.GameOver)
           ChangeState<ProcedureGameOver>(procedureOwner);
   }
   ```
4. **수명 관계를 기억할 것**: `ChangeState`는 `ProcedureMain.OnLeave`를 부르고, 거기서 `mGame.Shutdown()`이 돈다(`ProcedureMain.cs:33-39`). 즉 전이 시점에 SurvivalGame이 파괴된다.

**이 패턴을 쓸 때 함께 처리해야 하는 3가지**

| 항목 | 내용 |
|---|---|
| 구독 해제 대칭 | `SurvivalGame.Shutdown`(`:41-50`)이 `CharacterSelectedEventArgs`를 해제하지 않는다. 캐릭터를 고르지 않은 채 전이하면 **좀비 핸들러가 남고, 재진입 시 중복 구독으로 즉시 예외**다. 상세는 §5.1 [7] |
| 잔존 엔티티 정리 | 전이 전에 `GetEntities("그룹명")`으로 순회하며 `HideEntity` (§4-1 10단계). 정리 안 하면 다음 프로시저에 이전 엔티티가 그대로 남는다 |
| ID 연속성 | `Assets/GameMain/Utility/EntitySerialId.cs:9-11`에 **리셋 API가 없다.** 게임을 재시작해도 ID가 계속 증가한다. 기능상 문제는 없지만 로그 추적 시 혼란스럽다면 리셋 메서드를 추가할지 결정할 것(§8-12) |

### 4-4. 이벤트 주고받기

| # | 작업 | 코드 |
|---|---|---|
| 1 | **EventArgs 클래스 생성** — `Assets/GameMain/Game/CharacterSelectedEventArgs.cs`를 그대로 베낀다 | |
| | ```public class XEventArgs : GameEventArgs``` | |
| | ```public static readonly int EventId = typeof(XEventArgs).GetHashCode();``` | `:8` |
| | ```public override int Id => EventId;``` | `:9` |
| | ```public static XEventArgs Create(...) { var e = ReferencePool.Acquire<XEventArgs>(); ...; return e; }``` | `:13-18` |
| | ```public override void Clear() { /* 모든 필드 초기화 */ }``` | `:20-23` |
| | public 무인자 생성자 필수 (`Acquire<T>`의 `new()` 제약) | |
| 2 | **구독** — `GameEntry.GetComponent<EventComponent>().Subscribe(XEventArgs.EventId, OnX);` | `SurvivalGame.cs:31-34` |
| | 핸들러 시그니처는 `void (object sender, GameEventArgs e)`, 내부에서 캐스팅 | |
| | ⚠ 같은 (id, handler) 쌍 중복 Subscribe → 예외 | |
| 3 | **해제** — 구독한 곳과 대칭되는 곳(Shutdown/OnHide)에서 반드시 Unsubscribe | `SurvivalGame.cs:43-45` |
| | ⚠ 등록되지 않은 핸들러를 Unsubscribe해도 예외 | |
| | 1회성 이벤트는 핸들러 진입 즉시 self-unsubscribe. 단 **그 핸들러가 두 번 불릴 수 있는지** 반드시 검토할 것(§5.1 [7]) | `SurvivalGame.cs:84-85` |
| 4 | **발행** — `GameEntry.GetComponent<EventComponent>().Fire(this, XEventArgs.Create(...));` | `PlayerSelectLogic.cs:52-53` |
| | ⚠ **Fire에 넘긴 args는 프레임워크가 Release한다. 이후 절대 참조/재발행하지 말 것.** strict check가 항상 켜져 있어(`GameFramework.prefab:295` `mEnableStrictCheck: 0` = AlwaysEnable) 이중 Release는 즉시 `GameFrameworkException`이 난다 | |
| 5 | **타이밍 판단** — §3-5 참조. 같은 프레임 확정이 필요하면 `FireNow`(단 lock 없이 즉시 호출이라 재진입/스레드 안전성 없음) | `EventComponent.cs:85` |

### 4-5. 에셋 추가 / 로드하기

| # | 작업 | 파일 / 위치 |
|---|---|---|
| 1 | Addressables Groups 창에서 프리팹을 `Prefabs` 그룹에 드래그 | Window > Asset Management > Addressables > Groups |
| 2 | ★ **창에서 Address를 파일명 단축형으로 rename** (Unity 기본값인 전체 경로가 아님) + 라벨 `Prefabs` 부여. **`Prefabs.asset` YAML을 직접 편집하지 말 것** | |
| 3 | 코드에 상수 선언 — `private const string XAssetPath = "X";` | `SurvivalGame.cs:10-11` 관례 |
| 4 | 로드는 `EntityComponent.ShowEntity` 경유가 원칙. 직접 필요하면 `GameEntry.GetComponent<ResourceComponent>().LoadAsset(...)` | `ResourceComponent.cs:91-113` |
| 5 | ⚠ **같은 주소를 2회 이상 로드하면 핸들이 누수된다** — §5.1 [1] | |
| 6 | ⚠ **씬은 Addressable에 하나도 등록되어 있지 않다.** `SceneComponent` 경유 씬 로드는 현재 무조건 실패하고, `LoadSceneMode.Additive`가 하드코딩되어 있다 | `ResourceManager.cs:212` |
| 7 | ⚠ **씬에 직접 배치 + Addressable 등록을 동시에 하지 말 것.** PackedMode + PackTogether 구성에서 에셋이 씬 데이터와 번들에 각각 들어가 중복된다. `Environment.prefab`이 지금 그 상태다 | |

### 4-6. UI 폼 추가하기 (신규 배선 포함)

> **선행 조건: 배선이 0이다.** 첫 폼을 만들 때 아래 A 단계를 한 번만 하면, 이후는 B만 반복하면 된다. 근거는 §3-7.

**A. 최초 1회 배선**

| # | 작업 |
|---|---|
| A1 | MainScene에 **Canvas**(Render Mode: Screen Space - Overlay) + **CanvasScaler** + **GraphicRaycaster**를 만든다 |
| A2 | 같은 씬에 **EventSystem**을 추가한다. ⚠ 이 순간 `PlayerSelectLogic.cs:49`의 `EventSystem.current != null` 가드가 처음 살아나 **캐릭터 선택 클릭 동작이 바뀐다.** 반드시 §0-4 체크리스트로 재확인할 것 |
| A3 | `GameFramework` 프리팹 인스턴스의 **UIComponent 인스펙터에서 `mInstanceRoot`에 A1의 Canvas Transform을 지정**한다 (`Assets/Prefabs/GameFramework.prefab:150`이 현재 fileID 0). **이걸 안 하면 폼을 열어도 렌더링되지 않는다** |
| A4 | 같은 인스펙터의 `mUIGroups`(`:155`)에 최소 1개 추가: Name `Default`, Depth 0 |
| A5 | 그룹을 2개 이상 겹쳐 쓸 계획이면 `DefaultUIGroupHelper.SetDepth`가 빈 구현(`Assets/Scripts/UI/DefaultUIGroupHelper.cs:14-16`)이므로 **커스텀 UIGroupHelper를 만들어 `mUIGroupHelperTypeName`(`prefab:153`)에 지정**해야 한다. 그룹 1개면 지금은 넘어가도 된다 |

**B. 폼 한 개 추가**

| # | 작업 | 파일 / 위치 |
|---|---|---|
| B1 | **로직 클래스 생성** — `UIFormLogic` 상속 | `Assets/GameMain/UI/XForm.cs` (현재 빈 폴더) |
| | 훅: `OnInit / OnOpen / OnClose / OnPause / OnResume / OnCover / OnReveal / OnRefocus / OnUpdate / OnDepthChanged` | `Assets/Scripts/UI/UIForm.cs:103-228` |
| B2 | **프리팹 제작** — ★ **B1의 로직 컴포넌트를 프리팹 루트에 반드시 붙인다** | `Assets/Art/Prefabs/UI/` |
| | `UIForm.cs:94-98`이 `GetComponent<UIFormLogic>()`을 하고 없으면 `Log.Error` 후 return한다. **엔티티와 정반대 규약이다**(§3-7) | |
| | `UIForm` 컴포넌트 자체는 붙이지 않아도 된다 — `DefaultUIFormHelper.cs:36`이 `GetOrAddComponent<UIForm>()` | |
| | ⚠ `Assets/Art/Prefabs/UI/HUDCanvas.prefab` / `PauseMenuCanvas.prefab`은 Sample 스크립트가 붙어 있어 **그대로 못 쓴다**(§7-1) | |
| B3 | **Addressables 등록** — §4-5와 동일 절차 | |
| B4 | **열기** — `GameEntry.GetComponent<UIComponent>().OpenUIForm("주소", "Default", userData);` | `Assets/Scripts/UI/UIComponent.cs` |
| B5 | **닫기** — `CloseUIForm(...)`. 인스턴스는 파괴되지 않고 UI 풀로 반납된다(`mInstanceExpireTime: 60`, `prefab:148`) → **엔티티와 같은 이유로 Awake/Start 대신 `OnOpen`/`OnClose`를 쓸 것** | |

### 4-7. 사운드 재생하기 (BGM을 GF로 옮기는 것 포함)

| # | 작업 |
|---|---|
| 1 | **AudioMixer 에셋을 만든다** (Assets > Create > Audio Mixer). 그룹은 최소 `Master` / `BGM` / `SFX` |
| 2 | `GameFramework` 프리팹의 **SoundComponent 인스펙터**에서 `mAudioMixer`(`Assets/Prefabs/GameFramework.prefab:546`)에 1번 에셋을 지정 |
| 3 | 같은 인스펙터의 `mSoundGroups`(`:553`)에 그룹을 추가한다. 각 그룹에 AudioMixerGroup, Mute/Volume, AgentHelper 개수를 설정 |
| 4 | `mInstanceRoot`(`:545`)에 사운드 인스턴스가 붙을 Transform 지정 (미지정 시 UIComponent와 같은 방식으로 자동 GameObject 생성) |
| 5 | 오디오 클립을 **Addressables에 등록**(§4-5) |
| 6 | `GameEntry.GetComponent<SoundComponent>().PlaySound("주소", "그룹명", ...)` |
| 7 | ★ **BGM을 옮겼다면 `Assets/Scenes/MainScene.unity:167-295`의 "Background Music" GameObject를 반드시 제거한다.** 안 그러면 이중 재생된다 |

### 4-8. 설정 / 세이브 붙이기 (배선 불필요)

| # | 작업 |
|---|---|
| 1 | 키 상수 파일을 만든다 — `Assets/GameMain/Utility/SettingKeys.cs` 신설 권장. 문자열 리터럴을 코드에 흩뿌리면 §5.1 [8]과 같은 문제가 생긴다 |
| 2 | 읽기: `GameEntry.GetComponent<SettingComponent>().GetInt(SettingKeys.HighScore, 0)` — **Load는 `Start()`에서 자동으로 끝나 있다**(`SettingComponent.cs:64-70`) |
| 3 | 쓰기: `SetInt(...)` → ★ **`Save()`를 반드시 명시 호출**(`:72`). 자동 저장은 없다 |
| 4 | 복합 객체는 `SetObject<T>/GetObject<T>`(`:162-190`). Json 헬퍼가 유효하게 설정되어 있음을 확인했다(§2-1 4단계) |
| 5 | 저장 위치는 `Application.persistentDataPath/GameFrameworkSetting.dat`(`Assets/Scripts/Setting/DefaultSettingHelper.cs:20`, `:225`). 테스트 중 초기화하려면 이 파일을 지우면 된다 |
| 6 | 헬퍼를 `PlayerPrefsSettingHelper`로 바꾸려면 프리팹의 `mSettingHelperTypeName`(`GameFramework.prefab:829`)을 수정 (§8-16) |

### 4-9. 카메라 추종 붙이기

> `Assets/Sample/Scripts/Helpers/CameraFollow.cs`는 **쓰지 않는다**(§3-10). 아래를 새로 만든다.

| # | 작업 |
|---|---|
| 1 | ★ **MainCamera의 태그를 `MainCamera`로 바꾼다** (`Assets/Scenes/MainScene.unity:296-310`, 현재 `Untagged`). 1분이면 끝나고 §5.1 [9]의 마우스 조준이 즉시 살아난다 |
| 2 | `Assets/GameMain/Camera/PlayerCameraFollow.cs`(신규)를 만들어 MainCamera에 붙인다. `Player.Instance`(`Assets/GameMain/Entity/EntityLogic/Player.cs:17`)의 `CachedTransform`을 본다 |
| 3 | `Player.Instance`는 `OnShow`(`Player.cs:36-50`)에서 채워지므로 **매 프레임 null 체크 후 early return**할 것. 캐릭터 선택 중에는 계속 null이다 |
| 4 | ★ **`LateUpdate` 또는 `FixedUpdate`에 둔다.** 플레이어 이동이 `Player.FixedUpdate`의 `Rigidbody.MovePosition`(`Player.cs:69`, `:75`)이라 `Update`에 두면 떨림이 생긴다 |
| 5 | **orthographic size 4.5**(`MainScene.unity:401-402`)는 서바이버류에 지나치게 좁다. 값 조정이 필요하다 — 다만 적정값은 게임 디자인 결정이므로 **미확인** |
| 6 | offset은 현재 카메라의 초기 위치−(0,0,0)을 그대로 상수화하면 기존 앵글이 유지된다 |

---

## 5. 규약과 함정

## 5.1 지금 당장의 위험 요소

> 심각도 순 10개. **§4의 레시피를 실행하기 전에 이 절을 통독할 것.** 대부분은 "지금은 잠복, 기능을 추가하는 순간 발현"이다.
> 각 항목 끝의 **[상태]**는 지금 실제로 터지고 있는지를 나타낸다.

### 🔴 [1] Addressables 핸들 누수 — 메인 플로우에서 100% 발생 (게다가 살아있는 에셋을 조기 Release할 수 있다)

`Assets/Scripts/Resource/ResourceManager.cs:167-168`
```csharp
if (!mAssetHandles.ContainsKey(op.Result)) mAssetHandles[op.Result] = op;
```
딕셔너리 키가 **로드 결과 오브젝트**다. 같은 프리팹은 항상 동일 인스턴스를 돌려주므로 **2회차 이후 핸들은 추적 자체가 안 된다.** 그리고 이 프로젝트의 정상 시나리오가 정확히 중복 로드다 — `Assets/GameMain/Game/SurvivalGame.cs:58`이 `"Girl"`을 로드하고, `:69`(`SpawnPlayer`)가 **같은 `"Girl"`**을 다시 로드한다(`SpawnPlayer`의 assetName == characterKey). 해제는 `HideEntity` → 풀 만료(60초) → `Assets/Scripts/Entity/DefaultEntityHelper.cs:41` `UnloadAsset` → `ResourceManager.cs:185-189`가 **추적 중인 1개만** Release. → **N번 로드 / 1번 Release.**

파생 3건:
- **실패 경로 핸들 미해제** — `ResourceManager.cs:172-176`의 else 분기에 `Addressables.Release(op)`가 없다. 잘못된 주소를 반복 요청하면 별도로 누적된다. 지금 `"Projectile"`(`Assets/GameMain/Weapon/ProjectileWeapon.cs:11`)이 미등록 주소다.
- **조기 Release 위험** — 선택용 Girl 인스턴스가 60초 뒤 만료되면 `UnloadAsset(GirlPrefab)`이 호출되는데, 이 시점에 **플레이어 인스턴스가 같은 에셋을 쓰고 있다.** 지금은 Addressables 내부 refcount가 2→1이라 살아남지만, **한쪽만 로드되는 케이스로 바뀌면 사용 중인 프리팹이 언로드된다.**
- **서로 다른 두 주소가 같은 에셋을 가리키면 키가 충돌**하고, 한쪽 `UnloadAsset`이 엔트리를 지워 나머지 핸들은 영구 미해제가 된다.
- `mAssetHandles`가 로드된 오브젝트에 강참조를 유지하므로 `ResourceComponent.cs:52-64`의 `Resources.UnloadUnusedAssets()`로도 회수되지 않는다.

**[상태] 지금 이미 새고 있다.** 짧은 플레이 세션에서는 체감되지 않을 뿐이다. 수정 방향은 §8-8.

### 🔴 [2] Girl/Boy 프리팹에 EntityLogic 2종이 baked → 컴포넌트 중복 · 이벤트 2중 발행 · NRE 지뢰

`Assets/Art/Prefabs/Characters/Girl.prefab:44636`(`ToyBoxNightmare.Player`), `:44648`(`ToyBoxNightmare.PlayerSelectLogic`) — 둘 다 루트 GameObject(fileID 7515494869112236224, `:44417-44431`, `m_IsActive: 1`, `m_Enabled: 1`)에 붙어 있다. `Assets/Art/Prefabs/Characters/Boy.prefab`도 동일(`:54140`, `:54152`).

`Assets/Scripts/Entity/Entity.cs:22`의 `mEntityLogic`은 `[SerializeField]`가 아닌 순수 private 필드이고, `Entity` 컴포넌트 자체도 프리팹에 없이 런타임에 붙는다(`DefaultEntityHelper.cs:36`). 따라서 `Entity.cs:86-96`의 "기존 로직 Destroy" 분기는 **절대 실행되지 않고**, `:98`이 무조건 `AddComponent(entityLogicType)` 한다.

| 인스턴스 | 실제 컴포넌트 |
|---|---|
| 선택 화면 Girl | Player(baked) + PlayerSelectLogic(baked) + **PlayerSelectLogic(런타임)** |
| 스폰된 Player | Player(baked) + PlayerSelectLogic(baked) + **Player(런타임)** |

`Entity.Logic`은 **언제나 런타임에 붙은 것**을 가리킨다. baked된 쪽은 `OnInit`/`OnShow`가 불리지 않아 `Entity == null`, `CachedTransform == null`, `mPlayerData == null` 상태로 남는다.

구체적 사고:
1. **이벤트 2중 발행** — `Assets/GameMain/Entity/EntityLogic/PlayerSelectLogic.cs:46-54` `OnMouseUp`이 클릭 1회에 **2번** 실행된다. `ReferencePool.Acquire`가 2회, `CharacterSelectedEventArgs`가 2번 Fire. 지금 크래시하지 않는 유일한 이유는 `EventPoolMode.AllowNoHandler`(`External/GameFramework/GameFramework/Event/EventManager.cs:19`)라 두 번째가 조용히 삼켜지기 때문이다 → §5.1 [7]과 직결.
2. **NRE 지뢰** — `Assets/GameMain/Entity/EntityLogic/TargetableObject.cs:76`
   ```csharp
   if (otherEntity.Logic is TargetableObject && otherEntity.Id >= Entity.Id)
   ```
   `OnTriggerEnter`는 Unity가 **GameObject의 모든 컴포넌트에** 보내므로 baked `Player`에서도 실행되고, 거기서 `Entity`는 null이다. 앞 조건이 true일 때 **NullReferenceException**. 적/투사체에 트리거 콜라이더를 붙이는 순간(=적 스폰을 되살리는 순간) 발현된다.
3. 스폰된 플레이어를 클릭해도 baked `PlayerSelectLogic.OnMouseUp`이 살아 있어 이벤트가 또 발행된다.
4. `Girl.prefab:44652` / `Boy.prefab:54159`의 `otherCharacter` 필드는 현재 `PlayerSelectLogic.cs:17-20`에 없는 **stale 직렬화 데이터**(Sample `Assets/Sample/Scripts/Player/PlayerSelect.cs` 잔재)다. 값은 Boy.prefab을 가리키는 live 참조다.

**[상태] 이벤트 2중 발행은 지금 이미 일어나고 있다.** NRE는 잠복. 이 원칙("프리팹에 EntityLogic 금지")은 **에셋에서 100% 지켜지지 않고 있다** — `Assets/Prefabs/Enemy.prefab:44,46`과 `Assets/Prefabs/ExpGem.prefab:44,46`도 같은 상태다. 수정은 §8-6.

### 🔴 [3] 위 문제들이 전부 무음이다 — `Log.*`가 통째로 컴파일 제거

`ProjectSettings/ProjectSettings.asset:823` → `scriptingDefineSymbols: {}` (csc.rsp 없음)
`Assets/Scripts/Utility/Log.cs:26-28` → `[Conditional("ENABLE_LOG")] [Conditional("ENABLE_DEBUG_LOG")] [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]` (총 101개 attribute, public 메서드 20개 전부)

여기에 `Assets/Scripts/Entity/Entity.cs:105-112`, `:133-141`, `:209-216`이 EntityLogic의 `OnInit`/`OnShow`/`OnUpdate` 예외를 **try/catch로 삼킨 뒤 `Log.Error`로만 보고**한다.

> **결과: 엔티티 로직에서 NRE가 나도 콘솔에 아무것도 안 찍히고 그냥 초기화가 중단된다.**

침묵하는 대표 진단:

| 메시지 | 위치 |
|---|---|
| "Can not find procedure type '...'." | `Assets/Scripts/Procedure/ProcedureComponent.cs:68` |
| "Game Framework component type '...' is already exist." | `Assets/Scripts/Base/GameEntry.cs:110` |
| 엔티티 로드 실패 경고 | `Assets/GameMain/Game/SurvivalGame.cs:129` |
| "UI form '...' can not get UI form logic." | `Assets/Scripts/UI/UIForm.cs:97` |
| Entity try/catch가 삼킨 예외 전부 | `Assets/Scripts/Entity/Entity.cs:105-112` 등 |

**나머지 9개 항목의 재현·확인 비용이 전부 여기에 걸려 있다.** 절차는 §0-1.
(참고: DLL 내부의 `GameFrameworkLog`에는 Conditional이 없어 코어가 직접 찍는 로그는 나온다.)

**[상태] 지금 100% 침묵 중.**

### 🔴 [4] ObjectPool 규약 위반 — `HideEntity` 이중 호출은 예외를 던진다

`External/GameFramework/GameFramework/Entity/EntityManager.cs:505-521`
```csharp
EntityInfo entityInfo = GetEntityInfo(entityId);
if (entityInfo == null) throw new GameFrameworkException("Can not find entity '{0}'.");
```
`InternalHideEntity`(`:887-926`)가 `mEntityInfos.Remove` 후 recycle 큐에 넣으므로 **같은 엔티티에 HideEntity를 두 번 부르면 두 번째는 예외**다. (External 소스 기준 추론 — DLL 일치 여부는 §1-3의 이유로 미확인이나, 계약 자체는 GF 표준과 동일하다.)

현재 코드에 이미 이중 호출 경로가 박혀 있다:

| 경로 | 위치 | 조건 |
|---|---|---|
| 투사체 수명 만료 vs 트리거 명중 | `Assets/GameMain/Entity/EntityLogic/Projectile.cs:37`, `:52` | `OnTriggerEnter`는 한 물리 스텝에 여러 콜라이더에 대해 호출된다 → **적 2마리를 동시에 뚫으면 즉시 두 번** |
| 사망 연출 코루틴 | `Assets/GameMain/Entity/EntityLogic/PlayerSelectLogic.cs:64-68` `HideAfterDelay` | 1.5초 대기 중 다른 경로로 Hide되면 동일 예외 |
| 경험치 보석 수집 | `Assets/GameMain/Entity/EntityLogic/ExpGem.cs:45` | 같은 패턴 |

**세 곳 모두 `mHidden` 같은 가드가 전혀 없다.**

**[상태] 지금은 Projectile/ExpGem 스폰 경로가 죽어 있어 잠복.** §4-1/§4-2 레시피대로 적·투사체를 되살리는 **첫날에 터진다.** → §4-1 3단계의 가드를 **먼저** 넣을 것.

### 🔴 [5] `ProcedureComponent`가 프리팹에 2개, 한쪽은 타입명이 깨져 있고 등록 순서가 비결정적

| 위치 | fileID | 값 | 상태 |
|---|---|---|---|
| 자식 GO "Procedure" (`Assets/Prefabs/GameFramework.prefab:679-693`, 값은 `:691-693`) | 452741662902524506 | `ToyBoxNightmare.ProcedureMain` | ✅ 실제 동작 |
| 루트 GO "GameFramework" (`Assets/Prefabs/GameFramework.prefab:770-784`, 값은 `:782-784`) | 7401751991573398765 | `ProcedureMain` (**네임스페이스 누락**) | ❌ 항상 실패 |

- `Utility.Assembly.GetType`은 `Type.GetType` 기반이라 네임스페이스 없는 이름은 **확정적으로 null** → `ProcedureComponent.cs:68-69`에서 `Log.Error` 후 `yield break`. `:91`의 `Initialize`에는 절대 도달하지 못한다. **따라서 FSM 중복 생성 경합은 없고 그 부분은 결정론적이다.**
- 문제는 `GameEntry.RegisterComponent`(`Assets/Scripts/Base/GameEntry.cs:95-118`)가 **둘 중 먼저 Awake된 쪽만 등록하고 나머지를 `Log.Error` 후 거부**한다는 것이다(`:110`). **어느 쪽이 등록되는지는 Awake 순서에 달려 비결정적**이고(`ProjectSettings/MonoManager.asset` 없음), 그 거부 로그마저 §5.1 [3] 때문에 안 보인다.
- `[DisallowMultipleComponent]`(`ProcedureComponent.cs:19`)는 **같은 GameObject 안에서만** 막으므로 이 배치를 걸러내지 못한다.
- `Assets/Scenes/MainScene.unity:735-742`의 프리팹 오버라이드는 **값이 원본과 동일한 무의미 오버라이드**다. 씬에서 되돌려도 해결되지 않는다 — **고칠 지점은 프리팹 루트의 컴포넌트 자체다.**

**[상태] 지금은 `GetComponent<ProcedureComponent>()` 호출이 0건이라 무해.** `CurrentProcedure`를 읽는 코드를 추가하는 순간 **죽은 쪽 인스턴스를 잡아 원인 불명의 null**이 된다. 수정은 §8-3(5분).

### 🔴 [6] 빌드하면 프레임워크가 아예 없고, "재시작"은 프레임워크 영구 소멸이다

`ProjectSettings/EditorBuildSettings.asset:7-10` — enabled 씬이 `Assets/Scenes/SampleScene.unity` **하나뿐**이고 `Assets/Scenes/MainScene.unity`는 목록에 없다.

- 지금 빌드하면 빈 URP 템플릿 씬이 뜨고 GameFramework가 기동조차 안 한다.
- `Assets/Scripts/Base/GameEntry.cs:79-83` `Shutdown(ShutdownType.Restart)` → `SceneManager.LoadScene(0)` → **SampleScene**. GameFramework 프리팹에 `DontDestroyOnLoad`가 없어(`Assets` 전체 0건) Single 모드 로드가 `BaseComponent.OnDestroy`(`:211-213`) → `GameFrameworkEntry.Shutdown()`을 유발한다.
  → **재시작 = 복구 불가능한 종료.** 호출부가 `Assets/Scripts/Debugger/DebuggerComponent.OperationsWindow.cs:57`(디버거 UI)이라 **QA가 실수로 누르기 쉽다.**

**[상태] 에디터 개발에는 영향 없음, 빌드·재시작에서 100% 발현.** 수정은 §8-2.

### 🟠 [7] 이벤트 구독/해제 비대칭 — 프로시저 재진입 시 확정 예외

`Assets/GameMain/Game/SurvivalGame.cs:31-34`에서 3종 구독, `:41-50`에서 **2종만 해제**(`:46` 주석이 `CharacterSelectedEventArgs`는 "이미 해제됨"을 전제).

- **캐릭터를 고르지 않고 종료하면** `OnCharacterSelected` 핸들러가 좀비로 남는다. 지금은 GF Shutdown이 `EventPool.Shutdown`으로 전부 Clear하므로 덮이지만, **프로시저 전이(§4-3)를 도입하면 그대로 노출된다.**
- `EventPoolMode`에 `AllowDuplicateHandler`가 **없다**(`External/GameFramework/GameFramework/Event/EventManager.cs:19`). `ProcedureMain.OnEnter`(`Assets/GameMain/Procdure/ProcedureMain.cs:22`)가 두 번 실행되면 `new SurvivalGame()`이 같은 (id, handler)를 재구독 → **즉시 `GameFrameworkException`.**
- 반대로 `EventPool.Unsubscribe`는 미등록 핸들러에 대해 `"Event '{0}' not exists specified handler."` 예외를 던진다. `SurvivalGame.cs:84-85`의 self-unsubscribe가 **두 번 실행되면 크래시**인데, §5.1 [2]의 이벤트 2중 발행이 정확히 그 조건을 만든다.
  지금 살아있는 유일한 이유는 두 번째 이벤트가 **핸들러 목록이 빈 상태로 도착해** `HandleEvent`의 `TryGetValue` 분기를 타고 조용히 Release되기 때문이다 — **매우 얇은 우연**이다. `EventPoolMode`를 손대거나 self-unsubscribe를 옮기는 순간 깨진다.

**[상태] 잠복. §4-3(프로시저 전이) 도입 시 확정 발현.**

### 🟠 [8] 하드코딩 문자열/주소가 전부 런타임에만 터지고, 그 로그도 침묵한다

| 문자열 | 위치 | 틀렸을 때 |
|---|---|---|
| `"Girl"` / `"Boy"` (Addressable 주소) | `SurvivalGame.cs:10-11`, `:57`, `:72` | 조용히 `ShowEntityFailure` → 로그 침묵(§5.1 [3]) |
| `"Player"` (엔티티 그룹명) | `SurvivalGame.cs:62`, `:73` | 코어에서 `GameFrameworkException` |
| `"Projectile"` (주소·그룹) | `ProjectileWeapon.cs:11`, `:36` | **주소가 `Prefabs.asset`에 미등록** — 등록된 주소는 10개뿐(§3-4) |
| `"Die"` / `"IsWalking"` (Animator 파라미터) | `PlayerSelectLogic.cs:60`, `Player.cs:85` | 무음 무동작. 정의는 `Assets/Art/Animations/BoyAnimatorController.controller:11`, `:17`이고 Girl은 override controller로 **경로가 갈라져 있다**(§4-1 5단계) |
| `"ToyBoxNightmare.ProcedureMain"` | `Assets/Prefabs/GameFramework.prefab:693` | 클래스 rename 시 컴파일 에러 없이 런타임 사망(§5.1 [5]) |
| 헬퍼 타입명 6종 | `GameFramework.prefab:765`, `:829` 등 | 리플렉션 생성 실패 → 해당 서브시스템 전체 무동작 |

특히 `SurvivalGame.cs:57`의 `assetPath == GirlSelectAssetPath ? "Girl" : "Boy"`는 assetPath와 characterKey가 애초에 같은 값이라 **무의미한 삼항**인데, 이것이 "주소와 캐릭터 키가 별개"라는 착각을 유도한다. 실제로는 `SpawnPlayer(characterKey)`가 그 값을 **그대로 Addressable 주소로** 쓴다(`:72`) — §5.1 [1]의 중복 로드 원인이다.

**[상태] 상시. 리팩터링할 때마다 새로 발생할 수 있는 종류의 위험.**

### 🟠 [9] 씬-코드 결합 — `Camera.main`이 null이라 마우스 조준이 죽어 있다

`Assets/Scenes/MainScene.unity:296-311` — GameObject 이름만 "MainCamera"이고 `m_TagString: Untagged`다. 씬/Environment/Lighting/Floor Collider 어디에도 `MainCamera` 태그가 없다.

결과: `Assets/GameMain/Entity/EntityLogic/Player.cs:111`
```csharp
if (Camera.main == null || Mouse.current == null)
    return CachedTransform.position + CachedTransform.forward;
```
가 **항상 fallback**을 타서 마우스 조준 회전이 전혀 동작하지 않는다. `Assets/GameMain/Weapon/WeaponBase.cs:86-97`도 동일. 캐릭터 선택 클릭은 `OnMouseUp`이 `Camera.allCameras` 기반이라 계속 되므로, **증상이 "회전만 안 됨"으로만 나타나 원인 추적이 어렵다.**

부가 씬 결합:
- 카메라 추종 스크립트 없음 + orthographic size 4.5 고정 → 플레이어가 화면 밖으로 나간다(§3-10, §4-9)
- `PlayerData`에 Position을 안 넣어(`SurvivalGame.cs:74`) 플레이어가 항상 월드 원점에 뜬다(`EntityData.cs:16`)
- MainScene에 EventSystem이 없어 `PlayerSelectLogic.cs:49`의 가드가 항상 스킵된다. **UI Canvas를 추가하는 순간 클릭 동작이 바뀐다**(§4-6 A2)

**[상태] 지금 발현 중.** 수정은 태그 1개(§4-9 1단계).

### 🟠 [10] 커밋 88408fd가 남긴 반쪽 상태와 프리팹 지뢰

**(a) Missing Script 2건** — 스크립트 파일은 지웠는데 프리팹의 guid 참조가 남았다.

| 파일 | 참조 guid | 원본 |
|---|---|---|
| `Assets/Prefabs/Player.prefab:44` | `87685fd660623094d86f861ce737bde6` | 삭제된 `Assets/GameMain/Entity/EntityLogic/LostToy.cs` (fa2f999) |
| `Assets/Prefabs/UpgradeForm.prefab:48` | `72d058521a1bd5a44910d4d31a77fce9` | 삭제된 `Assets/GameMain/UI/UpgradeForm.cs` (88408fd) |

**(b) 삭제된 시스템을 부르는 죽은 코드가 그대로** — **주석만 풀면 컴파일도 안 된다.**
`Enemy.cs:64` `//player.TakeDamage(...)` / `Enemy.cs:71` `//SurvivalGame.Instance?.SpawnExpGem(...)` / `ExpGem.cs:44` `//SurvivalGame.Instance?.LevelSystem.AddExp(...)` / `TargetableObject.cs:82` `//AIUtility.PerformCollision(...)`. `Player.TakeDamage`, `SpawnExpGem`, `LevelSystem`, `AIUtility` 전부 존재하지 않는다.

**(c) NRE 확정 메서드가 public으로 남음** — `Assets/GameMain/Entity/EntityLogic/Player.cs:125-128` `UpgradeMoveSpeed`가 `:127`에서 `mPlayerData`를 null 체크 없이 접근한다. 호출자는 0건(UpgradeSystem 삭제)이지만 public이라 누구든 부를 수 있다.

**(d) Sample 공격 스크립트 5종이 Girl/Boy 프리팹에 살아 있다** — Missing Script도 죽은 오브젝트도 아니다. 실제 코드가 `Assets/Sample/Scripts/Attacks/`에 있고 런타임에 로드된다.

| 스크립트 | Girl.prefab 줄 | GameObject 활성 | 파라미터 |
|---|---|---|---|
| `LightningBolt` | `:5282` | `m_IsActive: 0` | |
| `LightningAttack` | `:17053` | **`m_IsActive: 1`** | damage 50, range 20, Cooldown 1 |
| `StinkAttack` | `:17425` | `m_IsActive: 0` | Cooldown 5, range 9 |
| `SlimeAttack` | `:37847` | `m_IsActive: 0` | Cooldown 3.5 |
| `FrostAttack` | `:55228` | `m_IsActive: 0` | maxFreezableEnemies 20 |

⚠ `Assets/Sample/Scripts/Attacks/FrostAttack.cs:29-43`
```csharp
void Awake() { for(int i = 0; i < maxFreezableEnemies; i++) Instantiate(frostDebuffPrefab); }
```
**누군가 인스펙터에서 이 GameObject를 켜는 순간 캐릭터 선택 화면에서 프리팹 20개가 조용히 Instantiate된다.** Addressables 밖이라 `ReleaseInstance`도 없다 → 순수 누수. 구동자인 `PlayerAttack`이 프리팹에서 제거되어 `Fire()`를 부르는 코드가 없기 때문에 지금은 동작하지 않을 뿐이다.

**(e) Rigidbody damping이 Infinity** — `Assets/Art/Prefabs/Characters/Girl.prefab:44489-44490`의 `m_LinearDamping: Infinity`, `m_AngularDamping: Infinity`, `m_IsKinematic: 0`, `m_Constraints: 80`. 이를 0으로 되돌리는 유일한 코드가 `PlayerSelectLogic.cs:71-74` `DeathComplete`인데 **private + Animation Event 미연결이라 호출되지 않는다.** 현재 이동이 `Rigidbody.MovePosition`(`Player.cs:75`)이라 굴러갈 뿐, **`AddForce`/`velocity` 기반으로 바꾸는 순간 캐릭터가 즉시 정지한다.**

**[상태] (a)(b)(c)는 무해한 잔해, (d)(e)는 인스펙터 조작 한 번으로 발현되는 지뢰.**

### 착수 순서 권고

> 위 10개를 한꺼번에 고치려 하지 말 것. 아래 순서가 총 비용이 가장 낮다.

| # | 작업 | 소요 | 없애는 항목 |
|---|---|---|---|
| 1 | **`ENABLE_LOG` 계열 define 켜기** (§0-1) | 5분 | [3] — 나머지 9개의 진단 비용을 한 번에 낮춘다 |
| 2 | **Girl/Boy 프리팹에서 baked `Player`/`PlayerSelectLogic` 제거** | 15분 | [2] 전체 + [7]의 트리거 조건. 직렬화 필드는 `PlayerSelectLogic.OnInit`(`:24-30`)이 `GetComponent`로 다시 채우므로 **기능 손실 없음.** 같은 작업에서 Sample 공격 5종(§5.1 [10-d])도 함께 판단 |
| 3 | **`ResourceManager`를 `assetName` 기준 참조 카운트 + 핸들 캐시로 교체** + 실패 경로에 `Addressables.Release(op)` 추가 | 반나절 | [1]. 동시에 `SpawnPlayer`가 선택 프리팹과 같은 주소를 재로드하는 구조를 재검토(§8-7) |
| 4 | **MainScene을 빌드 씬 0번으로 등록 / 프리팹 루트의 중복 `ProcedureComponent` 삭제 / MainCamera 태그 지정** | 각 5분 | [6] · [5] · [9]를 통으로 제거 |
| 5 | **Projectile/ExpGem을 되살리기 전에 `HideEntity` 1회 보장 가드 추가** (§4-1 3단계) | 30분 | [4] |

## 5-2. 프레임워크 규약 (어기면 생기는 문제)

| 규약 | 어기면 | 근거 |
|---|---|---|
| `Acquire<T>()`한 객체는 정확히 한 번 `Release`. Release는 내부에서 `Clear()`를 먼저 호출 | `Clear()`에 상태 초기화를 빠뜨리면 다음 Acquire가 **이전 프레임 데이터를 물고 나온다**. 이중 Release는 strict check로 즉시 예외 | `ReferencePool.ReferenceCollection.cs:68`, `:71-74` |
| 엔티티 제거는 항상 `EntityComponent.HideEntity`. Destroy/SetActive 직접 호출 금지 | 풀 관리가 깨져 인스턴스 누수 | `TargetableObject.cs:64`, `Projectile.cs:37/52` |
| **같은 엔티티에 `HideEntity`는 정확히 1회** | `GameFrameworkException` | §5.1 [4] |
| `EntityLogic` 오버라이드는 `protected internal override` | 컴파일 에러 | `Player.cs:29` |
| **엔티티 프리팹에 `EntityLogic`/`Entity`를 붙이지 않는다** | 컴포넌트 중복, 이벤트 다중 발행, NRE | §5.1 [2] |
| **UI 폼 프리팹에는 `UIFormLogic`을 반드시 붙인다** (엔티티와 정반대) | 폼은 열리지만 콜백이 하나도 안 돈다 | `Assets/Scripts/UI/UIForm.cs:94-98` |
| `EntityLogic`/`UIFormLogic`에서 Unity Awake/Start 사용 금지 | 풀링 재사용이라 Awake는 인스턴스당 한 번뿐 → 두 번째 스폰부터 초기화 누락 | |
| 새 `~Component`는 `GameFrameworkComponent` 상속 + `protected override void Awake() { base.Awake(); ... }` | base.Awake()를 빼면 GameEntry에 등록되지 않아 `GetComponent<T>()`가 영원히 null | `GameFrameworkComponent.cs:16-19` |
| 컴포넌트 참조는 Awake가 아니라 **Start**에서 잡는다 | Awake 순서가 보장되지 않아 null | `EntityComponent.cs:98-113` |
| `GameEntry.GetComponent<T>()`는 실패해도 **예외를 안 던지고 null을 준다** | 조용한 NullReferenceException. 실제로 IResourceManager 주입 7곳 전부 null 체크가 없다(§2-1 6단계) | `GameEntry.cs:29-43` |
| 같은 (id, handler)를 두 번 Subscribe하지 않는다 / 미등록 핸들러를 Unsubscribe하지 않는다 | 즉시 예외 | §5.1 [7] |
| private 필드는 `m` + PascalCase (GF 래퍼 계층) | 프리팹 직렬화 필드명을 바꾸면 **인스펙터 값이 전부 날아간다** | `BaseComponent.cs`의 `mFrameRate` 등 |

## 5-3. 새 GameFramework 모듈은 만들 수 없다

`GameFrameworkModule`은 `internal abstract`(`External/GameFramework/GameFramework/Base/GameFrameworkModule.cs:11`)라 Assembly-CSharp에서 상속 불가이고, `GetModule<T>`는 (a) T가 인터페이스여야 하고 (b) FullName이 `GameFramework.`로 시작해야 하며 (c) `Type.GetType`이 호출 어셈블리(GameFramework.dll) 기준으로만 해석된다(`GameFrameworkEntry.cs:40-55`).

> **새 전역 시스템이 필요하면 `GameFrameworkComponent`를 상속한 MonoBehaviour로 만들어 `Assets/Prefabs/GameFramework.prefab`에 자식 GameObject로 추가하고, `GameEntry.GetComponent<T>()`로 접근하는 것이 이 프로젝트의 유일한 확장 경로다.**

적 스포너를 별도 시스템으로 뺄 경우(§8-11) 이 경로를 쓴다. 이때 §5-2의 `base.Awake()` 규약과 "참조는 Start에서" 규약을 반드시 지킬 것.

## 5-4. 그 밖의 자잘한 함정 (알아두면 시간을 아끼는 것들)

- **폴더명 오타**: `Assets/GameMain/Procdure/` (Procedure 아님)
- **네임스페이스 대소문자**: `ToyBoxNightmare` (폴더명 ToyboxNightmare와 다름 — B가 대문자)
- **필드 네이밍이 균일하지 않다**: GameMain의 `[SerializeField]` 필드는 접두사 없는 camelCase(`characterKey`, `attackInterval`, `damage`), EntityData 파생만 `m` 접두사(`mMaxHP`, `mMoveSpeed`)
- **인코딩 깨짐(mojibake)**: `EntityData.cs:28,39,50,65`, `TargetableObject.cs:45-46,75,81`. **편집 시 인코딩을 건드리면 diff가 폭발한다.** 반대로 `Player.cs`, `Enemy.cs` 등은 UTF-8 정상
- **stale 주석 3건**: `WeaponBase.cs:11-13`(삭제된 AreaWeapon/Lightning/Frost/Stink/Slime을 가리킴) / `PlayerSelectLogic.cs:13`(Addressables 키를 `"GirlSelect"`/`"BoySelect"`라 하지만 실제는 `"Girl"`/`"Boy"`) / `Enemy.cs:16`("FrostWeapon이 사용한다"고 하지만 FrostWeapon은 삭제됨)
- **`EntitySerialId`에 리셋 API가 없다**(`Assets/GameMain/Utility/EntitySerialId.cs:9-11`). 프로시저 재진입/게임 재시작에도 ID가 계속 증가한다(§4-3, §8-12)
- **`TypeId`가 전부 하드코딩 `1`**(`SurvivalGame.cs:63`, `:74`, `ProjectileWeapon.cs:37`). `EntityData.cs:39` 주석은 "DataTable RowId"라 하지만 프로젝트에 DataTable 사용이 0건이다
- **`PlayerData`/`EnemyData`의 `[SerializeField]`는 인스펙터로 조절할 수 없다**(§4-1 1단계)
- **물리 모델이 혼재한다**: Enemy는 Transform 직접 이동(`Enemy.cs:54`), Player는 `Rigidbody.MovePosition`(`Player.cs:75`)
- **ExpGem은 콜라이더가 아니라 매 프레임 거리 계산으로 수집 판정**(`ExpGem.cs:41`) — 개수가 늘면 O(n) 부하
- **`Assets/Scripts` 하위에 Editor 폴더가 없다** → 원본 GF의 커스텀 인스펙터가 전부 제거되어 프리팹 인스펙터에 raw 필드명(`mEnableOpenUIFormSuccessEvent` 등)이 그대로 노출된다. **프리팹을 인스펙터로 편집할 때는 이 문서의 `GameFramework.prefab:줄번호` 표를 참조할 것**
- **동순위 모듈의 Update 순서는 정해져 있지 않다** (§2-2 핵심 3)

---

## 6. 현재 상태: 있는 것 / 없는 것 / 만들다 만 것

### 6-0. 서브시스템 존재 여부 한눈에

> "코드가 있는가"와 "실제로 동작하는가"는 다르다. 아래 표의 **판정** 열이 후자다.

| 서브시스템 | 코드 | 배선 | 판정 | 상세 |
|---|---|---|---|---|
| 프로시저 / FSM | ✅ | ✅ (상태 1개) | **동작** — 전이는 없음 | §3-1 |
| 엔티티 | ✅ | ✅ | **동작** | §3-2 |
| 이벤트 | ✅ | ✅ | **동작** | §3-5 |
| 에셋 로딩 (Addressables) | ✅ | ✅ | **동작하나 누수 있음** | §3-4, §5.1 [1] |
| 설정 / 세이브 | ✅ | ✅ | **동작 (사용 0건)** — 오늘 바로 쓸 수 있다 | §3-9, §4-8 |
| 입력 | ✅ | ✅ | **동작하나 3중 경로 혼재** | §3-6 |
| 무기 / 전투 | ✅ | ❌ | **고아 코드** — 부착 경로 0건 | §3-3 |
| 사운드 | ✅ | ❌ | **미배선** — `mAudioMixer`/`mSoundGroups` 비어 있음. BGM은 프레임워크 밖 순수 AudioSource | §3-8 |
| UI | ✅ (프레임워크만) | ❌ | **미배선 + UIForm 0개** — Canvas/EventSystem도 없음 | §3-7 |
| 카메라 | ❌ | ❌ | **없음** — Cinemachine 미설치, 추종 스크립트 없음, `Camera.main`이 null | §3-10, §5.1 [9] |
| 적 스폰 / 웨이브 | ❌ | ❌ | **없음** — `SpawnEnemy` 메서드 자체가 삭제됨 | §6-1 #5, #6 |
| 레벨 / 경험치 | ❌ | ❌ | **없음** — `LevelSystem` 파일 삭제(88408fd) | §6-1 #20 |
| 업그레이드 | ❌ | ❌ | **없음** — `UpgradeSystem`/`UpgradeDefinition` 파일 삭제(88408fd) | §6-1 #21 |
| 게임오버 / 재시작 | ❌ | ❌ | **없음** — `GameOver`를 true로 만드는 코드 0건 | §4-3 ★ |
| 데이터 테이블 / 외부 튜닝 데이터 | ✅ (프레임워크만) | ❌ | **없음** — DataTable/ScriptableObject/JSON 참조 0건. 튜닝값 100% C# const | §6-3 |
| 로컬라이제이션 | ✅ (프레임워크만) | ❌ | **없음** — 사용 0건 | §7-4 |
| 네트워크 / 다운로드 / 웹요청 | ✅ (프레임워크만) | ❌ | **없음** — 사용 0건 | §7-4 |
| 씬 관리 (`SceneComponent`) | ✅ | ❌ | **없음** — Addressable에 씬이 0개라 로드 자체가 실패 | §3-4 |
| asmdef 분리 | — | — | **없음** — 프로젝트 전체 0개 | §1-3, §8-19 |

### 6-1. `GameCoreLoop.md` 대조표

`GameCoreLoop.md`(546줄)는 **2026-03-09 커밋 84ce872 시점의 회고 문서**다. 미래 계획서가 아니라 과거 스냅샷이며, 이후 3개 커밋(afd656b, fa2f999, 88408fd)에서 방향이 크게 바뀌어 절반 이상이 무효다.

| # | 문서 주장 | 실제 | 판정 |
|---|---|---|---|
| 1 | ProcedureMain이 SurvivalGame 생성/Update/Shutdown | 그대로 | ✅ 구현됨 |
| 2 | GameMode enum = Survival 하나 | 그대로 (단 읽는 코드 없음) | ✅ 구현됨 |
| 3 | GameBase 추상 클래스 구조 | 명세 7개 멤버 전부 존재, **100% 일치** | ✅ 구현됨 |
| 4 | SurvivalGame.Initialize가 이벤트 **5종** 구독 + LevelSystem/UpgradeSystem 생성 + SpawnPlayer | 이벤트 **3종**만 구독. LevelSystem/UpgradeSystem 삭제. SpawnPlayer 대신 SpawnSelectCharacter 2개 | 🔄 대체됨 |
| 5 | SurvivalGame.Update가 생존시간 누적 + 스폰간격 단축 + 적 스폰 | **Update 오버라이드 자체가 없음** | ❌ 삭제됨 |
| 6 | 적 최대 50 / 반경 15f / 3→0.5초 스폰 | `SpawnEnemy` 메서드 없음. Enemy 스폰 코드 0건 | ❌ 삭제됨 |
| 7 | `HideEntityCompleteEventArgs`로 적 수 동기화 | 구독 없음, mEnemyIds 없음 | ❌ 삭제됨 |
| 8 | `GameOverEventArgs` / `LevelUpEventArgs` | 파일 삭제(88408fd) | ❌ 삭제됨 |
| 9 | TargetableObject: HP 0 → OnDead → HideEntity | **문서대로 동작한다** | ✅ 구현됨 |
| 10 | TargetableObject.OnTriggerEnter 충돌 처리 | `:82` 주석. 조기 return 로직만 남은 no-op | ⚠ 만들다 만 것 |
| 11 | EntityData 계층 및 수치(100/5f, 30/2f/10/5, 5/4f) | 4개 파일 존재, 수치도 정확히 일치 | ✅ 구현됨 |
| 12 | Player: `Input.GetAxisRaw` WASD 이동 | 신 InputSystem `Keyboard.current` + Rigidbody로 전면 재작성 | 🔄 다르게 구현 |
| 13 | `Player.AttachWeapon<T>()` | 메서드 자체 없음. 프로젝트 전체 `AttachWeapon` 0건 | ❌ 삭제됨 |
| 14 | `Player.TakeDamage` / `HealHitPoints` | 둘 다 없음. `UpgradeMoveSpeed`만 남고 호출자 0건 | ❌ 삭제됨 |
| 15 | Player.OnDead 오버라이드 → GameOver 발행, HideEntity 안 함 | 오버라이드 없음 → base가 그대로 HideEntity (**문서와 정반대**) | ❌ 삭제됨 |
| 16 | Enemy: 추적, 사거리 1.5f, 1초마다 데미지 10 | 추적/사거리/타이머 동작, **데미지 호출 주석** | ⚠ 만들다 만 것 |
| 17 | Enemy.OnDead → SpawnExpGem | **주석 처리** | ⚠ 만들다 만 것 |
| 18 | ExpGem: 반경 5f 자석, 0.5f 수집 시 AddExp | 자석 동작, **AddExp 주석** → 먹어도 경험치 0 | ⚠ 만들다 만 것 |
| 19 | Projectile: 직선 이동/수명/충돌 데미지 | 코드는 완성형, 스폰 경로가 죽어 도달 불가 | 🚫 도달 불가 |
| 20 | LevelSystem (Level/Exp/RequiredExp) | 파일 삭제(88408fd, -29줄) | ❌ 삭제됨 |
| 21 | UpgradeSystem + UpgradeDefinition + 업그레이드 6종 | 파일 전부 삭제(-73, -24줄) | ❌ 삭제됨 |
| 22 | WeaponBase: Player에 AddComponent, 간격마다 Attack | 클래스/로직 그대로, **부착·Initialize 호출자 0건** | 🚫 고아 코드 |
| 23 | ProjectileWeapon 플레이어 스폰 직후 자동 부착 | 부착 코드 없음, addressable `"Projectile"` 미등록 | ❌ 미구현 |
| 24 | AreaWeapon | 파일 삭제(-28줄) | ❌ 삭제됨 |
| 25 | UpgradeForm / UpgradeFormData / UpgradeItemUI, UI 그룹 "Default" | 3개 파일 삭제, `Assets/GameMain/UI/` 빈 폴더, `mUIGroups: []`, Canvas/EventSystem 없음 | ❌ 삭제됨 |
| 26 | EntitySerialId 전역 ID 생성기 | 문서와 완전 동일 | ✅ 구현됨 |
| 27 | §7 전체 루프 다이어그램 | 4~7단계(적 스폰/전투/보석/레벨업/업그레이드/게임오버) 전부 미실행 | ❌ 문서 무효 |
| 28 | §8 "신규 파일 26개" 목록 | 그중 9개 삭제됨 | ❌ 문서 무효 |
| — | **문서에 없는 신규**: 캐릭터 선택(Boy/Girl) 흐름 | `PlayerSelectLogic`, `CharacterSelectData`, `CharacterSelectedEventArgs` 추가 (fa2f999) | ➕ 문서 미기재 |
| — | **문서에 없는 신규**: 무기 4종 Frost/Lightning/Slime/Stink | fa2f999에서 추가 → 88408fd에서 전부 삭제 | ➕→❌ |
| — | **문서에 없는 신규**: Background Music | MainScene에 순수 AudioSource GameObject (8de9a6e) | ➕ 문서 미기재 |

**요약: 문서대로 살아있는 것 6 / 만들다 말았거나 도달 불가 6 / 삭제·미구현 15.**
**`GameCoreLoop.md`는 이제 역사 기록으로만 유효하다. 현재 코드베이스의 설계 문서로 신뢰해선 안 된다**(§8-21).

### 6-2. 커밋이 보여주는 방향

```
84ce872 (2026-03-09)  뱀서라이크 코어 루프 일괄 구현 + GameCoreLoop.md 작성 (47 files, +1554/-58)
afd656b (03-29)       AssetBundle→Addressables 전환, GameFramework 프리팹 대개편(1657줄), _Recovery 실수 커밋
fa2f999 (03-29)       캐릭터 선택 시스템 도입 + 무기 4종 추가 + Boy/Girl 프리팹 대규모 교체 (+136268/-25131)
                      ↳ 이때 웨이브/스폰/난이도/생존타이머/게임오버가 SurvivalGame.cs에서 -108줄로 삭제
8de9a6e (03-29)       BGM 추가
88408fd (2026-05-29)  업그레이드/무기/UI 시스템 대량 삭제 (29 files, +11/-811)
```

즉 **"뱀서라이크 자동 성장"에서 "캐릭터 선택 + 수동 조작 액션"으로 방향을 틀면서 기존 축을 걷어냈고, 새 축은 아직 채워 넣지 않은 상태**다. 마지막 커밋 이후 워킹트리는 clean이다.

참고로 fa2f999 이전 SurvivalGame의 튜닝값: `SpawnRadius 15f`, `MaxEnemyCount 50`, `InitSpawnInterval 3f`, `mSpawnInterval = Mathf.Max(0.5f, InitSpawnInterval - mSurvivalTime * 0.05f)`.
**`git show fa2f999^:Assets/GameMain/Game/SurvivalGame.cs`로 복원 가능하다.** 적 스폰을 되살릴 때 이 곡선만 가져오는 것을 권장한다(§8-11).

### 6-3. 현재 튜닝 파라미터 위치 (100% C# const / `[SerializeField]` 기본값)

DataTable / ScriptableObject / JSON 등 외부 데이터 소스를 GameMain 코드가 읽는 곳은 **없다.**

| 값 | 위치 |
|---|---|
| Enemy AttackInterval 1f, AttackRange 1.5f | `Assets/GameMain/Entity/EntityLogic/Enemy.cs:11-12` |
| Enemy HP 30 / Speed 2f / Damage 10 / Exp 5 | `Assets/GameMain/Entity/EntityData/EnemyData.cs:9-12` |
| Player HP 100 / Speed 5f | `Assets/GameMain/Entity/EntityData/PlayerData.cs:9-10` |
| ExpGem AttractRadius 5f / CollectRadius 0.5f | `Assets/GameMain/Entity/EntityLogic/ExpGem.cs:14-15` |
| ExpGem Exp 5 / Speed 4f | `Assets/GameMain/Entity/EntityData/ExpGemData.cs:7-8` |
| Weapon attackInterval 1f / FindNearestEnemy radius 20f | `Assets/GameMain/Weapon/WeaponBase.cs:19`, `:62` |
| Projectile DetectRadius 20f / damage 25 / speed 10f / lifetime 3f | `Assets/GameMain/Weapon/ProjectileWeapon.cs:12-16` |
| 선택 캐릭터 위치 ±2 | `Assets/GameMain/Game/SurvivalGame.cs:37-38` |
| 사망 연출 1.5초 | `Assets/GameMain/Entity/EntityLogic/PlayerSelectLogic.cs:61` |
| 언로드 인터벌 60 / 300초 | `Assets/Prefabs/GameFramework.prefab:875-876` |
| 프레임레이트 60 (프리팹 원본 30) | `Assets/Scenes/MainScene.unity:687-690` / `Assets/Prefabs/GameFramework.prefab:766` |
| 카메라 orthographic size 4.5 | `Assets/Scenes/MainScene.unity:401-402` |

---

## 7. 죽은 코드 / 정리 후보

### 7-1. `Assets/Sample` — 코드 참조 기준 dead, 에셋 참조 기준 live

**정체**: Unity 공식 무료 코스웨어 프로젝트 **"Zombie Toys"**의 게임플레이 스크립트 전량(34개 .cs, 2851줄, 추적 파일 113개).
근거: `Assets/Supplemental Resources/`의 `Zombie-Toys_Game-Design-Document.pdf`, `Zombie-Toys_Technical-Design-Document.pdf`, `Unity Certified Associate Courseware Instructor Resources.pdf` + 모든 스크립트의 튜토리얼용 주석. 프로젝트 껍데기 자체는 URP Empty Template(`Assets/Readme.asset:16`)이다.

**코드 참조: 완전 0건 (양방향).** `Assets/GameMain`, `Assets/Scripts`, `Assets/AssetManager`, `Assets/TutorialInfo` 어디에서도 Sample 타입명을 참조하지 않고, Sample→GameMain 참조도 0건이다. 클래스명 충돌도 없다(`Enemy`는 GameMain에만, `Ally`는 Sample에만, `GameManager`는 Sample에만 존재).

**그러나 에셋 참조는 살아 있다 — 삭제 시 Missing Script가 되는 대상:**

| 에셋 | 붙어 있는 Sample 스크립트 |
|---|---|
| `Assets/Art/Prefabs/Characters/Boy.prefab`, `Girl.prefab` | FrostAttack, LightningAttack, LightningBolt, SlimeAttack, StinkAttack (§5.1 [10-d]) |
| `Assets/Art/Prefabs/Temp/Boy.prefab`, `Temp/Girl.prefab` | PlayerMovement, PlayerAttack, PlayerHealth, PlayerSelect, PlayerInputPC |
| `Assets/Art/Prefabs/UI/HUDCanvas.prefab` | Countdown, FlashFade |
| `Assets/Art/Prefabs/UI/PauseMenuCanvas.prefab` | PauseMenu |
| `Assets/Art/Prefabs/Attacks/*.prefab` | SlimeDebuff, SlimeProjectile, StinkHit, StinkProjectile, AVPlayer |
| `Assets/Art/Prefabs/Effects/Character Selection Spotlights.prefab` | CharacterSpotlight, LookAtMouse |
| Zombunny / ZomBear / ZombieDuck / Hellephant / Clown 프리팹 | EnemyAttack, EnemyHealth, EnemyMovement |
| Dog / Sheep 프리팹 | Ally |
| `Assets/Sample/Scenes/Main.unity` (1953줄) | GameManager, AllyManager, MouseLocation, AnimatorDisabler 등 |

**⚠ `Assets/Art/Prefabs/Temp/*`는 고아 백업이 아니다.**
`Assets/Sample/Scenes/Main.unity:800`, `:1453`이 참조하는 것은 Characters/가 아니라 **Temp/** 프리팹이다. GUID 승계 때문이다: fa2f999 이전 `Characters/Boy.prefab`의 guid가 `3a6182fe...`였는데, fa2f999에서 Characters 쪽이 새 guid `163092cc...`를 받고 옛 guid를 Temp가 물려받았다. Main.unity는 그 이후 수정된 적이 없다. **Temp를 삭제하면 샘플 씬의 캐릭터 선택 흐름 전체가 깨진다.**

**적 프리팹은 라이브 Addressable 자산이다.** Zombunny/ZomBear/ZombieDuck/Hellephant/Clown/Dog/Sheep은 지금도 Addressables에 등록되어 있고 콜라이더도 붙어 있다. **향후 적 스폰을 붙이는 순간 EnemyAttack/EnemyHealth/EnemyMovement가 실제로 깨어난다**(§8-10).

**Sample 부활 시 주의**
- `Assets/Sample/Scripts/Player/PlayerInputPC.cs:83` `Input.GetButtonDown("SwitchAttack")`, `:102` `"SummonAlly"` — 이 축이 `ProjectSettings/InputManager.asset`에 없다. 런타임 `ArgumentException`이 난다
- `EnemyAttack.cs:54`, `:66`, `EnemyHealth.cs:117`, `Assets/Sample/Scripts/Helpers/CameraFollow.cs:15` — `GameManager.Instance` null 가드가 없다. GameManager 없는 씬(=MainScene)에 이 컴포넌트가 붙은 오브젝트를 드롭하면 **즉시 NRE**
- `EnemyAttack.cs:104`는 `GameManager.Instance.Player.IsAlive()`를 부르는데 코루틴 가드가 `Instance` null만 검사한다
- **데미지 API가 두 갈래로 갈라져 호환되지 않는다**: Sample `EnemyHealth.TakeDamage(int)` vs GameMain `TargetableObject.ApplyDamage(Entity, int)`. `LightningAttack.cs:37`은 `GetComponent<EnemyHealth>()`로만 찾으므로 **GameMain Enemy 엔티티에는 절대 데미지를 줄 수 없다**
- **완전 고아 3개**(씬·프리팹 참조 0건): `Touchpad.cs`, `MobileInterface.cs`, `PlayerInputTouch.cs`

**판정: 지금 삭제하면 안 된다.** 포팅이 끝날 때까지 레퍼런스로 남기되 프리팹 정리가 선행되어야 한다. 남긴다면 `Assets/Sample/Sample.asmdef`로 격리하는 편이 안전하다(§8-20).

### 7-2. Missing Script 프리팹 (즉시 정리 가능)

§5.1 [10-a]의 2건. `Assets/Prefabs/Player.prefab`의 `m_EditorClassIdentifier`가 `Assembly-CSharp::Player`(네임스페이스 없음)라 현재 `ToyBoxNightmare.Player`(guid `a69f8df185483194eae3a25fb9b641e4`)와 완전히 다른 레거시다. **실제 플레이어는 `Assets/Art/Prefabs/Characters/{Boy,Girl}.prefab`이다.**

### 7-3. 도달 불가 GameMain 코드

| 대상 | 이유 |
|---|---|
| `Assets/GameMain/Entity/EntityLogic/Enemy.cs` + `EntityData/EnemyData.cs` | `typeof(Enemy)`로 ShowEntity하는 코드 0건, `new EnemyData` 0건 |
| `Assets/GameMain/Entity/EntityLogic/ExpGem.cs` + `EntityData/ExpGemData.cs` | 유일한 스폰 경로 `SpawnExpGem`이 삭제됨 |
| `Assets/GameMain/Entity/EntityLogic/Projectile.cs` + `EntityData/ProjectileData.cs` | 유일한 생성자 ProjectileWeapon이 죽어 있고 addressable 주소도 없음 (이중 차단) |
| `Assets/GameMain/Weapon/WeaponBase.cs` + `ProjectileWeapon.cs` | `Initialize(Player)` 호출자 0건, AddComponent 0건, 프리팹 참조 0건 |
| `WeaponBase.OnFireStart/OnFireHeld/OnFireStop` (`:51-57`) | 호출자 0건. 이걸 오버라이드하던 무기 4종이 88408fd에서 삭제됨. **fa2f999에서 추가된 그 4개 무기는 단 한 번도 실행된 적이 없다** |
| `WeaponBase.GetMouseWorldPosition()` (`:86-97`) | 호출자 0건. `Player.cs:109-121`에 동일 로직이 중복 존재 |
| `Enemy.SetSpeedMultiplier` (`:17-20`) | 호출자 0건. FrostWeapon 삭제됨. 항상 1f |
| `Player.UpgradeMoveSpeed` (`:125-128`) | 호출자 0건 + NRE 확정 (§5.1 [10-c]) |
| `EnemyData.AttackDamage` / `ExpReward` | 유일 사용처가 주석 처리됨 |
| `SurvivalGame.mPlayer` (`:17`) | `:121`에서 대입만 되고 읽는 곳이 없음 |
| `GameMode` enum + `GameBase.GameMode` | 정의부 3곳(`GameMode.cs:11`, `GameBase.cs:38`, `SurvivalGame.cs:21`)만 존재. 매핑 테이블/switch 없음 |
| `GameBase.GameOver` | true로 만드는 코드 0건 (→ §4-3 ★에서 부활시킨다) |
| `ProcedureBase.UseNativeDialog` | 읽는 코드 0건 (순수 상속 강제 항목) |
| `TargetableObjectData`의 CampType 관련 (`:11-13, 17-19, 24-30`) | 전부 주석. `CampType`/`ImpactData` 타입이 프로젝트에 없음 |
| `PlayerSelectLogic.DeathComplete` (`:71-74`) | private + Animation Event 미연결 → 호출되지 않음 (§5.1 [10-e]) |

### 7-4. GF 래퍼 계층 미사용

- **21개 컴포넌트 중 GameMain이 실제로 쓰는 건 2개뿐**: `GameEntry.GetComponent<EntityComponent>` 10회, `<EventComponent>` 4회. 나머지(Config/DataNode/DataTable/Download/FileSystem/Localization/Network/Scene/Setting/Sound/UI/WebRequest)는 0회.
- **그런데 18개 모듈이 전부 생성되고 매 프레임 Update된다.** 정리하려면 프리팹에서 GO를 빼면 되지만, `DebuggerComponent`가 ResourceComponent/SettingComponent/NetworkComponent/ObjectPoolComponent/BaseComponent를 참조하고 `SoundComponent`가 `GetModule<ISceneManager>()`를 호출하므로 의존 관계를 먼저 확인해야 한다.
- `GameFramework.Resource.ResourceManager`(External의 Resource 폴더 전체, Priority 3) — **완전한 죽은 코드.** `GetModule<IResourceManager>()` 호출 0건(§2-1 5단계).
- `BaseComponent.EditorResourceHelper` (`:83-87`) — 선언 1건 외 참조 0건.
  (반면 `EditorResourceMode`는 아직 3곳에서 읽힌다: `BaseComponent.cs:175-179`, `LocalizationComponent.cs:134`, `DebuggerComponent.EnvironmentInformationWindow.cs:56`)
- `ResourceComponent.ApplicableGameVersion` / `InternalResourceVersion` (`:37-38`) — 주석은 "Debugger 호환용"이지만 실제로 `DebuggerComponent.EnvironmentInformationWindow.cs:56`이 화면에 그린다.
- `EntityComponent`의 조회/부착 API 전반 (`GetEntity`/`GetEntities`/`GetAllLoadedEntities` `:201-265`, `AttachEntity`/`DetachEntity` `:449-651`) — GameMain 사용 0건. **적 다수를 관리하려면 이 API로 갈아타야 한다**(§3-2, §4-1 10단계).
- `Assets/Scripts/UI/DefaultUIGroupHelper.cs:14-16` `SetDepth` — **빈 구현.** UI를 쓰기 시작하면 반드시 문제가 된다(§3-7, §4-6 A5).
- `Assets/Scripts/Download/WWWDownloadAgentHelper.cs`, `Assets/Scripts/WebRequest/WWWWebRequestAgentHelper.cs` — 프리팹이 UnityWebRequest 계열 헬퍼를 지정하고 있어 미사용.
- `Assets/Scripts` 하위에 Editor 폴더가 없다 → 커스텀 인스펙터 부재 (§5-4).

### 7-5. 별도 Addressables 진입점 (중복 구현)

`Assets/AssetManager/AddressablesController.cs`(20줄) + `Assets/AssetManager/AddressablesLoader.cs`(18줄)는 GF 리소스 경로와 완전히 별개의 미완성 병행 구현이다.

- `AddressablesController.Start()` 본문 2줄이 전부 주석 처리(`:13-14`) → `Instantiate()`(`:17`) 호출 경로 없음
- **단, 이 컴포넌트는 씬에 실존한다**: `Assets/Art/Prefabs/Environment/Environment.prefab:6689-6701`의 루트 GameObject에 붙어 있고(`_label: "Prefabs"` 직렬화까지 되어 있음), 그 프리팹이 `Assets/Scenes/MainScene.unity:635-678`에 배치되어 있다. Start()도 실제로 호출된다(본문이 비어 부작용이 없을 뿐)
- **되살려도 바로 동작하지 않는다**: 주석 첫 줄이 `GameObject.Find("Example Assets")`인데 MainScene에 그런 오브젝트가 없어 NRE가 난다
- 되살리면 이중 누수: `LoadResourceLocationsAsync` 핸들 미해제(`AddressablesLoader.cs:11`), `InstantiateAsync` 결과에 `ReleaseInstance` 없음(`:15`), `async void`라 예외 유실
- `AddressablesLoader`는 `public class AddressablesLoader : MonoBehaviour`인데 정적 메서드 하나뿐이라 MonoBehaviour 상속이 불필요하다

**판정: 삭제 후보.** GF 리소스 경로(`ResourceComponent` → `ResourceManager`)로 일원화할 것.

### 7-6. 그 외 정리 후보

| 대상 | 판정 근거 |
|---|---|
| `Assets/_Recovery/0.unity` (782줄) | Unity 크래시 복구 산출물. afd656b에서 실수 커밋(같은 커밋에서 .gitignore 4줄 제거). 빌드/로드 어디에도 무관. `Boy.fbx`만 참조 → **삭제 가능 (git 커밋 필요)** |
| `Assets/Scenes/SampleScene.unity` (432줄) | 빈 URP 템플릿인데 역설적으로 빌드 씬 목록의 유일한 항목. **MainScene을 등록한 뒤에 삭제할 것**(§8-2) |
| `Assets/Scenes/Main/` | **완전히 빈 폴더** (.meta만) → 삭제 가능 |
| ⚠ `Assets/Scenes/MainScene/` | **이름이 비슷하지만 삭제하면 안 된다.** 라이트맵/리플렉션 프로브 실데이터 폴더다: `LightingData.asset`, `Lightmap-0~5_comp_dir.png` + `_comp_light.exr`(6세트), `ReflectionProbe-0/1.exr`. 지우면 **MainScene 라이팅이 날아간다.** 관련 설정은 `Assets/Scenes/MainSettings.lighting` |
| `Assets/GameMain/UI/` | 완전히 빈 폴더 (88408fd에서 3파일 삭제). §4-6 작업 시 여기에 새 폼을 만든다 |
| `Assets/Prefabs/Enemy.prefab` (46줄), `ExpGem.prefab` (46줄) | Transform + 로직 스크립트뿐. Renderer/Collider/Rigidbody 없음, Addressable 미등록 → **로드 자체가 불가능**. 게다가 로직 스크립트가 baked되어 §5.1 [2]를 재생산한다 |
| 빈 Addressables 그룹 5개 | Default Local Group, Materials, Models, Shaders, Textures — 전부 `m_SerializeEntries: []` |
| 미사용 Addressable 주소 8개 | Clown, Environment, Sheep, Dog, ZomBear, Zombunny, ZombieDuck, Hellephant — 코드 리터럴 참조 0건. **적/아군 후보로 미리 등록한 것으로 보이므로 삭제하지 말 것**(§8-10) |
| `Assets/Readme.asset` + `Assets/TutorialInfo/` | URP Empty Template 웰컴 화면 → 삭제 가능 |
| `Assets/InputSystem_Actions.inputactions` | URP 템플릿 기본 액션맵. 코드 참조 0건. **§8-17 결정 전까지 보류** |
| **루트의 `.unitypackage` 3개** | `GmaeFrameworkScriptAndPrefabs.unitypackage`(383KB, 파일명 오타), `GmaeFrameworkScriptAndPrefabs2.unitypackage`(550KB), `ProjectAssets.unitypackage`(661MB). **정정: git에 커밋되어 있지 않다.** `.gitignore:63-64`가 `*.unitypackage` / `*.unitypackage.meta`를 제외하고 있고 `git ls-files \| grep -c unitypackage` = **0**(확인 완료). 저장소 문제가 아니라 **로컬 워킹트리 용량 문제**이므로 그냥 지우면 끝난다 |
| `Assets/Plugins/GameFramework.pdb` | **정정: git에 없다.** 추적 중인 것은 `GameFramework.dll`, `GameFramework.deps.json`과 각 `.meta` 4개뿐(확인 완료). pdb는 로컬 빌드 산출물이다 — 커밋하지 말 것(§1-4 지뢰 2) |
| 미사용 using | `TargetableObjectData.cs:3-4`, `TargetableObject.cs:3`, `ExpGemData.cs:1` |
| `External/GameFramework/GameFramework/bin`, `obj` | DLL 빌드 산출물이 소스 트리에 잔존. External 전체가 gitignore 대상이라 저장소 영향은 없음 |

### 7-7. GF 코어 소스의 알려진 손상

`External/GameFramework/GameFramework/Base/Log/GameFrameworkLog.cs`(107줄)가 주석 기계 번역 중 **잘려 나갔다.** `:58`, `:70`, `:82`, `:94`에 `// ... 계속해서 모든 XXX 메서드들에 대해 동일한 패턴으로 번역 ...` 플레이스홀더가 남아 있고, Info/Warning/Error/Fatal에 `(object message)` 오버로드가 **하나씩만** 남아 있다(원본은 각각 여러 제네릭 포맷 오버로드 보유).

현재는 `Assets/Scripts/Utility/Log.cs`가 자체적으로 `string.Format`을 해서 넘기므로 동작에 문제가 없지만, **DLL을 재빌드하면서 다른 코드가 잘린 오버로드를 부르면 컴파일이 깨진다**(§1-4 지뢰 3). 다른 파일에도 같은 절단이 있는지는 **미확인**(플레이스홀더 문자열 검색으로는 이 파일 1개만 검출됨).

원본 GameFramework 대비 확인된 수정:
1. 저작권 헤더에 `Modified © 2025 얌얌코딩` 추가 (`Homepage: https://www.yamyamcoding.com/`, `Feedback: mailto:eazuooz@gmail.com`)
2. 필드 네이밍 `m_Foo` → `mFoo` 전면 변경 (커밋 bbcdbcc, 68파일 2327줄) — **이 때문에 원본 GF 문서/예제 코드를 그대로 복붙할 수 없다**
3. 주석 중국어→영어/한국어 기계 번역 (도구: `External/GameFramework/translate_comments.py`)

---

## 8. 작업 시작 전 결정이 필요한 것

> §0의 4단계를 밟고 §5.1의 착수 순서 1~4번을 처리하면 아래 결정 중 상당수는 자동으로 해소된다.
> 각 항목에 **권장안**이 있으면 표기했다. 권장안이 있는 항목은 "결정"이 아니라 "확인"만 하면 된다.

### 8-1. 즉시 결정 — 개발 환경이 정상 동작하려면

**1. `Log.*`가 전부 컴파일 제거되는 것이 의도인가?**
→ **권장: 의도가 아니라고 보고 즉시 켠다.** `ENABLE_LOG;ENABLE_DEBUG_AND_ABOVE_LOG`(§0-1). 지금은 엔티티 예외까지 전부 침묵한다(§5.1 [3]). 진짜로 릴리스에서 로그를 빼고 싶다면 그건 **릴리스 빌드 타겟에서만** 비우면 되는 일이다. 만약 "전부 제거"가 정말 의도라면 `Assets/Scripts/Utility/Log.cs`(433줄) 전체가 사실상 죽은 코드이므로 삭제 검토 대상이 된다.

**2. `MainScene`을 `EditorBuildSettings`의 scene 0으로 넣을 것인가? `SampleScene`은 삭제할 것인가?**
→ **권장: MainScene을 0번으로 등록하고, SampleScene은 등록만 해제하고 파일은 당분간 남긴다.** 현재 빌드/`LoadScene(0)` 재시작이 빈 SampleScene으로 간다(§5.1 [6]).

**3. 프리팹 루트의 중복 `ProcedureComponent`(`Assets/Prefabs/GameFramework.prefab:770-784`)를 삭제할 것인가?**
→ **권장: 삭제.** 자식 'Procedure' GO(`:679-693`) 쪽이 정상이다. afd656b의 프리팹 대개편(1657줄) 사고로 추정되나 **미확인**. 씬 오버라이드(`Assets/Scenes/MainScene.unity:735-742`)는 무의미 오버라이드이므로 함께 정리(§5.1 [5]).

**4. `MainCamera`의 태그를 `MainCamera`로 바꿀 것인가, `Camera.main` 의존을 제거할 것인가?**
→ **권장: 태그 변경(1분).** 마우스 조준이 즉시 살아난다(§5.1 [9]). 카메라 추종은 **Sample `CameraFollow.cs`를 쓰지 말고 §4-9의 20줄짜리를 새로 작성한다**(`Assets/Sample/Scripts/Helpers/CameraFollow.cs:15`가 `GameManager.Instance` NRE — §7-1).

**5. Addressables Play Mode Script를 팀 표준으로 무엇으로 할 것인가?**
→ **권장: 일상 개발은 `Use Asset Database (fastest)`, 릴리스 검증 전에만 `New Build` + `Use Existing Build`.** 이 설정은 `Library/`에 있어 git 공유가 안 되므로 **§0-2를 README/CLAUDE.md에 반드시 명문화해야 한다**(§8-22).

### 8-2. 아키텍처 갈림길

**6. `Girl`/`Boy` 프리팹에 baked된 `Player`/`PlayerSelectLogic`을 제거할 것인가?**
→ **권장: 제거.** 프레임워크 관용에 맞고 이벤트 2중 발행·NRE 지뢰가 동시에 사라진다(§5.1 [2]). 직렬화 값(`characterKey`/`capsuleCollider`/`animator`/`rigidBody`)은 `PlayerSelectLogic.OnInit`(`:24-30`)이 `GetComponent`로 다시 채우므로 **기능 손실 없음.** 동시에 Sample 공격 스크립트 5종도 함께 판단해야 한다(특히 `FrostAttack.Awake`의 20개 Instantiate 지뢰, §5.1 [10-d]).

**7. `SpawnPlayer`가 선택 프리팹과 같은 Addressable을 재사용하는 것이 의도인가?**
→ 현재 구조상 인스턴스 재사용이 아니라 **같은 주소의 중복 로드**가 일어나 핸들 누수를 유발한다(§5.1 [1]). 선택지:
  - **(a)** 전투용 별도 Player 프리팹/주소를 만든다 — 선택 화면과 전투 캐릭터의 요구사항이 다르므로(선택 화면은 Collider·Animator만, 전투는 Rigidbody·무기 부착 지점 필요) 장기적으로 이쪽이 맞아 보인다
  - **(b)** 주소를 유지하되 §8-8의 참조 카운트로 누수만 해결한다 — 단기 비용이 낮다

**8. `ResourceManager`의 중복 로드 누수를 어떻게 고칠 것인가?**
→ 선택지:
  - **(a) 권장 — `assetName`별 참조 카운트 + 핸들 캐시**로 바꿔 한 번만 로드하고 카운트를 관리. GF EntityManager가 인스턴스마다 `entityAsset`을 들고 있어 GF 계약과 더 잘 맞는다.
  - (b) 핸들 리스트를 쌓아 `UnloadAsset`마다 하나씩 pop.
  - **어느 쪽이든 실패 경로(`Assets/Scripts/Resource/ResourceManager.cs:172-176`)의 핸들 미해제는 반드시 함께 처리해야 한다.**

**9. 적/무기/경험치 시스템을 되살릴 것인가, 완전히 버릴 것인가?**
→ 88408fd의 삭제 의도가 '완전 폐기'인지 '재설계를 위한 초기화'인지가 **미확인**이다. **이 문서에서 결론 낼 수 없는 유일한 제품 결정이다.**
  - **되살린다면**: 튜닝값을 예전처럼 C# const로 갈 것인지, DataTable/ScriptableObject 기반으로 바꿀 것인지(현재 `TypeId`가 전부 1로 하드코딩되어 있고 DataTable 참조가 0건이다). 그리고 **반드시 §4-1 3단계의 HideEntity 가드를 먼저 넣을 것**(§5.1 [4]).
  - **버린다면**: Enemy/ExpGem/Projectile/Weapon 소스 + `GameFramework.prefab:1073-1087`의 엔티티 그룹 3개 + `ExpGem`/`Player.UpgradeMoveSpeed`/`ProjectileWeapon`의 public setter까지 함께 정리.

**10. 적 에셋은 무엇을 쓸 것인가?**
→ **권장: 이미 Addressable에 등록되고 콜라이더도 있는 `Zombunny`/`ZomBear`/`ZombieDuck`/`Hellephant`/`Clown`/`Sheep`/`Dog`를 쓴다.** `Assets/Prefabs/Enemy.prefab`은 콜라이더 없음 + Addressable 미등록이라 처음부터 다시 만드는 것과 같다. `Assets/Scripts/Entity/Entity.cs:98`이 EntityLogic을 런타임에 붙이므로 프리팹에 Enemy 스크립트가 없어도 무방하다. 이 경우 **`EnemyData`에 에셋 키 필드가 필요**하다(현재 없음). 단 이 프리팹들에는 Sample의 `EnemyAttack`/`EnemyHealth`/`EnemyMovement`가 붙어 있어 §5.1 [2]와 같은 중복이 생기므로 **프리팹 복제 후 스크립트를 떼는 것이 안전하다.**

**11. 적 스폰을 어디에 둘 것인가?**
→ 선택지: `SurvivalGame.Update`로 되돌리기(84ce872 방식) vs 별도 Spawner 시스템(`GameFrameworkComponent` 파생, §5-3)으로 분리.
  **어느 쪽이든 `SurvivalGame.Update` 오버라이드 부활이 선행된다**(현재 프레임 루프 0줄, §2-2 / §4-3 ★). GF의 내장 오브젝트 풀이 있으므로 Sample `EnemySpawner`의 수동 풀링 코드는 중복이고, **'스폰 타이밍/난이도 곡선'만 가져오는 것이 맞아 보인다**(`git show fa2f999^:Assets/GameMain/Game/SurvivalGame.cs`로 복원 가능, §6-2).

**12. 게임오버·재시작을 프로시저 전이로 표현할 것인가, `SurvivalGame` 내부 상태로 처리할 것인가?**
→ **권장: 프로시저 전이.** 구체 패턴은 §4-3 ★에 확정안을 적어 두었다. 캐릭터 선택과 플레이를 `ProcedureCharacterSelect` / `ProcedureMain` / `ProcedureGameOver` 세 프로시저로 나누면 §5.1 [7]의 구독 비대칭도 자연 해소된다. 다만 다음 3개가 선결 조건이다:
  - `SurvivalGame.Shutdown`의 구독 해제 방어 (§5.1 [7])
  - 전이 시 잔존 엔티티 정리 코드 (§4-1 10단계)
  - `EntitySerialId` 리셋 API 추가 여부 (§5-4)

**13. 레이어 설계를 도입할 것인가?**
→ **권장: 도입.** `ProjectSettings/TagManager.asset`에 커스텀 레이어가 0개다. `Player`/`Enemy`/`Projectile`/`Pickup` 레이어 + Physics 충돌 매트릭스 + `OverlapSphere` layerMask를 도입하면 `FindNearestEnemy`(`Assets/GameMain/Weapon/WeaponBase.cs:62-83`)의 오탐/비용 문제를 동시에 해결할 수 있다. **적을 되살리기 전에 하는 것이 재작업이 적다.**

**14. UI를 GameFramework `UIComponent`로 갈 것인가, uGUI를 직접 쓸 것인가?**
→ 현재 `mUIGroups: []`(`Assets/Prefabs/GameFramework.prefab:155`), `mInstanceRoot: {fileID: 0}`(`:150`), UIForm 0개, MainScene에 Canvas/EventSystem 없음.
  **권장: `UIComponent`로 간다** — §4-6에 배선 절차를 확정해 두었다. 프레임워크가 이미 풀링·비동기 로드·그룹 관리를 제공하므로 직접 uGUI를 쓰면 그 이점을 버리게 된다.
  **단 세 가지를 미리 알고 시작할 것**: (a) `mInstanceRoot`에 실제 Canvas를 반드시 지정해야 한다(안 하면 렌더링 자체가 안 됨), (b) `DefaultUIGroupHelper.SetDepth`가 빈 구현이라 그룹 depth 정렬을 쓰려면 커스텀 헬퍼가 필요하다, (c) **UI 폼 프리팹에는 로직 컴포넌트를 반드시 붙여야 한다 — 엔티티와 정반대 규약이다**(§3-7).
  `Assets/Art/Prefabs/UI/`의 `HUDCanvas.prefab` / `PauseMenuCanvas.prefab`은 Sample 스크립트가 붙어 있어 **그대로 못 쓴다**(§7-1).

**15. BGM을 GF `SoundComponent`로 옮길 것인가?**
→ **권장: 옮긴다.** 현재 `Assets/Scenes/MainScene.unity:167-295`에 순수 AudioSource로 직접 배치되어 있어 GF 사운드 그룹/볼륨/뮤트 관리를 전혀 받지 못한다. 절차는 §4-7. `mAudioMixer`(`GameFramework.prefab:546`, fileID 0)와 `mSoundGroups`(`:553`, 빈 배열) 설정이 선행되어야 하고, **옮긴 뒤 씬의 AudioSource GameObject를 반드시 제거**해야 이중 재생을 피한다.

**16. 세이브/설정을 `SettingComponent`로 갈 것인가?**
→ **권장: 그렇다. 그리고 이건 결정이 필요 없다 — 이미 배선이 끝나 있어 오늘 바로 쓸 수 있다**(§3-9, §4-8). 결정할 것은 두 가지뿐:
  - 헬퍼를 `DefaultSettingHelper`(파일 기반, 현재 프리팹 기본값 `GameFramework.prefab:829`, 저장 경로 `Application.persistentDataPath/GameFrameworkSetting.dat`)로 둘 것인가, `PlayerPrefsSettingHelper`로 바꿀 것인가
  - 키 네이밍 규약과 상수 파일 위치 (`Assets/GameMain/Utility/SettingKeys.cs` 신설 제안)

### 8-3. 입력 · 형상관리 · 프로세스

**17. 입력 경로를 무엇으로 통일할 것인가?**
→ 현재 3중 경로가 공존한다(§3-6): 레거시 `OnMouseUp` / `Keyboard.current` 폴링 / 미사용 `.inputactions`.
  **결정하지 않으면 무기 입력을 붙이는 사람이 네 번째 경로를 또 만든다.**
  - **권장(단기): `Keyboard.current`/`Mouse.current` 폴링으로 통일**, `Input.GetAxis`/`GetButtonDown` 계열 신규 사용 금지. 캐릭터 선택의 `OnMouseUp` 1건만 예외로 두되 UI 전환 시 함께 제거.
  - **장기 검토**: `.inputactions` 채택. 채택 시 `PlayerInput` 컴포넌트를 **Entity 프리팹에 직접 붙이면 안 된다**(§5.1 [2]와 동일 함정). `EntityLogic.OnShow`에서 AddComponent 하거나 씬 단일 입력 라우터 → `Player.Instance` 전달 방식으로.
  - ⚠ `activeInputHandler: 2`(Both)를 1(New only)로 바꾸면 캐릭터 선택이 즉시 죽는다(`ProjectSettings/ProjectSettings.asset:920`).

**18. `External/GameFramework`가 `.gitignore:80`으로 git에서 완전히 제외되어 있는 것이 의도인가?**
→ 코어 소스가 형상관리 밖에 있고 팀원은 DLL만 받는다. 외부 저장소/서브모듈로 관리할 계획인지 확인 필요.
  함께 결정할 것:
  - **배포된 DLL이 Debug 빌드**이고 **소스가 DLL보다 약 2.5개월 최신**이다. 코어 재빌드 시 Release로 바꾸면 동작이 달라질 수 있다(§1-3).
  - **PostBuild XCOPY가 절대경로 하드코딩**(`External/GameFramework/GameFramework/GameFramework.csproj:13`)이라 다른 머신에서 조용히 실패한다 → `$(SolutionDir)..\..\Assets\Plugins\` 상대경로로 수정 권장(§1-4).
  - `GameFrameworkLog.cs`의 절단(§7-7)을 먼저 복구할지.

**19. `Assets/Scripts`(190파일)와 `Assets/GameMain`을 asmdef로 분리할 것인가?**
→ 현재 같은 Assembly-CSharp에 있어 GameMain이 `ShowEntityInfo` 같은 internal 타입에도 접근할 수 있다. 컴파일 시간도 전부 한 덩어리다. Sample 격리(§8-20)와 함께 검토할 사안이다.

**20. `Assets/Sample`을 삭제할 것인가, 격리할 것인가, 유지할 것인가?**
→ **권장: 격리(`Assets/Sample/Sample.asmdef` 생성) 후 유지.** §7-1의 에셋 의존 목록 전체가 삭제의 선행 조건이고, 특히 `Assets/Art/Prefabs/Temp/*`는 **고아가 아니라 `Assets/Sample/Scenes/Main.unity`가 GUID 승계로 참조하는 라이브 에셋**이다. 또한 Unity 공식 무료 배포물(Zombie Toys)이므로 저장소 공개 범위에 따라 라이선스/재배포 조건 확인이 필요할 수 있다.

**21. `GameCoreLoop.md`를 갱신할 것인가, "84ce872 시점 기록"으로 명시하고 동결할 것인가?**
→ **권장: 파일 첫 줄에 "⚠ 이 문서는 2026-03-09 커밋 84ce872 시점의 기록이며 현재 코드와 절반 이상 불일치한다. 현재 상태는 `ARCHITECTURE.md`를 볼 것."을 추가하고 동결.** 지금처럼 두면 이 문서를 읽은 사람이 존재하지 않는 시스템(LevelSystem, UpgradeSystem, AreaWeapon)을 전제로 작업하게 된다(§6-1).

**22. `CLAUDE.md`를 만들 것인가?**
→ **권장: 만든다.** 저장소 어디에도 없다(`.claude/`에는 `settings.local.json` 21줄 — allow 배열 15개 항목만).
  최소한 다음 4가지를 명문화하면 반복 실수를 크게 줄일 수 있다:
  1. **§0 부트스트랩 4단계** (define / Play Mode Script / MainScene 직접 열기 / 체크리스트) ← `Library/` 설정이 git 공유가 안 되므로 가장 가치가 크다
  2. **§5-2 프레임워크 규약** (특히 `protected internal override`, 엔티티 프리팹에 EntityLogic 금지 / UI 폼에는 필수, HideEntity 1회, 이벤트 중복 구독 금지)
  3. **§4의 레시피 인덱스**
  4. **§5.1 착수 순서**와 "아직 손대지 말 것" 목록

**23. `Assets/_Recovery/0.unity`와 로컬 `.unitypackage` 3개(총 662MB)를 정리할 것인가?**
→ **정정된 전제**: `.unitypackage`는 **git에 커밋되어 있지 않다**(`.gitignore:63-64`, `git ls-files` 0건 — 확인 완료). 저장소 용량 문제가 아니라 로컬 디스크 문제이므로 **그냥 지우면 끝난다** — 결정할 것이 사실상 없다. 단 `ProjectAssets.unitypackage`(661MB)가 원본 에셋 백업일 가능성이 있으므로 지우기 전에 내용 확인 권장.
→ `Assets/_Recovery/0.unity`(782줄)는 **git에 커밋되어 있다**(afd656b 실수 커밋). 이쪽은 실제로 삭제 커밋이 필요하다.

---

## 부록 A. 자주 쓰는 파일 경로 빠른 참조

| 목적 | 경로 |
|---|---|
| 게임 진입점 | `Assets/GameMain/Procdure/ProcedureMain.cs` |
| 게임 로직 본체 | `Assets/GameMain/Game/SurvivalGame.cs` |
| 플레이어 | `Assets/GameMain/Entity/EntityLogic/Player.cs` |
| 프레임워크 설정 (모든 인스펙터 값) | `Assets/Prefabs/GameFramework.prefab` |
| 실행 씬 | `Assets/Scenes/MainScene.unity` |
| Addressable 주소 목록 | `Assets/AddressableAssetsData/AssetGroups/Prefabs.asset` |
| Addressable 빌드 설정 | `Assets/AddressableAssetsData/AddressableAssetSettings.asset` |
| 리소스 어댑터 (누수 지점) | `Assets/Scripts/Resource/ResourceManager.cs` |
| 엔티티 코어 래퍼 | `Assets/Scripts/Entity/EntityComponent.cs`, `Entity.cs`, `DefaultEntityHelper.cs` |
| UI 코어 래퍼 | `Assets/Scripts/UI/UIComponent.cs`, `UIForm.cs`, `DefaultUIFormHelper.cs` |
| 설정/세이브 | `Assets/Scripts/Setting/SettingComponent.cs`, `DefaultSettingHelper.cs` |
| 로그 (컴파일 제거 중) | `Assets/Scripts/Utility/Log.cs` |
| 정의 심볼 | `ProjectSettings/ProjectSettings.asset:823` |
| 빌드 씬 목록 | `ProjectSettings/EditorBuildSettings.asset:7-10` |
| 코어 DLL | `Assets/Plugins/GameFramework.dll` (git 추적) |
| 코어 소스 (git 밖) | `External/GameFramework/GameFramework/` |
| 코어 솔루션 | `External/GameFramework/GameFramework.sln` |
| Animator 파라미터 정의 | `Assets/Art/Animations/BoyAnimatorController.controller:11`, `:17` |

## 부록 B. 이 문서에서 "미확인"으로 남은 것

| # | 항목 | 확인 방법 |
|---|---|---|
| 1 | 배포된 `GameFramework.dll`(2026-03-28)과 External 소스(2026-06-14)의 동작 일치 여부. §3-2·§5.1 [4]의 코어 동작 결론이 여기에 걸려 있다 | DLL 디컴파일 비교 또는 재빌드 후 diff |
| 2 | `GameFrameworkLog.cs` 외 다른 코어 파일의 번역 절단 여부 | External 전체를 원본 GameFramework와 diff |
| 3 | 프리팹 루트 중복 `ProcedureComponent`가 afd656b 사고인지 의도인지 | `git show afd656b -- Assets/Prefabs/GameFramework.prefab` |
| 4 | 88408fd의 대량 삭제가 '완전 폐기'인지 '재설계 초기화'인지 (§8-9의 전제) | 작성자 확인 필요 |
| 5 | `ProjectAssets.unitypackage`(661MB)의 내용 및 삭제 안전성 | 임포트 없이 내용 목록만 확인 |
| 6 | 서바이버류에 적합한 카메라 orthographic size (현재 4.5) | 게임 디자인 결정 사항 |

**이번 작성 중 확인이 끝나 목록에서 내려온 항목** — `DefaultSettingHelper`의 저장 경로(= `Application.persistentDataPath/GameFrameworkSetting.dat`, §3-9) / Json 헬퍼 타입명 유효성(= `GameFramework.prefab:765`에 정상 설정, §2-1 4단계) / `DefaultUIFormHelper`의 로직 부착 규약(= **AddComponent가 아니라 GetComponent**, 엔티티와 정반대, §3-7) / `Assets/Plugins/GameFramework.pdb`와 `.unitypackage` 3개의 git 추적 여부(= **둘 다 미추적**, §1-3 / §7-6).
