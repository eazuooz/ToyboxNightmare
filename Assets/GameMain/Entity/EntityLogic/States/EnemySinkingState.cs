using GameFramework.Fsm;

namespace ToyBoxNightmare
{
    /// <summary>바닥으로 가라앉으며 연출 시간을 채우는 상태. 다 차면 회수한다.</summary>
    public sealed class EnemySinkingState : EnemyStateBase
    {
        protected override void OnStateUpdate(IFsm<Enemy> fsm, Enemy owner, float elapseSeconds)
        {
            owner.TickDeathTimer(elapseSeconds);
            owner.TickSinking(elapseSeconds);

            if (!owner.IsDeathEffectFinished) return;

            owner.SafeHide();
        }
    }
}
