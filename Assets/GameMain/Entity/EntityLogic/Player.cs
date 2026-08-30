using UnityEngine;
using UnityEngine.InputSystem;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 플레이어 엔티티 로직.
    /// - Rigidbody 기반 이동 (FixedUpdate)
    /// - 마우스 방향으로 회전
    /// - WASD 이동
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Player : TargetableObject
    {
        // 싱글턴 - 다른 로직에서 플레이어 위치를 참조할 때 사용
        public static Player Instance { get; private set; }

        // ─── 애니메이터 파라미터 이름 ───
        // 이름이 틀려도 Unity 는 조용히 무시하므로 문자열을 흩뿌리지 않고 여기 모아 둔다.
        private const string WalkingBoolParam = "IsWalking";
        private const string DieTriggerParam  = "Die";

        /// <summary>
        /// 방향 벡터가 이보다 짧으면 "방향이 없다" 로 본다.
        /// 길이 0 에 가까운 벡터를 <c>Quaternion.LookRotation</c> 에 넘기면 회전이 튄다.
        /// </summary>
        private const float MinLookSqrMagnitude = 0.001f;

        private PlayerData      mPlayerData = null;
        private Rigidbody       mRigidbody  = null;
        private Animator        mAnimator   = null;
        private CapsuleCollider mCollider   = null;

        // FixedUpdate 에 전달할 이동/회전 방향 (OnUpdate 에서 읽어 저장)
        private Vector3 mMoveDirection = Vector3.zero;
        private Vector3 mLookDirection = Vector3.forward;

        /// <summary>장착 무기 묶음. 수명은 이 로직 인스턴스와 같다 — <see cref="OnInit"/> 에서 만든다.</summary>
        private WeaponLoadout mWeaponLoadout = null;

        // 사망 연출
        private bool  mDying         = false;
        private float mDeathTimer    = 0f;
        private bool  mDeathNotified = false;

        /// <summary>원본 GameManager 의 delayOnPlayerDeath.</summary>
        private const float DeathDelay = 1f;

        /// <summary>
        /// 피격/사거리 판정의 기준점. 콜라이더 중심이 트랜스폼 원점에서
        /// XZ 로 약 0.3 어긋나 있어(center (0.18, 0.7, 0.24)) 원점을 쓰면
        /// 접근 방향에 따라 판정 거리가 흔들린다.
        /// </summary>
        public Vector3 CenterPosition
        {
            get
            {
                return mCollider != null
                    ? CachedTransform.TransformPoint(mCollider.center)
                    : CachedTransform.position;
            }
        }

        // ─── 마우스 조준점 ───
        // 무기가 조준에 쓰는 유일한 좌표다. 프레임당 한 번 계산해 여기 담아 두고,
        // 캐릭터 회전(mLookDirection)도 같은 값에서 나온다 — 같은 프레임에 지면
        // 레이캐스트를 두 번 할 이유가 없고, 두 번 하면 보는 방향과 쏘는 방향이 갈릴 수 있다.

        /// <summary>마우스가 가리키는 지면 위 월드 좌표. 지면에 안 닿으면 캐릭터 전방 한 칸으로 대체된다.</summary>
        public Vector3 AimPoint { get; private set; }

        /// <summary>이번 프레임 지면 레이캐스트가 실제로 성공했는지. false 면 AimPoint 는 대체값이다.</summary>
        public bool HasAimPoint { get; private set; }

        // ─── EntityLogic 생명주기 ───

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            // 로드아웃 수명은 이 로직 인스턴스와 같다. 필드 초기화식에서는 this 를 넘길 수 없어
            // 여기서 만든다 — <c>Entity.cs:86-96</c> 이 "풀에서 꺼낸 로직 타입이 같으면 OnInit 없이
            // 재사용" 이라 OnInit 은 로직 인스턴스당 정확히 한 번만 돈다.
            mWeaponLoadout = new WeaponLoadout(this);

            mRigidbody = GetComponent<Rigidbody>();
            // includeInactive: true 가 필수다. 이 캐릭터 GameObject 는 선택화면
            // (PlayerSelectLogic)으로 먼저 쓰인 뒤 회수됐다가 Player 로 재사용되는데,
            // 회수 시점에 OnHide 가 SetActive(false) 를 걸어 두고 OnInit 은 OnShow 보다 먼저 돈다.
            // 기본값(false)이면 여기서 Animator 를 못 찾아 걷기·사망 애니메이션이 조용히 안 나온다.
            mAnimator  = GetComponentInChildren<Animator>(true);
            mCollider  = GetComponent<CapsuleCollider>();

            WarnOnMissingComponents();
        }

        /// <summary>
        /// 캐시가 비어도 사용처마다 null 가드가 있어 예외는 나지 않는다. 대신 이동이나
        /// 애니메이션이 <b>조용히</b> 죽어 원인을 찾기 어려우므로 여기서 한 번 알린다.
        /// </summary>
        private void WarnOnMissingComponents()
        {
            if (mRigidbody == null)
            {
                Log.Error("Player '{0}': Rigidbody 가 없어 이동이 동작하지 않는다.", Name);
            }

            if (mAnimator == null)
            {
                Log.Warning("Player '{0}': Animator 를 찾지 못했다. 이동/사망 애니메이션이 재생되지 않는다.", Name);
            }

            if (mCollider == null)
            {
                Log.Warning("Player '{0}': CapsuleCollider 가 없다. 중심점이 트랜스폼 원점으로 대체된다.", Name);
            }
        }

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            // 상태 리셋과 Instance 갱신은 userData 검사보다 <b>먼저</b> 한다.
            // 이 인스턴스는 풀에서 재사용되므로, 데이터가 없다고 여기서 그냥 빠져나가면
            // 이전 판의 mDying/mDeathTimer/mDeathNotified 와 꺼진 콜라이더를 그대로 물고
            // 등장해 1초 뒤 스스로 사라진다. 데이터가 없어도 인스턴스는 깨끗해야 한다.
            ResetRuntimeState();
            Instance = this;

            mPlayerData = userData as PlayerData;
            if (mPlayerData == null)
            {
                Log.Error("Player data is invalid.");
                return;
            }

            CachedTransform.position = mPlayerData.Position;
            CachedTransform.rotation = mPlayerData.Rotation;

            // 회전을 적용한 뒤에 다시 잡아야 스폰 자세를 그대로 바라본다.
            // 리셋 시점의 forward 는 아직 이전 판의 자세다.
            ResetAim();

            mWeaponLoadout.Equip();

            // 스폰 통보. OnHitPointChanged 는 비율이 **줄어들 때만** 오므로
            // 이것 없이는 HUD 가 새 판의 만피를 알 방법이 없다(이전 판 값을 물고 있게 된다).
            FireHealthChanged(HitPointRatio, HitPointRatio);
        }

        /// <summary>
        /// 풀에서 재사용되므로 이전 판의 이동/애니/사망 상태를 반드시 리셋한다.
        /// 빠뜨리면 두 번째 판이 죽은 포즈에 콜라이더가 꺼진 채로 시작한다.
        /// </summary>
        private void ResetRuntimeState()
        {
            mMoveDirection = Vector3.zero;
            mDying         = false;
            mDeathTimer    = 0f;
            mDeathNotified = false;

            ResetAim();

            if (mCollider != null)
            {
                mCollider.enabled = true;
            }

            if (mAnimator != null)
            {
                mAnimator.Rebind();
                mAnimator.Update(0f);
            }
        }

        protected internal override void OnHide(bool isShutdown, object userData)
        {
            mWeaponLoadout.Shutdown();
            mWeaponLoadout.Dispose();

            // 데이터는 base 체인 끝(EntityLogicBase)에서 ReferencePool 로 반납된다.
            // 반납이 참조를 지워 주지 않으므로 여기서 놓아야 FixedUpdate 의 mPlayerData null
            // 가드가 실제 방어가 된다. 안 지우면 그 가드는 "GameObject 가 꺼져 있다" 는
            // 암묵 전제에만 기대게 되고, 재스폰이 같은 인스턴스를 꺼내면 남의 이동속도를 읽는다.
            mPlayerData = null;

            // 무조건 지우면 안 된다. 재시작 경로에서는 새 Player 가 먼저 OnShow 되고
            // 이전 인스턴스가 그 뒤에 회수되므로, 그때 "새" Instance 를 지워 버리면
            // PlayerCameraFollow 가 타겟을 잃고 선택 앵글로 돌아간다.
            // UnityEngine.Object 라 ReferenceEquals 가 아니라 == 를 쓴다(파괴된 객체 비교).
            if (Instance == this)
            {
                Instance = null;
            }

            base.OnHide(isShutdown, userData);
        }

        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (mDying)
            {
                UpdateDeathSequence(elapseSeconds);
                return;
            }

            if (IsDead) return;

            // 조준점 → 이동 입력 → 무기 순서다. 무기가 이번 프레임의 조준점을 그대로 쓴다.
            ReadInput();
            mWeaponLoadout.ReadSwitchInput();
            mWeaponLoadout.OnUpdate();
        }

        /// <summary>
        /// 즉시 사라지지 않고 사망 애니메이션이 보일 시간을 준다.
        /// 이 타이머가 <see cref="OnDead"/> 대신 회수(Hide)를 담당한다.
        /// </summary>
        private void UpdateDeathSequence(float elapseSeconds)
        {
            mDeathTimer += elapseSeconds;
            if (mDeathTimer < DeathDelay) return;

            NotifyDied();
            SafeHide();
        }

        // ─── 물리 이동 (FixedUpdate) ───

        private void FixedUpdate()
        {
            if (mRigidbody == null || mPlayerData == null || IsDead) return;

            MoveByInput();
            TurnToLookDirection();
            UpdateWalkAnimation();
        }

        private void MoveByInput()
        {
            // normalized 는 길이 0 벡터에 대해 0 을 돌려주므로 0 나눗셈이 없다.
            Vector3 step = mMoveDirection.normalized * mPlayerData.MoveSpeed * Time.fixedDeltaTime;
            mRigidbody.MovePosition(mRigidbody.position + step);
        }

        private void TurnToLookDirection()
        {
            Vector3 look = mLookDirection;
            look.y = 0f;

            if (look.sqrMagnitude <= MinLookSqrMagnitude) return;

            mRigidbody.MoveRotation(Quaternion.LookRotation(look));
        }

        private void UpdateWalkAnimation()
        {
            if (mAnimator == null) return;

            mAnimator.SetBool(WalkingBoolParam, mMoveDirection.sqrMagnitude > 0f);
        }

        // ─── 입력 처리 ───

        /// <summary>
        /// 이번 프레임의 조준점과 이동 방향을 잡는다.
        ///
        /// 조준을 이동보다 먼저, 그리고 키보드 유무와 <b>무관하게</b> 갱신한다.
        /// 수동 발사에서는 조준점이 곧 발사 방향이라(무기가 <see cref="AimPoint"/> 를 읽고,
        /// Lightning 은 캐릭터 forward 로 쏜다) 키보드가 없다고 조준까지 멈추면
        /// 그 프레임의 발사가 통째로 엉뚱한 곳으로 나간다.
        /// </summary>
        private void ReadInput()
        {
            UpdateAimPoint();
            UpdateLookDirection();

            Keyboard keyboard = Keyboard.current;
            mMoveDirection = keyboard != null ? ReadMoveDirection(keyboard) : Vector3.zero;
        }

        /// <summary>WASD 와 방향키를 같은 축으로 합친다.</summary>
        private static Vector3 ReadMoveDirection(Keyboard keyboard)
        {
            float horizontal = ReadAxis(
                keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed,
                keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed);

            float vertical = ReadAxis(
                keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed,
                keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed);

            return new Vector3(horizontal, 0f, vertical);
        }

        /// <summary>양쪽을 동시에 누르면 0 이 된다(레거시 GetAxisRaw 와 같은 의미).</summary>
        private static float ReadAxis(bool positivePressed, bool negativePressed)
        {
            return (positivePressed ? 1f : 0f) - (negativePressed ? 1f : 0f);
        }

        /// <summary>
        /// 프레임당 딱 한 번 도는 지면 레이캐스트. 결과가 <see cref="AimPoint"/> /
        /// <see cref="HasAimPoint"/> 로 나가고, 캐릭터 회전도 여기서 나온 값을 쓴다.
        /// </summary>
        private void UpdateAimPoint()
        {
            HasAimPoint = MouseGround.TryGetGroundPoint(out Vector3 groundPoint);
            AimPoint    = HasAimPoint ? groundPoint : ForwardAimPoint;
        }

        /// <summary>스폰 시점의 조준 상태. 아직 입력을 읽기 전이므로 지금 보는 방향으로 채운다.</summary>
        private void ResetAim()
        {
            mLookDirection = CachedTransform.forward;
            HasAimPoint    = false;
            AimPoint       = ForwardAimPoint;
        }

        /// <summary>조준점 쪽으로 바라볼 방향을 갱신한다.</summary>
        private void UpdateLookDirection()
        {
            Vector3 toAim = AimPoint - CachedTransform.position;
            toAim.y = 0f;

            // 커서가 캐릭터와 겹치면 방향이 0 에 수렴한다. 그때는 직전 방향을 유지한다.
            if (toAim.sqrMagnitude <= MinLookSqrMagnitude) return;

            mLookDirection = toAim;
        }

        /// <summary>마우스 지면 조준점을 못 구할 때 쓰는 대체 조준점. 지금 보는 방향을 유지한다.</summary>
        private Vector3 ForwardAimPoint
        {
            get { return CachedTransform.position + CachedTransform.forward; }
        }

        // ─── 사망 ───

        /// <summary>피격음. 사망 여부와 무관하게 매 피격 울린다(원본 PlayerHealth 와 같다).</summary>
        protected override void OnDamaged(Entity attacker, int damageHitPoints)
        {
            base.OnDamaged(attacker, damageHitPoints);

            GameSound.PlaySfx(SoundTable.PlayerHurt, CenterPosition);
        }

        /// <summary>
        /// 체력이 줄어들 때마다 HUD 에 알린다. 피격 플래시도 HUD 가 이 값으로 판단한다.
        /// </summary>
        protected override void OnHitPointChanged(float fromRatio, float toRatio)
        {
            base.OnHitPointChanged(fromRatio, toRatio);

            FireHealthChanged(fromRatio, toRatio);
        }

        /// <summary>
        /// 체력 변화를 발행한다. HUD 가 없어도 게임이 돌아야 하므로 실패해도 조용히 넘어간다.
        /// </summary>
        private void FireHealthChanged(float fromRatio, float toRatio)
        {
            EventComponent events = GameEntry.GetComponent<EventComponent>();
            if (events == null) return;

            events.Fire(this, PlayerHealthChangedEventArgs.Create(fromRatio, toRatio));
        }

        /// <summary>
        /// 주의: <c>base.OnDead</c> 를 부르지 않는다. base 는 즉시 SafeHide 하므로
        /// 사망 연출이 통째로 생략되고, 연출 종료 시점의 Hide 와 겹쳐 중복이 된다.
        /// 회수는 <see cref="OnUpdate"/> 의 사망 타이머가 담당한다.
        /// </summary>
        protected override void OnDead(Entity attacker)
        {
            GameAssert.IsTrue(IsDead, "OnDead 는 체력이 0 이하가 된 뒤에만 호출되어야 한다.");

            if (mDying) return;

            mDying      = true;
            mDeathTimer = 0f;

            GameSound.PlaySfx(SoundTable.PlayerDeath, CenterPosition);

            // 사망 후에는 OnUpdate 가 무기를 굴리지 않으므로 여기서 확실히 끈다.
            // 안 그러면 총구 VFX 와 타겟 마커가 시체 위에 남는다.
            mWeaponLoadout.Shutdown();
            StopMovementAndCollision();
            PlayDeathAnimation();

            // 사망 이벤트는 여기서 쏘지 않는다. NotifyDied() 를 볼 것.
        }

        /// <summary>이동/충돌을 즉시 끊는다. 적이 시체를 계속 때리지 않게 콜라이더도 끈다.</summary>
        private void StopMovementAndCollision()
        {
            mMoveDirection = Vector3.zero;

            if (mCollider != null)
            {
                mCollider.enabled = false;
            }
        }

        private void PlayDeathAnimation()
        {
            if (mAnimator == null) return;

            mAnimator.SetBool(WalkingBoolParam, false);
            mAnimator.SetTrigger(DieTriggerParam);
        }

        /// <summary>
        /// 사망을 알린다. <b>사망 연출이 끝난 뒤에 부른다</b> — OnDead 에서 바로 쏘면
        /// 연출이 1프레임으로 잘린다.
        ///
        /// <c>EventComponent.Fire</c> 는 큐잉이라 다음 프레임에 디스패치된다. 그 프레임의
        /// 모듈 순서가 Event(7) → Fsm(1) → Entity(0) 이므로,
        /// <c>SurvivalGame.OnPlayerDied</c> 가 GameOver 를 세우면 <b>같은 프레임에</b>
        /// <c>ProcedureMain</c> 이 떠나면서 <c>HideAllLoadedEntities</c> 로 이 엔티티를
        /// 회수해 버린다. Entity 모듈은 그 뒤라 사망 타이머가 한 프레임분밖에 못 쌓인다.
        /// </summary>
        private void NotifyDied()
        {
            if (mDeathNotified) return;

            mDeathNotified = true;

            // Create 는 참조 풀에서 꺼내 오므로, 발행하지 못할 상황이면 만들기 전에 빠진다.
            EventComponent eventComponent = GameEntry.GetComponent<EventComponent>();
            if (eventComponent == null)
            {
                Log.Error("EventComponent 가 없어 PlayerDied 를 발행하지 못했다.");
                return;
            }

            int finalScore = SurvivalGame.Instance != null ? SurvivalGame.Instance.Score : 0;
            eventComponent.Fire(this, PlayerDiedEventArgs.Create(finalScore));
        }
    }
}
