using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 체력을 가지고 피격/사망 처리를 하는 엔티티의 공통 베이스.
    /// </summary>
    public abstract class TargetableObject : EntityLogicBase
    {
        public bool IsDead
        {
            get
            {
                return mTargetableObjectData != null && mTargetableObjectData.HitPoints <= 0;
            }
        }

        public void ApplyDamage(Entity attacker, int damageHitPoints)
        {
            if (mTargetableObjectData == null)
            {
                return;
            }

            // 이미 죽은 대상에 대한 추가 피격은 무시한다.
            // 사망 연출이 끝나고 Hide 될 때까지 엔티티가 살아 있으므로,
            // 가드가 없으면 그 사이의 피격이 OnDead 를 여러 번 호출해 Hide 가 중복된다.
            if (IsDead)
            {
                return;
            }

            float fromRatio = mTargetableObjectData.HitPointRatio;
            mTargetableObjectData.HitPoints -= damageHitPoints;
            float toRatio = mTargetableObjectData.HitPointRatio;

            if (fromRatio > toRatio)
            {
                // HPBar 업데이트 - 나중에 구현
            }

            if (mTargetableObjectData.HitPoints <= 0)
            {
                OnDead(attacker);
            }
        }

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            // 레이어 지정이 필요하면 Entity 의 게임오브젝트에 적용한다.
            // Entity 는 UnityGameFramework.Runtime.Entity
            // Entity.gameObject.layer = ...
        }

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            mTargetableObjectData = userData as TargetableObjectData;
            if (mTargetableObjectData == null)
            {
                Log.Error("Targetable object data is invalid.");
                return;
            }
        }

        protected virtual void OnDead(Entity attacker)
        {
            SafeHide();
        }

        private void OnTriggerEnter(Collider other)
        {
            // OnTriggerEnter 는 GameObject 에 붙은 모든 컴포넌트에 전달된다.
            // 프리팹에 EntityLogic 이 baked 되어 있으면 그 인스턴스는 OnInit 을 거치지
            // 않아 Entity 가 null 이므로, 아래 Entity.Id 접근에서 NRE 가 난다.
            if (Entity == null)
            {
                return;
            }

            Entity otherEntity = other.gameObject.GetComponent<Entity>();
            if (otherEntity == null)
            {
                return;
            }

            // 충돌은 양쪽 엔티티 모두에서 통지되므로 한쪽만 처리한다.
            // Id 가 작은 쪽이 처리하도록 해서 중복 판정을 막는다.
            if (otherEntity.Logic is TargetableObject && otherEntity.Id >= Entity.Id)
            {
                return;
            }

            // 충돌 결과 처리는 전투 시스템 복원 시 이 자리에 구현한다.
        }

        [SerializeField]
        private TargetableObjectData mTargetableObjectData = null;
    }
}
