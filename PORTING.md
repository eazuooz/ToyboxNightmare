<!-- 이 문서는 워크플로 산출물이다. §0 정정 사항이 본문(§1~§8)보다 우선한다. -->

> **읽는 순서**: §0(정정) → §3(선행 작업) → §4(마일스톤). 튜닝값이 필요할 때 §5를 찾아본다.
> 통합 우선순위와 작업 분담은 [`WORKPLAN.md`](WORKPLAN.md)를 볼 것.

---

# §0. 계획 정정 — 본문보다 우선한다

## ⚠ 확정된 설계 결정 — 무기 UX는 **뱀서라이크**다

사용자 결정: **자동 발사 + 다중 장착**(뱀서라이크). 원본 재현이 아니다.

**따라서 본문의 다음 서술은 무효다:**

| 무효가 된 서술 | 실제 |
|---|---|
| §2 매핑표 `PlayerAttack → WeaponController`(공용 쿨다운·Tab 전환·입력 라우팅) | `WeaponController` 를 만들지 않는다. 무기마다 **자기 쿨다운**을 돌린다 |
| §2/§4 의 `OnFireStart` / `OnFireHeld` / `OnFireStop` 훅 | 전부 제거됨. `WeaponBase.Attack()` 하나로 통일 |
| §4 M3 "마우스 좌클릭 발사 + Tab 전환" | 입력 없음. 쿨다운마다 **가장 가까운 적을 자동 조준** |
| §0-10 "Frost 는 공용 쿨다운을 소모하지 않는 예외" | 공용 쿨다운 자체가 없으므로 무의미 |
| §6 "Lightning/Frost 는 press, Stink/Slime 은 release 발사" | 전부 자동 발사 |
| §8-2 미결정 "무기 UX 방향" | **해결됨** |

**여전히 유효한 것**: §5 튜닝값 전부(데미지 50, 사거리 20, 마스크 17920, 쿨다운 값 등), 무기별 효과의 성질(Frost 빙결, Stink 도주, Slime DoT), M4 의 투사체·디버프 설계.

M4 의 Frost/Stink/Slime 도 같은 방식으로 자동화한다 — Frost 는 홀드가 아니라 주기적 콘 판정, Stink/Slime 은 자동 조준 투사체.

---


아래는 계획 초안을 양쪽 저장소와 대조 검증한 결과다. **본문(§1~§8)의 서술과 충돌하면 이 절이 이긴다.**
튜닝값(§5)은 표본 전수 대조에서 전부 일치했다 — 정정 대상은 수치가 아니라 **절차와 판정**이다.
검증 방법: 계획서의 주장을 양쪽 저장소에서 직접 대조했다(타깃 `SurvivalGame.cs`/`Enemy.cs`/`TargetableObject.cs`/`WeaponBase.cs`/`Prefabs.asset`/`GameFramework.prefab`, 원본 `EnemyHealth.cs`/`EnemyAttack.cs`/`EnemySpawner.cs`/`PlayerAttack.cs`/`AllyManager.cs` 및 프리팹 직렬화 값). 튜닝표 수치는 표본 전수(적 5종 스탯, 스포너 5종, Lightning 50/20/17920, Stink 5/9, StinkHit 4/4/512, StinkProjectile 10, SlimeDebuff 3/2/20, Sheep 10, allyCost 30, delayOnPlayerDeath 1, 선택 배치 -3)가 **전부 일치**했다. 문제는 수치가 아니라 절차와 판정에 있다.

---

## 1. [M1 즉시 막힘] 적 프리팹 클론 명세에 NavMeshAgent와 레이어 지정이 빠져 있다

확인 결과 타깃 `Assets/Art/Prefabs/Characters/Zombunny.prefab`에는 Sample 스크립트 3종 외에 **NavMeshAgent(:308, speed 3.5)가 있고 Rigidbody는 없다.** 선행 8은 "Sample 스크립트 3종 제거"만 지시한다. 씬은 미베이크(`MainScene.unity:121` fileID 0)이므로 클론을 그대로 스폰하면 계획서 §6이 스스로 경고한 "Failed to create agent + 이동 불능"이 **M1 첫 스폰에서** 발생한다. 또 M3 Lightning 마스크 17920(=9|10|14)과 M4 StinkHit 마스크 512는 **적이 레이어 9(Shootable)에 있어야만 명중하는데**, 클론 프리팹의 레이어를 9로 바꾸는 작업이 선행 9에도 M1~M4 에디터 작업에도 없다(선행 9는 레이어 "생성"만 지시). 
**수정**: 선행 8을 체크리스트로 확장 — (a) 스크립트 3종 제거, (b) **NavMeshAgent Disable**(M5에서 재활성, `OnShow`에서 코드로 제어), (c) **루트 레이어 = Shootable(9)** 지정, (d) 콜라이더 2종·hitParticles 자식 보존 확인. 선행 9와 같은 에디터 세션으로 묶는다.

## 2. [GF 규약 위반 유발] `OnShowEntitySuccess`에서 `AddComponent<WeaponController>` — 재시작마다 중복 부착된다

M3의 부착 경로(§4-2 0단계 인용)는 재시작이 없던 시점의 레시피다. M2가 프로시저 전이 재시작을 도입하면 Player 엔티티는 **풀에서 재사용**되고(엔티티 그룹 Player, capacity 4, expire 60s — `GameFramework.prefab:1069-1073`), 재시작 후 `SpawnPlayer` → `OnShowEntitySuccess`가 다시 돌아 **두 번째 WeaponController + 무기 4종이 중복 AddComponent**된다. 발사 2중 처리·쿨다운 경합으로, §5.1[2]와 동형의 baked 문제를 런타임에 재생산하는 지시다.
**수정**: `GetComponent<WeaponController>() ?? AddComponent` 패턴 + 매 Show마다 `Initialize(player)`로 상태(쿨다운, 슬롯 인덱스, Frost 켜짐)를 리셋. `Player.OnHide`에서 발사 중 상태 강제 종료도 함께 명시.

## 3. [설계 오류] 사망 연출 시퀀스의 순서가 원본과 다르고, `base.OnDead`와의 충돌을 경고하지 않는다

M1 지시 "캡슐 트리거화→Dead 트리거→**2초 후 침하 2.5/s**→HideEntity"는 원본과 다르다. 원본(`EnemyHealth.cs`)은 **Defeated 즉시 `Invoke(TurnOff, 2s)`를 걸고, 침하는 StartSinking 애니메이션 이벤트 시점부터 TurnOff까지** 진행된다. 계획대로면 2초 동안 서 있다가 침하가 시작되는 순간 사라진다. 또 타깃 `TargetableObject.OnDead`(`:62-65`)는 **즉시 HideEntity를 호출**하므로, `Enemy.OnDead`에서 `base.OnDead(attacker)`를 부르면(현재 코드 `Enemy.cs:72`가 그렇다) 연출 없이 즉사 + 시퀀스 끝의 Hide와 합쳐져 이중 Hide다.
**수정**: M1 명세를 "OnDead 오버라이드에서 **base 호출 금지**, Dead 트리거 + 2초 타이머 시작, `StartSinking()` 수신 시부터 침하, 타이머 만료 시 mHidden 가드 후 HideEntity 1회"로 교체. ZomBear는 FBX 이벤트가 없으므로(계획서 스스로 인정) **타이머 폴백 침하**를 명시.

## 4. [M2 확정 버그] 재시작 시 풀 재사용 엔티티의 시각·물리 상태 리셋이 계획에 없다

`PlayerSelectLogic.DisableAndHide`(`:57-62`)는 **capsuleCollider를 끄고 Die 트리거를 쏜 채** Hide한다. `OnShow`(`:32-43`)는 위치/키만 복원한다. M2 재시작으로 같은 인스턴스가 풀에서 나오면 **낙선했던 캐릭터는 콜라이더가 꺼져 클릭 불가 + 죽은 포즈**로 등장한다. Player도 동일: M2가 추가할 "Die" 트리거·이동/공격 잠금이 OnShow에서 풀리지 않는다. 계획은 Enemy에만 "OnShow 리셋"을 명시했다.
**수정**: M2 생성/수정 목록에 `PlayerSelectLogic.OnShow`(collider.enabled=true, `animator.Rebind()` 또는 ResetTrigger)와 `Player.OnShow`(잠금 해제, 애니메이터 리셋) 추가. M2 완료 판정에 "재시작 후 두 캐릭터 모두 클릭 가능·기본 포즈" 항목 추가.

## 5. [M1 설계 공백] `GetEntities("Enemy")`로는 종별 상한(4/3/2/2/2)을 판정할 수 없다

스포너 5개는 각자 cap을 갖는데(`Spawn Points.prefab` maxEnemies 4/3/2/2/2 — 검증 일치) `GetEntities("Enemy")`는 그룹 전체를 반환한다. 계획서의 두 대안("이벤트 증감 또는 GetEntities")을 병기만 하고 종별 분리 방법을 안 정했다.
**수정**: `GetEntities("Enemy")` 순회 시 `Entity.EntityAssetName`(=Addressables 주소)으로 필터해 종별 카운트하는 방식으로 확정. 이벤트 증감 방식을 쓴다면 `HideEntityCompleteEventArgs`에서 어느 종이 죽었는지 식별할 키(EntityAssetName) 사용을 명시.

## 6. [선행 2 미결 → M3 차단] "Sample 공격 스크립트 5종 제거 여부 판단"은 판단이 아니라 필수다

M3/M4는 Girl/Boy의 자식 GO(LightningBolt, FrostCone, Antenna)를 재사용한다. baked `LightningAttack`은 지금 활성 GO에 있고(`Girl.prefab` FrostAttack만 `m_IsActive: 0`), Sample 코드는 `GameManager.Instance`/`MouseLocation`을 참조한다 — 무기를 켜는 순간 NRE. 선행 작업표에 "판단"으로 남겨두면 M3 시작 시점에 에디터 작업이 한 번 더 필요해진다.
**수정**: 선행 2에서 **제거 확정**. 단 (a) 자식 GO·LineRenderer·파티클·발사점 트랜스폼은 보존, (b) 제거 직전 인스펙터 실효값을 §5 이관표와 최종 대조(특히 `lightningHit: {fileID: 0}` 같은 씬 주입 필드는 프리팹에 값이 없음을 기록).

## 7. [매핑표 "그대로"→실제 "개조"] Countdown/FlashFade

"HUDForm 프리팹 하위에 그대로 부착"은 세 가지와 충돌한다: (a) M3의 "Sample 직접 참조 금지" 원칙과 모순, (b) `Countdown.Awake`의 슬라이더 초기화는 §5-2 "UIFormLogic에서 Awake/Start 금지" 규약 위반을 프리팹 계층에 심는 것, (c) `BeginCountdown`의 `StartCoroutine`은 CloseUIForm(비활성화→풀 반납) 시 죽어 **재오픈 시 슬라이더가 stale 값으로 고정**된다.
**수정**: 두 스크립트를 `Assets/GameMain/UI/`로 복사·정리(코루틴 → `OnUpdate` 또는 OnClose에서 상태 리셋), 매핑표 판정을 "개조"로 변경.

## 8. [매핑표 자기모순] LightningBolt "그대로"

매핑표는 "그대로", M3 본문은 "Sample 코드 기반 **정리본 신규 작성**", §6은 LineRenderer 머티리얼 URP 재작성 필요 — 세 곳이 서로 다르다. 실제로는 코드 신규 작성 + 머티리얼 재작성이므로 "개조"다. 판정 하나로 통일하고 M3 에디터 작업에 "볼트 머티리얼 URP 파티클/Unlit 재작성"을 명기할 것(현재 M3 에디터 작업엔 히트 이펙트 등록만 있다).

## 9. [M2 공백] ProcedureGameOver → ProcedureMain 복귀 트리거가 정의되지 않았다

M6 전에는 UI가 없다. "재진입 시 ProcedureMain으로 복귀 = 재시작"이라고만 쓰면 구현자는 OnEnter 즉시 복귀(무한 루프)나 임의 구현을 하게 된다. 잔존 엔티티 정리를 **어느 프로시저의 어느 훅에서** 하는지도 미지정이다(ProcedureMain.OnLeave는 `mGame.Shutdown()`만 한다 — `ProcedureMain.cs:33-39`).
**수정**: "ProcedureGameOver.OnUpdate에서 R키(`Keyboard.current.rKey.wasPressedThisFrame`) 폴링 → ChangeState<ProcedureMain>", 엔티티 전량 정리는 "ProcedureGameOver.OnEnter에서 `GetAllLoadedEntities()` 순회 HideEntity"로 위치를 못 박는다.

## 10. [튜닝표 누락 2건] Frost의 쿨다운 예외, 선택 배치의 좌우 반전

(a) 원본 `PlayerAttack.ToggleFrost`는 `attackCooldown`을 **설정하지 않는다** — Frost는 공용 쿨다운을 소모하지 않는 유일한 무기다. 요약의 "전 무기 공용 쿨다운"과 표 어디에도 이 예외가 없어, WeaponController 구현 시 Frost에 쿨다운을 넣는 오이식을 유발한다. (b) 배치 차이는 "±3 vs ±2"(거리)만 기록했지만 실제로는 **좌우도 뒤집혔다**: 원본 Boy x=-3/Girl x=+3, 타깃 Girl x=-2/Boy x=+2(`SurvivalGame.cs:37-38`). 원본 재현이 목표면 거리와 좌우를 함께 정정해야 한다.
**수정**: 튜닝표에 "Frost: 쿨다운 없음(공용 쿨다운 미소모, `PlayerAttack.cs` ToggleFrost)" 행 추가, 미결정 11을 "speed 6/5 + 배치 **좌우·거리**"로 갱신.

## 11. [순서 모순] 미결정 3(디버프 구현 방식)이 M4 착수를 막는데, M4는 이미 한쪽으로 쓰여 있다

M4는 `SlimeDebuffLogic`·Frost 디버프의 프리팹 제작 + Addressables 등록(엔티티안)을 에디터 작업으로 지시하면서, §8-3은 "미결정 — 후자(Enemy 내부 상태)가 저위험"이라 한다. 저위험안을 채택하면 M4의 에디터 작업 절반이 무효가 되고, 엔티티안을 채택하면 검증 안 된 `AttachEntity` 체인을 처음 밟게 된다.
**수정**: 미결정 3을 "M4 진입 조건"으로 승격하고, 계획 기본안을 **Enemy 내부 상태 + VFX 자식 GO(비엔티티)**로 확정 서술(SlimeDebuff의 6틱 타이머·공격봉인 bool·Frost freeze를 Enemy가 보유, VFX만 붙였다 뗌). M4 파일 목록에서 `SlimeDebuffLogic`(EntityLogic)을 조건부로 표기.

## 12. [의미 변화 과소평가] Plane(y=0) 조준은 MouseLocation의 "무효 판정"을 재현할 수 없다

원본 `MouseLocation`은 Floor 레이어(256) 레이캐스트라 **바닥 밖을 가리키면 실패(IsValid=false)**하고, 이것이 Stink 사거리 판정·레티클 3색(빨강=무효)의 전제다. 타깃의 수학적 무한 평면(`Player.cs:109-121`)은 항상 성공하므로 맵 밖 조준도 유효가 된다. 매핑표는 "IsValid 재도입"이라고만 쓰고 방식이 없다 — 무한 평면 위에서는 재도입할 근거 자체가 없다.
**수정**: M4 선행으로 "Floor(8) 레이어 레이캐스트(마스크 256, 거리 100) 유틸을 `Assets/GameMain/Utility/`에 신설, 실패 시 invalid 반환"을 명시하고, Player 회전은 기존 평면 방식 유지 / Stink·Slime 조준만 유틸 사용으로 분리한다. 이는 선행 9(레이어 도입) 및 바닥 오브젝트의 레이어 8 지정 에디터 작업과 의존 관계이므로 그 항목에 "Floor Collider 프리팹 레이어 지정"을 추가할 것.

---

**종합**: 튜닝값 추출과 corrections 반영의 품질은 높다(표본 전수 일치). 그러나 계획은 (1) 에디터 작업의 "컴포넌트 단위" 명세가 얕아 M1·M3에서 각 1회씩 막히고, (2) 풀링 재사용(재시작 도입 후)의 상태 리셋을 Enemy에만 적용해 Player/PlayerSelect/WeaponController에서 GF 규약 위반을 재생산하며, (3) 매핑표의 "그대로" 4건 중 3건(Countdown/FlashFade, LightningBolt, MouseLocation)이 실제로는 개조다. 위 1·2·3·4를 반영하기 전에는 M1~M2 완료 판정을 통과할 수 없다.

---
# ZombieToys → ToyboxNightmare 포팅 계획

> 근거 문서: `D:/Github/ToyboxNightmare/ARCHITECTURE.md` §4(레시피)·§5.1(위험 Top 10)·§5-2(규약)·§6(현재 상태) 직접 확인 완료. 분석 자료와 검증 corrections가 충돌하는 항목은 전부 corrections를 채택했다(SlimeDebuff 2회/20dmg, StinkHit 반경 4, FrostAttack 비활성 시작, 레이어 13=Frost VFX 전용, Ally도 SetActive 풀 패턴, 크로스 프리팹 주입 16건 등).

---

## 1. 요약 — 무엇을 옮기고 무엇을 버리는가

**옮기는 것 (게임의 실체):**
- **적 5종**(Zombunny/ZomBear/ZombieDuck/Clown/Hellephant)의 스탯·스폰 케이던스·"간격 루프+범위 플래그" 공격 설계·사망 연출(2초+침하 2.5/s). 프리팹·Addressables 주소는 타깃에 이미 존재한다(`Assets/AddressableAssetsData/AssetGroups/Prefabs.asset`, 주소 10개).
- **플레이어 전투**: 피격/사망(HP 100), 공격 4종(Lightning/Frost/Stink/Slime)의 상태기계와 튜닝값 전부, 전 무기 공용 쿨다운+무기 전환.
- **아군(양) 디코이 시스템**: 점수→allyPoints, 30점 소환, 10초 어그로 전환, 회수 시 포인트 몰수.
- **게임 루프**: 캐릭터 선택 → 스포너 기동 → 전투 → 게임오버 → 재시작. 단 재시작은 씬 리로드가 아니라 **프로시저 전이**(§4-3 ★ 확정 패턴)로 재설계.
- **UI**: HUD(점수/체력/쿨다운/피격 플래시/게임오버), 일시정지 — GF `UIComponent` UIForm 2개로.
- **튜닝값 전부** (§5 이관표).

**버리는 것:** 씬 오버라이드 배선(16건), 자체 SetActive 풀링 3계열, `SceneManager.LoadScene` 재시작, 카메라 Animator 핸드오버 트릭, 모바일 계층(원본에서도 죽은 코드), 싱글턴 3종(GameManager/MouseLocation/AllyManager — SurvivalGame으로 흡수). 상세는 §7.

**핵심 구조 전환 원칙 3가지:**
1. 원본의 "씬에 미리 배치 + 인스펙터 오버라이드 주입"은 전부 **`ShowEntity` + `EntityData` 페이로드 + `EventComponent` 이벤트**로 뒤집는다.
2. 원본의 "Instantiate 1회 + SetActive 토글" 풀은 전부 **GF ObjectPool(ShowEntity/HideEntity 정확히 1회)**이 대체한다.
3. 값 이관은 반드시 **프리팹 직렬화 값 기준**(코드 기본값과 다른 필드가 7개: damage 50, phaseDuration 0.01, stink range 9, stink speed 10, slime 2회/20dmg, explosionRadius 4).

---

## 2. 시스템 매핑표

| ZombieToys 타입 | GF 구현물 | 난이도 | 근거 |
|---|---|---|---|
| `GameManager` | `SurvivalGame` 확장 (점수·EnemyTarget·GameOver 플래그) + `ProcedureGameOver` 신설 | **재설계** | 싱글턴→`SurvivalGame.Instance` 이미 존재(`SurvivalGame.cs:14`). 씬 리로드 재시작 불가(§5.1[6]) → §4-3 ★ 패턴 |
| `EnemySpawner` ×5 | `SurvivalGame.Update` 내 **순수 C# 스포너 레코드**(주소·좌표·rate·cap) — MonoBehaviour 아님 | **재설계** | 자체 풀이 GF ObjectPool과 정면 충돌. `Update` 오버라이드 부활 선행(§4-3 ★ 1단계). 상한 카운트는 `GetEntities("Enemy")`(§4-1 10단계) |
| `AllyManager` + `Ally` | `SurvivalGame` 내 아군 서브시스템 + `Ally`(EntityLogic)+`AllyData` | **재설계** | Ally도 SetActive 풀 패턴(correction) → ShowEntity/HideEntity. Invoke 타이머→Update 누적 타이머 |
| `MouseLocation` | `Player.GetMouseWorldPosition()`(`Player.cs:109-121`, 기존) 유지, 소비자 늘면 공용 유틸 승격 | **그대로/개조** | 타깃은 수학적 Plane(y=0). IsValid 개념(사거리 밖 판정)은 Stink/Slime 이식 시 재도입 |
| `PlayerMovement` | `Player.cs` (이식 완료) | **그대로** | MovePosition+MoveRotation+IsWalking 동일. speed 5→**6** 정정만 |
| `PlayerHealth` | `Player : TargetableObject` + `PlayerData` — `ApplyDamage` 경로 + `OnDead` 오버라이드 | **개조** | 골격 존재(`TargetableObject.cs:19-39`). UI 연동은 이벤트로(PlayerDamagedEventArgs 등) |
| `PlayerInputPC` | `Player`/`WeaponController` 내 `Keyboard.current`/`Mouse.current` 폴링 | **개조** | §8-17 단기 권장안. 레거시 축 `SwitchAttack`/`SummonAlly`는 타깃 InputManager에 없어 호출 시 `ArgumentException`(§7-1) |
| `PlayerSelect` | `PlayerSelectLogic` (이식 완료) | **그대로** | 원본과 달리 선택 후 재스폰 방식 — 유지 |
| `PlayerAttack` | `WeaponController`(신규, `OnShowEntitySuccess`에서 Player에 AddComponent) — 공용 쿨다운·4슬롯 전환·입력 라우팅 | **재설계** | `WeaponBase`는 자동발사 모델이라 의미가 다름. §4-2 0단계: 부착 경로 신설 필수(`SurvivalGame.cs:119-123`) |
| `LightningAttack` | `LightningWeapon : WeaponBase`(`OnFireStart`) — 히트스캔+`ApplyDamage` | **개조** | `WeaponBase.cs:13` 주석이 정확히 이 4종을 위한 훅 명시 |
| `LightningBolt` | 플레이어 프리팹 자식 GO의 VFX MonoBehaviour (비엔티티) | **그대로** | 순수 연출. 자기 SetActive(false) 소멸은 엔티티가 아니므로 허용 |
| `FrostAttack` | `FrostWeapon : WeaponBase`(`OnFireStart`/`OnFireStop`) + 콘 트리거 | **개조** | Awake 20개 사전 Instantiate는 폐기 — GF 풀이 대체. `maxFreezableEnemies=20`은 동시 상한 로직으로만 |
| `FrostDebuff` | 디버프 엔티티(신규 EntityLogic) 또는 `Enemy` 내부 상태+VFX — §8 미결정 | **재설계** | `Enemy.SetSpeedMultiplier`(`Enemy.cs:17-20`)가 이미 Freeze 수신부 후보 |
| `StinkAttack`/`StinkProjectile`/`StinkHit` | `StinkWeapon`(`OnFireStop`) + 포물선 `LobProjectile`(신규 EntityLogic: 시작/끝점+arc 커브) + AoE 히트 | **재설계** | 타깃 `Projectile.cs`는 직선 전용. Runaway 원점 버그는 이식 금지 |
| `SlimeAttack`/`SlimeProjectile`/`SlimeDebuff` | `SlimeWeapon`(`OnFireStop`) + 유도 `HomingProjectile`(타겟은 Transform이 아니라 **엔티티 Id**) + DoT 디버프 | **재설계** | 풀 재사용 오브젝트 추적 버그(원본에도 존재) 차단. 스티키 타겟에 생존 검사 추가 |
| `LightningHit`/`SlimeHit`(`AVPlayer`) | 파티클+사운드 단발 이펙트 엔티티 1종으로 통일(또는 무기 소유 오브젝트) | **개조** | `AVPlayer.cs` 코드 자체는 재사용 가능 |
| `EnemyHealth`/`EnemyAttack`/`EnemyMovement` | `Enemy`(EntityLogic, 기존 도달불가 코드 부활)+`EnemyData` 5종 스탯 확장 | **개조** | OnEnable 리셋→`OnShow` 리셋, 코루틴→`OnUpdate` 타이머(타깃 `Enemy.cs`가 이미 이 방식). Freeze/Runaway/공격봉인 public API 추가 |
| `CameraFollow`+`AnimatorDisabler`+카메라 Animator | `Assets/GameMain/Camera/PlayerCameraFollow.cs`(신규, §4-9 레시피) — 인트로는 코드 트윈 | **재설계** | §4-9: Sample `CameraFollow.cs`는 쓰지 않는다(GameManager NRE). anim의 m_Enabled 커브 트릭 폐기 |
| `Countdown`/`FlashFade` | HUDForm 프리팹 하위에 그대로 부착, 초기화만 `OnOpen`에서 폼 로직이 호출 | **그대로** | 표시 전용. Awake 초기화는 UIForm 풀링과 충돌(§4-6 B5) |
| `HUDCanvas` | `HUDForm : UIFormLogic`(신규) — **프리팹 루트에 로직 필수 부착**(엔티티와 정반대) | **개조** | `UIForm.cs:94-98` GetComponent. 기존 `HUDCanvas.prefab`은 Sample 스크립트 baked라 그대로 못 씀(§7-1) |
| `PauseMenu` | `PauseForm : UIFormLogic` + MasterMixer 에셋 복사 | **개조** | timeScale=0 방식은 유지 가능. 버튼은 인스펙터 PersistentCall 대신 코드 바인딩 |
| BGM(씬 AudioSource) | GF `SoundComponent`(§4-7) — 배선 후 씬 GO 제거 | **개조** | 타깃 씬에 이미 Background Music GO 존재(`MainScene.unity:167-295`) — 이중 재생 주의 |
| `Spawn Points.prefab` | 씬 GO 불필요 — 좌표 6개를 코드 상수/테이블로 | **버림(데이터만)** | |
| `EventSystem` | MainScene에 EventSystem+`InputSystemUIInputModule` 신규(§4-6 A2) | **재설계** | 추가 순간 `PlayerSelectLogic.cs:49` 가드가 살아나 클릭 동작이 바뀜 — §0-4 재확인 필수 |

---

## 3. 선행 수정 사항 (포팅 시작 전, 타깃 쪽)

§5.1 착수 순서 권고와 동일한 순서. **전부 이 포팅과 직접 충돌하는 것만** 추렸다.

| # | 작업 | 근거 | 비용 |
|---|---|---|---|
| 1 | **`ENABLE_LOG;ENABLE_DEBUG_AND_ABOVE_LOG` define 켜기** (플랫폼별 반복) | §5.1[3] — 아래 모든 작업의 진단 비용이 여기 걸림. 엔티티 NRE조차 침묵 중 | 5분 |
| 2 | **Girl/Boy 프리팹에서 baked `Player`/`PlayerSelectLogic` 제거** + 같은 작업에서 **Sample 공격 스크립트 5종**(LightningBolt/LightningAttack/StinkAttack/SlimeAttack/FrostAttack, `Girl.prefab:5282~55228`) 제거 여부 판단 | §5.1[2] — 이벤트 2중 발행 현재 진행형. 적 스폰을 켜는 순간 `TargetableObject.cs:76` NRE 발현. FrostAttack GO를 켜면 20개 Instantiate 누수(§5.1[10-d]) | 15분(에디터 작업 — 사용자 요청) |
| 3 | **MainCamera 태그 지정** (`MainScene.unity:296-310`, 현재 Untagged) | §5.1[9] — 마우스 조준(M3 이후 전부)과 원본 `MouseLocation` 상당물의 전제. §4-9 1단계 | 1분(에디터) |
| 4 | **`HideEntity` 1회 가드**: `Projectile.cs:37/52`, `ExpGem.cs:45`, `PlayerSelectLogic.cs:64-68`에 `mHidden` 가드 + **`TargetableObject.ApplyDamage`에 `if (IsDead) return;` 선두 가드** | §5.1[4], §4-1 3단계(★필수). 원본 `EnemyHealth.TakeDamage:79` 가드의 GF 등가물. "2초 사망 연출 후 제거" 지연 구간의 추가 피격이 정확히 이중 Hide 조건 | 30분 |
| 5 | **`ResourceManager` 핸들 누수 수정** — `assetName` 기준 참조 카운트 + 실패 경로 `Addressables.Release(op)` | §5.1[1] — 지금도 새는 중. 적 스폰이 시작되면 같은 주소 반복 로드로 누수가 폭증 | 반나절. `Assets/Scripts/Resource/ResourceManager.cs`는 예외적으로 자유 수정 가능(CLAUDE.md) |
| 6 | **MainScene을 빌드 씬 0번 등록** + **프리팹 루트 중복 `ProcedureComponent` 삭제**(`GameFramework.prefab:770-784`) | §5.1[6][5] — 프로시저 전이(M2)를 도입하기 전에. 새 프로시저 등록은 반드시 **자식 'Procedure' GO** 쪽(`:679-693`) | 각 5분(에디터) |
| 7 | **`SurvivalGame.Shutdown` 구독 해제 대칭화** — `CharacterSelectedEventArgs` 좀비 핸들러 방어 | §5.1[7] — M2의 프로시저 전이 도입 시 확정 예외 | 30분 |
| 8 | **적 5종 프리팹 복제 + Sample 스크립트 3종 제거 + Addressables 재등록** | §4-2 4단계, §8-10 — 타깃 Zombunny 등에 `EnemyMovement/Attack/Health`가 baked. `OnTriggerEnter`가 접촉 즉시 NRE(GameManager null). Groups 창에서만 등록, 주소는 짧은 키(§4-5) | 에디터 작업(사용자 요청) |
| 9 | **레이어 도입 결정 실행**(§8-13 권장) — Floor(8)/Shootable(9)/Blocking(10)을 원본과 같은 인덱스로 + 13은 `FrostFX`로 명명(Frost VFX 전용 — correction 확인), 14는 Lightning 마스크에 포함되므로 환경(Arches) 배치 시 함께 | 적을 되살리기 **전에** 해야 재작업이 없다. 마스크 값 256/512/17920을 그대로 이식 가능해짐 | 에디터 작업 |

NavMesh 베이크는 선행이 아니라 **M5 진입 조건**으로 미룬다(§8 미결정 1 참조). M1은 기존 `Enemy.cs:54` Transform 직선 추적으로 시작한다.

---

## 4. 마일스톤 — 각 단계가 "플레이 가능"으로 끝난다

### M1. 적 스폰 + 추적 + 사망

원본 의미 보존: "캐릭터 선택 후에야 스포너 기동, 종별 고정 간격·고정 상한, 사망 시 2초 연출 후 풀 복귀 + 점수".

**생성/수정 파일:**
- `Assets/GameMain/Entity/EntityData/EnemyData.cs` — 5종 스탯 인자(HP/데미지/공격간격/점수/이동속도/에셋키) 추가. **생성자에서 `HitPoints = maxHP` 필수**(§4-1 1단계)
- `Assets/GameMain/Entity/EntityLogic/Enemy.cs` — 부활. 추적 갱신 0.5초 주기(원본 재현), `OnShow`에서 상태 전부 리셋(§5-2: Awake/OnEnable 금지), 사망 시퀀스(캡슐 트리거화→"Dead" 트리거→2초 후 침하 2.5/s→`HideEntity` 1회), `mHidden` 가드, `StartSinking()` 애니 이벤트 수신용 public 메서드(FBX에 이벤트 baked — ZomBear 제외 4종)
- `Assets/GameMain/Game/SurvivalGame.cs` — `Update(elapseSeconds, realElapseSeconds)` 오버라이드 부활(§4-3 ★ 1단계), 스포너 레코드 5개(주소·좌표·rate·cap) 타이머 구동, `CharacterSelected` 수신 후에만 타이머 시작(원본 SetActive 게이트 등가), 살아있는 적 카운트는 `OnShowEntitySuccess`/`HideEntityComplete` 증감 또는 `GetEntities("Enemy")`(§4-1 10단계)
- `Assets/GameMain/Game/EnemyDiedEventArgs.cs` — 신규(§4-4 레시피 그대로, scoreValue 운반)
- `Assets/GameMain/Procdure/ProcedureMain.cs` — `OnUpdate`에서 `mGame?.Update(e, r)` 호출(§4-3 ★ 3단계)
- `Assets/GameMain/Camera/PlayerCameraFollow.cs` — 신규(§4-9: `Player.Instance` null 가드, `FixedUpdate`/`LateUpdate`, offset (0,15,-22), smoothing 5)

**에디터 작업:** 선행 8번(적 프리팹 클론+스트립+등록), 엔티티 그룹 "Enemy" 확인(`GameFramework.prefab:1067-1087` — 이미 존재), PlayerCameraFollow를 MainCamera에 부착.

**완료 판정:** 캐릭터 선택 → 5종이 각자 좌표/간격/상한대로 스폰, 플레이어를 0.5초 지연 폴링으로 추적, 임시 디버그 킬(예: K 키로 최근접 적 `ApplyDamage`)로 사망 연출 후 `HideEntity` — 이중 Hide 예외 없음, 리스폰된 적이 만피/초기 상태(OnShow 리셋 검증), `EnemyDiedEventArgs` 발행 확인. 동시 최대 13마리 상한 유지.

### M2. 플레이어 피격 + 게임오버

**생성/수정:**
- `Enemy.cs` — 공격 로직: 사거리(원본은 트리거 반경, 1차는 타깃 방식인 거리 판정 유지 가능) + `timeBetweenAttacks` 간격으로 `player.ApplyDamage`. 주석(`Enemy.cs:64`)을 푸는 게 아니라 새로 작성(주석만 풀면 컴파일 깨짐 — CLAUDE.md)
- `Player.cs` — `OnDead` 오버라이드: 즉시 HideEntity하지 않고 "Die" 트리거 + 이동/공격 잠금 + `PlayerDiedEventArgs` 발행, `delayOnPlayerDeath`(1초) 후 처리
- `SurvivalGame.cs` — `PlayerDied` 수신 → `GameOver = true`(§4-3 ★ 2단계). 적 추적 대상 null 처리(적 전원 이동 정지 — 원본 EnemyTarget=null 등가)
- `Assets/GameMain/Procdure/ProcedureGameOver.cs` — 신규(§4-3 레시피: `UseNativeDialog` 구현, 자식 'Procedure' GO에 FQN 등록). 재진입 시 `ProcedureMain`으로 복귀 = 재시작
- 전이 전 **잔존 엔티티 전량 `HideEntity`**(§4-3 함께 처리 3종), 구독 해제 대칭(선행 7번) 검증

**에디터 작업:** `GameFramework.prefab` 자식 Procedure GO의 `mAvailableProcedureTypeNames`에 `ToyBoxNightmare.ProcedureGameOver` 추가.

**완료 판정:** 적 접촉 → 체력 감소(로그로 확인, HUD는 M6) → 사망 → 게임오버 상태 → 재시작하면 캐릭터 선택부터 재진입, **재진입 시 중복 구독 예외 없음**(§5.1[7]의 확정 발현 조건 통과), 이전 판 엔티티 잔존 없음.

### M3. 공격 1종 — Lightning (권장 근거 포함)

**Lightning을 첫 무기로 하는 근거:** (a) 즉발 히트스캔이라 **투사체 엔티티·디버프·적 수신 API가 전부 불필요** — 신규 EntityLogic 0개. (b) 데미지 경로가 `ApplyDamage` 단 1개(§4-2 5단계와 일치). (c) 연출(LightningBolt)이 플레이어 프리팹 자식 GO로 자기완결 — GF 풀링과 충돌하지 않음. (d) 원본에서도 기본 활성 무기(인덱스 0)다. Frost는 적 Freeze API, Stink/Slime은 신규 투사체 EntityLogic이 필요해 전부 M4로.

**생성/수정:**
- `Assets/GameMain/Weapon/WeaponController.cs` — 신규: 공용 쿨다운 1개 + 슬롯 전환(Tab) + 입력 라우팅(`Mouse.current.leftButton` press/hold/release → `OnFireStart/OnFireHeld/OnFireStop`). §4-2 0단계대로 `SurvivalGame.OnShowEntitySuccess` Player 분기에서 `AddComponent` + `Initialize(player)` — 프리팹에 미리 붙이지 않는다
- `Assets/GameMain/Weapon/LightningWeapon.cs` — 신규: `OnFireStart`에서 전방 range 20 레이캐스트(마스크 17920), 명중 시 `ApplyDamage(Owner.Entity, 50)`, 볼트 EndPoint 설정
- LightningBolt VFX 스크립트 — Sample 코드 기반 정리본을 `Assets/GameMain/`에 신규 작성(Sample 직접 참조 금지)

**에디터 작업:** Girl/Boy 프리팹에 볼트/발사점(Antenna 등가) 자식 GO 구성 — 단 **EntityLogic/Entity는 절대 붙이지 않음**(§4-1 4단계). 히트 이펙트 프리팹 Addressables 등록(Groups 창).

**완료 판정:** 마우스 조준 회전(선행 3번 효과) + 클릭 발사 → 적 명중·데미지 50·쿨다운 1초 동작, 빗나감 시 최대사거리 볼트, 벽(Blocking) 차폐 동작.

### M4. 나머지 공격 3종 + 디버프

**생성/수정:**
- `Enemy.cs` — 디버프 수신 API: `Freeze/UnFreeze`(애니메이터 off+이동 0 — `SetSpeedMultiplier` 확장), `Runaway/ComeBack`(★도주 좌표는 `CachedTransform.position + dir*10`으로 — 원본 월드원점 버그 이식 금지), 공격봉인 bool. `OnShow`/`OnHide`에서 디버프 상태 전량 리셋(안 하면 재사용 적이 빙결 상태로 나옴)
- `FrostWeapon.cs` — 홀드형 콘 트리거. 디버프는 GF 엔티티(또는 Enemy 내부 상태 — §8 미결정 3) freezeDelay 1s/freezeDuration 2s
- `LobProjectile.cs`(EntityLogic)+`LobProjectileData` — 시작/끝점+arc 커브 포물선, 트리거 조기폭발, `mHidden` 가드(도착폭발 vs 조기폭발 같은 프레임 경합), 코루틴 금지 → `OnUpdate` 상태머신
- `StinkWeapon.cs` — release 발사, 사거리 9 판정+레티클. **원본의 '헛방에도 쿨다운 5초 소모' 비대칭은 Slime처럼 bool 반환으로 교정**
- `StinkHitLogic` — OverlapSphere(반경 **4**, Shootable) → Runaway, 4초 후 ComeBack. ★4초 전 배열 재순회 대신 **살아있는 엔티티 Id 목록**으로(리스폰 적 오염 방지)
- `HomingProjectile.cs`(EntityLogic) — 타겟을 **엔티티 Id**로 보유, 매 `OnUpdate`에 `GetEntity(id)` 유효성 검사(HideEntity된 타겟 추적 금지 — 원본 잠복 결함 수정), speed 20/반경 1
- `SlimeWeapon.cs` — 스티키 타겟에 **생존 검사** 추가 후 발사, 성공 시만 쿨다운 3.5s
- `SlimeDebuffLogic` — 3초간 **0.5초 간격 6틱 × 20dmg**(프리팹 실효값 — correction), 부착 중 공격봉인, "틱 완료" vs "적 사망" Release 경합에 `mHidden` 가드

**에디터 작업:** 투사체/히트/디버프 프리팹 제작(Sample 프리팹 복제 후 스크립트 스트립) + Addressables 등록(현재 공격 계열 주소 0건), "Projectile" 엔티티 그룹 활용, 레티클 머티리얼 URP 재작성(§6).

**완료 판정:** Tab 전환 4종 순환(공용 쿨다운이 전환에도 유지), Frost 홀드→1초 후 빙결→해제 2초 후 복귀, Stink 포물선→반경 4 도주 4초, Slime 유도→6틱 DoT+공격봉인, 어떤 조합에서도 HideEntity 예외 없음, 리스폰 적에 잔존 디버프 없음.

### M5. 아군 (양)

**진입 조건: NavMesh.** 타깃 `MainScene.unity:121`이 `m_NavMeshData: {fileID: 0}` — 미베이크. 패키지는 있음(`com.unity.ai.navigation` 2.0.10). 사용자에게 에디터 베이크 요청(agentRadius 0.5/height 1.2/slope 45 — 원본 값). 베이크가 미뤄지면 Ally를 직선 이동으로 단순화하는 대안 유지.

**생성/수정:**
- `Ally.cs`(EntityLogic)+`AllyData` — Duration 10, `OnShow`에서 `agent.Warp(position)`(GF 풀링은 활성화 후 OnShow라 Warp 필수), `OnHide`에서 agent off+ResetPath
- `SurvivalGame.cs` — allyPoints 적립(EnemyDied 구독), 30점 소환 가능, 소환 시 **추적 대상 전환**(`Enemy`가 폴링할 `ChaseTarget` 프로퍼티 — 이벤트화하지 않음, 원본 폴링 의미 보존), 10초 후 회수+**allyPoints=0 몰수**, 소환 목적지는 "소환 순간의 플레이어 위치 1회 지정"(추적 아님). ★사망 후 소환 NRE·사망 후 Invoke 복원 등 원본 순서 버그 2건에 null/상태 가드
- 입력: `Keyboard.current.digit1Key`

**완료 판정:** 30점 → 소환 표시 → 1키 → 양이 AllySpawnPoint(29.93, 0, 4.61)에 스폰 → 적 전원이 양 추적 → 10초 후 복귀+포인트 0. 적 추적도 NavMesh로 전환됐다면 장애물 우회 확인.

### M6. UI / 연출

**생성/수정:**
- §4-6 A단계 최초 배선: Canvas+EventSystem(InputSystemUIInputModule) 추가, `UIComponent`의 `mInstanceRoot`(`GameFramework.prefab:150`)·`mUIGroups`(`:155`, "Default") 설정. **EventSystem 추가로 `PlayerSelectLogic.cs:49` 가드가 살아나므로 캐릭터 선택 클릭 재검증**(§4-6 A2)
- `Assets/GameMain/UI/HUDForm.cs` — 점수/체력 슬라이더/쿨다운/피격 플래시/게임오버 텍스트. 전부 이벤트 구독(ScoreChanged/PlayerDamaged/CooldownStarted/PlayerDied). Countdown/FlashFade 코드는 재사용하되 초기화는 `OnOpen`에서. allyImage 초기 off는 **코드로**(프리팹 값에 의존하면 재현 안 됨 — correction). Ally 버튼은 코드 바인딩(원본 m_Target null 버그 이관 금지)
- `Assets/GameMain/UI/PauseForm.cs` — timeScale=0 토글, MasterMixer 복사, Esc는 Input System으로
- HUD/Pause 프리팹 신규 제작(**루트에 UIFormLogic 필수** — 엔티티와 정반대 규약) + Addressables 등록
- 카메라 인트로: 선택 앵글(0,4,6 / 30,180,0) → 게임 앵글 1초 코드 트윈 → `PlayerCameraFollow` 인계. anim 트릭 폐기
- BGM → `SoundComponent`(§4-7 전체) + **씬 Background Music GO 제거**(이중 재생 방지)

**완료 판정:** 전체 루프가 원본 체감과 일치 — 선택→인트로 트랜지션→HUD 점수/체력/쿨다운 실시간, 피격 플래시, Esc 일시정지(스폰 타이머 정지 — timeScale 전파 확인, §8 미결정 8), 게임오버 텍스트→재시작.

---

## 5. 튜닝값 이관표

전부 **프리팹/씬 직렬화 실효값** 기준 (corrections 반영).

### 적 5종

| 값 | Zombunny | ZomBear | ZombieDuck | Clown | Hellephant | 출처 |
|---|---|---|---|---|---|---|
| maxHealth | 100 | 100 | 120 | 150 | 200 | `Zombunny.prefab:216` / `ZomBear.prefab:215` / `ZombieDuck.prefab:651` / `Clown.prefab:455` / `Hellephant.prefab:216` |
| scoreValue | 10 | 10 | 20 | 25 | 50 | 각 prefab :217/:216/:652/:456/:217 |
| attackDamage | 10 | 10 | 20 | 30 | 35 | `Zombunny.prefab:242` / `ZomBear.prefab:201` / `ZombieDuck.prefab:622` / `Clown.prefab:441` / `Hellephant.prefab:202` |
| timeBetweenAttacks | 0.5 | 0.5 | 1 | 1.5 | 2 | 위와 인접 라인 |
| 공격 트리거 반경 | 0.8 | 0.8 | 1.0 | 0.8 | 1.63 | `Zombunny.prefab:254` 등 |
| NavMeshAgent | speed 3.5 전 종 공통, stop 1.1(Hellephant만 r1.3/stop1.9) | | | | | `Zombunny.prefab:314-319`, `Hellephant.prefab:314-319` |
| 공통 | sinkSpeed 2.5 / deathEffectTime 2 / runAwayDistance 10 / 추적 갱신 0.5s(코드 상수) | | | | | 전 프리팹 + `EnemyMovement.cs:25` |

### 스포너 (씬 오버라이드 없음 — 프리팹 원본 값)

| 스포너 | 적 | rate | max | 좌표 | 출처 |
|---|---|---|---|---|---|
| ZomBunny | Zombunny | 5s | 4 | (-20.5, 0, 12.5) | `Spawn Points.prefab:259-262, :198` |
| ZomBear | ZomBear | 6s | 3 | (22.5, 0, 15) | `:244-247, :120` |
| Hellephant | Hellephant | 10s | 2 | (0, 0, 32) | `:214-217, :150` |
| Duck | ZombieDuck | 10s | 2 | (-0.86, 0, -33.1) | `:274-277, :162` |
| Clown | Clown | 15s | 2 | (-26.55, 0, 5.29) | `:229-232, :174` |
| Ally 스폰점 | — | — | — | (29.93, 0, 4.61) | `:186` |

동시 상한 합계 13. "대기 먼저" 방식(활성화 후 rate초 뒤 첫 스폰), 풀 만석 시 그 틱 스킵 — `EnemySpawner.cs:49-64, :72-89`.

### 플레이어 / 공격

| 값 | 실효값 | 출처 |
|---|---|---|
| maxHealth | 100 | `Girl.prefab:5355`, `Boy.prefab:4639` (타깃 `PlayerData.cs:9` 일치) |
| 이동 speed | **6** (타깃은 5 — 정정 필요) | `Girl.prefab:5410`, `Boy.prefab:5430` vs 타깃 `PlayerData.cs:10` |
| numberOfAttacks | 4 | `Girl.prefab:5342` |
| Rigidbody | drag ∞ / angularDrag ∞ / constraints 80(FreezeRot X\|Z) | `Girl.prefab:4250-4257` (타깃도 동일 — `ARCHITECTURE.md` §5.1[10-e]) |
| Lightning: Cooldown/damage/range/mask | 1 / **50**(코드 20 아님) / 20 / **17920**(=9\|10\|14) | `Girl.prefab:5389-5394` |
| LightningBolt: rayHeight/effectDuration/phaseDuration | 2 / 0.75 / **0.01**(코드 0.1 아님) | `Girl.prefab:4573-4575` |
| Frost: maxFreezableEnemies / freezeDelay / freezeDuration | 20 / 1 / 2 | `Girl.prefab:5289`, `FrostDebuff.prefab:212-213` |
| Frost 시작 상태 | **비활성**(활성 시작은 Lightning뿐 — correction) | `Girl.prefab:349,354` |
| Stink: Cooldown/range | 5 / **9**(코드 5 아님) | `Girl.prefab:5304-5305` |
| StinkProjectile speed | **10**(코드 20 아님) | `StinkProjectile.prefab:9584` |
| StinkHit: 반경/지속/마스크 | **4**(코드 3 아님) / 4 / 512 | `StinkHit.prefab:180-184` |
| Slime: Cooldown/마스크 | 3.5 / 512 | `Girl.prefab:5424`(Boy는 `:4572`), `SlimeAttack.prefab:77` |
| SlimeProjectile: speed/반경 | 20 / 1 | `SlimeProjectile.prefab:44-45` |
| SlimeDebuff | 3초 × **초당 2회 × 20dmg**(코드 4×10 아님) = 0.5s 간격 6틱 120dmg | `SlimeDebuff.prefab:120-122` |
| 발사점(Antenna) localPos | (0.123, 0.948, 1.019) | `Girl.prefab:2932` |
| 레티클 3색 | 빨강(1,0,0)/노랑(1,0.922,0.016)/초록(0,1,0) | `StinkAttack.prefab:78-80` |

### 게임 루프 / 카메라 / UI / 기타

| 값 | 실효값 | 출처 |
|---|---|---|
| delayOnPlayerDeath | 1 | `Main.unity:177` |
| allyCost / Ally Duration / 회수 시 포인트 | 30 / 10s / **0으로 몰수** | `Main.unity:210`, `Sheep.prefab:1267`, `AllyManager.cs:81` |
| CameraFollow smoothing / offset | 5 / (0,15,-22) | `Main.unity:1073-1074` |
| 카메라 | 직교 size 4.5, near 0.01, 선택뷰 pos(0,4,6) rot(30,180,0), 인트로 1초 | `Main.unity:1026-1030,1053-1054`, `CameraStartTransition.anim` |
| 선택 캐릭터 배치 | Boy x=-3 / Girl x=+3 (타깃은 ∓2) | `Main.unity:719-720` vs `SurvivalGame.cs:37-38` |
| MouseLocation 마스크/거리 | 256(레이어8 Floor) / 100f | `Main.unity:160-162`, `MouseLocation.cs:63` |
| FlashFade | 색(1,0,0,0.1), speed 5 | `HUDCanvas.prefab:438-439` |
| Countdown 갱신 주기 | 0.25s | `Countdown.cs:12` |
| NavMesh 베이크 | radius 0.5 / height 1.2 / slope 45 | `Main.unity:110-112` |
| BGM | `Assets/UnityTechnologies/Audio/music_rev1_loop_01.wav`, loop, MasterMixer | `Main.unity:818-824` |
| 사망 연출 타이밍 | 낙선자 Destroy 1초(GF판 1.5초 코루틴) | `PlayerSelect.cs:66` vs `PlayerSelectLogic.cs:61` |

---

## 6. 호환성 주의

**Built-in → URP:**
- 적 머티리얼·HitParticles·디버프 파티클·LineRenderer(LightningBolt)·스카이박스 전부 Built-in 셰이더 → 미변환 시 마젠타. 머티리얼 업그레이드는 에디터 작업(사용자 요청).
- 레티클 틴트가 `material.SetColor("_TintColor",…)`(`StinkAttack.cs:62-68`) — 레거시 Particles 전용 프로퍼티라 URP 셰이더에 없음. URP 파티클 셰이더 재작성 + `_BaseColor` 치환.
- 라이트맵/ReflectionProbe는 URP 재베이크 필수. 단 타깃 `Assets/Scenes/MainScene/`에 이미 베이크 데이터 존재 — 환경 배치 변경 시에만 재베이크.
- uGUI(ScreenSpaceOverlay)와 FlashFade/Slider는 RP 독립 — 무영향.

**레거시 입력 → Input System:**
- 타깃 `activeInputHandler: 2`(Both)지만 **원본 커스텀 축 `SwitchAttack`/`SummonAlly`는 타깃 `InputManager.asset`에 없다** — 레거시 호출을 이식하면 즉시 `ArgumentException`(§7-1). 치환표: Fire1→`Mouse.current.leftButton`(isPressed/wasReleased), SwitchAttack→`tabKey`, SummonAlly→`digit1Key`, Cancel→`escapeKey`.
- 입력 계약 재현 주의: Lightning/Frost는 **press**, Stink/Slime은 **release** 발사. `StopFiring`도 쿨다운 게이트를 통과해야 함(쿨다운 중 릴리즈 시 Frost가 켜진 채 남는 원본 특성 — 재현 여부 결정).
- `OnMouseUp`(캐릭터 선택)은 레거시 의존 — Both 모드에서만 동작. 장기적으로 레이캐스트 클릭으로 교체(§8-17).
- M6에서 EventSystem 추가 시 `InputSystemUIInputModule`을 쓸 것(원본은 StandaloneInputModule).

**NavMesh:**
- 타깃 씬 미베이크(`MainScene.unity:121` fileID 0), 패키지는 설치됨(manifest 2.0.10 — 원본은 2.0.9). 베이크 전 NavMeshAgent 활성화는 "Failed to create agent" + 이동 불능.
- GF 풀링과의 결합: 원본은 "비활성 상태에서 위치 세팅 → 활성화 → OnEnable에서 agent on" 순서였으나 GF는 활성화 후 `OnShow`가 불림 → **`OnShow`에서 `agent.Warp(pos)`, `OnHide`에서 `agent.enabled=false`+ResetPath** 패턴 필수. 침하 연출 전에 agent를 먼저 꺼야 Y를 되끌어올리지 않음.
- 원본 스폰 좌표 5곳이 타깃 씬 폴리곤 위인지는 베이크 후 확인 — **미확인**.

**Unity 6000.2 → 6000.3:**
- `Rigidbody.drag` → `linearDamping` 마이그레이션은 타깃에서 이미 완료(Girl.prefab의 `m_LinearDamping`). 가라앉기 연출은 `linearDamping = 0f`(`PlayerSelectLogic.cs:71-73`에 훅 이미 존재).
- 원본 구프리팹 포맷 13종 중 10개는 임포트 시 자동 업그레이드, 3개(FrostAttack/FrostCone/StinkProjectile)는 이미 신포맷 — 혼용 주의만.
- DeathComplete/StartSinking 애니메이션 이벤트는 FBX .meta에 baked — 캐릭터/적 FBX를 그대로 가져오면 이벤트도 따라온다. 수신 메서드명을 동일하게 유지하거나(런타임 AddComponent라 수신 가능) "no receiver" 경고 감수.

---

## 7. 버리는 것

| 대상 | 근거 |
|---|---|
| `MobileInterface` / `Touchpad` / `PlayerInputTouch` + 모바일 UI 전반 | 원본 저장소에서조차 미부착 죽은 코드(GUID 전수 grep 0건). PC 타깃 |
| `Dog.prefab` (아군 후보) | 원본에서 미참조. 타깃 Addressables 주소는 남겨둠(§8 미결정 7) |
| `SceneManager.LoadScene` 씬 리로드 재시작 | §5.1[6] — GF 기동 프리팹에 DontDestroyOnLoad 없음, 재시작=영구 소멸. 프로시저 전이로 대체 |
| 씬 오버라이드 배선 16건 + `Spawn Points`/`Attack Effects` 씬 상주 오브젝트 | GF에서 원천 불가(런타임 스폰). EntityData/이벤트로 대체 |
| 자체 풀링 3계열(EnemySpawner/AllyManager/씬 상주 투사체·디버프) | GF ObjectPool과 정면 충돌(§5-2: Destroy/SetActive 금지) |
| 카메라 Animator + `CameraStartTransition.anim`의 m_Enabled 커브 트릭 + `AnimatorDisabler` | 컴포넌트 활성 상태 실수 하나로 즉사하는 취약 구조. 코드 트윈으로 대체 |
| `GameManager`/`MouseLocation`/`AllyManager` 싱글턴 | SurvivalGame으로 흡수 |
| HUD Ally Button의 `m_Target: {fileID:0}` OnClick | 원본 버그 — 코드 바인딩으로 대체 |
| `EnemyMovement.Runaway`의 월드 원점 도주 좌표 | 원본 버그 — 이식 금지, 적 위치 기준으로 수정 |
| `EnemyHealth.Spawner` 필드 | 어디서도 대입 안 되는 죽은 필드 |
| SlimeProjectile의 "타겟 null 시 소등" 경로 / StinkHit의 4초 전 배열 재순회 / SlimeAttack 스티키 죽은 타겟 | 풀링 환경에서 원본부터 있던 결함 — 엔티티 Id 유효성 검사로 대체 |
| `Assets/VFX Reference Prefabs/`의 AVPlayer 중복 사본, `StinkTargetRing.prefab` 등 미참조 이펙트 | 씬 참조 없음. Addressables 등록 원본은 캐릭터 baked 사본/Attacks 사본 기준 |
| CanvasScaler 참조해상도 800x600 | ConstantPixelSize 모드에서 무효값 — 이관 불필요 |
| Sample `CameraFollow.cs` | `GameManager.Instance` NRE(§4-9, §7-1) — 신규 작성 |

---

## 8. 미결정 (사용자 결정 필요)

1. **적 이동: NavMeshAgent vs Transform 직선 추적** — 계획은 M1 직선/M5 전 NavMesh 베이크(에디터 작업)지만, 장난감 지형 특성상 직선 추적은 벽 끼임이 예상됨. 베이크 시점과 적 이동의 NavMesh 전환 시점(M1로 앞당길지) 결정 필요. 원본 스폰 좌표가 타깃 씬 워커블 폴리곤 위인지 **미확인**.
2. **무기 UX 방향** — 원본의 "수동 발사 + 공용 쿨다운 + Tab 전환 + 카운트다운 HUD"를 그대로 갈지, 뱀서라이크(자동발사·다중 장착, `WeaponBase` 원설계)로 갈지. 이 계획은 원본 재현 기준으로 작성했다.
3. **디버프 구현 방식** — GF 자식 엔티티(`AttachEntity` — 타깃에서 사용 실적 0, Addressables 체인 미검증) vs `Enemy` 내부 상태+VFX 엔티티. 후자가 저위험.
4. **점수 경제** — 원본 "점수=아군 화폐" 유지 vs 삭제된 경험치/레벨 시스템(ExpGem)과 통합. 88408fd 삭제가 폐기인지 초기화인지 작성자 확인 필요(§8-9, 부록 B-4).
5. **5종 스탯 테이블 위치** — `EnemyData` 생성자 하드코딩 vs 딕셔너리 vs ScriptableObject. 현재 타깃은 100% C# const(§6-3), `TypeId` 전부 1.
6. **레이어 13/14 명명** — 원본에서 이름 소실. 13은 `FrostFX`(콘 트리거 전용), 14는 Lightning 차폐용 환경 레이어로 새 이름 부여 권장 — 확정 필요.
7. **Dog 제2아군 채택 여부** — 타깃 Addressables에 이미 등록됨. 캐릭터별 아군 차별화 등 기획 결정.
8. **일시정지 전파** — `Time.timeScale=0`이 GF 내부 타이머(엔티티 OnUpdate elapseSeconds, 스폰 타이머)에 전파되는지 **미확인** — M6 전 검증 필요. 안 되면 GF `BaseComponent`의 게임 속도 API 검토.
9. **카메라 인트로 재현 범위** — 1초 트랜지션 재현 vs 즉시 게임 앵글. 재현 시 최종 키프레임 (1,15,-22) 사용. orthographic size 4.5는 서바이버류에 좁다는 지적(§4-9 5단계) — 적정값은 게임 디자인 결정.
10. **동시 투사체 1발 캡** — 원본은 씬당 인스턴스 1개라 암묵적으로 "동시 비행 1발/동시 슬라임 1마리". GF 엔티티화하면 자연히 다중 가능 — 캡 유지 여부.
11. **원본 체감 차이 수용 여부 소소 2건** — 플레이어 speed 6 vs 타깃 5, 선택 캐릭터 배치 ±3 vs 타깃 ±2. 원본 재현이 목표면 둘 다 원본 값으로.

