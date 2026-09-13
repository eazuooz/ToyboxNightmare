using GameFramework.Fsm;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 플레이어 상태의 공통 베이스.
    ///
    /// <b>상태는 무상태(stateless)다.</b> 모든 값은 <c>fsm.Owner</c> 에서 읽는다 —
    /// 그래야 상태 인스턴스를 엔티티 풀 재사용 사이에 그대로 돌려쓸 수 있다
    /// (<see cref="Player.OnInit"/> 이 한 번만 만든다).
    ///
    /// 전이는 <b>상태 안에서만</b> 시작할 수 있다. <c>IFsm&lt;T&gt;</c> 에 ChangeState 가 없어서
    /// 바깥에서 상태를 바꿀 방법이 없기 때문이다. 그래서 소유자가 조건을 노출하고
    /// 현재 상태가 그것을 폴링한다 — <see cref="ProcedureMain"/> 도 같은 방식이다.
    /// </summary>
    public abstract class PlayerStateBase : FsmState<Player>
    {
        /// <summary>
        /// 어느 상태에 있든 사망이 시작되면 사망 연출로 넘어간다.
        /// 전이했으면 true — 호출한 상태는 그 프레임의 나머지 일을 하지 말아야 한다.
        /// </summary>
        protected bool TryChangeToDying(IFsm<Player> fsm)
        {
            if (!fsm.Owner.IsDeathSequenceRunning) return false;

            ChangeState<PlayerDyingState>(fsm);
            return true;
        }
    }
}
