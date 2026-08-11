using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 캐릭터 선택 단계에서 씬에 배치되는 엔티티 로직.
    /// 클릭 시 CharacterSelectedEventArgs 를 발생시키고,
    /// 선택받지 못한 캐릭터는 사망 연출 후 HideEntity 된다.
    ///
    /// Addressables 키: "Girl", "Boy" (SurvivalGame 이 넘기는 문자열과 동일)
    /// </summary>
    public class PlayerSelectLogic : EntityLogicBase
    {
        [SerializeField] private string           characterKey    = "Girl";
        [SerializeField] private CapsuleCollider  capsuleCollider = null;
        [SerializeField] private Animator         animator        = null;
        [SerializeField] private Rigidbody        rigidBody       = null;

        // 프리팹 원본 감쇠값. DisableAndHide 이후 재사용될 때 되돌리기 위해 보관한다.
        private float mInitialLinearDamping = 0f;

        // 마우스를 이 캐릭터 위에서 눌렀는지. 누른 곳과 뗀 곳이 모두 자신일 때만 선택으로 친다
        // (레거시 OnMouseUp 의 의미를 그대로 유지한다).
        private bool mPressedOnSelf = false;

        private const float PickDistance = 1000f;

        public string CharacterKey => characterKey;

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);
            capsuleCollider = GetComponent<CapsuleCollider>();
            animator        = GetComponentInChildren<Animator>();
            rigidBody       = GetComponent<Rigidbody>();

            if (rigidBody != null)
            {
                mInitialLinearDamping = rigidBody.linearDamping;
            }
        }

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            // 풀에서 재사용되므로 DisableAndHide 가 남긴 상태를 반드시 되돌린다.
            // 이걸 빼면 재시작 시 낙선했던 캐릭터가 콜라이더가 꺼진 죽은 포즈로 등장한다.
            ResetVisualState();
            mPressedOnSelf = false;

            var data = userData as CharacterSelectData;
            if (data != null)
            {
                characterKey = data.CharacterKey;
                CachedTransform.position = data.Position;
                CachedTransform.rotation = data.Rotation;
            }
        }

        private void ResetVisualState()
        {
            if (capsuleCollider != null)
            {
                capsuleCollider.enabled = true;
            }

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }

            if (rigidBody != null)
            {
                rigidBody.linearDamping = mInitialLinearDamping;
            }
        }

        /// <summary>
        /// 클릭 판정. 예전에는 Unity 레거시 메시지인 OnMouseUp 을 썼는데,
        /// 그건 구 Input Manager 백엔드가 켜져 있어야만 호출된다.
        /// activeInputHandler 를 New only 로 바꾸면 <b>에러 없이 조용히 죽으므로</b>
        /// Input System 으로 직접 판정한다.
        /// </summary>
        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (IsHiding)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                mPressedOnSelf = IsPointerOnSelf(mouse);
            }

            if (!mouse.leftButton.wasReleasedThisFrame)
            {
                return;
            }

            bool selected = mPressedOnSelf && IsPointerOnSelf(mouse);
            mPressedOnSelf = false;

            if (selected)
            {
                GameEntry.GetComponent<EventComponent>().Fire(
                    this, CharacterSelectedEventArgs.Create(characterKey));
            }
        }

        /// <summary>마우스 커서가 이 캐릭터의 콜라이더 위에 있는가.</summary>
        private bool IsPointerOnSelf(Mouse mouse)
        {
            if (capsuleCollider == null || !capsuleCollider.enabled)
            {
                return false;
            }

            // UI 위 클릭은 무시한다. 현재 씬에 EventSystem 이 없어 항상 통과하지만,
            // M6 에서 Canvas 를 추가하면 이 가드가 살아난다.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return false;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                return false;
            }

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());

            // 자기 콜라이더만 검사하지 않고 Physics.Raycast 로 최근접을 본다.
            // 앞을 가로막은 물체가 있으면 뚫고 선택되지 않는다.
            if (!Physics.Raycast(ray, out RaycastHit hit, PickDistance))
            {
                return false;
            }

            return hit.collider == capsuleCollider;
        }

        /// <summary>선택받지 못한 캐릭터에게 SurvivalGame 이 호출한다.</summary>
        public void DisableAndHide()
        {
            if (capsuleCollider != null) capsuleCollider.enabled = false;
            if (animator != null)        animator.SetTrigger("Die");
            StartCoroutine(HideAfterDelay(1.5f));
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SafeHide();
        }

        // Death 애니메이션 이벤트에서 호출 (선택적)
        private void DeathComplete()
        {
            if (rigidBody != null) rigidBody.linearDamping = 0f;
        }
    }
}
