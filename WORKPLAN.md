# 작업 계획 — 통합 우선순위

> 두 갈래 분석을 하나로 합친 것이다.
> - **저장소 현황·위험**: [`ARCHITECTURE.md`](ARCHITECTURE.md) §5.1 위험 Top 10
> - **포팅 설계**: [`PORTING.md`](PORTING.md) §0 정정 12건 + §3 선행 9건 + §4 마일스톤
>
> **라벨**: `[에디터]` = 사용자가 Unity에서 직접 / `[코드]` = Claude가 파일 수정
> **아직 아무 작업도 시작하지 않았다.**

---

## 왜 배치로 묶었나

이 저장소의 문제는 "버그 10개"가 아니라 **만들다 만 시스템 하나**다. 개별 항목을 하나씩 고치면 같은 파일과 같은 프리팹을 여러 번 열게 된다. 아래 4개 배치는 **한 번 열어서 끝나는 단위**로 묶은 것이다.

배치 A(에디터)와 B·C(코드)는 **건드리는 대상이 겹치지 않으므로 동시 진행 가능**하다. 사용자가 A를 하는 동안 Claude가 B·C를 돌리는 것이 가장 빠르다.

```
        ┌─ A [에디터] 40~60분 ─┐
시작 ───┤                      ├─→ D [코드] ─→ M1 ─→ M2 ─→ M3 ─→ M4~M6
        └─ B, C [코드] ────────┘
```

---

## 배치 A — ✅ 완료

| # | 작업 | 담당 | 결과 |
|---|---|---|---|
| A1 | Scripting Define Symbols | Claude | ✅ Standalone·Android 두 타겟 |
| A2 | Addressables Play Mode Script | 사용자 | ✅ 설정됨 (단 `Library/` 저장이라 **git 공유 안 됨** — 클론 시 각자 설정) |
| A3 | MainCamera 태그 | 사용자 | ✅ |
| A4 | 빌드 씬 목록 | Claude | ✅ MainScene 0번, SampleScene 비활성 |
| A5 | 중복 `ProcedureComponent` | 사용자 + Claude | ✅ 루트 컴포넌트 제거(사용자) + 씬 고아 오버라이드 2건 제거(Claude) |
| A6 | Girl/Boy 프리팹 | 사용자 | ✅ 7종 × 2 제거, 보존 대상 전부 살아 있음 |
| A7 | 레이어 정의 | Claude | ✅ 8/9/10/13/14 |
| A8 | 적 프리팹 5종 | 사용자 + Claude | ✅ 스크립트 3종 제거·NavAgent 비활성(사용자) + Hellephant 레이어 0→9(Claude) |
| A9 | Missing Script | 사용자 | ✅ 두 프리팹 삭제 |

**최종 검증**: 프리팹/씬 1204개 스캔 — dangling 참조 0건, missing script 0건.
Girl/Boy 보존 확인 — `Antenna`·`FrostCone`·LineRenderer 1개·파티클 10개, 앵커 333개.
적 5종 보존 확인 — 콜라이더 2종·HitParticles, 전 종 레이어 9.

> **규약 예외 기록**: A5의 씬 오버라이드와 A8의 Hellephant 레이어는 사용자 요청으로 Claude가 `.unity`/`.prefab`을 직접 편집했다. 그 외 프리팹 작업은 전부 에디터에서 수동 처리했다.

<details><summary>배치 A 원래 절차 (참고용)</summary>

| # | 작업 | 위치 |
|---|---|---|
| A2 | Addressables Play Mode Script = `Use Asset Database (fastest)` | Addressables > Groups |
| A3 | 카메라 GO Tag → `MainCamera` | MainScene |
| A5 | **루트** GameObject의 `ProcedureComponent` 삭제 | `GameFramework.prefab` |
| A6 | Girl/Boy 컴포넌트 7종 제거 | `Assets/Art/Prefabs/Characters/` |
| A8 | 적 5종 스크립트 3종 제거 + NavAgent OFF + 레이어 9 | `Assets/Art/Prefabs/Characters/` |
| A9 | Missing Script 프리팹 정리 | `Assets/Prefabs/` |

### A5 상세 — 중복 `ProcedureComponent`

`GameFramework.prefab`을 열면 `ProcedureComponent`가 **2개** 있다.

| 위치 | 값 | 처리 |
|---|---|---|
| 루트 `GameFramework` GO | `ProcedureMain` (네임스페이스 없음) | **삭제** |
| 자식 `Procedure` GO | `ToyBoxNightmare.ProcedureMain` | 유지 |

루트 쪽 컴포넌트 헤더 우클릭 → Remove Component. 씬에 남는 무의미 오버라이드는 프리팹 인스턴스에서 Revert 하면 정리된다.

### A6 상세 — Girl/Boy 프리팹 (양쪽 다)

**Remove Component 할 것 (총 7개):**
- 루트의 `Player`, `PlayerSelectLogic` ← 런타임에 `Entity.cs:98`이 자동 AddComponent 하므로 **기능 손실 없다**
- Sample 공격 5종: `LightningBolt`, `LightningAttack`, `StinkAttack`, `SlimeAttack`, `FrostAttack`

**GameObject는 지우지 말 것.** 자식 GO 계층·LineRenderer·파티클·발사점(`Antenna`, localPos `(0.123, 0.948, 1.019)`)은 M3~M4에서 재사용한다. **컴포넌트만** 제거한다.

제거 전에 각 공격 스크립트의 인스펙터 값을 `PORTING.md` §5와 대조해 둘 것. `lightningHit: None` 처럼 씬에서 주입되던 필드는 프리팹에 값이 없다.

### A8 상세 — 적 5종 (Zombunny / ZomBear / ZombieDuck / Clown / Hellephant)

> **클론 불필요.** 5종 모두 이미 정확한 짧은 키로 Addressables에 등록돼 있다(확인 완료).

각 프리팹마다:
- [ ] `EnemyMovement`, `EnemyHealth`, `EnemyAttack` **Remove Component**
      (남기면 접촉 즉시 `GameManager.Instance` NRE)
- [ ] **NavMeshAgent 체크 해제** (컴포넌트 삭제 아님)
      씬 미베이크 상태라 켜진 채 스폰하면 "Failed to create agent" + 이동 불능. M5에서 코드로 재활성한다
- [ ] 콜라이더 2종과 `hitParticles` 자식 GO **보존**
- [ ] **Hellephant만** 루트 레이어 `Default(0)` → **`Shootable(9)`**
      나머지 4종은 이미 9다(원본 ZombieToys 값). A7에서 9를 `Shootable`로 명명해 이제 유효하다

### A9 상세 — Missing Script

- `Assets/Prefabs/Player.prefab` — Transform + Missing Script 하나뿐인 껍데기. 컴포넌트 제거 또는 프리팹째 삭제
- `Assets/Prefabs/UpgradeForm.prefab` — 참조 0건. 삭제

<details><summary>참고: 이전에 스크립트로 편집을 시도했을 때 드러난 사실</summary>

프리팹 편집을 코드로 시도했다가 규약에 맞춰 되돌렸다. 그 과정에서 확인된 것:

1. **적 프리팹은 레거시 직렬화 포맷이다.** Girl/Boy는 `- component: {fileID: X}`인데 적 프리팹은 `- 114: {fileID: X}`를 쓴다. 스크립트로 프리팹을 다루는 도구를 만들 일이 있으면 두 포맷을 모두 처리해야 한다.
2. **적 5종은 이미 Addressables에 등록돼 있다** — 등록된 10개 주소: Boy, Clown, Dog, Environment, Girl, Hellephant, Sheep, ZomBear, ZombieDuck, Zombunny.
3. **레이어 9가 이미 프리팹에 박혀 있었다** — 4종은 손댈 필요 없고 Hellephant만 0이다.
4. Girl/Boy 프리팹의 앵커는 각각 332개, 제거 대상 컴포넌트는 각 7개다.

</details>

<details><summary>원래 계획의 상세 절차 (구버전, 참고용)</summary>

### A6 상세 — Girl/Boy 프리팹

**제거할 것** (루트에 baked 되어 있다):
- `ToyBoxNightmare.Player` — `Girl.prefab:44636`
- `ToyBoxNightmare.PlayerSelectLogic` — `:44648`
- Sample 공격 5종 GO: `LightningBolt`(`:5282`), `LightningAttack`(`:17053`), `StinkAttack`(`:17425`), `SlimeAttack`(`:37847`), `FrostAttack`(`:55228`)

Boy.prefab도 동일하게. 런타임에 `Entity.cs:98`이 `AddComponent` 하므로 **기능 손실이 없다** — 직렬화 값은 `PlayerSelectLogic.OnInit:24-30`이 `GetComponent`로 다시 채운다.

**반드시 보존할 것** (M3~M4에서 재사용):
- 발사점 `Antenna` 트랜스폼 — localPos `(0.123, 0.948, 1.019)`
- `FrostCone` 자식 GO, LineRenderer, 파티클 시스템

**제거 직전에**: 각 공격 스크립트의 인스펙터 실효값을 `PORTING.md` §5 표와 최종 대조할 것. 특히 `lightningHit: {fileID: 0}` 처럼 **씬에서 주입되던 필드는 프리팹에 값이 없다** — 그런 항목은 "없음"으로 기록.

### A7 상세 — 레이어

원본 마스크 값(Lightning `17920`, StinkHit `512`, MouseLocation `256`)을 그대로 쓰려면 **인덱스가 원본과 같아야 한다**:

| 인덱스 | 이름 | 용도 |
|---|---|---|
| 8 | `Floor` | 마우스 조준 레이캐스트 대상 |
| 9 | `Shootable` | 적. Lightning/Stink/Slime 명중 대상 |
| 10 | `Blocking` | 벽. Lightning 차폐 |
| 13 | `FrostFX` | Frost 콘 트리거 전용 |
| 14 | (환경) | Lightning 마스크에 포함 — Arches 등 배치 시 |

+ **Floor Collider 프리팹에 레이어 8 지정** — 이게 없으면 M4의 Stink/Slime 조준 유효 판정이 동작하지 않는다.

### A8 상세 — 적 5종 프리팹 (Zombunny / ZomBear / ZombieDuck / Clown / Hellephant)

각 프리팹마다 **4가지 전부** 해야 한다. 하나라도 빠지면 M1 또는 M3에서 막힌다:

- [ ] Sample 스크립트 3종 제거: `EnemyMovement`, `EnemyHealth`, `EnemyAttack`
      (남겨두면 `OnTriggerEnter`가 접촉 즉시 `GameManager.Instance` NRE)
- [ ] **NavMeshAgent를 Disable** (컴포넌트 삭제 아님 — 체크 해제)
      씬이 미베이크(`MainScene.unity:121` fileID 0)라 켜진 채 스폰하면 "Failed to create agent" + 이동 불능. M5에서 코드로 재활성한다.
- [ ] **루트 레이어 = `Shootable`(9)**
      ← 이게 빠지면 M3 Lightning이 **절대 안 맞는다**. 원인 추적이 매우 어렵다.
- [ ] 콜라이더 2종(캡슐+트리거)과 `hitParticles` 자식 GO **보존**
- [ ] Addressables Groups 창에서 등록 (Address = 짧은 키. 이미 등록된 10개 주소 참고)

</details>

</details>

---

## 배치 B `[코드]` — ✅ 완료 — 엔티티 안전 기반

전부 `Assets/GameMain/Entity/` 안. "풀링 재사용 안전성"이라는 한 가지 개념이라 한 번에 훑는 게 맞다. **배치 A와 병렬 가능**(파일이 겹치지 않는다).

| # | 작업 | 대상 | 해소 |
|---|---|---|---|
| B1 | `TargetableObject`에 `mHidden` 가드 + `SafeHide()` 공통 메서드 신설. `ApplyDamage` 선두에 `if (IsDead) return;` | `TargetableObject.cs` | §5.1[4] |
| B2 | 직접 `HideEntity` 호출 지점을 `SafeHide()`로 전환 | `Projectile.cs:37/52`, `ExpGem.cs:45`, `PlayerSelectLogic.cs:64-68` | §5.1[4] |
| B3 | `OnShow` 상태 리셋: `PlayerSelectLogic`(collider.enabled=true, 애니 트리거 리셋), `Player`(이동/공격 잠금 해제) | 두 파일 | PORTING §0-4 |
| B4 | 죽은 주석 4건 제거 + `Player.UpgradeMoveSpeed`(`:125-128`, NRE 확정) 제거 | `Enemy.cs:64,71`, `ExpGem.cs:44`, `TargetableObject.cs:82`, `Player.cs` | §5.1[10-b][10-c] |

**B3이 중요한 이유**: 지금은 재시작이 없어서 안 보이지만, D3에서 프로시저 전이를 넣는 순간 **낙선했던 캐릭터가 콜라이더 꺼진 죽은 포즈로 재등장**한다. `DisableAndHide`(`:57-62`)가 끈 것을 아무도 다시 켜지 않는다.

---

## 배치 C `[코드]` — ✅ 완료 — Addressables 누수

`Assets/Scripts/Resource/ResourceManager.cs` 한 파일. **A·B와 병렬 가능.**
이 파일은 100% 자작 코드라 upstream 부채가 없다 — 프레임워크 계층이지만 자유롭게 고쳐도 된다.

| # | 작업 | 해소 |
|---|---|---|
| C1 | `mAssetHandles` 키를 **로드 결과 오브젝트 → `assetName` 문자열**로 바꾸고 refcount 보유 | §5.1[1] |
| C2 | `UnloadAsset`은 카운트 0일 때만 `Addressables.Release` | §5.1[1] |
| C3 | 실패 경로(`:172-176`)에 `Addressables.Release(op)` 추가 | §5.1[1] |

**지금도 새고 있다.** 캐릭터 선택 한 번에 주소당 1 refcount. 적 스폰이 시작되면 같은 주소를 반복 로드하므로 누수가 폭증한다 — M1 전에 끝내는 게 좋다.

---

## 배치 D `[코드]` — 게임 루프 뼈대

M1·M2가 전부 이 위에 올라간다. **A4·A5 완료가 선행 조건**이다.

| # | 작업 | 해소 |
|---|---|---|
| D1 | `SurvivalGame.Update()` 오버라이드 부활 + `ProcedureMain.OnUpdate`에서 호출 | 스포너·타이머의 전제 |
| D2 | 이벤트 구독/해제 대칭화 — `CharacterSelectedEventArgs` 좀비 핸들러 방어 | §5.1[7] |
| D3 | `ProcedureGameOver` 신설: `OnEnter`에서 `GetAllLoadedEntities()` 순회 Hide, `OnUpdate`에서 R키 폴링 → `ChangeState<ProcedureMain>` | §5.1[6] 대체 재시작 |
| D4 | 이벤트 클래스 신설: `EnemyDiedEventArgs`, `PlayerDiedEventArgs`, `ScoreChangedEventArgs` | M1·M2·M6 연결 |

→ **`[에디터]` 후속 1건**: `GameFramework.prefab` 자식 `Procedure` GO의 `mAvailableProcedureTypeNames`에 `ToyBoxNightmare.ProcedureGameOver` 추가 (네임스페이스 포함 전체 이름)

---

## 그 다음 — 포팅 마일스톤

상세는 [`PORTING.md`](PORTING.md) §4. 각 마일스톤은 **플레이 가능한 상태로 끝난다.**

| | 내용 | 신규 EntityLogic | 에디터 작업 |
|---|---|---|---|
| **M1** | 적 5종 스폰 + 추적 + 사망 연출 + 점수 | `Enemy` 부활 | A8 완료분 사용, 카메라 추종 부착 |
| **M2** | 플레이어 피격 + 게임오버 + 재시작 | — | ProcedureGameOver 등록(D4 후속) |
| **M3** | 공격 1종 (Lightning) | 0개 ← 그래서 첫 무기 | 볼트 머티리얼 URP 재작성 |
| **M4** | 나머지 공격 3종 + 디버프 | 투사체 2종 | 투사체 프리팹 + Addressables |
| **M6** | UI 기반 — HUD / 점수 / 체력 / 쿨다운 게이지 | — | UI 그룹 배선, EventSystem, UIForm 프리팹 |
| **M5** | 아군(양) | `Ally` | Addressables 이미 등록됨(Sheep/Dog) |
| **M7** | 연출 — 카메라 인트로 / 스포트라이트 / 사운드 | — | 스포트라이트 프리팹 배치 |
| **M8** | 일시정지 / 옵션 | — | UIForm 프리팹 1개, UI 그룹 2개 |

> **실행 순서는 M6 → M5 → M7 → M8 이다.** 번호는 원래 계획의 것을 유지한다.
> M6 을 먼저 하는 이유: 남은 92건 중 약 25건이 UI 계층 부재 하나에 막혀 있고,
> 전역 쿨다운을 되돌린 뒤로 "5초 동안 왜 안 나가는지" 를 화면에서 알 방법이 없다.

**M3에서 Lightning을 먼저 하는 이유**: 즉발 히트스캔이라 투사체 엔티티·디버프·적 수신 API가 전부 불필요하다. 신규 EntityLogic 0개로 "무기가 작동한다"를 검증할 수 있다.

---

## M5–M8 상세 — 원본 전수 대조 결과 (2026-08-14)

원본 좀비토이 스크립트 28개를 전부 우리 것과 대조했다. **197개 항목 중 92개가 남았다**
(미구현 75 / 부분구현 17). 나머지 105개는 구현 완료이거나 GameFramework 로 대체됐다.

**대전제: 전부 GameFramework 경로로 만든다.** UI 는 `UIComponent`/`UIFormLogic`,
사운드는 `SoundComponent`, 통신은 `EventComponent`, 스폰은 `EntityComponent` 를 쓴다.
씬에 캔버스를 직접 배치하거나 `AudioSource.Play()` 를 흩뿌리는 축소판 경로는 쓰지 않는다.

### 남은 92건이 걸려 있는 병목

| | 병목 | 막힌 항목 |
|---|---|---|
| ① | **UI 계층 부재** — `mUIGroups: []`, 씬에 EventSystem 없음, `UIFormLogic` 0개, UI Addressables 0건 | ~25 |
| ② | **적 추격 대상이 `Player.Instance` 고정** — 원본은 `GameManager.EnemyTarget` 간접 참조 | ~12 |

②는 아군을 소환해도 적이 그쪽으로 안 몰린다는 뜻이다. `SurvivalGame.ChaseTarget` 하나로 풀린다.

### 이미 준비된 자산 (착수 비용이 낮은 이유)

- `HUDCanvas.prefab` / `PauseMenuCanvas.prefab` 이 `Assets/Art/Prefabs/UI/` 에 **원본과 byte-identical** 로 있다(guid 동일).
- `FlashFade.cs` 가 `Assets/Sample/Scripts/UI/` 에 그대로 있다(Assembly-CSharp 이라 이미 컴파일된다).
- `ScoreChangedEventArgs` / `PlayerDiedEventArgs` 는 이미 발행되고 있다. 전자의 주석이 "HUD 가 구독한다(M6)" 라고 예고해 뒀다.
- `TargetableObject.OnHitPointChanged` 훅이 이미 있다 — **오버라이드가 0건**일 뿐이다.
- Sheep / Dog 가 Addressables 에 이미 등록돼 있다.
- NavMesh 는 베이크돼 있다(`Assets/Scenes/MainScene/NavMesh.asset`). M5 의 선행이 아니다.

### M6 — UI 기반 (약 25건 해제)

1. 씬에 UI 루트 Canvas(Screen Space Overlay) + EventSystem(`InputSystemUIInputModule`) 추가
2. `GameFramework.prefab` 의 `UIComponent`: `mInstanceRoot` = 그 Canvas, `mUIGroups` 에 `Default`(depth 0) 추가
3. `Assets/GameMain/UI/HUDForm.cs` — `UIFormLogic` 파생. **프리팹에 직접 부착**(엔티티와 정반대 규약) ✅
4. `HUDCanvas` 를 `Assets/GameMain/UI/` 로 복제 + `HUDForm` 부착 + Addressables 등록
5. 신규 이벤트 2종: `PlayerHealthChangedEventArgs`, `WeaponCooldownStartedEventArgs`
6. `Player` 가 `OnHitPointChanged` 오버라이드 → 발행. `WeaponLoadout.BeginCooldown` → Player 경유 발행
7. HUD 가 구독: 점수 / 체력 슬라이더 / **쿨다운 게이지** / 게임오버 텍스트 / 피격 붉은 플래시

**EventSystem 은 M6 에서 넣지 않았다.** HUD 는 표시 전용이라 필요가 없고, 넣는 순간 아래 함정이
발동한다. 버튼이 실제로 필요해지는 M8 에서 함께 넣고 그때 선택 클릭을 재검증한다.

**함정 2개**
- EventSystem 을 넣는 순간 `PlayerSelectLogic` 의 `IsPointerOverGameObject()` 가드가 **처음으로 살아난다.**
  지금은 EventSystem 이 없어 항상 통과하고 있었다. 넣은 뒤 캐릭터 선택이 되는지 반드시 재검증할 것.
- `DefaultUIGroupHelper.SetDepth` 가 **빈 구현**이다. UI 그룹을 2개 이상 겹쳐 쓰려면(M8) 커스텀 헬퍼가 필요하다.

### M5 — 아군(양)

1. **추격 대상 추상화** — `SurvivalGame.ChaseTarget` 을 두고 `Enemy.UpdateChase` 가 폴링.
   원본의 폴링 지연이 게임 느낌의 일부라 이벤트로 바꾸지 말 것.
2. `Ally : EntityLogicBase` + `AllyData`(ReferencePool). 이동은 `Enemy.PlaceOnNavMesh` 패턴을 그대로 복제.
3. 점수 → 소환 포인트(비용 30) / 동시 1마리 / 지속 10초 후 자동 회수 / 회수 시 포인트 몰수
4. 소환 입력(1키) + HUD 아이콘(M6 선행)
5. 스폰 좌표 `(29.93, 0, 4.61)` — 원본 값

### M7 — 연출 / 사운드

- 카메라 인트로 전환: 선택 앵글 → 게임 앵글 1초 (최종 pitch 30°)
- 캐릭터 선택 스포트라이트 — 원본 `CharacterSpotlight`/`LookAtMouse` 는 `GameManager`/`MouseLocation` 에
  의존해 그대로 넣으면 NRE 다. `Player.AimPoint` 를 쓰는 GameMain 대체 스크립트로 다시 쓴다.
- 사운드: **`SoundComponent` 경로로 통일한다.** `mSoundGroups`(Music/SFX) 등록 + `mAudioMixer` 지정 +
  오디오 클립 Addressables 등록. 클립 6종은 `Assets/Audio/` 에 있으나 Addressables 에는 0건이다.

### M8 — 일시정지 / 옵션

- `PauseMenuForm`(UIFormLogic) + Esc(Input System) + 볼륨 슬라이더 2종 + 음소거 스냅샷 + Quit
- HUD 위에 띄우므로 **UI 그룹 2개** → `DefaultUIGroupHelper.SetDepth` 커스텀 필요

### 대상 외로 분류

- 모바일 터치 입력(`PlayerInputTouch` / `Touchpad` / `MobileInterface`) — 원본에도 터치 UI 프리팹이 없어
  참고 자산이 0이다. PC 전용으로 확정하면 세 파일은 삭제 대상.

---

## 결정 기록

| # | 결정 | 답 | 반영 |
|---|---|---|---|
| ① | 무기 UX | **(b) 뱀서라이크 — 자동 발사 + 다중 장착** | `WeaponBase` 재작성(입력 훅 제거), `PORTING.md` §0 에 무효화된 서술 표기 |
| ② | NavMesh 시점 | **지금 베이크 + M1 부터 NavMesh** | 베이크 완료(radius 0.5 / height 1.2 / slope 45). 스폰 좌표 6곳 전부 NavMesh 위 확인 |
| ③ | `88408fd` 폐기/초기화 | **미정** | M5(아군) 전까지 필요. 점수 = 아군 화폐 vs 경험치 통합 |
| ④ | 무기 UX 재결정 | **원본 수동 조준으로 복귀** | 자동조준이 재미없다는 플레이 판정. 마커 링 풀 등 자동조준 인프라 전량 삭제 |
| ⑤ | 오디오 경로 | **GameFramework `SoundComponent`** | "전부 프레임워크로" 지시. `AudioSource` 직접 재생 경로는 정리 대상 |
| ⑥ | 모바일 지원 | **미정** | 안 할 거면 터치 입력 3파일 삭제 |

## 진행 현황

| | 상태 |
|---|---|
| 배치 A·B·C | ✅ 완료 |
| Unity CLI 연결 | ✅ 완료 |
| 1. PlayerCameraFollow | ✅ 완료 (플레이어 없을 때 선택 앵글 복귀 포함) |
| 2. 입력 백엔드 정리 | ⚠️ 코드만 완료 — `activeInputHandler = 1` 전환은 보류(에디터 재시작 필요) |
| 3. 배치 D — 게임 루프 뼈대 | ✅ 완료 (리뷰 6건 반영) |
| 4. M1 — 적 스폰·추적·사망·점수 | ✅ 완료 (리뷰 4건 반영) |
| 5. M2 — 피격·게임오버·재시작 | ✅ 완료 (사망 연출 + PlayerDead 승리 연출) |
| 6. M3 — 공격 1종 (Lightning) | ✅ 코드 완료, 플레이 검증 대기 |
| 7. M4 — 나머지 공격 3종 + 디버프 | ✅ 완료 |
| 8. 원본 수동 조준 복원 | ✅ 완료 (자동조준 폐기, 전역 쿨다운) |
| 9. 가독성 리팩터 + 예외 보강 | ✅ 완료 (189항목 / 방어 81건) |
| 10. `EntityData` → `ReferencePool` | ✅ 완료 (strict check 통과) |
| 11. **M6 — UI 기반** | ✅ 코드·배선 완료, 플레이 검증 통과 (점수/체력/쿨다운/게임오버) |
| 12. M5 — 아군(양) | ⬜ 결정 ③ 필요 |
| 13. M7 — 연출 / 사운드 | ⬜ |
| 14. M8 — 일시정지 / 옵션 | ⬜ |

## 남은 결정

이것들은 **답이 갈리면 만드는 코드가 달라진다.** 나머지 미결정(PORTING.md §8)은 해당 마일스톤에서 정해도 된다.

**1. 무기 UX — 원본 재현인가, 뱀서라이크인가?** → M3 설계가 갈린다
- (a) **원본 재현**: 마우스 수동 발사 + Tab 전환 + 전 무기 공용 쿨다운 1개. `WeaponController` 신설. 계획서는 이 기준으로 작성됨
- (b) **뱀서라이크**: 자동 발사 + 다중 장착. 기존 `WeaponBase`의 원설계가 이쪽이다

**2. 적 이동 — NavMesh를 언제 켜나?** → M1 작업량이 갈린다
- (a) M1은 Transform 직선 추적, M5 전에 베이크 (계획서 기준)
- (b) M1부터 NavMesh — 지금 베이크하고 시작. 장난감 지형이라 직선 추적은 벽 끼임이 예상됨

**3. `88408fd` 삭제가 폐기인가 초기화인가?** → 점수 경제 설계가 갈린다
원본은 "점수 = 아군 소환 화폐(30점)"인데, 삭제된 ExpGem/레벨업을 되살리면 두 경제가 충돌한다. 이건 코드로 판정할 수 없어 확인이 필요하다.

> 디버프 구현 방식(PORTING.md §8-3)은 검증 결과 **`Enemy` 내부 상태 + VFX 자식 GO** 를 기본안으로 확정했다. `AttachEntity`는 이 저장소에서 사용 실적이 0이라 위험하다. 별도 지시가 없으면 이대로 간다.
