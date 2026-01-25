using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    public abstract class TargetableObject : EntityLogic
    {
        public bool IsDead
        {
            get
            {
                return mTargetableObjectData != null && mTargetableObjectData.HitPoints <= 0;
            }
        }

        //public abstract ImpactData GetImpactData();

        public void ApplyDamage(Entity attacker, int damageHitPoints)
        {
            if (mTargetableObjectData == null)
            {
                return;
            }

            float fromRatio = mTargetableObjectData.HitPointRatio;
            mTargetableObjectData.HitPoints -= damageHitPoints;
            float toRatio = mTargetableObjectData.HitPointRatio;

            if (fromRatio > toRatio)
            {
                // HPBar 있으면 연결
                // GameEntry.HPBar.ShowHPBar(this.Entity, fromRatio, toRatio);
            }

            //if (mTargetableObjectData.HitPoints <= 0)
            //{
            //    OnDead(attacker);
            //}
        }

        protected internal override void OnInit(object userData)
        {
            base.OnInit(userData);

            // 레이어 세팅이 필요하면 Entity의 게임오브젝트에 적용
            // Entity is UnityGameFramework.Runtime.Entity
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

        //protected virtual void OnDead(Entity attacker)
        //{
        //    GameEntry.Entity.HideEntity(Entity);
        //}

        private void OnTriggerEnter(Collider other)
        {
            Entity otherEntity = other.gameObject.GetComponent<Entity>();
            if (otherEntity == null)
            {
                return;
            }

            // 충돌 중복 방지 로직을 유지하고 싶으면 “Entity.Id”로 비교
            if (otherEntity.Logic is TargetableObject && otherEntity.Id >= Entity.Id)
            {
                return;
            }

            // 충돌 라우팅(프로젝트에 맞게)
            // AIUtility.PerformCollision(this, otherEntity);
        }

        [SerializeField]
        private TargetableObjectData mTargetableObjectData = null;
    }
}
