using GameFramework.Fsm;
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

        /// <summary>
        /// 낙선 통보가 왔는가. 상태를 밖에서 바꿀 수 없으므로(IFsm 에 ChangeState 가 없다)
        /// <see cref="DisableAndHide"/> 는 이 깃발만 세우고, 상태가 폴링해서 넘어간다.
        /// </summary>
        private bool mRejected = false;

        /// <summary>낙선 연출 경과 시간.</summary>
        private float mRejectedElapsed = 0f;

        // ─── 상태 기계 ───
        // 선택 대기 / 낙선 연출 두 상태를 GameFramework FSM 으로 돌린다.
        private IFsm<PlayerSelectLogic>       mFsm    = null;
        private FsmState<PlayerSelectLogic>[] mStates = null;

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

            // 상태 인스턴스는 인스턴스당 1회만 만든다. 값을 전부 fsm.Owner 에서 읽으므로
            // 풀 재사용 사이에 그대로 돌려쓸 수 있다.
            mStates = new FsmState<PlayerSelectLogic>[]
            {
                new PlayerSelectSelectableState(),
                new PlayerSelectDyingState(),
            };

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
            mPressedOnSelf   = false;
            mRejected        = false;
            mRejectedElapsed = 0f;

            var selectData = userData as CharacterSelectData;
            if (selectData == null)
            {
                // 스폰 측 계약 위반. 프리팹 기본 키/위치로 남아 두 캐릭터가 겹칠 수 있다.
                Log.Error("Character select data is invalid.");

                // 배치는 못 했어도 클릭은 받아야 한다. 상태 기계 없이 두면
                // 이 캐릭터를 영영 고를 수 없다.
                CreateStateMachine();
                return;
            }

            characterKey             = selectData.CharacterKey;
            CachedTransform.position = selectData.Position;
            CachedTransform.rotation = selectData.Rotation;

            // 상태 기계는 **맨 마지막**에 세운다. 상태가 소유자 값을 바로 읽기 때문이다.
            CreateStateMachine();
        }

        protected internal override void OnHide(bool isShutdown, object userData)
        {
            // 가장 먼저 부순다. 회수된 엔티티의 상태가 계속 도는 것을 막는다.
            DestroyStateMachine();

            base.OnHide(isShutdown, userData);
        }

        /// <summary>
        /// FSM 이름. 코어는 (소유자 타입, 이름) 으로 식별하므로 인스턴스마다 달라야 한다.
        /// </summary>
        private string FsmName
        {
            get { return Entity != null ? Entity.Id.ToString() : GetInstanceID().ToString(); }
        }

        private void CreateStateMachine()
        {
            FsmComponent fsmComponent = GameEntry.GetComponent<FsmComponent>();
            if (fsmComponent == null)
            {
                Log.Error("PlayerSelectLogic: FsmComponent 가 없다. 캐릭터를 고를 수 없다.");
                return;
            }

            mFsm = fsmComponent.CreateFsm(FsmName, this, mStates);
            mFsm.Start<PlayerSelectSelectableState>();
        }

        /// <summary>멱등하다. 이미 부쉈거나 프레임워크가 먼저 내려갔으면 조용히 넘어간다.</summary>
        private void DestroyStateMachine()
        {
            if (mFsm == null) return;

            FsmComponent fsmComponent = GameEntry.GetComponent<FsmComponent>();
            if (fsmComponent != null)
            {
                fsmComponent.DestroyFsm(mFsm);
            }

            mFsm = null;
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
        /// 프레임 갱신은 전부 FSM 상태가 한다(FsmManager 가 이것보다 먼저 돈다).
        /// 여기에 로직을 다시 넣으면 상태 밖에 숨은 분기가 생기므로 그러지 말 것.
        /// </summary>
        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
        }

        // ─── 상태가 쓰는 창구 ───

        /// <summary>상태를 굴려도 되는가. 회수 요청이 나갔으면 아니다.</summary>
        internal bool CanRunStates
        {
            get { return !IsHiding; }
        }

        /// <summary>낙선 통보가 왔는가. <see cref="PlayerSelectSelectableState"/> 가 폴링한다.</summary>
        internal bool IsRejected
        {
            get { return mRejected; }
        }

        /// <summary>누름 상태 초기화. 선택 대기 상태로 들어갈 때마다 부른다.</summary>
        internal void ClearPressState()
        {
            mPressedOnSelf = false;
        }

        /// <summary>
        /// 클릭 판정. 예전에는 Unity 레거시 메시지인 OnMouseUp 을 썼는데,
        /// 그건 구 Input Manager 백엔드가 켜져 있어야만 호출된다.
        /// activeInputHandler 를 New only 로 바꾸면 <b>에러 없이 조용히 죽으므로</b>
        /// Input System 으로 직접 판정한다.
        /// </summary>
        internal void TickSelectInput()
        {
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

        /// <summary>
        /// 낙선 연출을 시작한다. <see cref="PlayerSelectDyingState"/> 진입 시 1회.
        /// </summary>
        internal void StartRejectedPresentation()
        {
            mRejectedElapsed = 0f;

            if (capsuleCollider != null)
            {
                capsuleCollider.enabled = false;
            }

            if (animator != null)
            {
                animator.SetTrigger(DieTriggerParam);
            }
        }

        /// <summary>연출 시간이 다 차면 회수한다.</summary>
        internal void TickHideDelay(float elapseSeconds)
        {
            mRejectedElapsed += elapseSeconds;
            if (mRejectedElapsed < HideDelayAfterDeath) return;

            SafeHide();
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

        /// <summary>
        /// 선택받지 못한 캐릭터에게 SurvivalGame 이 호출한다.
        /// 깃발만 세운다 — 실제 연출과 회수는 <see cref="PlayerSelectDyingState"/> 가 한다.
        /// </summary>
        public void DisableAndHide()
        {
            // 이미 회수 요청이 나갔으면 연출을 시작하지 않는다. 회수 중인 엔티티의
            // 상태는 CanRunStates 에서 걸려 SafeHide 에 도달하지 못한다.
            if (IsHiding) return;

            mRejected = true;
        }

        // Death 애니메이션 이벤트에서 호출 (선택적)
        private void DeathComplete()
        {
            if (rigidBody != null) rigidBody.linearDamping = 0f;
        }
    }
}
