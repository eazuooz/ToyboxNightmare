using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 투사체 엔티티. 방향으로 이동하다 적에 닿으면 데미지 후 소멸.
    /// </summary>
    public class Projectile : EntityLogic
    {
        private ProjectileData mData = null;
        private float mElapsed = 0f;

        protected internal override void OnShow(object userData)
        {
            base.OnShow(userData);

            mData = userData as ProjectileData;
            if (mData == null)
            {
                Log.Error("Projectile data is invalid.");
                return;
            }

            CachedTransform.position = mData.Position;
            CachedTransform.forward  = mData.Direction;
            mElapsed = 0f;
        }

        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            mElapsed += elapseSeconds;
            if (mElapsed >= mData.Lifetime)
            {
                GameEntry.GetComponent<EntityComponent>().HideEntity(Entity);
                return;
            }

            CachedTransform.position += mData.Direction * mData.Speed * elapseSeconds;
        }

        private void OnTriggerEnter(Collider other)
        {
            Entity otherEntity = other.GetComponent<Entity>();
            if (otherEntity == null) return;

            if (otherEntity.Logic is Enemy enemy)
            {
                enemy.ApplyDamage(Entity, mData.Damage);
                GameEntry.GetComponent<EntityComponent>().HideEntity(Entity);
            }
        }
    }
}
