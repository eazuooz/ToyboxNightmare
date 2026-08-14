using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 체력을 가지고 피격/사망 처리를 하는 엔티티의 공통 베이스.
    /// </summary>
    public abstract class TargetableObject : EntityLogicBase
    {
        [SerializeField]
        private TargetableObjectData mTargetableObjectData = null;

        public bool IsDead
        {
            get
            {
                return mTargetableObjectData != null && mTargetableObjectData.HitPoints <= 0;
            }
        }

        public void ApplyDamage(Entity attacker, int damageHitPoints)
        {
            // 아직 OnShow 를 거치지 않았거나 잘못된 userData 로 뜬 인스턴스.
            // 런타임에 실제로 생길 수 있는 상황이고 OnShow 가 이미 로그를 남기므로 조용히 빠진다.
            if (mTargetableObjectData == null) return;

            // 이미 죽은 대상에 대한 추가 피격은 무시한다.
            // 사망 연출이 끝나고 Hide 될 때까지 엔티티가 살아 있으므로,
            // 가드가 없으면 그 사이의 피격이 OnDead 를 여러 번 호출해 Hide 가 중복된다.
            if (IsDead) return;

            // 회복 경로는 없다. 음수가 들어오면 체력이 도로 늘어 사망 판정이 영영 안 난다.
            GameAssert.IsTrue(damageHitPoints >= 0,
                "ApplyDamage 에 음수 데미지가 들어왔다. 회복은 이 경로로 처리하지 않는다.");

            float ratioBeforeDamage = mTargetableObjectData.HitPointRatio;
            mTargetableObjectData.HitPoints -= damageHitPoints;
            float ratioAfterDamage = mTargetableObjectData.HitPointRatio;

            // 사망 여부와 무관하게 매 피격 호출한다. 원본도 치명타에서 피격 이펙트를 낸다.
            OnDamaged(attacker, damageHitPoints);

            if (ratioBeforeDamage > ratioAfterDamage)
            {
                OnHitPointChanged(ratioBeforeDamage, ratioAfterDamage);
            }

            if (mTargetableObjectData.HitPoints <= 0)
            {
                OnDead(attacker);
            }
        }

        /// <summary>피격될 때마다 호출된다. 타격 이펙트/사운드를 여기서 낸다.</summary>
        protected virtual void OnDamaged(Entity attacker, int damageHitPoints)
        {
        }

        /// <summary>체력 비율이 실제로 줄었을 때 호출된다. HUD 갱신 이벤트를 여기서 발행한다.</summary>
        protected virtual void OnHitPointChanged(float fromRatio, float toRatio)
        {
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
            if (Entity == null) return;

            Entity otherEntity = other.gameObject.GetComponent<Entity>();
            if (otherEntity == null) return;

            if (IsCollisionHandledByOther(otherEntity)) return;

            // 충돌 결과 처리는 전투 시스템 복원 시 이 자리에 구현한다.
        }

        /// <summary>
        /// 충돌은 양쪽 엔티티 모두에서 통지되므로 한쪽만 처리해야 중복 판정이 안 난다.
        /// 상대도 TargetableObject 일 때만 Id 비교로 담당을 정한다.
        /// </summary>
        private bool IsCollisionHandledByOther(Entity otherEntity)
        {
            return otherEntity.Logic is TargetableObject && otherEntity.Id >= Entity.Id;
        }
    }
}
