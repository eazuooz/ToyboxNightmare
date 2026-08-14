using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace ToyBoxNightmare
{
    public class ProcedureMain : ProcedureBase
    {
        private SurvivalGame mGame = null;

        public override bool UseNativeDialog => false;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            Log.Info("ProcedureMain: Enter");

            mGame = new SurvivalGame();
            mGame.Initialize();
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            // OnEnter 가 아직 돌지 않았거나 이미 OnLeave 로 정리된 상태 방어.
            if (mGame == null) return;

            mGame.Update(elapseSeconds, realElapseSeconds);

            // 게임오버는 SurvivalGame 이 PlayerDied 를 받아 세운다.
            // 전이 시 OnLeave 가 mGame.Shutdown() 을 부르고, ProcedureGameOver 가
            // 남은 엔티티를 회수한다.
            if (mGame.GameOver)
            {
                ChangeState<ProcedureGameOver>(procedureOwner);
            }
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            // 순서가 중요하다. 엔티티를 먼저 회수해야 각 OnHide 훅이 아직 살아 있는
            // SurvivalGame.Instance 를 볼 수 있다. Shutdown 이 먼저 돌면 Instance 가
            // null 인 상태에서 훅이 실행된다.
            CleanupEntities();

            mGame?.Shutdown();
            mGame = null;

            base.OnLeave(procedureOwner, isShutdown);
        }

        /// <summary>
        /// 이 판에서 만든 엔티티를 전부 회수한다.
        ///
        /// 로딩 중인 엔티티까지 반드시 같이 처리해야 한다. GetAllLoadedEntities 는
        /// 이미 인스턴스화된 것만 반환하므로, ShowEntity 직후 Addressables 로드가
        /// 진행 중인 엔티티는 빠진다. 그대로 두면 로드가 끝나는 순간 이전 판의 적이
        /// 다음 판 화면에 스폰된다.
        /// </summary>
        private static void CleanupEntities()
        {
            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();

            // 앱 종료 경로(isShutdown)로 들어오면 컴포넌트가 이미 파괴됐을 수 있다.
            // 이때는 회수할 대상도 함께 사라진 뒤라 조용히 빠져나가는 게 맞다.
            if (entityComponent == null) return;

            int loadingCount = entityComponent.GetAllLoadingEntityIds().Length;
            int loadedCount  = entityComponent.GetAllLoadedEntities().Length;

            entityComponent.HideAllLoadingEntities();
            entityComponent.HideAllLoadedEntities();

            if (loadingCount > 0 || loadedCount > 0)
            {
                Log.Info("ProcedureMain: 엔티티 회수 — 로드됨 {0}, 로딩 중 {1}", loadedCount, loadingCount);
            }
        }
    }
}
