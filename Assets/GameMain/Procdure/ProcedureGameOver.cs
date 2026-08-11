using UnityEngine.InputSystem;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 게임오버 상태. R 키를 누르면 ProcedureMain 으로 돌아가 새 판을 시작한다.
    ///
    /// 엔티티 회수는 여기서 하지 않는다. 게임을 소유한 <see cref="ProcedureMain"/> 이
    /// OnLeave 에서 정리하므로, 이 프로시저에 진입한 시점에는 이미 비어 있다.
    ///
    /// 재시작을 씬 리로드로 하지 않는 이유: GameFramework 기동 프리팹에
    /// DontDestroyOnLoad 가 없어서 Single 모드 씬 로드가 프레임워크를 통째로
    /// 종료시킨다(= 복구 불가). 프로시저 전이가 유일하게 안전한 재시작 경로다.
    /// </summary>
    public class ProcedureGameOver : ProcedureBase
    {
        public override bool UseNativeDialog => false;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            // 회수가 제대로 됐는지 확인용. 0 이 아니면 ProcedureMain.OnLeave 를 의심할 것.
            var entityComponent = GameEntry.GetComponent<EntityComponent>();
            int remaining = entityComponent == null ? 0 : entityComponent.GetAllLoadedEntities().Length;

            Log.Info("ProcedureGameOver: Enter. 잔존 엔티티 {0}개. R 키로 재시작.", remaining);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                ChangeState<ProcedureMain>(procedureOwner);
            }
        }
    }
}
