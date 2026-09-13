using GameFramework.Fsm;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 쓰러지는 연출 구간. 침하는 다음 상태가 맡는다.
    ///
    /// 침하 시작 신호는 둘이다 — FBX 에 baked 된 <c>StartSinking</c> 애니메이션 이벤트(4종),
    /// 그리고 그 이벤트가 없는 ZomBear 를 위한 시간 비율 폴백. 소유자가 둘을 합쳐
    /// 하나의 조건으로 노출한다(상태는 밖에서 못 바뀌므로 폴링해야 한다).
    /// </summary>
    public sealed class EnemyDyingState : EnemyStateBase
    {
        protected override void OnStateUpdate(IFsm<Enemy> fsm, Enemy owner, float elapseSeconds)
        {
            owner.TickDeathTimer(elapseSeconds);

            if (owner.ShouldStartSinking)
            {
                ChangeState<EnemySinkingState>(fsm);
                return;
            }

            // 침하 없이 연출 시간이 다 차는 경우는 폴백 비율(0.5) 때문에 정상 경로에서는
            // 오지 않지만, 비율을 1 로 바꿔도 엔티티가 남지 않도록 여기서도 회수한다.
            if (!owner.IsDeathEffectFinished) return;

            owner.SafeHide();
        }
    }
}
