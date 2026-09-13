using GameFramework.Fsm;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 낙선 연출 상태. 쓰러지는 모습을 보여 준 뒤 스스로 회수된다.
    ///
    /// 예전에는 이 대기를 <c>StartCoroutine(HideAfterDelay)</c> 로 돌렸다. 엔티티는
    /// 풀 회수 시 <c>SetActive(false)</c> 가 되므로 코루틴이 조용히 죽을 수 있고,
    /// 죽은 코루틴은 SafeHide 에 도달하지 못한다. 상태 타이머는 그 함정이 없다.
    /// </summary>
    public sealed class PlayerSelectDyingState : PlayerSelectStateBase
    {
        protected override void OnEnter(IFsm<PlayerSelectLogic> fsm)
        {
            base.OnEnter(fsm);

            // 연출 시작은 상태 진입의 일부다. 소유자가 미리 해 두면
            // "연출은 시작됐는데 상태는 아직 안 바뀐" 프레임이 생긴다.
            fsm.Owner.StartRejectedPresentation();
        }

        protected override void OnStateUpdate(IFsm<PlayerSelectLogic> fsm, PlayerSelectLogic owner, float elapseSeconds)
        {
            owner.TickHideDelay(elapseSeconds);
        }
    }
}
