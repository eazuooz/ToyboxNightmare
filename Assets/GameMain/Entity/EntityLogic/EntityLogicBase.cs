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
    /// </summary>
    public abstract class EntityLogicBase : EntityLogic
    {
        private bool mHidden = false;

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
        }

        protected internal override void OnHide(bool isShutdown, object userData)
        {
            mHidden = true;

            base.OnHide(isShutdown, userData);
        }

        /// <summary>
        /// HideEntity 를 정확히 1회만 호출한다. 중복 호출은 조용히 무시된다.
        /// 엔티티 제거는 반드시 이 메서드를 통할 것 — Destroy/SetActive 는 풀을 깨뜨린다.
        /// </summary>
        protected void SafeHide()
        {
            if (mHidden)
            {
                return;
            }

            mHidden = true;
            GameEntry.GetComponent<EntityComponent>().HideEntity(Entity);
        }
    }
}
