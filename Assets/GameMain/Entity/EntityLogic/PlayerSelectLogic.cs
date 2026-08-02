using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
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

        /// <summary>3D 모델 위에서 마우스를 클릭하면 호출된다. Collider 필요.</summary>
        private void OnMouseUp()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
#endif
            GameEntry.GetComponent<EventComponent>().Fire(
                this, CharacterSelectedEventArgs.Create(characterKey));
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
