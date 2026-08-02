using UnityEngine;
using UnityGameFramework.Runtime;

namespace ToyBoxNightmare
{
    /// <summary>
    /// 투사체 엔티티. 방향으로 이동하다 적에 닿으면 데미지 후 소멸.
    /// </summary>
    public class Projectile : EntityLogicBase
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

            if (mData == null || IsHiding)
            {
                return;
            }

            mElapsed += elapseSeconds;
            if (mElapsed >= mData.Lifetime)
            {
                SafeHide();
                return;
            }

            CachedTransform.position += mData.Direction * mData.Speed * elapseSeconds;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 한 물리 스텝에 여러 콜라이더와 겹치면 이 콜백이 연속 호출된다.
            // 가드가 없으면 두 번째 적에게도 데미지가 들어가고 Hide 가 중복된다.
            if (Entity == null || mData == null || IsHiding)
            {
                return;
            }

            Entity otherEntity = other.GetComponent<Entity>();
            if (otherEntity == null) return;

            if (otherEntity.Logic is Enemy enemy)
            {
                enemy.ApplyDamage(Entity, mData.Damage);
                SafeHide();
            }
        }
    }
}
