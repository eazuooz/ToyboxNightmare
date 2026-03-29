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

        private PlayerData mPlayerData = null;
        private Rigidbody  mRigidbody  = null;
        private Animator   mAnimator   = null;

        // FixedUpdate 에 전달할 이동/회전 방향 (OnUpdate 에서 읽어 저장)
        private Vector3 mMoveDirection = Vector3.zero;
        private Vector3 mLookDirection = Vector3.forward;

        // ─── EntityLogic 생명주기 ───

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);
            mRigidbody = GetComponent<Rigidbody>();
            mAnimator  = GetComponentInChildren<Animator>();
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
        }

        protected internal override void OnHide(bool isShutdown, object userData)
        {
            Instance = null;
            base.OnHide(isShutdown, userData);
        }

        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (IsDead) return;

            ReadMoveInput();
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

        // ─── 스탯 ───

        public void UpgradeMoveSpeed(float amount)
        {
            mPlayerData.MoveSpeed += amount;
        }
    }
}
