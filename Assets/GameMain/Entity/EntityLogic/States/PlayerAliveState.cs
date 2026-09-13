using GameFramework.Fsm;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 살아서 조작 가능한 상태. 조준·이동 입력·무기를 굴린다.
    /// </summary>
    public sealed class PlayerAliveState : PlayerStateBase
    {
        protected override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

            if (TryChangeToDying(fsm)) return;

            Player owner = fsm.Owner;

            // 이미 체력이 0 인데 사망 연출이 아직 시작 안 된 프레임이 있을 수 있다.
            // 그 사이 입력을 받으면 시체가 한 프레임 더 움직인다.
            if (owner.IsDead) return;

            // 일시정지는 별도 상태로 뺀다. timeScale 0 이어도 OnUpdate 자체는 계속 불리므로
            // 여기서 걸러 주지 않으면 메뉴를 띄운 채 사격이 나간다.
            if (GamePause.IsPaused)
            {
                ChangeState<PlayerPausedState>(fsm);
                return;
            }

            owner.TickAliveInput();
        }
    }
}
