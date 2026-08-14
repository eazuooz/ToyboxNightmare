using System.Collections.Generic;
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

        // 장착된 무기. 각자 자기 쿨다운을 돌린다.
        private readonly List<WeaponBase> mWeapons = new List<WeaponBase>();

        /// <summary>
        /// true 면 한 번에 하나만 쓰고 Tab 으로 전환한다(무기별 확인용).
        /// false 면 장착한 무기가 전부 동시에 나간다(뱀서라이크 본래 형태).
        /// </summary>
        private const bool SingleWeaponMode = true;

        private int mActiveWeaponIndex = 0;

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

        // ─── EntityLogic 생명주기 ───

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);
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

            mPlayerData = userData as PlayerData;
            if (mPlayerData == null)
            {
                Log.Error("Player data is invalid.");
                return;
            }

            Instance = this;
            CachedTransform.position = mPlayerData.Position;
            CachedTransform.rotation = mPlayerData.Rotation;

            ResetRuntimeState();
            EquipWeapons();
        }

        /// <summary>
        /// 풀에서 재사용되므로 이전 판의 이동/애니/사망 상태를 반드시 리셋한다.
        /// 빠뜨리면 두 번째 판이 죽은 포즈에 콜라이더가 꺼진 채로 시작한다.
        /// </summary>
        private void ResetRuntimeState()
        {
            mMoveDirection = Vector3.zero;
            mLookDirection = CachedTransform.forward;
            mDying         = false;
            mDeathTimer    = 0f;
            mDeathNotified = false;

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

        // ─── 무기 ───

        /// <summary>
        /// 이 캐릭터가 들고 시작하는 무기. 레벨업 시스템이 붙으면 여기에 추가하는 형태가 된다.
        ///
        /// 무기는 MonoBehaviour 가 아니라 이 로직이 소유하는 순수 객체다. 로직 인스턴스가
        /// 살아남는 재사용 경로에서는 무기도 그대로 재활용한다.
        ///
        /// 단, 그 경로에 기대면 안 된다 — <c>Entity.cs:86-96</c> 은 풀에서 꺼낸 인스턴스의
        /// 로직 타입이 다르면 컴포넌트를 파괴하고 새로 붙인다. 선택화면은
        /// <see cref="PlayerSelectLogic"/>, 플레이는 <see cref="Player"/> 라 <b>판마다
        /// 이 로직이 통째로 재생성되는 것이 정상 경로</b>다. 무기가 캐릭터에 남긴 것은
        /// <see cref="DisposeWeapons"/> 가 치운다.
        /// </summary>
        private void EquipWeapons()
        {
            if (mWeapons.Count == 0)
            {
                mWeapons.Add(new LightningWeapon());
                mWeapons.Add(new FrostWeapon());
                mWeapons.Add(new StinkWeapon());
                mWeapons.Add(new SlimeWeapon());
            }

            for (int i = 0; i < mWeapons.Count; i++)
            {
                mWeapons[i].Initialize(this);
            }

            // 재스폰 때마다 첫 무기부터 시작한다.
            mActiveWeaponIndex = 0;
            ApplyWeaponSelection();

            if (SingleWeaponMode && mWeapons.Count > 0)
            {
                Log.Info("무기 {0}종 장착. Tab 으로 전환. 현재 → {1}",
                    mWeapons.Count, mWeapons[mActiveWeaponIndex].GetType().Name);
            }
        }

        /// <summary>
        /// 활성 무기만 켠다. 꺼진 무기는 OnUpdate 를 받지 못하고 총구 VFX·타겟 마커도 함께 꺼진다.
        /// </summary>
        private void ApplyWeaponSelection()
        {
            if (mWeapons.Count > 0)
            {
                GameAssert.InRange(mActiveWeaponIndex, mWeapons.Count, "mActiveWeaponIndex");
            }

            for (int i = 0; i < mWeapons.Count; i++)
            {
                bool isSelectedWeapon = !SingleWeaponMode || i == mActiveWeaponIndex;
                mWeapons[i].SetActive(isSelectedWeapon);

                if (isSelectedWeapon)
                {
                    // 전환 즉시 쏠 수 있게 쿨다운을 채워 준다.
                    mWeapons[i].ResetCooldown();
                }
            }
        }

        /// <summary>
        /// 무기를 굴린다. 입력 처리 뒤에 부르므로 조준이 한 프레임 밀리지 않는다 —
        /// MonoBehaviour 였을 때는 이 순서가 보장되지 않았다.
        /// </summary>
        private void UpdateWeapons(float elapseSeconds)
        {
            for (int i = 0; i < mWeapons.Count; i++)
            {
                mWeapons[i].OnUpdate(elapseSeconds);
            }
        }

        /// <summary>
        /// 무기를 전부 끈다. 총구 VFX·타겟 마커·Frost 콘이 여기서 정리된다.
        /// 사망과 회수 양쪽에서 부른다 — 죽으면 OnUpdate 가 멈춰 무기 스스로는 못 끈다.
        /// </summary>
        private void ShutdownWeapons()
        {
            for (int i = 0; i < mWeapons.Count; i++)
            {
                mWeapons[i].SetActive(false);
            }
        }

        /// <summary>
        /// 무기가 캐릭터에 남긴 것을 전부 지운다(복제한 마커 링, 꺼 놓은 VFX 루트).
        ///
        /// 이 캐릭터 GameObject 는 풀에 돌아가 <see cref="PlayerSelectLogic"/> 으로도 재사용된다.
        /// 정리를 안 하면 재시작마다 링이 쌓이고, 선택화면 캐릭터의 안테나 이펙트가 꺼진다.
        /// </summary>
        private void DisposeWeapons()
        {
            for (int i = 0; i < mWeapons.Count; i++)
            {
                mWeapons[i].Dispose();
            }
        }

        protected internal override void OnHide(bool isShutdown, object userData)
        {
            ShutdownWeapons();
            DisposeWeapons();

            Instance = null;
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

            ReadMoveInput();
            ReadWeaponSwitchInput();
            UpdateWeapons(elapseSeconds);
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

        private void ReadWeaponSwitchInput()
        {
            if (!SingleWeaponMode || mWeapons.Count <= 1) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame) return;

            mActiveWeaponIndex = (mActiveWeaponIndex + 1) % mWeapons.Count;
            ApplyWeaponSelection();

            Log.Info("무기 전환 → {0} ({1}/{2})",
                mWeapons[mActiveWeaponIndex].GetType().Name,
                mActiveWeaponIndex + 1, mWeapons.Count);
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
        /// 키보드가 없으면 조준까지 통째로 건너뛴다. 이동을 못 하는 상황에서
        /// 캐릭터만 마우스를 따라 도는 것이 더 어색하기 때문이다.
        /// </summary>
        private void ReadMoveInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            mMoveDirection = ReadMoveDirection(keyboard);
            UpdateLookDirection();
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

        /// <summary>마우스가 가리키는 지면 쪽으로 바라볼 방향을 갱신한다.</summary>
        private void UpdateLookDirection()
        {
            Vector3 toMouse = GetMouseWorldPosition() - CachedTransform.position;
            toMouse.y = 0f;

            // 커서가 캐릭터와 겹치면 방향이 0 에 수렴한다. 그때는 직전 방향을 유지한다.
            if (toMouse.sqrMagnitude > MinLookSqrMagnitude)
            {
                mLookDirection = toMouse;
            }
        }

        /// <summary>마우스 지면 조준점을 못 구할 때 쓰는 대체 조준점. 지금 보는 방향을 유지한다.</summary>
        private Vector3 ForwardAimPoint
        {
            get { return CachedTransform.position + CachedTransform.forward; }
        }

        private Vector3 GetMouseWorldPosition()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null || Mouse.current == null) return ForwardAimPoint;

            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Ray     ray            = mainCamera.ScreenPointToRay(screenPosition);
            Plane   groundPlane    = new Plane(Vector3.up, Vector3.zero);

            // 카메라가 지면과 평행하면 교차점이 없다. 정상적으로 일어날 수 있으므로 로그 없이 대체값.
            if (!groundPlane.Raycast(ray, out float distance)) return ForwardAimPoint;

            return ray.GetPoint(distance);
        }

        // ─── 사망 ───

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

            // 사망 후에는 OnUpdate 가 무기를 굴리지 않으므로 여기서 확실히 끈다.
            // 안 그러면 총구 VFX 와 타겟 마커가 시체 위에 남는다.
            ShutdownWeapons();
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
