using GameFramework.Fsm;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 고를 수 있는 상태. 마우스 클릭을 기다린다.
    /// </summary>
    public sealed class PlayerSelectSelectableState : PlayerSelectStateBase
    {
        protected override void OnEnter(IFsm<PlayerSelectLogic> fsm)
        {
            base.OnEnter(fsm);

            // 풀에서 재사용되므로 이전 판의 누름 상태가 남아 있으면 안 된다.
            fsm.Owner.ClearPressState();
        }

        protected override void OnStateUpdate(IFsm<PlayerSelectLogic> fsm, PlayerSelectLogic owner, float elapseSeconds)
        {
            // 낙선 통보가 왔으면 이번 프레임 입력은 받지 않는다.
            // 상태를 밖에서 바꿀 수 없으므로(IFsm 에 ChangeState 가 없다) 소유자가 깃발만 세우고
            // 여기서 폴링해 넘어간다.
            if (owner.IsRejected)
            {
                ChangeState<PlayerSelectDyingState>(fsm);
                return;
            }

            owner.TickSelectInput();
        }
    }
}
