using GameFramework.Fsm;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 쫓을 대상이 없는 상태. 플레이어가 아직 안 떴거나 이미 죽었을 때다.
    /// 플레이어가 죽었으면 승리 연출을 <b>한 번만</b> 재생한다(중복 방지는 소유자가 한다).
    /// </summary>
    public sealed class EnemyIdleState : EnemyStateBase
    {
        protected override void OnStateUpdate(IFsm<Enemy> fsm, Enemy owner, float elapseSeconds)
        {
            if (TryChangeToDying(fsm, owner)) return;

            Player player = Player.Instance;

            bool isPlayerSpawned = player != null && player.Available;
            if (!isPlayerSpawned)
            {
                owner.StopMoving();
                return;
            }

            if (player.IsDead)
            {
                owner.StopMoving();
                owner.NotifyPlayerDead();
                return;
            }

            ChangeState<EnemyChaseState>(fsm);
        }
    }
}
