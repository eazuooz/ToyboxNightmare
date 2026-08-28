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

            int remainingEntityCount = CountRemainingEntities();

            Log.Info("ProcedureGameOver: Enter. 잔존 엔티티 {0}개. R 키로 재시작.", remainingEntityCount);
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            CloseHud();

            base.OnLeave(procedureOwner, isShutdown);
        }

        /// <summary>
        /// HUD 를 닫는다. <see cref="ProcedureMain"/> 이 진입에서 열고 여기서 닫아 짝이 맞는다.
        /// Main 쪽 OnLeave 에서 닫으면 게임오버 문구가 보이기도 전에 사라진다.
        /// </summary>
        private static void CloseHud()
        {
            UIComponent ui = GameEntry.GetComponent<UIComponent>();
            if (ui == null) return;

            UIForm form = ui.GetUIForm(UITable.HudForm);
            if (form == null) return;

            ui.CloseUIForm(form);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (!IsRestartRequested()) return;

            ChangeState<ProcedureMain>(procedureOwner);
        }

        /// <summary>재시작 키(R)가 이번 프레임에 눌렸는가.</summary>
        private static bool IsRestartRequested()
        {
            // 키보드가 연결돼 있지 않으면 Keyboard.current 가 null 이다(패드만 꽂힌 경우 등).
            Keyboard keyboard = Keyboard.current;

            return keyboard != null && keyboard.rKey.wasPressedThisFrame;
        }

        /// <summary>
        /// 회수가 제대로 됐는지 확인용. 0 이 아니면 ProcedureMain.OnLeave 를 의심할 것.
        /// </summary>
        private static int CountRemainingEntities()
        {
            // 앱 종료 중이면 컴포넌트가 이미 파괴됐을 수 있다. 그때는 셀 대상도 없다.
            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null) return 0;

            return entityComponent.GetAllLoadedEntities().Length;
        }
    }
}
