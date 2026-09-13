using GameFramework.Fsm;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 일시정지 상태. 아무 입력도 읽지 않는다.
    ///
    /// 진입할 때 이동 방향을 비우는 것이 핵심이다 — 안 그러면 재개하는 순간
    /// 정지 직전의 입력으로 한 걸음 튀어나간다(FixedUpdate 는 계속 돌기 때문이다).
    /// </summary>
    public sealed class PlayerPausedState : PlayerStateBase
    {
        protected override void OnEnter(IFsm<Player> fsm)
        {
            base.OnEnter(fsm);

            fsm.Owner.ClearMoveInput();
        }

        protected override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

            // 정지 중 사망은 현재 경로상 일어나지 않지만, 나중에 그런 경로가 생겨도
            // 정지 상태에 갇히지 않도록 함께 본다.
            if (TryChangeToDying(fsm)) return;

            if (GamePause.IsPaused) return;

            ChangeState<PlayerAliveState>(fsm);
        }
    }
}
