using GameFramework.Fsm;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 캐릭터 선택 엔티티 상태의 공통 베이스.
    ///
    /// <b>상태는 무상태(stateless)다.</b> 값은 전부 <c>fsm.Owner</c> 에서 읽는다 —
    /// 그래야 상태 인스턴스를 엔티티 풀 재사용 사이에 그대로 돌려쓸 수 있다
    /// (<see cref="PlayerSelectLogic.OnInit"/> 이 한 번만 만든다).
    /// </summary>
    public abstract class PlayerSelectStateBase : FsmState<PlayerSelectLogic>
    {
        /// <summary>
        /// 회수 요청이 나간 뒤에는 어떤 상태도 돌지 않는다는 것을 한곳에서 보장한다.
        /// 파생은 <see cref="OnStateUpdate"/> 만 채우고 이 가드를 다시 쓰지 않는다.
        /// </summary>
        protected sealed override void OnUpdate(IFsm<PlayerSelectLogic> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

            PlayerSelectLogic owner = fsm.Owner;
            if (!owner.CanRunStates) return;

            OnStateUpdate(fsm, owner, elapseSeconds);
        }

        protected abstract void OnStateUpdate(IFsm<PlayerSelectLogic> fsm, PlayerSelectLogic owner, float elapseSeconds);
    }
}
