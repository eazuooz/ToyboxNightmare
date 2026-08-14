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

        /// <summary>사망 연출을 보여 주고 회수하기까지의 시간. 원본 낙선 연출 길이와 같다.</summary>
        private const float HideDelayAfterDeath = 1.5f;

        /// <summary>애니메이터 트리거 이름. 이름이 틀려도 Unity 는 조용히 무시한다.</summary>
        private const string DieTriggerParam = "Die";

        public string CharacterKey => characterKey;

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);
            capsuleCollider = GetComponent<CapsuleCollider>();
            // includeInactive: true 가 필수다. 재시작으로 이 인스턴스가 풀에서 다시 나올 때
            // OnInit 은 OnShow(= SetActive(true)) 보다 먼저 돌아 GameObject 가 아직 꺼져 있다.
            // 기본값(false)이면 null 이 되어 낙선 캐릭터의 Die 연출이 나오지 않는다.
            animator        = GetComponentInChildren<Animator>(true);
            rigidBody       = GetComponent<Rigidbody>();

            if (rigidBody != null)
            {
                mInitialLinearDamping = rigidBody.linearDamping;
            }

            WarnOnMissingComponents();
        }

        /// <summary>
        /// 캐시가 비면 예외 대신 <b>선택이 조용히 죽는다</b> — 콜라이더가 없으면 이 캐릭터를
        /// 영영 고를 수 없고, 두 캐릭터가 모두 그러면 게임이 진행되지 않는다.
        /// </summary>
        private void WarnOnMissingComponents()
        {
            if (capsuleCollider == null)
            {
                Log.Error("PlayerSelectLogic '{0}': CapsuleCollider 가 없어 클릭 선택이 불가능하다.", Name);
            }

            if (animator == null)
            {
                Log.Warning("PlayerSelectLogic '{0}': Animator 를 찾지 못했다. 낙선 연출이 재생되지 않는다.", Name);
            }
        }

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            // 풀에서 재사용되므로 DisableAndHide 가 남긴 상태를 반드시 되돌린다.
            // 이걸 빼면 재시작 시 낙선했던 캐릭터가 콜라이더가 꺼진 죽은 포즈로 등장한다.
            ResetVisualState();
            mPressedOnSelf = false;

            var selectData = userData as CharacterSelectData;
            if (selectData == null)
            {
                // 스폰 측 계약 위반. 프리팹 기본 키/위치로 남아 두 캐릭터가 겹칠 수 있다.
                Log.Error("Character select data is invalid.");
                return;
            }

            characterKey             = selectData.CharacterKey;
            CachedTransform.position = selectData.Position;
            CachedTransform.rotation = selectData.Rotation;
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

            if (IsHiding) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                mPressedOnSelf = IsPointerOnSelf(mouse);
            }

            if (!mouse.leftButton.wasReleasedThisFrame) return;

            // 누른 곳과 뗀 곳이 모두 자신일 때만 선택으로 친다.
            bool clickCompletedOnSelf = mPressedOnSelf && IsPointerOnSelf(mouse);
            mPressedOnSelf = false;

            if (clickCompletedOnSelf)
            {
                FireCharacterSelected();
            }
        }

        private void FireCharacterSelected()
        {
            // Create 는 참조 풀에서 꺼내 오므로, 발행하지 못할 상황이면 만들기 전에 빠진다.
            EventComponent eventComponent = GameEntry.GetComponent<EventComponent>();
            if (eventComponent == null)
            {
                Log.Error("EventComponent 가 없어 CharacterSelected 를 발행하지 못했다.");
                return;
            }

            eventComponent.Fire(this, CharacterSelectedEventArgs.Create(characterKey));
        }

        /// <summary>마우스 커서가 이 캐릭터의 콜라이더 위에 있는가.</summary>
        private bool IsPointerOnSelf(Mouse mouse)
        {
            if (capsuleCollider == null || !capsuleCollider.enabled) return false;

            // UI 위 클릭은 무시한다. 현재 씬에 EventSystem 이 없어 항상 통과하지만,
            // M6 에서 Canvas 를 추가하면 이 가드가 살아난다.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;

            Camera cam = Camera.main;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());

            // 자기 콜라이더만 검사하지 않고 Physics.Raycast 로 최근접을 본다.
            // 앞을 가로막은 물체가 있으면 뚫고 선택되지 않는다.
            if (!Physics.Raycast(ray, out RaycastHit hit, PickDistance)) return false;

            return hit.collider == capsuleCollider;
        }

        /// <summary>선택받지 못한 캐릭터에게 SurvivalGame 이 호출한다.</summary>
        public void DisableAndHide()
        {
            // 이미 회수 요청이 나갔으면 연출을 다시 시작하지 않는다. 회수되면 GameObject 가
            // 꺼져 코루틴이 죽으므로, 두 번째 코루틴은 SafeHide 까지 도달하지도 못한다.
            if (IsHiding) return;

            if (capsuleCollider != null)
            {
                capsuleCollider.enabled = false;
            }

            if (animator != null)
            {
                animator.SetTrigger(DieTriggerParam);
            }

            StartCoroutine(HideAfterDelay(HideDelayAfterDeath));
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
