using GameFramework;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 이 프로젝트의 모든 엔티티 로직이 상속하는 공통 베이스.
    ///
    /// GameFramework 의 EntityManager 는 같은 엔티티에 HideEntity 가 두 번 들어오면
    /// GameFrameworkException 을 던진다(EntityManager.ShowEntity/HideEntity 경로).
    /// 수명 만료와 충돌 명중이 같은 물리 스텝에 겹치거나, 사망 연출 도중 추가 피격이
    /// 들어오는 상황이 실제로 발생하므로 모든 Hide 는 SafeHide() 를 거치게 한다.
    ///
    /// 엔티티는 풀에서 재사용되므로 mHidden 은 반드시 OnShow 에서 리셋한다.
    ///
    /// <b><see cref="EntityData"/> 의 풀 반납도 여기서 한다.</b> 스폰 데이터는
    /// ReferencePool 에서 나오므로 정확히 한 번 돌려줘야 한다. 소유권은 OnShow 가 잡고
    /// OnHide 가 놓는다 — 자세한 순서는 <see cref="OnHide"/> 주석 참조.
    /// </summary>
    public abstract class EntityLogicBase : EntityLogic
    {
        private bool mHidden = false;

        /// <summary>
        /// 이 엔티티가 스폰될 때 받은 데이터. 풀에 돌려줄 책임이 이 인스턴스에 있다.
        /// null 이면 소유한 것이 없다는 뜻이고, 그 자체가 이중 반납 가드다.
        /// </summary>
        private EntityData mOwnedData = null;

        /// <summary>
        /// 이미 Hide 요청이 나갔는지. HideEntity 는 즉시 처리되지 않고 다음 Update 의
        /// 회수 큐로 넘어가므로, 그 사이에 들어오는 추가 처리를 막을 때 쓴다.
        /// </summary>
        protected bool IsHiding
        {
            get { return mHidden; }
        }

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            mHidden = false;

            // 스폰 데이터의 소유권을 여기서 잡는다. EntityData 가 아닌 userData(또는 null)로
            // 뜬 인스턴스는 애초에 반납할 것이 없으므로 그대로 null 로 남는다.
            mOwnedData = userData as EntityData;
        }

        /// <summary>
        /// 파생 로직들은 자기 정리를 먼저 하고 <c>base.OnHide</c> 를 <b>마지막에</b> 부른다
        /// (Player, Enemy 둘 다 그렇다). 그래서 데이터 반납은 이 메서드의 맨 끝이어야
        /// 파생이 아직 데이터를 읽고 있는 시점을 앞지르지 않는다.
        /// </summary>
        protected internal override void OnHide(bool isShutdown, object userData)
        {
            mHidden = true;

            base.OnHide(isShutdown, userData);

            ReleaseOwnedData();
        }

        /// <summary>
        /// 스폰 데이터를 풀에 돌려준다. 이중 Release 는 코어에서 즉시 예외이므로
        /// 참조를 먼저 비우고 나서 반납한다.
        /// </summary>
        private void ReleaseOwnedData()
        {
            if (mOwnedData == null) return;

            EntityData data = mOwnedData;
            mOwnedData = null;

            ReferencePool.Release(data);
        }

        /// <summary>
        /// HideEntity 를 정확히 1회만 호출한다. 중복 호출은 조용히 무시된다.
        /// 엔티티 제거는 반드시 이 메서드를 통할 것 — Destroy/SetActive 는 풀을 깨뜨린다.
        ///
        /// 외부(SurvivalGame, 프로시저 등)에서도 호출할 수 있게 public 이다.
        /// EntityComponent.HideEntity 를 직접 부르면 이 가드를 우회하게 되므로 쓰지 말 것.
        /// </summary>
        public void SafeHide()
        {
            if (mHidden) return;

            // Entity 는 OnInit 에서 잡힌다. null 이라는 것은 이 인스턴스가 EntityManager 를
            // 거치지 않고 프리팹에 baked 된 로직이라는 뜻이다(규약 위반). 회수할 대상 자체가
            // 없으므로 요청을 버린다 — HideEntity(null) 은 코어에서 예외가 된다.
            GameAssert.IsTrue(Entity != null,
                "SafeHide 대상의 Entity 가 null 이다. 프리팹에 EntityLogic 이 baked 되어 있는지 확인할 것.");
            if (Entity == null) return;

            // 게임 종료나 씬 언로드 중에는 EntityComponent 가 먼저 파괴돼 있을 수 있다.
            // 그 경우 엔티티도 함께 정리되므로 조용히 빠진다.
            EntityComponent entityComponent = GameEntry.GetComponent<EntityComponent>();
            if (entityComponent == null) return;

            mHidden = true;
            entityComponent.HideEntity(Entity);
        }
    }
}
