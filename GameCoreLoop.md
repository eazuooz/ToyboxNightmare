# ToyboxNightmare - 서바이벌 게임 루프 구현 작업 내역

---

## 1. 아키텍처 개요

이 프로젝트는 **GameFramework** (오픈소스 Unity 프레임워크)를 기반으로 한다.
프레임워크가 제공하는 핵심 시스템들을 이해해야 이번 작업 내용을 파악할 수 있다.

### 1-1. Procedure (절차/씬 상태 머신)

GameFramework의 **Procedure**는 게임의 실행 흐름을 FSM(유한 상태 머신)으로 관리하는 시스템이다.
`ProcedureBase`를 상속해 씬 전환, 로딩, 게임플레이 등 각 "단계"를 하나의 Procedure 클래스로 표현한다.

```
ProcedureBase
  ├─ OnInit(procedureOwner)     // 한 번만 실행, 초기화
  ├─ OnEnter(procedureOwner)    // 이 Procedure 진입 시
  ├─ OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds)  // 매 프레임
  ├─ OnLeave(procedureOwner, isShutdown)  // 이 Procedure 떠날 때
  └─ OnDestroy(procedureOwner)  // 파괴 시
```

**이번 작업 - `ProcedureMain.cs`**

`ProcedureMain`은 실제 게임 플레이 단계를 담당하는 Procedure이다.
기존에는 껍데기만 있었고, 이번에 `SurvivalGame`과 연결했다.

```csharp
public class ProcedureMain : ProcedureBase
{
    private SurvivalGame mGame = null;

    protected override void OnEnter(ProcedureOwner procedureOwner)
    {
        mGame = new SurvivalGame();
        mGame.Initialize();  // 게임 시작
    }

    protected override void OnUpdate(...)
    {
        mGame?.Update(elapseSeconds, realElapseSeconds);  // 매 프레임 전달
    }

    protected override void OnLeave(...)
    {
        mGame?.Shutdown();   // 게임 종료 및 정리
        mGame = null;
    }
}
```

> Procedure는 게임의 "언제"를 담당한다. 게임이 시작되면 `ProcedureMain`에 진입하고,
> `SurvivalGame`의 생명주기 전체가 이 Procedure 안에서 돌아간다.

---

### 1-2. GameMode & GameBase (게임 모드 추상화)

**`GameMode`** 는 게임의 종류를 나타내는 enum이다. 현재는 `Survival` 하나만 존재한다.

```csharp
public enum GameMode : byte
{
    Survival,
}
```

**`GameBase`** 는 모든 게임 모드의 공통 인터페이스(추상 클래스)다.
Procedure는 `GameBase`만 알고, 실제 게임 종류(Survival, etc.)는 GameBase 구현체가 담당한다.

```
GameBase (추상)
  ├─ abstract GameMode GameMode  // 어떤 모드인지
  ├─ bool GameOver               // 게임 종료 여부
  ├─ Initialize()
  ├─ Shutdown()
  ├─ Update(elapseSeconds, realElapseSeconds)
  ├─ OnShowEntitySuccess(...)    // 엔티티 표시 성공 이벤트 (virtual)
  └─ OnShowEntityFailure(...)    // 엔티티 표시 실패 이벤트 (virtual)
```

**이번 작업 - `SurvivalGame.cs`**

`GameBase`를 구현한 서바이벌 모드 게임 클래스.
기존에는 플레이어 스폰 코드조차 전부 주석 처리된 빈 껍데기였다.
이번에 서바이벌 게임의 핵심 루프 전체를 구현했다.

| 항목 | 값 |
|------|----|
| 플레이어 스폰 위치 기준 적 스폰 반경 | 15f |
| 최대 동시 적 수 | 50마리 |
| 초기 적 스폰 간격 | 3초 |
| 최소 스폰 간격 (시간이 지날수록 짧아짐) | 0.5초 |
| 간격 감소 공식 | `Max(0.5, 3.0 - 생존시간 × 0.05)` |

```csharp
// Initialize: 이벤트 5개 구독, LevelSystem/UpgradeSystem 생성, 플레이어 스폰
public override void Initialize()
{
    Instance      = this;
    LevelSystem   = new LevelSystem();
    UpgradeSystem = new UpgradeSystem();

    events.Subscribe(ShowEntitySuccessEventArgs.EventId,  OnShowEntitySuccess);
    events.Subscribe(ShowEntityFailureEventArgs.EventId,  OnShowEntityFailure);
    events.Subscribe(HideEntityCompleteEventArgs.EventId, OnHideEntityComplete);
    events.Subscribe(GameOverEventArgs.EventId,           OnGameOver);
    events.Subscribe(LevelUpEventArgs.EventId,            OnLevelUp);

    SpawnPlayer();
}

// Update: 생존시간 누적, 스폰 간격 점진적 단축, 적 스폰
public override void Update(float elapseSeconds, float realElapseSeconds)
{
    mSurvivalTime  += elapseSeconds;
    mSpawnInterval  = Mathf.Max(0.5f, InitSpawnInterval - mSurvivalTime * 0.05f);

    mSpawnTimer += elapseSeconds;
    if (mSpawnTimer >= mSpawnInterval && EnemyCount < MaxEnemyCount)
    {
        mSpawnTimer = 0f;
        SpawnEnemy();
    }
}
```

**이벤트 처리**

| 이벤트 | 처리 내용 |
|--------|-----------|
| `ShowEntitySuccessEventArgs` | Player 타입이면 무기 부착 + UpgradeSystem 초기화 |
| `ShowEntityFailureEventArgs` | 경고 로그 출력 |
| `HideEntityCompleteEventArgs` | `mEnemyIds`에서 ID 제거 (적 수 카운트 동기화) |
| `GameOverEventArgs` | `GameOver = true`, 생존 시간 로그 |
| `LevelUpEventArgs` | `UpgradeSystem.PickRandom(3)` → UpgradeForm UI 열기 |

---

## 2. Entity 시스템

GameFramework의 **Entity** 시스템은 게임 오브젝트를 오브젝트 풀과 함께 관리한다.
`EntityComponent.ShowEntity()` / `HideEntity()`로 스폰/반환하며, 직접 Destroy를 쓰지 않는다.

### 2-1. EntityData 계층 구조 (데이터 컨테이너)

엔티티를 스폰할 때 `userData`로 전달되는 데이터 클래스들이다.
엔티티 로직(동작)과 데이터(수치)를 분리하는 역할을 한다.

```
EntityData (추상) ─ 기존
  ├─ Id         : int       (EntitySerialId로 생성한 고유 ID)
  ├─ TypeId     : int       (DataTable RowId 예약, 현재 1 고정)
  ├─ Position   : Vector3
  └─ Rotation   : Quaternion

  ├─ TargetableObjectData (추상) ─ 기존
  │    ├─ HitPoints     : int   (현재 HP, 런타임에 변경됨)
  │    ├─ MaxHitPoints  : int   (추상, 서브클래스에서 정의)
  │    └─ HitPointRatio : float (HitPoints / MaxHitPoints)
  │
  │    ├─ [신규] PlayerData
  │    │    ├─ MaxHitPoints = 100
  │    │    ├─ MoveSpeed    = 5f  (업그레이드로 증가 가능)
  │    │    └─ 생성 시 HitPoints = MaxHitPoints로 초기화
  │    │
  │    └─ [신규] EnemyData
  │         ├─ MaxHitPoints = 30
  │         ├─ MoveSpeed    = 2f
  │         ├─ AttackDamage = 10
  │         └─ ExpReward    = 5   (사망 시 경험치 보석에 전달)
  │
  ├─ [신규] ExpGemData
  │    ├─ ExpAmount  = 5   (수집 시 LevelSystem에 전달)
  │    └─ MoveSpeed  = 4f  (자석 이동 속도)
  │
  └─ [신규] ProjectileData
       ├─ Damage    : int
       ├─ Speed     : float
       ├─ Lifetime  : float
       └─ Direction : Vector3
```

### 2-2. EntityLogic 계층 구조 (동작 로직)

엔티티의 실제 행동을 담당하는 클래스들. `EntityLogic`을 상속하며 다음 생명주기를 가진다.

```
OnInit   - 오브젝트 풀 생성 시 한 번만 호출
OnShow   - 풀에서 꺼내 활성화될 때 (userData 수신)
OnUpdate - 매 프레임
OnHide   - 풀로 반환될 때
```

```
EntityLogic (GF 제공)
  └─ TargetableObject (추상) ─ 기존 수정
       ├─ IsDead       : bool  (HitPoints <= 0)
       ├─ ApplyDamage(attacker, damage)  ← [수정] HP 0 시 OnDead() 실제 호출
       ├─ OnDead(attacker)               ← [수정] HideEntity() 실제 호출
       │
       ├─ [신규] Player
       └─ [신규] Enemy
```

#### `TargetableObject.cs` 수정 내용

기존 코드에서 HP 0 판정과 `OnDead()` 호출이 모두 주석 처리되어 있었다.
이번에 주석을 해제하고 올바른 코드로 교체했다.

```csharp
// [수정 전] 주석 처리
//if (mTargetableObjectData.HitPoints <= 0)
//    OnDead(attacker);

// [수정 후] 실제 동작
if (mTargetableObjectData.HitPoints <= 0)
    OnDead(attacker);

// [수정 전] 주석 처리
//protected virtual void OnDead(Entity attacker)
//    GameEntry.Entity.HideEntity(Entity);

// [수정 후] 올바른 컴포넌트 접근 방식으로 수정
protected virtual void OnDead(Entity attacker)
{
    GameEntry.GetComponent<EntityComponent>().HideEntity(Entity);
}
```

---

### 2-3. 신규 EntityLogic 상세

#### `Player.cs`

플레이어 캐릭터 로직. `TargetableObject`를 상속한다.

- **싱글턴** : `Player.Instance`로 적/보석이 플레이어 위치를 참조한다.
- **이동** : `Input.GetAxisRaw("Horizontal/Vertical")`로 WASD 이동. `MoveSpeed`는 PlayerData에서 읽는다.
- **무기 부착** : `AttachWeapon<T>()` - 제네릭으로 무기 컴포넌트를 `AddComponent` 후 초기화.
- **외부 호출 API**
  - `TakeDamage(attacker, damage)` - 적이 공격할 때 사용
  - `HealHitPoints(amount)` - 업그레이드 "생명력 회복" 시 사용
  - `UpgradeMoveSpeed(amount)` - 업그레이드 "이동속도 증가" 시 사용
- **사망 처리** : `OnDead()` 오버라이드 → `HideEntity` 대신 `GameOverEventArgs` 발행.
  플레이어는 사망 연출을 위해 바로 숨기지 않는다.

```csharp
protected override void OnDead(Entity attacker)
{
    Log.Info("Player is dead. Game Over!");
    GameEntry.GetComponent<EventComponent>().Fire(this, GameOverEventArgs.Create());
    // HideEntity 호출 안 함 - 사망 연출 유지
}
```

#### `Enemy.cs`

적 캐릭터 로직. `TargetableObject`를 상속한다.

- **추적** : 매 프레임 `Player.Instance` 위치로 이동 (MoveSpeed = 2f)
- **공격** : 공격 사거리(1.5f) 이내 진입 시 이동 멈추고, 1초 간격으로 플레이어에게 데미지(10)
- **사망** : `SurvivalGame.Instance.SpawnExpGem(위치, ExpReward)` 호출 후 `base.OnDead()` (HideEntity)

```csharp
protected override void OnDead(Entity attacker)
{
    SurvivalGame.Instance?.SpawnExpGem(CachedTransform.position, mEnemyData.ExpReward);
    base.OnDead(attacker);  // → EntityComponent.HideEntity()
}
```

#### `ExpGem.cs`

경험치 보석 로직. `EntityLogic`을 직접 상속한다. (HP 없음)

- **자석 이동** : 플레이어가 반경 5f 이내에 들어오면 MoveSpeed(4f)로 끌려감
- **수집** : 반경 0.5f 이내 도달 시 `LevelSystem.AddExp(ExpAmount)` 호출 후 HideEntity

#### `Projectile.cs`

투사체 로직. `EntityLogic`을 직접 상속한다.

- **이동** : `OnShow` 시 받은 방향(Direction)으로 Speed만큼 직선 이동
- **수명** : `Lifetime` 경과 시 자동 HideEntity
- **충돌** : `OnTriggerEnter`에서 Enemy 감지 → `enemy.ApplyDamage()` 후 자신 HideEntity

---

## 3. 게임 시스템 (Game/)

### 3-1. LevelSystem

경험치와 레벨 상승을 관리한다. `SurvivalGame`이 인스턴스를 소유한다.

```
LevelSystem
  ├─ Level      : int  (초기값 1)
  ├─ CurrentExp : int
  └─ RequiredExp: int  (= Level × 100. 1레벨→100, 2레벨→200, ...)
```

`AddExp(amount)` 호출 시:
1. `CurrentExp += amount`
2. `CurrentExp >= RequiredExp`이면 레벨업 반복 처리 (while 루프, 한 번에 여러 레벨도 가능)
3. 레벨업마다 `LevelUpEventArgs.Create(Level)`을 이벤트 시스템에 Fire

### 3-2. UpgradeSystem

레벨업 시 제공할 업그레이드 풀을 관리한다. `SurvivalGame`이 소유한다.

**`Initialize(player)`** - Player 스폰 직후 호출. 플레이어 참조가 필요한 업그레이드를 클로저로 묶는다.

**기본 업그레이드 풀 (6종)**

| 이름 | 설명 | 효과 |
|------|------|------|
| 투사체 강화 | 투사체 데미지 +10 | `weapon.Damage += 10` |
| 속사 | 공격 간격 20% 감소 | `weapon.AttackInterval *= 0.8f` |
| 투사체 가속 | 투사체 속도 +3 | `weapon.Speed += 3f` |
| 이동 속도 증가 | 플레이어 이동 속도 +1 | `player.UpgradeMoveSpeed(1f)` |
| 범위 공격 추가 | AreaWeapon 장착 (중복 방지) | `player.AttachWeapon<AreaWeapon>()` |
| 생명력 회복 | HP +30 즉시 회복 | `player.HealHitPoints(30)` |

**`PickRandom(count)`** - 풀에서 중복 없이 랜덤하게 count개 선택. Fisher-Yates 방식의 인덱스 셔플로 구현.

### 3-3. UpgradeDefinition

업그레이드 하나를 나타내는 데이터 객체.

```csharp
public class UpgradeDefinition
{
    public string Name        { get; }
    public string Description { get; }
    private readonly Action mApply;

    public void Apply() => mApply?.Invoke();
}
```

UI가 이름/설명을 표시하고, 플레이어가 선택하면 `Apply()`를 호출해 효과를 실행한다.

### 3-4. 커스텀 이벤트 (GameEventArgs)

GameFramework의 이벤트 시스템을 사용하기 위해 `GameEventArgs`를 상속해 커스텀 이벤트를 정의했다.
`ReferencePool.Acquire<T>()`로 객체 풀링을 활용한다.

**`GameOverEventArgs`**
- 데이터 없음. 플레이어 사망 시 Fire.
- `EventId = typeof(GameOverEventArgs).GetHashCode()`

**`LevelUpEventArgs`**
- `Level : int` 를 담아 Fire.
- `Clear()` 시 Level = 0 리셋 (풀 반환 시 초기화)

---

## 4. 무기 시스템 (Weapon/)

무기는 `Player` GameObject에 `AddComponent`로 붙는 MonoBehaviour 컴포넌트이다.
Entity 시스템과 별개로, Unity의 일반 컴포넌트 방식으로 동작한다.

### 4-1. WeaponBase (추상)

```csharp
public abstract class WeaponBase : MonoBehaviour
{
    protected Player Owner { get; private set; }
    public float AttackInterval { get; set; }  // 최소 0.1f 강제

    public void Initialize(Player owner) { ... }

    private void Update()  // Unity Update - 타이머로 Attack() 주기 호출
    {
        if (Owner == null || Owner.IsDead) return;
        mAttackTimer += Time.deltaTime;
        if (mAttackTimer >= attackInterval)
        {
            mAttackTimer = 0f;
            Attack();
        }
    }

    protected abstract void Attack();
}
```

> `Initialize` 시 `mAttackTimer = attackInterval`으로 초기화해서, 부착 즉시 첫 공격이 발동된다.

### 4-2. ProjectileWeapon

기본 무기. 플레이어 스폰 직후 자동으로 부착된다.

- **탐색** : `Physics.OverlapSphere(반경 20f)`로 주변 Enemy 전부 검색, 가장 가까운 Enemy 선택
- **발사** : `EntityComponent.ShowEntity()`로 `Projectile` 엔티티 스폰. 방향 벡터와 수치를 `ProjectileData`에 담아 전달

| 기본 수치 | 값 |
|-----------|----|
| 데미지 | 25 |
| 투사체 속도 | 10f |
| 투사체 수명 | 3초 |
| 적 탐색 반경 | 20f |
| 공격 간격 (WeaponBase) | 1초 |

### 4-3. AreaWeapon

레벨업 업그레이드로만 획득 가능한 두 번째 무기.

- **공격** : `Physics.OverlapSphere(반경 3f)`로 주변 Enemy 전부에게 데미지 15 적용
- 탐색과 공격이 동시에 이루어진다. (투사체 없음, 즉발)
- `player.GetComponent<AreaWeapon>()`으로 중복 부착을 방지한다.

---

## 5. UI 시스템 (UI/)

GameFramework의 **UIComponent** 위에 레벨업 선택 화면을 구현했다.

### 5-1. 데이터 흐름

```
LevelSystem.AddExp() → 레벨업
  → LevelUpEventArgs 발행
    → SurvivalGame.OnLevelUp()
      → UpgradeSystem.PickRandom(3) : List<UpgradeDefinition>
        → UpgradeFormData(options) 생성
          → UIComponent.OpenUIForm("UpgradeForm", "Default", formData)
            → UpgradeForm.OnOpen(userData)
```

### 5-2. UpgradeForm

`UIFormLogic`을 상속하는 GameFramework UI 폼.

- `OnOpen` 시 `Time.timeScale = 0f`로 게임 일시 정지
- Inspector에서 연결된 `UpgradeItemUI[]` 배열(3개)에 업그레이드 옵션을 Setup
- 플레이어가 카드 선택 시 `OnSelectUpgrade(index)` → `mOptions[index].Apply()` → `CloseUIForm()`
- `OnClose` 시 `Time.timeScale = 1f`로 게임 재개

### 5-3. UpgradeItemUI

개별 업그레이드 카드 UI 컴포넌트.

- `TextMeshProUGUI` 2개 (이름, 설명) + `Button` 1개
- `Setup(name, description, onClick)` : `UpgradeForm`이 호출해 내용을 동적으로 채운다
- 클릭 리스너는 `Awake`에서 한 번만 등록하고, `mOnClick` Action을 교체하는 방식

---

## 6. Utility (Utility/)

### EntitySerialId

전역 엔티티 ID 생성기. 모든 엔티티 스폰 시 `EntitySerialId.Next()`로 고유 ID를 받는다.

```csharp
public static class EntitySerialId
{
    private static int mNext = 0;
    public static int Next() => ++mNext;
}
```

> 게임 세션 내에서 단조 증가하는 정수 ID를 보장한다. 씬 재시작 시 0으로 초기화된다.

---

## 7. 전체 게임 루프 흐름

```
[앱 시작]
  └─ ProcedureMain.OnEnter()
       ├─ SurvivalGame 생성
       └─ SurvivalGame.Initialize()
            ├─ 이벤트 5종 구독
            ├─ LevelSystem, UpgradeSystem 생성
            └─ SpawnPlayer()
                 └─ EntityComponent.ShowEntity(id, typeof(Player), ...)
                      └─ (비동기 로드 완료 후) ShowEntitySuccessEventArgs 발행
                           └─ OnShowEntitySuccess()
                                ├─ mPlayer = Player 참조 저장
                                ├─ player.AttachWeapon<ProjectileWeapon>()
                                └─ UpgradeSystem.Initialize(player)

[매 프레임] ProcedureMain.OnUpdate() → SurvivalGame.Update()
  ├─ 생존 시간 누적
  ├─ 스폰 간격 갱신 (시간이 지날수록 빨라짐)
  └─ 스폰 타이머 초과 & 적 수 < 50 → SpawnEnemy()
       └─ 플레이어 기준 반경 15f 랜덤 위치에 Enemy 스폰

[Player 매 프레임]
  └─ WASD 이동
       └─ ProjectileWeapon.Update() (Unity Update)
            └─ 공격 간격마다 가장 가까운 적 방향으로 Projectile 스폰

[Enemy 매 프레임]
  ├─ Player.Instance 방향으로 이동
  └─ 사거리 1.5f 이내 진입 시 1초마다 player.TakeDamage(10)
       └─ TargetableObject.ApplyDamage()
            └─ HP <= 0 → OnDead()
                 ├─ (Enemy) ExpGem 스폰 → base.OnDead() → HideEntity
                 └─ (Player) GameOverEventArgs 발행
                      └─ SurvivalGame.OnGameOver() → GameOver = true

[ExpGem 매 프레임]
  ├─ 플레이어 반경 5f 이내 → 자석 이동
  └─ 플레이어 반경 0.5f 이내 → LevelSystem.AddExp(5) → HideEntity
       └─ CurrentExp >= RequiredExp → 레벨업
            └─ LevelUpEventArgs 발행
                 └─ SurvivalGame.OnLevelUp()
                      ├─ UpgradeSystem.PickRandom(3)
                      └─ UIComponent.OpenUIForm("UpgradeForm", ...)
                           ├─ Time.timeScale = 0 (게임 일시 정지)
                           ├─ 카드 3개 표시
                           └─ 선택 시 → Apply() → Time.timeScale = 1 (재개)

[ProcedureMain.OnLeave]
  └─ SurvivalGame.Shutdown()
       └─ 이벤트 5종 구독 해제, 적 ID Set 초기화, Instance = null
```

---

## 8. 파일 변경 요약

### 수정된 파일 (3개)

| 파일 | 변경 내용 |
|------|-----------|
| `TargetableObject.cs` | HP 0 시 `OnDead()` 실제 호출, `OnDead()` 에서 `HideEntity()` 실제 호출 |
| `SurvivalGame.cs` | 빈 껍데기 → 완전한 서바이벌 게임 루프 구현 |
| `ProcedureMain.cs` | `SurvivalGame` 생성·Update·Shutdown 연결, 불필요한 주석/보일러플레이트 정리 |

### 신규 파일 (26개 + .meta)

| 경로 | 파일 |
|------|------|
| `Entity/EntityData/` | `PlayerData.cs`, `EnemyData.cs`, `ExpGemData.cs`, `ProjectileData.cs` |
| `Entity/EntityLogic/` | `Player.cs`, `Enemy.cs`, `ExpGem.cs`, `Projectile.cs` |
| `Game/` | `LevelSystem.cs`, `UpgradeSystem.cs`, `UpgradeDefinition.cs`, `GameOverEventArgs.cs`, `LevelUpEventArgs.cs` |
| `Weapon/` | `WeaponBase.cs`, `ProjectileWeapon.cs`, `AreaWeapon.cs` |
| `UI/` | `UpgradeForm.cs`, `UpgradeFormData.cs`, `UpgradeItemUI.cs` |
| `Utility/` | `EntitySerialId.cs` |
