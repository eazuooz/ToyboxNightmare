using GameFramework.Fsm;

namespace ToyBoxNightmare
{
    /// <summary>플레이어(또는 아군 미끼)를 쫓으며 사거리 안에서 공격하는 상태.</summary>
    public sealed class EnemyChaseState : EnemyStateBase
    {
        protected override void OnStateUpdate(IFsm<Enemy> fsm, Enemy owner, float elapseSeconds)
        {
            if (TryChangeToDying(fsm, owner)) return;

            Player player = Player.Instance;

            bool isPlayerSpawned = player != null && player.Available;
            if (!isPlayerSpawned)
            {
                owner.StopMoving();
                ChangeState<EnemyIdleState>(fsm);
                return;
            }

            if (player.IsDead)
            {
                owner.StopMoving();
                owner.NotifyPlayerDead();
                ChangeState<EnemyIdleState>(fsm);
                return;
            }

            owner.TickChase(player, elapseSeconds);
            owner.TickAttack(player, elapseSeconds);
        }
    }
}
