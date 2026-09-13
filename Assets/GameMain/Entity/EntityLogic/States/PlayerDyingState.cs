using GameFramework.Fsm;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 사망 연출 상태. 즉시 사라지지 않고 사망 애니메이션이 보일 시간을 준다.
    /// 회수(Hide)는 <see cref="Player.OnDead"/> 가 아니라 이 상태의 타이머가 담당한다.
    /// </summary>
    public sealed class PlayerDyingState : PlayerStateBase
    {
        protected override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

            fsm.Owner.TickDeathSequence(elapseSeconds);
        }
    }
}
