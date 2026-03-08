using UnityEngine;
using UnityGameFramework.Runtime;
using GameFramework.Event;

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

            // ���̾� ������ �ʿ��ϸ� Entity�� ���ӿ�����Ʈ�� ����
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

        protected virtual void OnDead(Entity attacker)
        {
            GameEntry.GetComponent<EntityComponent>().HideEntity(Entity);
        }

        private void OnTriggerEnter(Collider other)
        {
            Entity otherEntity = other.gameObject.GetComponent<Entity>();
            if (otherEntity == null)
            {
                return;
            }

            // �浹 �ߺ� ���� ������ �����ϰ� ������ ��Entity.Id���� ��
            if (otherEntity.Logic is TargetableObject && otherEntity.Id >= Entity.Id)
            {
                return;
            }

            // �浹 �����(������Ʈ�� �°�)
            // AIUtility.PerformCollision(this, otherEntity);
        }

        [SerializeField]
        private TargetableObjectData mTargetableObjectData = null;
    }
}
