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
        private bool  mDying      = false;
        private float mDeathTimer = 0f;

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
            mAnimator  = GetComponentInChildren<Animator>();
            mCollider  = GetComponent<CapsuleCollider>();
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

            // 풀에서 재사용되므로 이전 판의 이동/애니/사망 상태를 반드시 리셋한다.
            mMoveDirection = Vector3.zero;
            mLookDirection = CachedTransform.forward;
            mDying         = false;
            mDeathTimer    = 0f;

            if (mCollider != null)
            {
                mCollider.enabled = true;
            }

            if (mAnimator != null)
            {
                mAnimator.Rebind();
                mAnimator.Update(0f);
            }

            EquipWeapons();
        }

        // ─── 무기 ───

        /// <summary>
        /// 이 캐릭터가 들고 시작하는 무기. 레벨업 시스템이 붙으면 여기에 추가하는 형태가 된다.
        /// </summary>
        private void EquipWeapons()
        {
            Equip<LightningWeapon>();
            Equip<FrostWeapon>();
            Equip<StinkWeapon>();
            Equip<SlimeWeapon>();

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
        /// 활성 무기만 켜고 나머지는 컴포넌트째 끈다.
        /// 꺼진 무기는 Update/LateUpdate 가 돌지 않으므로 완전히 멈춘다.
        /// </summary>
        private void ApplyWeaponSelection()
        {
            for (int i = 0; i < mWeapons.Count; i++)
            {
                WeaponBase weapon = mWeapons[i];
                if (weapon == null)
                {
                    continue;
                }

                bool active = !SingleWeaponMode || i == mActiveWeaponIndex;
                weapon.enabled = active;

                if (active)
                {
                    // 전환 즉시 쏠 수 있게 쿨다운을 채워 준다.
                    weapon.Initialize(this);
                }
            }
        }

        /// <summary>
        /// 무기를 장착한다. 엔티티는 풀에서 재사용되므로 이미 붙어 있으면 다시 붙이지 않고
        /// Initialize 로 상태만 리셋한다. 매번 AddComponent 하면 재시작할 때마다
        /// 무기가 중복되어 발사가 2중, 3중으로 나간다.
        /// </summary>
        private void Equip<T>() where T : WeaponBase
        {
            T weapon = GetComponent<T>();
            if (weapon == null)
            {
                weapon = gameObject.AddComponent<T>();
            }

            if (!mWeapons.Contains(weapon))
            {
                mWeapons.Add(weapon);
            }

            weapon.Initialize(this);
        }

        protected internal override void OnHide(bool isShutdown, object userData)
        {
            Instance = null;
            base.OnHide(isShutdown, userData);
        }

        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (mDying)
            {
                // 즉시 사라지지 않고 사망 애니메이션이 보일 시간을 준다.
                mDeathTimer += elapseSeconds;
                if (mDeathTimer >= DeathDelay)
                {
                    SafeHide();
                }
                return;
            }

            if (IsDead) return;

            ReadMoveInput();
            ReadWeaponSwitchInput();
        }

        private void ReadWeaponSwitchInput()
        {
            if (!SingleWeaponMode || mWeapons.Count <= 1)
            {
                return;
            }

            var kb = Keyboard.current;
            if (kb == null || !kb.tabKey.wasPressedThisFrame)
            {
                return;
            }

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

            // 이동
            Vector3 move = mMoveDirection.normalized * mPlayerData.MoveSpeed * Time.fixedDeltaTime;
            mRigidbody.MovePosition(mRigidbody.position + move);

            // 회전
            Vector3 look = mLookDirection;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
                mRigidbody.MoveRotation(Quaternion.LookRotation(look));

            // 애니메이터
            if (mAnimator != null)
                mAnimator.SetBool("IsWalking", mMoveDirection.sqrMagnitude > 0f);
        }

        // ─── 입력 처리 ───

        private void ReadMoveInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float h = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                    - (kb.aKey.isPressed || kb.leftArrowKey.isPressed  ? 1f : 0f);
            float v = (kb.wKey.isPressed || kb.upArrowKey.isPressed   ? 1f : 0f)
                    - (kb.sKey.isPressed || kb.downArrowKey.isPressed  ? 1f : 0f);
            mMoveDirection = new Vector3(h, 0f, v);

            // 마우스가 가리키는 지면 방향으로 회전
            Vector3 mousePos = GetMouseWorldPosition();
            Vector3 lookDir  = mousePos - CachedTransform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
                mLookDirection = lookDir;
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (Camera.main == null || Mouse.current == null)
                return CachedTransform.position + CachedTransform.forward;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float dist))
                return ray.GetPoint(dist);

            return CachedTransform.position + CachedTransform.forward;
        }

        // ─── 사망 ───

        /// <summary>
        /// 주의: <c>base.OnDead</c> 를 부르지 않는다. base 는 즉시 SafeHide 하므로
        /// 사망 연출이 통째로 생략되고, 연출 종료 시점의 Hide 와 겹쳐 중복이 된다.
        /// 회수는 <see cref="OnUpdate"/> 의 사망 타이머가 담당한다.
        /// </summary>
        protected override void OnDead(Entity attacker)
        {
            if (mDying)
            {
                return;
            }

            mDying      = true;
            mDeathTimer = 0f;

            // 이동/충돌을 즉시 끊는다. 적이 시체를 계속 때리지 않게 콜라이더도 끈다.
            mMoveDirection = Vector3.zero;
            if (mCollider != null)
            {
                mCollider.enabled = false;
            }

            if (mAnimator != null)
            {
                mAnimator.SetBool("IsWalking", false);
                mAnimator.SetTrigger("Die");
            }

            int finalScore = SurvivalGame.Instance != null ? SurvivalGame.Instance.Score : 0;
            GameEntry.GetComponent<EventComponent>().Fire(this, PlayerDiedEventArgs.Create(finalScore));
        }
    }
}
