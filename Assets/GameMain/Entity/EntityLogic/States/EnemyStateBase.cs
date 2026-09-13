using GameFramework.Fsm;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 적 상태의 공통 베이스.
    ///
    /// <b>모든 상태에서 디버프 타이머를 먼저 굴린다.</b> 사망 연출 중에도, 플레이어가 죽은
    /// 뒤에도 돌아야 하기 때문이다 — 이 호출을 상태별 분기 뒤로 미루면 플레이어 사망 순간
    /// 빙결이 영구화된다. 그래서 파생 상태는 <see cref="OnStateUpdate"/> 만 구현하고,
    /// 여기를 반드시 거쳐 가게 되어 있다.
    ///
    /// 상태는 무상태다. 모든 값은 <c>fsm.Owner</c> 에서 읽는다.
    /// </summary>
    public abstract class EnemyStateBase : FsmState<Enemy>
    {
        protected sealed override void OnUpdate(IFsm<Enemy> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

            Enemy owner = fsm.Owner;

            // 데이터 없이 떴거나 이미 회수 요청이 나간 엔티티는 아무것도 하지 않는다.
            if (!owner.CanRunStates) return;

            owner.TickDebuffs(elapseSeconds);

            OnStateUpdate(fsm, owner, elapseSeconds);
        }

        /// <summary>디버프를 굴린 뒤의 상태별 갱신.</summary>
        protected abstract void OnStateUpdate(IFsm<Enemy> fsm, Enemy owner, float elapseSeconds);

        /// <summary>살아있는 상태들이 공통으로 보는 전이. 사망이 시작됐으면 사망 연출로 넘어간다.</summary>
        protected bool TryChangeToDying(IFsm<Enemy> fsm, Enemy owner)
        {
            if (!owner.IsDying) return false;

            ChangeState<EnemyDyingState>(fsm);
            return true;
        }
    }
}
